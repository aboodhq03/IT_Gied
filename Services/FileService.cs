using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace IT_Gied.Services
{
    /// <summary>
    /// Service to handle file and image operations.
    /// خدمة مسؤولة عن التعامل مع الملفات والصور.
    /// </summary>
    public interface IFileService
    {
        Task<string> UploadImageAsync(IFormFile file, string folderName);
        void DeleteImage(string imageName, string folderName);
    }

    public class FileService : IFileService
    {
        private readonly string _basePath;

        public FileService()
        {
            // Initializing the base path for uploads (wwwroot/Uplode)
            _basePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Uplode");
        }

        /// <summary>
        /// Saves an image to the specified folder and returns the unique file name.
        /// يقوم بحفظ الصورة في المجلد المحدد ويرجع اسم الملف الفريد.
        /// </summary>
        /// <param name="file">The uploaded file.</param>
        /// <param name="folderName">The sub-folder under Uplode.</param>
        public async Task<string> UploadImageAsync(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0) return string.Empty;

            // Generate unique name using GUID and Timestamp to prevent collisions
            // إنشاء اسم فريد للملف باستخدام GUID وطابع زمني لمنع تضارب الأسماء
            string uniqueName = $"{Guid.NewGuid()}_{DateTime.Now:yyyyMMdd_HHmmss}{Path.GetExtension(file.FileName)}";
            
            string folderPath = Path.Combine(_basePath, folderName);
            
            // Ensure the target directory exists
            // التأكد من أن المجلد المستهدف موجود، وإن لم يكن سيتم إنشاؤه
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = Path.Combine(folderPath, uniqueName);

            // Save the file to the server's disk
            // حفظ الملف على قرص الخادم
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return uniqueName;
        }

        /// <summary>
        /// Deletes an image from the server if it exists.
        /// يقوم بحذف الصورة من الخادم إذا كانت موجودة.
        /// </summary>
        public void DeleteImage(string imageName, string folderName)
        {
            if (string.IsNullOrWhiteSpace(imageName)) return;

            string filePath = Path.Combine(_basePath, folderName, imageName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
