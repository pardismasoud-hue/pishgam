namespace WebApi.Contracts.Reports;

public class TicketsByCompanyReportRowDto
{
    public Guid CompanyProfileId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public int InProgressTickets { get; set; }
    public int WaitingTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public int ClosedTickets { get; set; }
}
