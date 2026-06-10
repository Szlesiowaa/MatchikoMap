using MatchikoMap.Controllers;
using MatchikoMap.Data;
using MatchikoMap.Models;
using MatchikoMap.Services.FriendshipsService;
using MatchikoMap.Utils;
using Humanizer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace MatchikoMap.Services.MatchmakingService
{
    public class MatchmakingService(AppDbContext _db, IFriendshipsService _fs) : IMatchmakingService
    {

        public async Task<IMatchmakingEntryResponse?> MyEntriesAsync(int userId)
        {
            var entry = await _db.MatchmakingEntries
                .Include(e => e.Game)
                .FirstOrDefaultAsync(e =>
                    e.CreatorUserId == userId && e.CreatorStatus == MatchmakingEntryUserStatus.Searching && e.ExpiringAt > DateTime.UtcNow
                );

            if (entry != null){
                return new MatchmakingEntryCreated
                {
                    MatchId = entry.MatchId,
                    Status = entry.CreatorStatus,
                    ExpiringAt = entry.ExpiringAt,
                    Game = new GameDto
                    {
                        Id = entry.GameId,
                        Name = entry.Game.Name,
                        IconPath = entry.Game.IconPath,
                        GridPath = entry.Game.GridPath,
                        PosterPath = entry.Game.PosterPath
                    }
                };
            }

            var result = await _db.MatchmakingEntries
                .Where(e =>
                    (e.CreatorUserId == userId ||
                    e.JoinerUserId == userId) &&
                    e.ExpiringAt>DateTime.UtcNow)
                .Select(e => new MatchmakingEntryJoined
                {
                    Status = (MatchmakingEntryUserStatus)(e.CreatorUserId == userId
                        ? e.CreatorStatus
                        : e.JoinerStatus!),
                    ExpiringAt = e.ExpiringAt,
                    Game = new GameDto
                    {
                        Id = e.GameId,
                        Name = e.Game.Name,
                        IconPath = e.Game.IconPath,
                        GridPath = e.Game.GridPath,
                        PosterPath = e.Game.PosterPath
                    },
                    MatchId = e.MatchId,
                    User = new MapUserDto
                    {
                        Id = (int)(e.CreatorUserId == userId
                        ? e.JoinerUserId!
                        : e.CreatorUserId),

                        UserName = e.CreatorUserId == userId
                        ? e.JoinerUser!.UserName
                        : e.CreatorUser.UserName,

                        Latitude = e.CreatorUserId == userId
                        ? e.JoinerUser!.Latitude
                        : e.CreatorUser.Latitude,

                        Longitude = e.CreatorUserId == userId
                        ? e.JoinerUser!.Longitude
                        : e.CreatorUser.Longitude,

                        IsActive = e.CreatorUserId == userId
                        ? e.JoinerUser!.IsActive
                        : e.CreatorUser.IsActive,

                        ProfileImageUrl = e.CreatorUserId == userId
                        ? e.JoinerUser!.ProfileImageUrl
                        : e.CreatorUser.ProfileImageUrl,

                        DefaultAvatarValue = e.CreatorUserId == userId
                        ? e.JoinerUser!.DefaultAvatarValue
                        : e.CreatorUser.DefaultAvatarValue,
                    },
                    Description = e.Description,
                    Type = e.Type
                })
                .FirstOrDefaultAsync();

            return result;
        }
        
        public async Task<MatchmakingEntryCreated> CreateNewEntryAsync(JoinOrCreateRequest dto, Game game, int userId)
        {
            var entry = new MatchmakingEntry
            {
                GameId = game.Id,
                CreatorUserId = userId,
                CreatorStatus = MatchmakingEntryUserStatus.Searching,
                ExpiringAt = DateTime.UtcNow.AddHours(1),
                Description = dto.Description,
                Type = dto.Type
            };
            _db.MatchmakingEntries.Add(entry);
            await _db.SaveChangesAsync();
            return new MatchmakingEntryCreated
            {
                MatchId =entry.MatchId, 
                Status = MatchmakingEntryUserStatus.Searching,
                ExpiringAt = DateTime.UtcNow.AddHours(1),
                Game = new GameDto
                {
                    Id = game.Id,
                    Name = game.Name,
                    IconPath = game.IconPath,
                    GridPath = game.GridPath,
                    PosterPath = game.PosterPath
                }
            };
        }

        public async Task<IMatchmakingEntryResponse> JoinOrCreateEntryAsync(JoinOrCreateRequest dto, Game game, User user)
        {
            if (dto.Type == MatchmakingEntryType.Manual)
            {
                return await CreateNewEntryAsync(dto, game, user.Id);
            }
            var now = DateTime.UtcNow.AddMinutes(1);

            var candidate = await _db.MatchmakingEntries
                .Where(e =>
                    e.GameId == dto.GameId &&
                    e.CreatorUserId != user.Id &&
                    e.JoinerUserId == null &&
                    e.ExpiringAt > now &&
                    e.CreatorStatus == MatchmakingEntryUserStatus.Searching &&
                    !_db.MatchmakingRejects
                        .Where(r => r.UserId == user.Id)
                        .Select(r => r.MatchId)
                        .Contains(e.MatchId)
                )
                .OrderBy(e => e.ExpiringAt)
                .Select(e => new MatchmakingNotification
                {
                    MatchId = e.MatchId,
                    Creator = new MapUserDto
                    {
                        Id = e.CreatorUserId,
                        UserName = e.CreatorUser.UserName,
                        Latitude = e.CreatorUser.Latitude,
                        Longitude = e.CreatorUser.Longitude,
                        IsActive = e.CreatorUser.IsActive,
                        ProfileImageUrl = e.CreatorUser.ProfileImageUrl,
                        DefaultAvatarValue = e.CreatorUser.DefaultAvatarValue
                    },
                    Joiner = new MapUserDto
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        Latitude = user.Latitude,
                        Longitude = user.Longitude,
                        IsActive = user.IsActive,
                        ProfileImageUrl = user.ProfileImageUrl,
                        DefaultAvatarValue = user.DefaultAvatarValue
                    },
                    Game = new GameDto
                    {
                        Id = game.Id,
                        Name = game.Name,
                        IconPath = game.IconPath,
                        GridPath = game.GridPath,
                        PosterPath = game.PosterPath,
                    },
                    Description = e.Description,
                    ExpiringAt = e.ExpiringAt
                })
                .FirstOrDefaultAsync();
            try
            {
                await TryJoinEntryAsync(candidate,user.Id);
            }
            catch(MatchmakingJoiningFailedException)
            {
                return await CreateNewEntryAsync(dto, game, user.Id);
            }

            return candidate!;
        }

        public async Task<List<MatchmakingEntryForListResponse>> NearbyEntriesAsync(User user, int? gameId)
        {
            Game? game;
            if (gameId.HasValue) 
            {
                game = await _db.Games.FirstOrDefaultAsync(g => g.Id == gameId);
                if (game == null) return [];
            }

            double radiusKm = user.RadiusKm;
            double lat = (double)user.Latitude!;
            double lng = (double)user.Longitude!;

            double latDelta = radiusKm / 111.0;
            double lngDelta = radiusKm / (111.0 * Math.Cos((double)(lat * Math.PI / 180)));

            var query = _db.MatchmakingEntries.AsQueryable();

            query = query.Where(me =>
                me.CreatorStatus == MatchmakingEntryUserStatus.Searching &&
                me.ExpiringAt > DateTime.UtcNow &&
                me.CreatorUser != user &&
                me.CreatorUser.Latitude >= lat - latDelta &&
                me.CreatorUser.Latitude <= lat + latDelta &&
                me.CreatorUser.Longitude >= lng - lngDelta &&
                me.CreatorUser.Longitude <= lng + lngDelta &&
                !_db.MatchmakingRejects.Any(r =>
                    r.MatchId == me.MatchId &&
                    r.UserId == user.Id)
            );

            if (gameId.HasValue)
            {
                query = query.Where(me => me.GameId == gameId.Value);
            }

            var result = await query.Select(me => new MatchmakingEntryForListResponse
            {
                MatchId = me.MatchId,
                Creator = new MapUserDto
                {
                    Id = me.CreatorUserId,
                    UserName = me.CreatorUser.UserName,
                    Latitude = me.CreatorUser.Latitude,
                    Longitude = me.CreatorUser.Longitude,
                    IsActive = me.CreatorUser.IsActive,
                    ProfileImageUrl = me.CreatorUser.ProfileImageUrl,
                    DefaultAvatarValue = me.CreatorUser.DefaultAvatarValue
                },
                Game = new GameDto
                {
                    Id = me.GameId,
                    Name = me.Game.Name,
                    IconPath = me.Game.IconPath,
                    GridPath = me.Game.GridPath,
                    PosterPath = me.Game.PosterPath
                },
                ExpiringAt = me.ExpiringAt,
                Description = me.Description,
            }).ToListAsync();

            return result;
        }

        public async Task<FriendCards?> AcceptAsync(int userId, int matchId)
        {
            var entry = await _db.MatchmakingEntries
                .Include(me => me.CreatorUser)
                .Include(me => me.JoinerUser)
                .FirstOrDefaultAsync(me => 
                    me.MatchId == matchId && 
                    (me.JoinerUserId == userId || me.CreatorUserId == userId)
                ) ?? throw new MatchmakingNotFoundException();
            if (entry.JoinerUser == null) throw new MatchmakingNotReadyException();

            User me;
            User friend;

            if (entry.CreatorUserId == userId)
            {
                me = entry.CreatorUser;
                friend = entry.JoinerUser;
                entry.CreatorStatus = MatchmakingEntryUserStatus.Accepted;
            }
            else if (entry.JoinerUserId == userId)
            {
                me = entry.JoinerUser;
                friend = entry.CreatorUser;
                entry.JoinerStatus = MatchmakingEntryUserStatus.Accepted;
            }
            else throw new MatchikoMapException("Coś poszło nie tak");

            FriendCards? fc = null;
            if (entry.CreatorStatus == MatchmakingEntryUserStatus.Accepted && entry.JoinerStatus == MatchmakingEntryUserStatus.Accepted)
            {
                _db.MatchmakingEntries.Remove(entry);
                await _db.SaveChangesAsync();
                var relationship= await _db.FriendRelations.FirstOrDefaultAsync(fr => (fr.UserId == me.Id && fr.FriendId == friend.Id) || (fr.UserId == friend.Id && fr.FriendId == me.Id));
                if (relationship == null || relationship.Status=="Pending" || relationship.ConversationId == null) fc = await _fs.CreateRelationshipAsync(me, friend, null);
                else throw new AlreadyFriendsException((int)relationship.ConversationId!, friend.Id);
            } else await _db.SaveChangesAsync();
            return fc;
        }

        public async Task<MatchmakingRejectResponse?> CancelOrRejectAsync(int userId, int matchId)
        {
            var entry = await _db.MatchmakingEntries
                .Include(me => me.Game)
                .Include(me => me.JoinerUser)
                .FirstOrDefaultAsync(me => me.MatchId == matchId && (me.CreatorUserId == userId || me.JoinerUserId == userId))
                ?? throw new MatchmakingNotFoundException();


            if (entry.JoinerUserId == userId || (entry.CreatorUserId == userId && entry.JoinerUserId != null))
            {
                MatchmakingRejectResponse response = new()
                {
                    AmICreator = false,
                    CreatorUserId = entry.CreatorUserId,
                    JoinerUserId = (int)entry.JoinerUserId,
                    Data = new MatchmakingEntryCreated
                    {
                        MatchId = entry.MatchId,
                        Status = MatchmakingEntryUserStatus.Searching,
                        ExpiringAt = DateTime.UtcNow.AddMinutes(30),
                        Game = new GameDto
                        {
                            Id = entry.GameId,
                            Name = entry.Game.Name,
                            IconPath = entry.Game.IconPath,
                            GridPath = entry.Game.GridPath,
                            PosterPath = entry.Game.PosterPath
                        }
                    }
                };
                if (entry.CreatorUserId == userId)response.AmICreator = true;
                MatchmakingReject reject = new()
                {
                    Entry = entry,
                    User = entry.JoinerUser!
                };
                entry.ExpiringAt = DateTime.UtcNow.AddMinutes(30);
                entry.JoinerStatus = null;
                entry.JoinerUser = null;
                entry.JoinerUserId = null;
                entry.CreatorStatus = MatchmakingEntryUserStatus.Searching;

                _db.MatchmakingRejects.Add(reject);
                await _db.SaveChangesAsync();
                return response;
            }
            else if (entry.CreatorUserId == userId && entry.JoinerUserId == null)
            {
                _db.MatchmakingEntries.Remove(entry);
                await _db.SaveChangesAsync();
                return null;
            }
            else throw new MatchikoMapException("Coś poszło nie tak.");
        }

        public async Task TryJoinEntryAsync(MatchmakingNotification? candidate, int userId)
        {
            if (candidate != null)
            {
                var newExpiry = DateTime.UtcNow.AddMinutes(30);
                var updated = await _db.MatchmakingEntries
                    .Where(e => e.MatchId == candidate.MatchId && e.JoinerUserId == null && e.ExpiringAt > DateTime.UtcNow)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(e => e.JoinerUserId, userId)
                        .SetProperty(e => e.JoinerStatus, MatchmakingEntryUserStatus.Pending)
                        .SetProperty(e => e.CreatorStatus, MatchmakingEntryUserStatus.Pending)
                        .SetProperty(e => e.ExpiringAt, newExpiry)
                        );
                if (updated == 1)
                {
                    candidate.ExpiringAt = newExpiry;
                    return;
                }
            }
            throw new MatchmakingJoiningFailedException();
        }

        public async Task<MatchmakingNotification> JoinEntryByMatchIdAsync(int matchId, User user)
        {
            bool isAlreadyRejected = await _db.MatchmakingRejects.AnyAsync(mr => mr.UserId == user.Id && mr.MatchId == matchId);
            if (isAlreadyRejected) throw new MatchmakingJoiningFailedException();

            var now = DateTime.UtcNow.AddMinutes(1);

            var candidate = await _db.MatchmakingEntries
                .Where(e =>
                    e.MatchId == matchId &&
                    e.JoinerUserId == null &&
                    e.ExpiringAt > now &&
                    e.CreatorStatus == MatchmakingEntryUserStatus.Searching
                )
                .Select(e => new MatchmakingNotification
                {
                    MatchId = e.MatchId,
                    Creator = new MapUserDto
                    {
                        Id = e.CreatorUserId,
                        UserName = e.CreatorUser.UserName,
                        Latitude = e.CreatorUser.Latitude,
                        Longitude = e.CreatorUser.Longitude,
                        IsActive = e.CreatorUser.IsActive,
                        ProfileImageUrl = e.CreatorUser.ProfileImageUrl,
                        DefaultAvatarValue = e.CreatorUser.DefaultAvatarValue
                    },
                    Joiner = new MapUserDto
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        Latitude = user.Latitude,
                        Longitude = user.Longitude,
                        IsActive = user.IsActive,
                        ProfileImageUrl = user.ProfileImageUrl,
                        DefaultAvatarValue = user.DefaultAvatarValue
                    },
                    Game = new GameDto
                    {
                        Id = e.GameId,
                        Name = e.Game.Name,
                        IconPath = e.Game.IconPath,
                        GridPath = e.Game.GridPath,
                        PosterPath = e.Game.PosterPath,
                    },
                    Description = e.Description,
                    ExpiringAt = e.ExpiringAt
                })
                .FirstOrDefaultAsync();

            await TryJoinEntryAsync(candidate, user.Id);
            return candidate!;
        }
    }
}
