using InventoryManagement.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Core.Entities
{
    public class ErrorLog : Common
    {
        [Key]
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
        public ErrorSeverity Severity { get; set; }
        public string? MethodName { get; set; }
        public string? InnerException { get; set; }
        public string? LineNo { get; set; } 
        public int? UserId { get; set; }
        public string? IpAddress { get; set; }
    }
}
