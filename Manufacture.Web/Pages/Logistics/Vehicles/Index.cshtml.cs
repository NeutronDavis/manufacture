using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Logistics.Vehicles
{
    public class IndexModel : PageModel
    {
        private readonly IFleetService _fleetService;

        public IndexModel(IFleetService fleetService)
        {
            _fleetService = fleetService;
        }

        public List<VehicleDto> Vehicles { get; set; } = new();
        public List<string> VehicleTypes { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TypeFilter { get; set; }

        public async Task OnGetAsync()
        {
            var vehicles = await _fleetService.GetVehiclesAsync();
            VehicleTypes = vehicles
                .Select(v => v.Type)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(t => t)
                .ToList();

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var term = Search.Trim();
                vehicles = vehicles.Where(v =>
                    v.RegistrationNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    v.MakeAndModel.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (v.AssignedDriverName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(TypeFilter))
            {
                vehicles = vehicles
                    .Where(v => string.Equals(v.Type, TypeFilter, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            Vehicles = vehicles;
        }
    }
}
