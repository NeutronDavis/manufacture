using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Logistics.Maintenance
{
    public class CreateModel : PageModel
    {
        private readonly IFleetService _fleetService;
        private readonly IImageUploadService _imageUploadService;

        public CreateModel(IFleetService fleetService, IImageUploadService imageUploadService)
        {
            _fleetService = fleetService;
            _imageUploadService = imageUploadService;
        }

        [BindProperty]
        public MaintenanceLogDto MaintenanceInput { get; set; } = new();

        [BindProperty]
        public IFormFile? BeforeImage { get; set; }

        [BindProperty]
        public IFormFile? AfterImage { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public List<VehicleDto> AllVehicles { get; set; } = new();

        public async Task OnGetAsync(int? vehicleId, string? returnUrl)
        {
            ReturnUrl = returnUrl;
            AllVehicles = await _fleetService.GetVehiclesAsync();

            if (vehicleId.HasValue && vehicleId.Value > 0)
            {
                MaintenanceInput.VehicleId = vehicleId.Value;
            }
            else if (AllVehicles.Any())
            {
                MaintenanceInput.VehicleId = AllVehicles.First().Id;
            }

            MaintenanceInput.ServiceDate = DateTime.UtcNow;
            MaintenanceInput.Cost = 45000;
            MaintenanceInput.Description = "Scheduled Engine Oil & Filter Service";
            MaintenanceInput.WorkshopOrVendor = "Mandilas Motors Lagos";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            AllVehicles = await _fleetService.GetVehiclesAsync();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                if (BeforeImage is { Length: > 0 })
                {
                    await using var beforeStream = BeforeImage.OpenReadStream();
                    MaintenanceInput.BeforeImageUrl = await _imageUploadService.UploadAsync(
                        beforeStream,
                        BeforeImage.FileName,
                        "uploads/maintenance");
                }

                if (AfterImage is { Length: > 0 })
                {
                    await using var afterStream = AfterImage.OpenReadStream();
                    MaintenanceInput.AfterImageUrl = await _imageUploadService.UploadAsync(
                        afterStream,
                        AfterImage.FileName,
                        "uploads/maintenance");
                }

                var created = await _fleetService.CreateMaintenanceLogAsync(MaintenanceInput);
                TempData["SuccessMessage"] =
                    $"Maintenance service cost of ₦{created.Cost:N0} recorded for {created.VehicleRegNumber}.";

                if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                {
                    return Redirect(ReturnUrl);
                }

                return RedirectToPage("/Logistics/Maintenance/Details", new { id = created.Id });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }
    }
}
