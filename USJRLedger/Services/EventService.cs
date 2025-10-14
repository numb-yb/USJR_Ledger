using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using USJRLedger.Models;

namespace USJRLedger.Services
{
    public class EventService
    {
        private readonly DataService _dataService;

        public EventService(DataService dataService)
        {
            _dataService = dataService;
        }

        public async Task<List<Event>> GetAllEventsAsync()
        {
            return await _dataService.LoadFromFileAsync<Event>("events.json");
        }

        public async Task<List<Event>> GetEventsByOrganizationAsync(string organizationId)
        {
            var events = await _dataService.LoadFromFileAsync<Event>("events.json");
            return events.Where(e => e.OrganizationId == organizationId).ToList();
        }

        public async Task<List<Event>> GetEventsBySchoolYearAsync(string schoolYearId)
        {
            var events = await _dataService.LoadFromFileAsync<Event>("events.json");
            return events.Where(e => e.SchoolYearId == schoolYearId).ToList();
        }

        public async Task<Event> GetEventByIdAsync(string id)
        {
            var events = await _dataService.LoadFromFileAsync<Event>("events.json");
            return events.FirstOrDefault(e => e.Id == id);
        }

        public async Task<Event> CreateEventAsync(string organizationId, string schoolYearId,
                                                string name, DateTime eventDate, string createdBy)
        {
            var events = await _dataService.LoadFromFileAsync<Event>("events.json");

            var newEvent = new Event
            {
                OrganizationId = organizationId,
                SchoolYearId = schoolYearId,
                Name = name,
                EventDate = eventDate,
                CreatedBy = createdBy
            };

            events.Add(newEvent);
            await _dataService.SaveToFileAsync(events, "events.json");

            return newEvent;
        }

        public async Task<decimal> GetEventBalanceAsync(string eventId)
        {
            var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");
            var eventTransactions = transactions.Where(t => t.EventId == eventId &&
                                                          t.ApprovalStatus == ApprovalStatus.Approved);

            decimal totalIncome = eventTransactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
            decimal totalExpense = eventTransactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

            return totalIncome - totalExpense;
        }
    }
}