using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using USJRLedger.Models;

namespace USJRLedger.Services
{
    public class SchoolYearService
    {
        private readonly DataService _dataService;

        public SchoolYearService(DataService dataService)
        {
            _dataService = dataService;
        }

        public async Task<List<SchoolYear>> GetAllSchoolYearsAsync()
        {
            return await _dataService.LoadFromFileAsync<SchoolYear>("schoolyears.json");
        }

        public async Task<List<SchoolYear>> GetSchoolYearsByOrganizationAsync(string organizationId)
        {
            var schoolYears = await _dataService.LoadFromFileAsync<SchoolYear>("schoolyears.json");
            return schoolYears.Where(sy => sy.OrganizationId == organizationId).ToList();
        }

        public async Task<SchoolYear> GetActiveSchoolYearAsync(string organizationId)
        {
            var schoolYears = await _dataService.LoadFromFileAsync<SchoolYear>("schoolyears.json");
            return schoolYears.FirstOrDefault(sy => sy.OrganizationId == organizationId && sy.IsActive);
        }

        public async Task<SchoolYear> GetSchoolYearByIdAsync(string id)
        {
            var schoolYears = await _dataService.LoadFromFileAsync<SchoolYear>("schoolyears.json");
            return schoolYears.FirstOrDefault(sy => sy.Id == id);
        }

        public async Task<SchoolYear> StartSchoolYearAsync(string organizationId, string semester, string year)
        {
            var schoolYears = await _dataService.LoadFromFileAsync<SchoolYear>("schoolyears.json");

            // First, deactivate any currently active school year
            var currentActive = schoolYears.FirstOrDefault(sy => sy.OrganizationId == organizationId && sy.IsActive);
            if (currentActive != null)
            {
                currentActive.IsActive = false;
                currentActive.EndDate = DateTime.Now;
            }

            // Create a new active school year
            var newSchoolYear = new SchoolYear
            {
                OrganizationId = organizationId,
                Semester = semester,
                Year = year,
                StartDate = DateTime.Now,
                IsActive = true
            };

            schoolYears.Add(newSchoolYear);
            await _dataService.SaveToFileAsync(schoolYears, "schoolyears.json");

            return newSchoolYear;
        }

        public async Task EndSchoolYearAsync(string schoolYearId)
        {
            var schoolYears = await _dataService.LoadFromFileAsync<SchoolYear>("schoolyears.json");
            var schoolYear = schoolYears.FirstOrDefault(sy => sy.Id == schoolYearId);

            if (schoolYear != null)
            {
                schoolYear.IsActive = false;
                schoolYear.EndDate = DateTime.Now;

                await _dataService.SaveToFileAsync(schoolYears, "schoolyears.json");
            }
        }
    }
}