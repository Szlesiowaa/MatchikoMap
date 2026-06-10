using Microsoft.EntityFrameworkCore;
using MatchikoMap.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace MatchikoMap.Data
{
    // dodać
    // migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
    // do metody Up w migracji
    // i
    // migrationBuilder.Sql("DROP EXTENSION IF EXISTS pg_trgm;");
    // do metody Down w tej samej migracji

    public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<User,IdentityRole<int>,int>(options)
    {
        public override DbSet<User> Users => Set<User>();
        public DbSet<Conversation> Conversations => Set<Conversation>();
        public DbSet<ConversationMember> ConversationMembers => Set<ConversationMember>();
        public DbSet<Message> Messages => Set<Message>();
        public DbSet<MessageAttachment> MessageAttachments => Set<MessageAttachment>();
        public DbSet<FriendRelation> FriendRelations => Set<FriendRelation>();
        public DbSet<BlockedUser> BlockedUsers => Set<BlockedUser>();
        public DbSet<UserSettings> UserSettings => Set<UserSettings>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<AppSetting> AppSettings => Set<AppSetting>();
        public DbSet<UserPreferences> UserPreferences { get; set; }
        public DbSet<Game> Games => Set<Game>();
        public DbSet<UserFavouriteGames> UserFavouriteGames => Set<UserFavouriteGames>();
        public DbSet<MatchmakingEntry> MatchmakingEntries { get; set; }
        public DbSet<MatchmakingReject> MatchmakingRejects { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)

        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserPreferences>().Property(p => p.Hobby).HasColumnType("jsonb");
            modelBuilder.Entity<UserPreferences>().Property(p => p.Games).HasColumnType("jsonb");
            modelBuilder.Entity<UserPreferences>().Property(p => p.Books).HasColumnType("jsonb");
            modelBuilder.Entity<UserPreferences>().Property(p => p.Food).HasColumnType("jsonb");
            modelBuilder.Entity<UserPreferences>().Property(p => p.Drink).HasColumnType("jsonb");
            modelBuilder.Entity<UserPreferences>().Property(p => p.Music).HasColumnType("jsonb");
            modelBuilder.Entity<UserPreferences>().Property(p => p.FavoriteGames).HasColumnType("jsonb");
            // zmiana nazwy tabeli Users
            modelBuilder.Entity<User>(b =>
            {
                b.Ignore(u => u.PhoneNumber);
                b.Ignore(u => u.PhoneNumberConfirmed);
                b.Ignore(u => u.TwoFactorEnabled);
                b.ToTable("Users");
                b.Property(u => u.RadiusKm).HasDefaultValue(30);
                b.HasIndex(u => u.Email).IsUnique();
                b.HasIndex(u => u.UserName).IsUnique();
            });
            modelBuilder.Entity<User>()
                .Property(u => u.LocationPermission)
                .HasDefaultValue(true);

            // ------------------------ tabela conversations -------------------------
            modelBuilder.Entity<Conversation>()
                .HasIndex(c => new { c.Name, c.isGroup });

            modelBuilder.Entity<Conversation>()
                .HasIndex(c => new { c.Latitude, c.Longitude });

            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.Game)
                .WithMany(g => g.Conversations)
                .HasForeignKey(c => c.GameId)
                .OnDelete(DeleteBehavior.SetNull); // ważne przy nullable

            // --------------------- tabela conversation_members ---------------------
            // klucz główny na kolumnach conversation_id i user_id
            modelBuilder.Entity<ConversationMember>().HasKey(cm => new { cm.ConversationId, cm.UserId });

            // relacja conversation_members.user_id <-> users.id
            modelBuilder.Entity<ConversationMember>()
                .HasOne(cm => cm.User)
                .WithMany(u => u.ConversationMembers)
                .HasForeignKey(cm => cm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // relacja conversation_members.conversation_id <-> conversations.id
            modelBuilder.Entity<ConversationMember>()
                .HasOne(cm => cm.Conversation)
                .WithMany(c => c.Members)
                .HasForeignKey(cm => cm.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);


            // --------------------- tabela messages ---------------------
            // relacja messages.conversation_id <-> conversations.id
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            // relacja messages.sender_id <-> users.id
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.Messages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasIndex(m => new { m.ConversationId, m.CreatedAt });

            // -------------------- tabela messageAttachments ------------
            // relacja messageAttachment.id <-> messages.id
            modelBuilder.Entity<MessageAttachment>()
                .HasOne(a => a.Message)
                .WithMany(m => m.Attachments)
                .HasForeignKey(a => a.MessageId)
                .OnDelete(DeleteBehavior.Cascade);


            // --------------------- tabela friends ---------------------
            modelBuilder.Entity<FriendRelation>().HasKey(fr => new { fr.UserId, fr.FriendId });

            // relacja friends.user_id <-> users.id
            modelBuilder.Entity<FriendRelation>()
                .HasOne(fr => fr.User)
                .WithMany(u => u.FriendsInitiated)
                .HasForeignKey(fr => fr.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // relacja friends.friend_id <-> users.id
            modelBuilder.Entity<FriendRelation>()
                .HasOne(fr => fr.Friend)
                .WithMany(u => u.FriendsReceived)
                .HasForeignKey(fr => fr.FriendId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FriendRelation>()
                .HasOne(fr => fr.Conversation)
                .WithMany(c => c.FriendRelations)
                .HasForeignKey(fr => fr.ConversationId)
                .OnDelete(DeleteBehavior.SetNull);

            // --------------------- tabela blocked_users ---------------------
            modelBuilder.Entity<BlockedUser>().HasKey(bu => new { bu.UserId, bu.BlockedUserId });

            // relacja blocked_users.user_id <-> users.id
            modelBuilder.Entity<BlockedUser>()
                .HasOne(bu => bu.User)
                .WithMany(u => u.UsersBlocked)
                .HasForeignKey(bu => bu.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // relacja blocked_users.user_id <-> users.id
            modelBuilder.Entity<BlockedUser>()
                .HasOne(bu => bu.Blocked)
                .WithMany(u => u.BlockedByUsers)
                .HasForeignKey(bu => bu.BlockedUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // --------------------- tabela user_settings ---------------------
            // klucz główny na user_id
            modelBuilder.Entity<UserSettings>().HasKey(us => us.UserId);

            // relacja 1:1 user_settings.user_id <-> users.id
            modelBuilder.Entity<UserSettings>()
                .HasOne(us => us.User)
                .WithOne(u => u.Settings)
                .HasForeignKey<UserSettings>(us => us.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // --------------------- tabela refresh_tokens ---------------------
            // klucz główny na user_id
            modelBuilder.Entity<RefreshToken>().HasKey(rt => rt.Id);
            modelBuilder.Entity<RefreshToken>()
                .HasIndex(rt => rt.Token)
                .IsUnique();

            // relacja 1:N refresh_token.user_id <-> users.id
            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // appsettings
            modelBuilder.Entity<AppSetting>(entity =>
            {
                entity.HasKey(x => x.Key);

                entity.Property(x => x.Key)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.Value)
                    .IsRequired()
                    .HasMaxLength(500);
            });

            // gry
            modelBuilder.Entity<Game>()
                .HasIndex(g => g.NormalizedName)
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops")
                .HasDatabaseName("IX_Games_NormalizedName_Trigram");

            modelBuilder.Entity<Game>()
                .HasIndex(g => g.Name)
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops")
                .HasDatabaseName("IX_Games_Name_Trigram");

            // ulubione gry użytkownika
            modelBuilder.Entity<UserFavouriteGames>()
                .HasKey(fg => new { fg.UserId, fg.GameId });

            modelBuilder.Entity<UserFavouriteGames>()
                .HasIndex(fg => fg.UserId);

            modelBuilder.Entity<UserFavouriteGames>()
                .HasOne(fg => fg.User)
                .WithMany(u => u.FavouriteGames)
                .HasForeignKey(fg => fg.UserId);

            modelBuilder.Entity<UserFavouriteGames>()
                .HasOne(fg => fg.Game)
                .WithMany(g => g.FavouritedBy)
                .HasForeignKey(fg => fg.GameId);

            // -------------- MatchmakingEntry --------------
            modelBuilder.Entity<MatchmakingEntry>(entity =>
            {
                entity.HasKey(e => e.MatchId);

                entity.HasOne(e => e.Game)
                    .WithMany()
                    .HasForeignKey(e => e.GameId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.CreatorUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatorUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.JoinerUser)
                    .WithMany()
                    .HasForeignKey(e => e.JoinerUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.CreatorStatus)
                    .HasConversion<string>();

                entity.Property(e => e.JoinerStatus)
                    .HasConversion<string>();

                entity.Property(e => e.Type)
                    .HasConversion<string>();

                entity.Property(e => e.Description)
                    .HasMaxLength(5000);

                entity.HasIndex(e => new { e.GameId, e.CreatorStatus, e.ExpiringAt });
                entity.HasIndex(e => new { e.CreatorUserId, e.GameId });
            });

            /*
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX ux_matchmaking_active_user_game
                ON ""MatchmakingEntries"" (""CreatorUserId"", ""GameId"")
                WHERE ""IsActive"" = TRUE;
            ");
            */

            // -------------- MatchmakingRejects ----------------
            modelBuilder.Entity<MatchmakingReject>(entity =>
            {
                entity.HasKey(e => new { e.MatchId, e.UserId });

                entity.HasOne(e => e.Entry)
                    .WithMany()
                    .HasForeignKey(e => e.MatchId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.MatchId, e.UserId });
            });


            modelBuilder.Entity<UserPreferences>()
                .HasOne(up => up.User)
                .WithOne(u => u.Preferences)
                .HasForeignKey<UserPreferences>(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserPreferences>()
                .HasIndex(up => up.UserId)
                .IsUnique();
        }
    }
}
