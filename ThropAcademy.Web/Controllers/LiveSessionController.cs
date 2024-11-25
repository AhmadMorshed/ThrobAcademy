using Microsoft.AspNetCore.Mvc;
using Throb.Repository.Interfaces;

namespace ThropAcademy.Web.Controllers
{
    public class LiveSessionController : Controller
    {
        private readonly ILiveSessionRepository _liveSessionRepository;

        public LiveSessionController(ILiveSessionRepository liveSessionRepository)
        {
            _liveSessionRepository = liveSessionRepository;
        }

        public IActionResult Index()
        {
            var livesession=_liveSessionRepository.GetAll();
            return View(livesession);
        }
    }
}
