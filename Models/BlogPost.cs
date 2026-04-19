namespace WebNangCao.Models
{
    public class BlogPost : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? AuthorId { get; set; }
        public ApplicationUser? Author { get; set; }
        public ICollection<BlogComment> Comments { get; set; } = new List<BlogComment>();
    }
}
