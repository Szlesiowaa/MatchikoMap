using MatchikoMap.Data;
using MatchikoMap.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace MatchikoMap.Utils
{
    public class DatabaseSeeder
    {
        public static JsonSerializerOptions GetOptions()
        {
            return new()
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public static async Task InitGlobalConversations(AppDbContext _db, JsonSerializerOptions options)
        {
            var flag = await _db.AppSettings.FirstOrDefaultAsync(x => x.Key == "AreGlobalConversationsAddedToDatabase");
            if (flag == null)
            {
                flag = new AppSetting
                {
                    Key = "AreGlobalConversationsAddedToDatabase",
                    Value = "false"
                };

                _db.AppSettings.Add(flag);
                await _db.SaveChangesAsync();
            }
            if (flag?.Value == "true") return;

            var counties = JsonSerializer.Deserialize<List<County>>(await File.ReadAllTextAsync("Data/lista_powiatow.json",Encoding.UTF8),options)!;

            var games = JsonSerializer.Deserialize<List<GameDto>>(await File.ReadAllTextAsync("Data/lista_gier.json", Encoding.UTF8), options)!;

            var newGames = new List<Game>();

            var conversations = new List<Conversation>();

            foreach (var dto in games)
            {

                var game = new Game
                {
                    Name = dto.Name,
                    NormalizedName = Normalize(dto.Name),
                    Acronym = GenerateAcronym(dto.Name),
                    IconPath = dto.IconPath,
                    GridPath = dto.GridPath,
                    PosterPath = dto.PosterPath
                };

                newGames.Add(game);
            }

            if (newGames.Count > 0)
            {
                await _db.Games.AddRangeAsync(newGames);
            }

            await _db.SaveChangesAsync();

            foreach (var game in newGames)
            {
                foreach (var county in counties)
                {
                    /*
                    var exists = await _db.Conversations.AnyAsync(c =>
                        c.Name == game &&
                        c.Latitude == county.Lat &&
                        c.Longitude == county.Lng);

                    if (exists) continue;
                    */
                    conversations.Add(new Conversation
                    {
                        Name = game.Name,
                        isGroup = true,
                        Latitude = county.Lat,
                        Longitude = county.Lng,
                        LocationName = county.Name,
                        Messages = [],
                        GameId = game.Id
                    });
                }
                
            }

            await _db.Conversations.AddRangeAsync(conversations);
            flag!.Value = "true";
            await _db.SaveChangesAsync();
        }

        public static string Normalize(string input)
        {
            return input
                .Trim()
                .ToUpperInvariant()
                .Normalize(System.Text.NormalizationForm.FormD)
                .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                .Aggregate("", (s, c) => s + c);
        }
        public static string GenerateAcronym(string name)
        {
            if (string.IsNullOrWhiteSpace(name))return string.Empty;

            var words = Regex
                .Split(name.ToUpperInvariant(), @"[^A-Z0-9]+") // wszystko co nie litera/cyfra
                .Where(w => !string.IsNullOrWhiteSpace(w));

            if (words.Count() < 3) return string.Empty;

            var chars = new List<char>();

            foreach (var word in words)
            {
                if (char.IsDigit(word[0]))
                    chars.AddRange(word);
                else
                    chars.Add(word[0]);
            }

            return new string([.. chars]);
        }
    }
    public class County
    {
        public string Name { get; set; } = null!;
        public double Lat { get; set; }
        public double Lng { get; set; }
    }
}
