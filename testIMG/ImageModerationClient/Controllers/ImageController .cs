using ImageModerationClient.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

namespace ImageModerationClient.Controllers
{
    public class ImageController : Controller
    {
        private readonly HttpClient _httpClient;

        public ImageController()
        {
            _httpClient = new HttpClient();
        }

        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(UploadImageModel model)
        {
            if (model.ImageFile == null || model.ImageFile.Length == 0)
            {
                ViewBag.Message = "Please select a valid image file.";
                return View("Upload");
            }

            var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(model.ImageFile.OpenReadStream());
            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "ImageFile",
                FileName = model.ImageFile.FileName
            };
            content.Add(fileContent);

            var response = await _httpClient.PostAsync("https://localhost:7220/api/Image/moderate", content);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                var moderationResult = JObject.Parse(result);
                ViewBag.Message = "Image uploaded and processed successfully.";
                ViewBag.Result = moderationResult;
            }
            else
            {
                ViewBag.Message = "Error occurred while processing the image.";
                ViewBag.Result = await response.Content.ReadAsStringAsync();
            }

            return View("Upload");
        }
    }
}
