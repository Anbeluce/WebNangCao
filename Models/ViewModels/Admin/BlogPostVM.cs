using System;
using System.ComponentModel.DataAnnotations;

namespace WebNangCao.Models.ViewModels.Admin
{
    public class BlogPostListVM
    {
        public int Id { get; set; }

        [Display(Name = "Tiêu đề")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Tác giả")]
        public string AuthorName { get; set; } = string.Empty;

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Trạng thái công khai")]
        public BlogPostStatus Status { get; set; }

        [Display(Name = "Trạng thái xóa")]
        public bool IsDeleted { get; set; }
    }
}
