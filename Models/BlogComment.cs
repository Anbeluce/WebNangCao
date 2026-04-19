namespace WebNangCao.Models
{
    public class BlogComment : BaseEntity
    {
        public int BlogPostId { get; set; }
        public string? AuthorId { get; set; }
        public string Content { get; set; } = string.Empty;
        public BlogPost BlogPost { get; set; } = null!;
        public ApplicationUser? Author { get; set; }
    }
}
