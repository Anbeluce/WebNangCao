namespace WebNangCao.Models.Dtos
{
    public class ApartmentDropdownItemDto
    {
        public int Id { get; set; }
        public string ApartmentNumber { get; set; } = string.Empty;
        public int Floor { get; set; }
        public double Area { get; set; }
        public string OwnerName { get; set; } = string.Empty;
    }
}
