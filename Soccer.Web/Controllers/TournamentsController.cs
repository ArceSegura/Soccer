using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soccer.Web.Data;
using Soccer.Web.Data.Entities;
using Soccer.Web.Helpers;
using Soccer.Web.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Soccer.Web.Controllers
{
    public class TournamentsController : Controller
    {
        private readonly DataContext _context;
        private readonly IImageHelper _imageHelper;
        private readonly IConverterHelper _converterHelper;
        private readonly ICombosHelper _combosHelper;

        public TournamentsController(
            DataContext context,
            IImageHelper imageHelper,
            IConverterHelper converterHelper,
            ICombosHelper combosHelper)
        {
            _context = context;
            _imageHelper = imageHelper;
            _converterHelper = converterHelper;
            _combosHelper = combosHelper;
        }
        public async Task<IActionResult> Index()
        {
            return View(await _context.Tournaments.Include(t => t.Groups).OrderBy(t => t.StartDate).ToListAsync());

        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TournamentViewModel tournamentViewModel)
        {
            if (ModelState.IsValid)
            {
                string path = string.Empty;

                if (tournamentViewModel.LogoFile != null)
                {
                    path = await _imageHelper.UploadImageAsync(tournamentViewModel.LogoFile, "Tournaments");
                }

                TournamentEntity tournamentEntity = _converterHelper.ToTournamentEntity(tournamentViewModel, path, true);
                _context.Add(tournamentEntity);

                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    if (ex.InnerException.Message.Contains("duplicate"))
                    {
                        ModelState.AddModelError(string.Empty, $"Already exists the team {tournamentEntity.Name}.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, ex.InnerException.Message);
                    }
                }
            }

            return View(tournamentViewModel);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            TournamentEntity tournamentEntity = await _context.Tournaments
                .Include(t => t.Groups)
                .ThenInclude(t => t.Matches)
                .ThenInclude(t => t.Local)
                .Include(t => t.Groups)
                .ThenInclude(t => t.Matches)
                .ThenInclude(t => t.Visitor)
                .Include(t => t.Groups)
                .ThenInclude(t => t.GroupDetails)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tournamentEntity == null)
            {
                return NotFound();
            }

            return View(tournamentEntity);
        }


        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            TournamentEntity tournamentEntity = await _context.Tournaments.FindAsync(id);
            if (tournamentEntity == null)
            {
                return NotFound();
            }

            TournamentViewModel tournamentViewModel = _converterHelper.ToTournamentViewModel(tournamentEntity);
            return View(tournamentViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TournamentViewModel tournamentViewModel)
        {
            if (ModelState.IsValid)
            {
                string path = tournamentViewModel.LogoPath;

                if (tournamentViewModel.LogoFile != null)
                {
                    path = await _imageHelper.UploadImageAsync(tournamentViewModel.LogoFile, "Tournaments");
                }

                TournamentEntity tournamentEntity = _converterHelper.ToTournamentEntity(tournamentViewModel, path, false);
                _context.Update(tournamentEntity);

                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    if (ex.InnerException.Message.Contains("duplicate"))
                    {
                        ModelState.AddModelError(string.Empty, $"Already exists the team {tournamentEntity.Name}.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, ex.InnerException.Message);
                    }
                }
            }

            return View(tournamentViewModel);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            TournamentEntity tournamentEntity = await _context.Tournaments
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tournamentEntity == null)
            {
                return NotFound();
            }

            _context.Tournaments.Remove(tournamentEntity);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> AddGroup(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            TournamentEntity tournamentEntity = await _context.Tournaments.FindAsync(id);
            if (tournamentEntity == null)
            {
                return NotFound();
            }

            GroupViewModel groupViewModel = new GroupViewModel
            {
                Tournament = tournamentEntity,
                TournamentId = tournamentEntity.Id
            };

            return View(groupViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddGroup(GroupViewModel groupViewModel)
        {
            if (ModelState.IsValid)
            {
                GroupEntity groupEntity = await _converterHelper.ToGroupEntityAsync(groupViewModel, true);
                _context.Add(groupEntity);
                await _context.SaveChangesAsync();
                return RedirectToAction($"{nameof(Details)}/{groupViewModel.TournamentId}");
            }

            return View(groupViewModel);
        }

        public async Task<IActionResult> EditGroup(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            GroupEntity groupEntity = await _context.Groups
                .Include(g => g.Tournament)
                .FirstOrDefaultAsync(g => g.Id == id);
            if (groupEntity == null)
            {
                return NotFound();
            }

            GroupViewModel groupViewModel = _converterHelper.ToGroupViewModel(groupEntity);
            return View(groupViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGroup(GroupViewModel groupViewModel)
        {
            if (ModelState.IsValid)
            {
                GroupEntity groupEntity = await _converterHelper.ToGroupEntityAsync(groupViewModel, false);
                _context.Update(groupEntity);
                await _context.SaveChangesAsync();
                return RedirectToAction($"{nameof(Details)}/{groupViewModel.TournamentId}");
            }

            return View(groupViewModel);
        }

        public async Task<IActionResult> DeleteGroup(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            GroupEntity groupEntity = await _context.Groups
                .Include(g => g.Tournament)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (groupEntity == null)
            {
                return NotFound();
            }

            _context.Groups.Remove(groupEntity);
            await _context.SaveChangesAsync();
            return RedirectToAction($"{nameof(Details)}/{groupEntity.Tournament.Id}");
        }

        public async Task<IActionResult> DetailsGroup(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            GroupEntity groupEntity = await _context.Groups
                .Include(g => g.Matches)
                .ThenInclude(g => g.Local)
                .Include(g => g.Matches)
                .ThenInclude(g => g.Visitor)
                .Include(g => g.Tournament)
                .Include(g => g.GroupDetails)
                .ThenInclude(gd => gd.Team)
                .FirstOrDefaultAsync(g => g.Id == id);
            if (groupEntity == null)
            {
                return NotFound();
            }

            return View(groupEntity);
        }

        public async Task<IActionResult> AddGroupDetail(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            GroupEntity groupEntity = await _context.Groups.FindAsync(id);
            if (groupEntity == null)
            {
                return NotFound();
            }

            GroupDetailViewModel groupDetailViewModel = new GroupDetailViewModel
            {
                Group = groupEntity,
                GroupId = groupEntity.Id,
                Teams = _combosHelper.GetComboTeams()
            };

            return View(groupDetailViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddGroupDetail(GroupDetailViewModel groupDetailViewModel)
        {
            if (ModelState.IsValid)
            {
                GroupDetailEntity groupDetailEntity = await _converterHelper.ToGroupDetailEntityAsync(groupDetailViewModel, true);
                _context.Add(groupDetailEntity);
                await _context.SaveChangesAsync();
                return RedirectToAction($"{nameof(DetailsGroup)}/{groupDetailViewModel.GroupId}");
            }

            groupDetailViewModel.Group = await _context.Groups.FindAsync(groupDetailViewModel.GroupId);
            groupDetailViewModel.Teams = _combosHelper.GetComboTeams();
            return View(groupDetailViewModel);
        }

        public async Task<IActionResult> AddMatch(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var groupEntity = await _context.Groups.FindAsync(id);
            if (groupEntity == null)
            {
                return NotFound();
            }

            var model = new MatchViewModel
            {
                Date = DateTime.Today,
                Group = groupEntity,
                GroupId = groupEntity.Id,
                Teams = _combosHelper.GetComboTeams(groupEntity.Id)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMatch(MatchViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.LocalId != model.VisitorId)
                {
                    var matchEntity = await _converterHelper.ToMatchEntityAsync(model, true);
                    _context.Add(matchEntity);
                    await _context.SaveChangesAsync();
                    return RedirectToAction($"{nameof(DetailsGroup)}/{model.GroupId}");
                }

                ModelState.AddModelError(string.Empty, "The local and visitor must be differents teams.");
            }

            model.Group = await _context.Groups.FindAsync(model.GroupId);
            model.Teams = _combosHelper.GetComboTeams(model.GroupId);
            return View(model);
        }

        public async Task<IActionResult> EditGroupDetail(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var groupDetailEntity = await _context.GroupDetails
                .Include(gd => gd.Group)
                .Include(gd => gd.Team)
                .FirstOrDefaultAsync(gd => gd.Id == id);
            if (groupDetailEntity == null)
            {
                return NotFound();
            }

            var model = _converterHelper.ToGroupDetailViewModel(groupDetailEntity);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGroupDetail(GroupDetailViewModel model)
        {
            if (ModelState.IsValid)
            {
                var groupDetailEntity = await _converterHelper.ToGroupDetailEntityAsync(model, false);
                _context.Update(groupDetailEntity);
                await _context.SaveChangesAsync();
                return RedirectToAction($"{nameof(DetailsGroup)}/{model.GroupId}");
            }

            return View(model);
        }
        public async Task<IActionResult> EditMatch(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var matchEntity = await _context.Matches
                .Include(m => m.Group)
                .Include(m => m.Local)
                .Include(m => m.Visitor)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (matchEntity == null)
            {
                return NotFound();
            }

            var model = _converterHelper.ToMatchViewModel(matchEntity);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMatch(MatchViewModel model)
        {
            if (ModelState.IsValid)
            {
                var matchEntity = await _converterHelper.ToMatchEntityAsync(model, false);
                _context.Update(matchEntity);
                await _context.SaveChangesAsync();
                return RedirectToAction($"{nameof(DetailsGroup)}/{model.GroupId}");
            }

            return View(model);
        }

        public async Task<IActionResult> DeleteGroupDetail(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var groupDetailEntity = await _context.GroupDetails
                .Include(gd => gd.Group)
                .FirstOrDefaultAsync(gd => gd.Id == id);
            if (groupDetailEntity == null)
            {
                return NotFound();
            }

            _context.GroupDetails.Remove(groupDetailEntity);
            await _context.SaveChangesAsync();
            return RedirectToAction($"{nameof(DetailsGroup)}/{groupDetailEntity.Group.Id}");
        }

        public async Task<IActionResult> DeleteMatch(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var matchEntity = await _context.Matches
                .Include(m => m.Group)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (matchEntity == null)
            {
                return NotFound();
            }

            _context.Matches.Remove(matchEntity);
            await _context.SaveChangesAsync();
            return RedirectToAction($"{nameof(DetailsGroup)}/{matchEntity.Group.Id}");
        }


    }
}
