using System.ComponentModel.DataAnnotations;

namespace WebNangCao.Models.ViewModels.Admin
{
    public class BlogPostCreateVM
    {
        [Required(ErrorMessage = "Vui lòng nhập tiêu đề bài viết")]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập nội dung bài viết")]
        [Display(Name = "Nội dung")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Trạng thái")]
        public BlogPostStatus Status { get; set; } = BlogPostStatus.Draft;
    }
}
