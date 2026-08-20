namespace Manufacture.Models.Entities
{
    public class Vehicle
    {
        public int Id { get; set; }
        public string RegistrationNumber { get; set; } = string.Empty; // e.g. "KJA-892-XA"
        public string MakeAndModel { get; set; } = string.Empty; // e.g. "Toyota HiAce Delivery Van"
        public string Type { get; set; } = "Van"; // Van, Truck, Sedan, Motorcycle
        public string AssignedDriverName { get; set; } = string.Empty;
        public string DriverPhoneNumber { get; set; } = string.Empty;
        public string FuelType { get; set; } = "PMS (Petrol)"; // PMS, AGO (Diesel)
        public decimal TankCapacityLitres { get; set; } = 70;
        public decimal CurrentOdometerKm { get; set; }
        public string Status { get; set; } = "Active"; // Active, InMaintenance, Grounded
        public DateTime LastServiceDate { get; set; } = DateTime.UtcNow.AddMonths(-1);
    }

    public class FuelLog
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public string VehicleRegNumber { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public decimal LitresDispensed { get; set; }
        public decimal PricePerLitre { get; set; }
        public decimal TotalCost => LitresDispensed * PricePerLitre;
        public decimal OdometerReadingKm { get; set; }
        public string PetrolStation { get; set; } = string.Empty;
        public string ReceiptNumber { get; set; } = string.Empty;
        public string RouteDescription { get; set; } = string.Empty; // e.g. "Mainland Supermarket Delivery Route"
    }
}
