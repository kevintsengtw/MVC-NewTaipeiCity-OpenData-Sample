using System.ComponentModel.DataAnnotations;

namespace Sample.Models
{
    public class HotSpot
    {
        [Display(Name = "熱點代碼")]
        public string ID { get; set; }

        [Display(Name = "熱點名稱")]
        public string Spot_Name { get; set; }

        [Display(Name = "熱點分類")]
        public string Type { get; set; }

        [Display(Name = "業者")]
        public string Company { get; set; }

        [Display(Name = "鄉鎮市區")]
        public string District { get; set; }

        [Display(Name = "地址")]
        public string Address { get; set; }

        [Display(Name = "機關名稱")]
        public string Apparatus_Name { get; set; }

        [Display(Name = "緯度")]
        public string Latitude { get; set; }

        [Display(Name = "經度")]
        public string Longitude { get; set; }
    }

}