public class BillViewModel
{
    public string MaHoaDon { get; set; }
    public decimal TongTien { get; set; }
    public string TenCuDan { get; set; }
    public string NoiDungChuyenKhoan => $"THANH TOAN {MaHoaDon}";

    // Thông tin ngân hàng (Có thể để trong cấu hình AppSettings)
    public string BankId = "MB";
    public string AccountNo = "123456789";
    public string AccountName = "NGUYEN VAN A";
}