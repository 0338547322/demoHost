using System.ComponentModel.DataAnnotations;

namespace testIMG.Models
{
    public class UploadImageModel
    {
        public IFormFile ImageFile { get; set; }
    }
}
