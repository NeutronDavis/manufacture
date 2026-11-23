using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Logistics.FuelLogs
{
    public class CreateModel : PageModel
    {
        private readonly IFleetService _fleetService;
        private readonly IFleetDispatchService _dispatchService;

        public CreateModel(IFleetService fleetService, IFleetDispatchService dispatchService)
        {
            _fleetService = fleetService;
            _dispatchService = dispatchService;
        }

        [BindProperty]
        public FuelLogDto LogInput { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public List<VehicleDto> Vehicles { get; set; } = new();
        public List<EmployeeDto> Drivers { get; set; } = new();
        public VehicleDto? LockedVehicle { get; set; }
        public string? CharterBookingRef { get; set; }
        public string? FleetDispatchRef { get; set; }
        public bool IsVehicleLocked => LogInput.CharterBookingId.HasValue || LogInput.FleetDispatchId.HasValue;

        public async Task OnGetAsync(
            int? vehicleId,
            string? returnUrl,
            int? charterBookingId,
            int? fleetDispatchId,
            CancellationToken cancellationToken = default)
        {
            ReturnUrl = returnUrl;
            Vehicles = await _fleetService.GetVehiclesAsync(cancellationToken);
            Drivers = await _fleetService.GetLogisticsDriversAsync(cancellationToken);

            if (charterBookingId.HasValue)
            {
                var booking = await _fleetService.GetCharterBookingAsync(charterBookingId.Value, cancellationToken)
                    ?? throw new InvalidOperationException("Charter booking not found.");
                LogInput.CharterBookingId = booking.Id;
                LogInput.VehicleId = booking.VehicleId;
                LogInput.EmployeeId = booking.DriverEmployeeId;
                LogInput.RouteDescription = booking.BookingRef;
                CharterBookingRef = booking.BookingRef;
            }
            else if (fleetDispatchId.HasValue)
            {
                var dispatch = await _dispatchService.GetDispatchAsync(fleetDispatchId.Value, cancellationToken)
                    ?? throw new InvalidOperationException("Fleet dispatch not found.");
                LogInput.FleetDispatchId = dispatch.Id;
                LogInput.VehicleId = dispatch.VehicleId;
                LogInput.EmployeeId = dispatch.DriverEmployeeId;
                LogInput.RouteDescription = dispatch.DispatchRef;
                FleetDispatchRef = dispatch.DispatchRef;
            }

            if (LogInput.VehicleId > 0)
            {
                LockedVehicle = Vehicles.FirstOrDefault(v => v.Id == LogInput.VehicleId);
                if (LockedVehicle != null)
                {
                    LogInput.FuelType = LockedVehicle.FuelType;
                    if (!LogInput.EmployeeId.HasValue)
                    {
                        LogInput.EmployeeId = LockedVehicle.AssignedEmployeeId;
                    }
                }
            }

            if (vehicleId.HasValue && !IsVehicleLocked)
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

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken = default)
        {
            Vehicles = await _fleetService.GetVehiclesAsync(cancellationToken);
            Drivers = await _fleetService.GetLogisticsDriversAsync(cancellationToken);
            LockedVehicle = Vehicles.FirstOrDefault(v => v.Id == LogInput.VehicleId);

            if (LogInput.CharterBookingId.HasValue)
            {
                var booking = await _fleetService.GetCharterBookingAsync(LogInput.CharterBookingId.Value, cancellationToken);
                CharterBookingRef = booking?.BookingRef;
            }

            if (LogInput.FleetDispatchId.HasValue)
            {
                var dispatch = await _dispatchService.GetDispatchAsync(LogInput.FleetDispatchId.Value, cancellationToken);
                FleetDispatchRef = dispatch?.DispatchRef;
            }

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

                if (LogInput.CharterBookingId.HasValue)
                {
                    return Redirect($"/Fleet/Charter/Details/{LogInput.CharterBookingId.Value}");
                }

                if (LogInput.FleetDispatchId.HasValue)
                {
                    return Redirect($"/Fleet/Dispatches/Details/{LogInput.FleetDispatchId.Value}");
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
