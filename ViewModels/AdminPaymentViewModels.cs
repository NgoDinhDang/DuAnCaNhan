using System;
using System.Collections.Generic;
using STOREBOOKS.Models;

namespace STOREBOOKS.ViewModels
{
    public class PaymentFilterViewModel
    {
        public string? Status { get; set; }
        public string? Method { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Keyword { get; set; }

        public bool HasFilters =>
            !string.IsNullOrWhiteSpace(Status) && Status != "all" ||
            !string.IsNullOrWhiteSpace(Method) && Method != "all" ||
            FromDate.HasValue ||
            ToDate.HasValue ||
            !string.IsNullOrWhiteSpace(Keyword);
    }

    public class PaymentSummaryViewModel
    {
        public int TotalPayments { get; set; }
        public int SuccessCount { get; set; }
        public int PendingCount { get; set; }
        public int FailedCount { get; set; }
        public decimal SuccessAmount { get; set; }
    }

    public class PaymentListItemViewModel
    {
        public int PaymentId { get; set; }
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? TransactionId { get; set; }
        public string? ResponseMessage { get; set; }
        public Payment Payment { get; set; } = null!;
    }

    public class PaymentAdminIndexViewModel
    {
        public List<PaymentListItemViewModel> Payments { get; set; } = new();
        public PaymentFilterViewModel Filters { get; set; } = new();
        public PaymentSummaryViewModel Summary { get; set; } = new();
        public List<string> AvailableStatuses { get; set; } = new() { "Pending", "Success", "Failed", "Cancelled" };
        public List<string> AvailableMethods { get; set; } = new() { "MoMo", "COD", "VNPay" };
    }
}


