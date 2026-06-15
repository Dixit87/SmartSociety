using Microsoft.AspNetCore.Mvc.Rendering;

namespace SmartSociety.Models
{
    public class FlatUpsertViewModel
    {
        public Flat Flat { get; set; } = new Flat();
        
        public IEnumerable<SelectListItem> BlockList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> UserList { get; set; } = new List<SelectListItem>();
    }
}
