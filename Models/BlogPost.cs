namespace WebNangCao.Models
{
    public enum BlogPostStatus
    {
        Draft,      // Nháp
        Published,  // Công khai
        Archived    // Lưu trữ
    }

    public class BlogPost : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        
        public BlogPostStatus Status { get; set; } = BlogPostStatus.Draft;

        public string? AuthorId { get; set; }
        public ApplicationUser? Author { get; set; }
        public ICollection<BlogComment> Comments { get; set; } = new List<BlogComment>();
    }
}
