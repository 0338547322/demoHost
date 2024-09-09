using Azure;
using Azure.AI.ContentSafety;
using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace testIMG.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ImageController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        // Đọc khóa và endpoint từ cấu hình
        private string Endpoint => _configuration["10022002"];
        private string ApiKey => _configuration["19102001"];

        // Phương thức kiểm duyệt hình ảnh
        [HttpPost("moderate")]
        public async Task<IActionResult> ModerateImage(IFormFile imageFile)
        {
            // Kiểm tra xem file có tồn tại và có kích thước lớn hơn 0
            if (imageFile == null || imageFile.Length == 0)
            {
                return BadRequest("Invalid file.");
            }

            try
            {
                // Tạo đối tượng ContentSafetyClient với thông tin cấu hình
                var client = new ContentSafetyClient(new Uri(Endpoint), new AzureKeyCredential(ApiKey));

                // Đọc dữ liệu hình ảnh từ file upload vào một MemoryStream
                using (var imageStream = new MemoryStream())
                {
                    await imageFile.CopyToAsync(imageStream); // Sao chép dữ liệu từ file upload vào stream
                    imageStream.Position = 0; // Đặt vị trí đọc của stream về đầu

                    // Tạo đối tượng ContentSafetyImageData từ dữ liệu hình ảnh
                    var imageData = new ContentSafetyImageData(BinaryData.FromBytes(imageStream.ToArray()));

                    // Tạo đối tượng yêu cầu kiểm duyệt hình ảnh
                    var request = new AnalyzeImageOptions(imageData);

                    // Gửi yêu cầu kiểm duyệt hình ảnh lên Azure Content Safety
                    Response<AnalyzeImageResult> response;
                    try
                    {
                        response = client.AnalyzeImage(request);
                    }
                    catch (RequestFailedException ex)
                    {
                        return StatusCode(500, $"Analyze image failed. Status code: {ex.Status}, Error code: {ex.ErrorCode}, Error message: {ex.Message}");
                    }

                    // Lấy kết quả kiểm duyệt từ dịch vụ
                    var result = response.Value;
                    var hateSeverity = result.CategoriesAnalysis.FirstOrDefault(a => a.Category == ImageCategory.Hate)?.Severity ?? 0; //ngôn từ thù địch
                    var selfHarmSeverity = result.CategoriesAnalysis.FirstOrDefault(a => a.Category == ImageCategory.SelfHarm)?.Severity ?? 0; //tự hại bản thân
                    var sexualSeverity = result.CategoriesAnalysis.FirstOrDefault(a => a.Category == ImageCategory.Sexual)?.Severity ?? 0; // tình dục 
                    var violenceSeverity = result.CategoriesAnalysis.FirstOrDefault(a => a.Category == ImageCategory.Violence)?.Severity ?? 0; // bạo lực 


                    // Khởi tạo biến chứa thông báo
                    string message = "Image successfully moderated.";

                    // Tạo danh sách thông báo tương ứng với từng mức độ nghiêm trọng
                    if (sexualSeverity != 0)
                    {
                        message += $"Hình ảnh có tính khiêu dâm mức độ: {sexualSeverity}.";
                    }
                    if (hateSeverity != 0)
                    {
                        message += $" Hình ảnh có tính thù địch mức độ: {hateSeverity}.";
                    }
                    if (selfHarmSeverity != 0)
                    {
                        message += $" Hình ảnh có tính tự làm hại bản thân mức độ: {selfHarmSeverity}.";
                    }
                    if (violenceSeverity != 0)
                    {
                        message += $" Hình ảnh có tính bạo lực mức độ: {violenceSeverity}.";
                    }


                    // Trả kết quả kiểm duyệt về cho client
                    return Ok(new
                    {
                        HateSeverity = hateSeverity,
                        SelfHarmSeverity = selfHarmSeverity,
                        SexualSeverity = sexualSeverity,
                        ViolenceSeverity = violenceSeverity,
                        Message = message
                    });
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi và trả về thông báo lỗi cho client
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
