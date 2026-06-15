using Microsoft.AspNetCore.Mvc.Rendering;

namespace SmartSociety.Models
{
    public class ParkingSlotUpsertViewModel
    {
        public ParkingSlot ParkingSlot { get; set; } = new ParkingSlot();
        
        // Flat List for Dropdown
        public IEnumerable<SelectListItem> FlatList { get; set; } = new List<SelectListItem>();
    }
}
