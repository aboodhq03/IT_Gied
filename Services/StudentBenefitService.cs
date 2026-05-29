using IT_Gied.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IT_Gied.Services
{
    public class StudentBenefitService : IStudentBenefitService
    {
        private readonly AppDbContext _context;

        public StudentBenefitService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<StudentBenefit>> GetActiveBenefitsAsync()
        {
            return await _context.StudentBenefits
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<StudentBenefit>> GetAllBenefitsAsync()
        {
            return await _context.StudentBenefits
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<StudentBenefit?> GetByIdAsync(int id)
        {
            return await _context.StudentBenefits.FindAsync(id);
        }

        public async Task AddAsync(StudentBenefit benefit)
        {
            benefit.CreatedAt = DateTime.UtcNow;
            _context.StudentBenefits.Add(benefit);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(StudentBenefit benefit)
        {
            var existingBenefit = await _context.StudentBenefits.FindAsync(benefit.Id);
            if (existingBenefit == null)
            {
                throw new InvalidOperationException("Benefit not found.");
            }

            existingBenefit.Title = benefit.Title;
            existingBenefit.Description = benefit.Description;
            existingBenefit.Category = benefit.Category;
            existingBenefit.ProviderName = benefit.ProviderName;
            existingBenefit.Link = benefit.Link;
            existingBenefit.Icon = benefit.Icon;
            existingBenefit.IsActive = benefit.IsActive;

            _context.StudentBenefits.Update(existingBenefit);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var benefit = await _context.StudentBenefits.FindAsync(id);
            if (benefit != null)
            {
                _context.StudentBenefits.Remove(benefit);
                await _context.SaveChangesAsync();
            }
        }

        public async Task SetActiveAsync(int id, bool isActive)
        {
            var benefit = await _context.StudentBenefits.FindAsync(id);
            if (benefit != null)
            {
                benefit.IsActive = isActive;
                await _context.SaveChangesAsync();
            }
        }
    }
}
