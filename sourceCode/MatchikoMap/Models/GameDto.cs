namespace MatchikoMap.Models
{
    public class GameDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? IconPath { get; set; }
        public string? GridPath { get; set; }
        public string? PosterPath { get; set; }
    }
}
