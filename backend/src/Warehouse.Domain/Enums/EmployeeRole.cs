namespace Warehouse.Domain.Enums;

// The 12 roles from docs/requirements.md section 3. Numbers are stored in the database, never reorder or reuse them.
// SuperAdmin is created only by signup/seed. Employees pages can create the others.
public enum EmployeeRole
{
    SuperAdmin = 1,
    WarehouseAdmin = 2,
    WarehouseManager = 3,
    ShipmentReceivingClerk = 4,
    SortingOperator = 5,
    LoaderDispatchOperator = 6,
    Dispatcher = 7,
    Driver = 8,
    InventoryAuditOfficer = 9,
    FinanceOfficer = 10,
    CarrierRepresentative = 11,
    ClientMerchant = 12
}
