using Microsoft.EntityFrameworkCore;
using testIMG.Models;
namespace testIMG.Data
{
    public class ImageContext : DbContext
    {
        public ImageContext(DbContextOptions<ImageContext> options) : base(options) { }

        public DbSet<UploadImageModel> Images { get; set; }
    }
}
