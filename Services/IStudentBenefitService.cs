using IT_Gied.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IT_Gied.Services
{
    public interface IStudentBenefitService
    {
        Task<List<StudentBenefit>> GetActiveBenefitsAsync();
        Task<List<StudentBenefit>> GetAllBenefitsAsync();
        Task<StudentBenefit?> GetByIdAsync(int id);
        Task AddAsync(StudentBenefit benefit);
        Task UpdateAsync(StudentBenefit benefit);
        Task DeleteAsync(int id);
        Task SetActiveAsync(int id, bool isActive);
    }
}
