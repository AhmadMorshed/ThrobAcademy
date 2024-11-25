using Microsoft.AspNetCore.Mvc;
using Throb.Repository.Interfaces;

namespace ThropAcademy.Web.Controllers
{
    public class DriveSessionController : Controller
    {
        private readonly IDriveSessionRepository _driveSessionRepository;

        public DriveSessionController(IDriveSessionRepository driveSessionRepository)
        {
            _driveSessionRepository = driveSessionRepository;
        }

        public IActionResult Index()
        {
            var drivesession = _driveSessionRepository.GetAll();
            return View();
        }
    }
}
