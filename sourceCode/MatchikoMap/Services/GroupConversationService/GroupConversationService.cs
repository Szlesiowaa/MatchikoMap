using MatchikoMap.Data;
using MatchikoMap.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace MatchikoMap.Services.GroupConversationService
{
    public class GroupConversationService(AppDbContext _db) : IGroupConversationService
    {

        public async Task<GroupConversationResult?> GetNearestConversationsForSpecifiedGameAsync(string gameName, User user)
        {
            var gameId = await _db.Games.FirstOrDefaultAsync(g => g.Name == gameName);
            if (gameId == null) return null;

            double radiusKm = user.RadiusKm;
            double lat = (double)user.Latitude!;
            double lng = (double)user.Longitude!;

            double latDelta = radiusKm / 111.0;
            double lngDelta = radiusKm / (111.0 * Math.Cos((double)(lat * Math.PI / 180)));

            Console.WriteLine(gameName);

            var candidates = await _db.Conversations
                .Include(c => c.Game)
                .Where(c =>
                    c.isGroup &&
                    c.GameId != null &&
                    c.Name == gameName &&
                    c.Latitude >= lat - latDelta &&
                    c.Latitude <= lat + latDelta &&
                    c.Longitude >= lng - lngDelta &&
                    c.Longitude <= lng + lngDelta
                )
                .Select(c => new
                {
                    ConversationId = c.Id,
                    GameId = (int)c.GameId!,
                    c.Name,
                    IsGroup = c.isGroup,
                    c.Latitude,
                    c.Longitude,
                    c.LocationName,
                    c.Game!.IconPath,
                    DistanceSquared = ((double)(c.Latitude! - lat) * (double)(c.Latitude! - lat)) +
                                      ((double)(c.Longitude! - lng) * (double)(c.Longitude! - lng))
                })
                .OrderBy(x => x.DistanceSquared)
                .ToListAsync();

            var result = candidates.Select(c => new GroupConversationDto
            {
                ConversationId = c.ConversationId,
                GameId = c.GameId,
                Name = c.Name,
                IsGroup = c.IsGroup,
                Latitude = c.Latitude,
                Longitude = c.Longitude,
                LocationName = c.LocationName,
                Distance = Math.Sqrt(c.DistanceSquared),
                IconPath = c.IconPath
            }).ToList();

            GroupConversationResult response = new()
            {
                Latitude = lat,
                Longitude = lng,
                Conversations = result,
                GameId = gameId.Id
            };
            return response;
        }

        public async Task<List<GroupConversationDto>> GetAllConversationsForUserGamesAsync(int userId, User user)
        {
            double radiusKm = user.RadiusKm;
            double lat = (double)user.Latitude!;
            double lng = (double)user.Longitude!;

            double latDelta = radiusKm / 111.0;
            double lngDelta = radiusKm / (111.0 * Math.Cos((double)(lat * Math.PI / 180)));

            var conversations = await _db.Conversations
                .Where(c => c.isGroup &&
                    _db.UserFavouriteGames
                        .Where(ufg => ufg.UserId == userId)
                        .Select(ufg => (int?)ufg.GameId)
                        .Contains(c.GameId)
                )
                .Where(c =>
                    c.Latitude >= lat - latDelta &&
                    c.Latitude <= lat + latDelta &&
                    c.Longitude >= lng - lngDelta &&
                    c.Longitude <= lng + lngDelta
                )
                .Select(c => new
                {
                    Conversation = c,
                    GameIconPath = c.Game!.IconPath,
                    c.GameId,
                    Distance = Math.Sqrt(
                        Math.Pow((double)(c.Latitude!.Value - lat), 2) +
                        Math.Pow((double)(c.Longitude!.Value - lng), 2))
                })
                .ToListAsync();

            var result = conversations
                .GroupBy(x => x.Conversation.GameId)
                .SelectMany(g => g.OrderBy(x => x.Distance))
                .Select(x => new GroupConversationDto
                {
                    ConversationId = x.Conversation.Id,
                    GameId = (int)x.GameId!,
                    Name = x.Conversation.Name,
                    IsGroup = x.Conversation.isGroup,
                    Latitude = x.Conversation.Latitude,
                    Longitude = x.Conversation.Longitude,
                    LocationName = x.Conversation.LocationName,
                    Distance = x.Distance,
                    IconPath = x.GameIconPath
                })
                .ToList();
            return result;
        }
    }
}
