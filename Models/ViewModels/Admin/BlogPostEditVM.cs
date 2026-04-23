using System.ComponentModel.DataAnnotations;

namespace WebNangCao.Models.ViewModels.Admin
{
    public class BlogPostEditVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề bài viết")]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập nội dung bài viết")]
        [Display(Name = "Nội dung")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Trạng thái")]
        public BlogPostStatus Status { get; set; } = BlogPostStatus.Draft;

        [Display(Name = "Trạng thái ẩn/xóa")]
        public bool IsDeleted { get; set; }
    }
}
