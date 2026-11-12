using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Logistics.FuelLogs
{
    public class CreateModel : PageModel
    {
        private readonly IFleetService _fleetService;

        public CreateModel(IFleetService fleetService)
        {
            _fleetService = fleetService;
        }

        [BindProperty]
        public FuelLogDto LogInput { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public List<VehicleDto> Vehicles { get; set; } = new();
        public List<EmployeeDto> Drivers { get; set; } = new();

        public async Task OnGetAsync(int? vehicleId, string? returnUrl)
        {
            ReturnUrl = returnUrl;
            Vehicles = await _fleetService.GetVehiclesAsync();
            Drivers = await _fleetService.GetLogisticsDriversAsync();

            if (vehicleId.HasValue)
            {
                LogInput.VehicleId = vehicleId.Value;
                var vehicle = Vehicles.FirstOrDefault(v => v.Id == vehicleId.Value);
                if (vehicle != null)
                {
                    LogInput.FuelType = vehicle.FuelType;
                    LogInput.EmployeeId = vehicle.AssignedEmployeeId;
                }
            }

            LogInput.LitresDispensed = 45;
            LogInput.PricePerLitre = 980;
            LogInput.PetrolStation = "NNPC Mega Station";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Vehicles = await _fleetService.GetVehiclesAsync();
            Drivers = await _fleetService.GetLogisticsDriversAsync();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var log = await _fleetService.CreateFuelLogAsync(LogInput);
                TempData["SuccessMessage"] =
                    $"Fuel expense of ₦{log.TotalCost:N0} ({log.LitresDispensed} units) logged for vehicle {log.VehicleRegNumber}.";

                if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                {
                    return Redirect(ReturnUrl);
                }

                return RedirectToPage("/Logistics/FuelLogs/Index");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }
    }
}
