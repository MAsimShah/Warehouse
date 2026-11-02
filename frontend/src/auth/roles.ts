// Same 12 roles as the API enum EmployeeRole (docs/requirements.md section 3). Values are the enum names.
export const ROLE_LABELS: Record<string, string> = {
  SuperAdmin: 'Super Admin',
  WarehouseAdmin: 'Warehouse Admin',
  WarehouseManager: 'Warehouse Manager',
  ShipmentReceivingClerk: 'Shipment Receiving Clerk',
  SortingOperator: 'Sorting Operator',
  LoaderDispatchOperator: 'Loader / Dispatch Operator',
  Dispatcher: 'Dispatcher',
  Driver: 'Driver / Delivery Rider',
  InventoryAuditOfficer: 'Inventory / Audit Officer',
  FinanceOfficer: 'Finance Officer',
  CarrierRepresentative: 'Carrier Representative',
  ClientMerchant: 'Client / Merchant',
}

// Roles an admin can give to an employee. Super Admin is created by signup only.
export const ASSIGNABLE_ROLES = Object.keys(ROLE_LABELS).filter((role) => role !== 'SuperAdmin')

export function roleLabel(role: string): string {
  return ROLE_LABELS[role] ?? role
}

// Only the Super Admin can open the Employees page.
export function canManageEmployees(user: { isSuperAdmin: boolean } | null): boolean {
  return !!user?.isSuperAdmin
}
