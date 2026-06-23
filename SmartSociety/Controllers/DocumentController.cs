using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using SmartSociety.Models;
using SmartSociety.Repositories;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SmartSociety.Controllers
{
    public class DocumentController : Controller
    {
        private readonly IDocumentRepository _repository;
        private readonly IWebHostEnvironment _env;

        public DocumentController(IDocumentRepository repository, IWebHostEnvironment env)
        {
            _repository = repository;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var documents = await _repository.GetAllDocumentsAsync();
            return View(documents);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(DocumentUploadViewModel model)
        {
            try
            {
                if (model.File == null || model.File.Length == 0)
                {
                    return Json(new { success = false, message = "Please select a valid file to upload." });
                }

                // 1. Ensure directory exists
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "documents");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // 2. Generate unique filename and save file
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.File.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.File.CopyToAsync(fileStream);
                }

                // 3. Save to database
                var document = new SocietyDocument
                {
                    Title = model.Title,
                    Category = model.Category,
                    IsVisibleToResidents = model.IsVisibleToResidents,
                    FilePath = $"/uploads/documents/{uniqueFileName}"
                };

                await _repository.InsertDocumentAsync(document);

                return Json(new { success = true, message = "Document uploaded successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error uploading document: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDocument(DocumentEditViewModel model)
        {
            try
            {
                var existingDoc = await _repository.GetDocumentByIdAsync(model.DocumentId);
                if (existingDoc == null) return Json(new { success = false, message = "Document not found." });

                string? newFilePath = null;

                if (model.File != null && model.File.Length > 0)
                {
                    // 1. Ensure directory exists
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "documents");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    // 2. Generate unique filename and save new file
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.File.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.File.CopyToAsync(fileStream);
                    }
                    newFilePath = $"/uploads/documents/{uniqueFileName}";

                    // 3. Delete old file
                    var oldPhysicalPath = Path.Combine(_env.WebRootPath, existingDoc.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldPhysicalPath))
                    {
                        System.IO.File.Delete(oldPhysicalPath);
                    }
                }

                // Update properties
                existingDoc.Title = model.Title;
                existingDoc.Category = model.Category;
                existingDoc.IsVisibleToResidents = model.IsVisibleToResidents;
                if (newFilePath != null) existingDoc.FilePath = newFilePath;

                await _repository.UpdateDocumentAsync(existingDoc);
                
                return Json(new { success = true, message = "Document updated successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating document: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(int documentId)
        {
            try
            {
                var document = await _repository.GetDocumentByIdAsync(documentId);
                if (document != null)
                {
                    // 1. Delete physical file
                    var physicalPath = Path.Combine(_env.WebRootPath, document.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(physicalPath))
                    {
                        System.IO.File.Delete(physicalPath);
                    }

                    // 2. Delete from DB
                    await _repository.DeleteDocumentAsync(documentId);
                }

                return Json(new { success = true, message = "Document deleted successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting document: " + ex.Message });
            }
        }
    }
}
