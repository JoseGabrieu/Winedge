using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Winedge.Data;
using Winedge.Models;

namespace Winedge.Controllers
{
    [Authorize]
    public class UserDevicesController : Controller
    {
        private readonly AppDbContext _context;

        public UserDevicesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /UserDevices
        public async Task<IActionResult> Index()
        {
            var username = User.Identity?.Name;

            // Verifica se o usuário possui a role "admin"
            var isAdmin = User.IsInRole("admin");

            List<UserDevice> devices;

            if (isAdmin)
            {
                // Admin pode ver todos os dispositivos
                devices = await _context.UserDevices.ToListAsync();
            }
            else
            {
                // Usuário comum vê apenas os próprios dispositivos
                devices = await _context.UserDevices
                    .Where(d => d.User == username)
                    .ToListAsync();
            }

            ViewBag.IsAdmin = isAdmin; // opcional: pode usar na view se quiser

            return View(devices);
        }



        // GET: /UserDevices/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var userDevice = await _context.UserDevices.FirstOrDefaultAsync(m => m.Id == id);
            if (userDevice == null) return NotFound();

            return View(userDevice);
        }

        // GET: /UserDevices/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /UserDevices/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,User,Device")] UserDevice userDevice)
        {
            if (ModelState.IsValid)
            {
                _context.Add(userDevice);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(userDevice);
        }

        // GET: /UserDevices/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var userDevice = await _context.UserDevices.FindAsync(id);
            if (userDevice == null) return NotFound();

            return View(userDevice);
        }

        // POST: /UserDevices/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,User,Device")] UserDevice userDevice)
        {
            if (id != userDevice.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(userDevice);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserDeviceExists(userDevice.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(userDevice);
        }

        // GET: /UserDevices/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var userDevice = await _context.UserDevices
                .FirstOrDefaultAsync(m => m.Id == id);
            if (userDevice == null) return NotFound();

            return View(userDevice);
        }

        // POST: /UserDevices/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userDevice = await _context.UserDevices.FindAsync(id);
            if (userDevice != null)
            {
                _context.UserDevices.Remove(userDevice);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool UserDeviceExists(int id)
        {
            return _context.UserDevices.Any(e => e.Id == id);
        }
    }
}
