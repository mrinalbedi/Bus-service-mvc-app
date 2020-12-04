using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BusService.Models;
using Microsoft.AspNetCore.Http;

namespace BusService.Controllers
{
    public class RouteStopController : Controller
    {
        private readonly BusServiceContext _context;

        public RouteStopController(BusServiceContext context)
        {
            _context = context;
        }

        // GET: RouteStop
        public async Task<IActionResult> Index(string BusRouteCode)
        {
            if(!string.IsNullOrEmpty(BusRouteCode))
            {
                //savve in cookie or session variable..
                Response.Cookies.Append("BusRouteCode", BusRouteCode);
                HttpContext.Session.SetString("BusRouteCode", BusRouteCode);
            }
            else if(Request.Query["BusRouteCode"].Any())
            {
                //store in cookies or session variable..
                Response.Cookies.Append("BusRouteCode", Request.Query["BusRouteCode"].ToString());
                HttpContext.Session.SetString("BusRouteCode", Request.Query["BusRouteCode"].ToString());
                BusRouteCode = Request.Query["BusRouteCode"].ToString();
            }
            else if(Request.Cookies["BusRouteCode"]!=null)
            {
                BusRouteCode = Request.Cookies["BusRouteCode"].ToString();
            }
            else if(HttpContext.Session.GetString("BusRouteCode")!=null)
            {
                BusRouteCode = HttpContext.Session.GetString("BusRouteCode");
            }
            else
            {
                TempData["message"] = "Please select a route";
                return RedirectToAction("Index", "BusRoute");
            }

            //fetching name from the selected routecode 
            var busRoute = _context.BusRoute.Where(b => b.BusRouteCode == BusRouteCode).FirstOrDefault();
            ViewBag.bRouteCode = BusRouteCode;
            ViewBag.bRouteName = busRoute.RouteName;

            var busServiceContext = _context.RouteStop.Include(r => r.BusRouteCodeNavigation).Include(r => r.BusStopNumberNavigation)
                .Where(m=>m.BusRouteCode==BusRouteCode)
                .OrderBy(o=>o.OffsetMinutes);
            return View(await busServiceContext.ToListAsync());
        }

        // GET: RouteStop/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var routeStop = await _context.RouteStop
                .Include(r => r.BusRouteCodeNavigation)
                .Include(r => r.BusStopNumberNavigation)
                .FirstOrDefaultAsync(m => m.RouteStopId == id);
            if (routeStop == null)
            {
                return NotFound();
            }

            return View(routeStop);
        }

        // GET: RouteStop/Create
        public IActionResult Create()
        {
            string BusRCode = string.Empty;
            if(Request.Cookies["BusRouteCode"]!=null)
            {
                BusRCode = Request.Cookies["BusRouteCode"].ToString();
            }
            else if(HttpContext.Session.GetString("BusRouteCode")!=null)
            {
                BusRCode = HttpContext.Session.GetString("BusRouteCode");
            }
            //fetching name from the selected routecode 
            var busRoute = _context.BusRoute.Where(b => b.BusRouteCode == BusRCode).FirstOrDefault();
            ViewBag.bRouteCode = BusRCode;
            ViewBag.bRouteName = busRoute.RouteName;

            ViewData["BusRouteCode"] = new SelectList(_context.BusRoute, "BusRouteCode", "BusRouteCode");
            ViewData["BusStopNumber"] = new SelectList(_context.BusStop.OrderBy(l=>l.Location), "BusStopNumber", "Location");
            return View();
        }

        // POST: RouteStop/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RouteStopId,BusRouteCode,BusStopNumber,OffsetMinutes")] RouteStop routeStop)
        {
            //Creating a local variable and checking its values is stored in cookies or session variable.
            string BusRCode = string.Empty;
            if (Request.Cookies["BusRouteCode"] != null)
            {
                BusRCode = Request.Cookies["BusRouteCode"].ToString();
            }
            else if (HttpContext.Session.GetString("BusRouteCode") != null)
            {
                BusRCode = HttpContext.Session.GetString("BusRouteCode");
            }
            //fetching name from the selected routecode 
            var busRoute = _context.BusRoute.Where(b => b.BusRouteCode == BusRCode).FirstOrDefault();
            ViewBag.bRouteCode = BusRCode;
            ViewBag.bRouteName = busRoute.RouteName;

            routeStop.BusRouteCode = BusRCode;

            //Validations
            if(routeStop.OffsetMinutes<0)
            {
                ModelState.AddModelError("", "Offeset Minutes cannot be less than zero");
            }
            else
            {
                var allRecords = _context.RouteStop.Where(a => a.OffsetMinutes == 0 && a.BusRouteCode == routeStop.BusRouteCode);
                if(!allRecords.Any())
                {
                    ModelState.AddModelError("","There should be offset minutes with 0");
                }
            }
            if(routeStop.OffsetMinutes==0)
            {
                var IsZeroExist = _context.RouteStop.Where(a => a.OffsetMinutes == 0 && a.BusRouteCode == routeStop.BusRouteCode);
                if(IsZeroExist.Any())
                {
                    ModelState.AddModelError("", "There is already a record with offset minutes as 0 in the database");
                }
            }
            var isDuplicate = _context.RouteStop.Where(a => a.BusRouteCode == routeStop.BusRouteCode && a.BusStopNumber == routeStop.BusStopNumber);
            if(isDuplicate.Any())
            {
                ModelState.AddModelError("", "Please add unique combination of bus route and bus stop number");
            }
            if (ModelState.IsValid)
            {
                _context.Add(routeStop);
                await _context.SaveChangesAsync();
                TempData["message"] = "New route stop added successfully";
                return RedirectToAction(nameof(Index));
            }

            ViewData["BusRouteCode"] = new SelectList(_context.BusRoute, "BusRouteCode", "BusRouteCode", routeStop.BusRouteCode);
            ViewData["BusStopNumber"] = new SelectList(_context.BusStop.OrderBy(l=>l.Location), "BusStopNumber", "Location", routeStop.BusStopNumber);
            return View(routeStop);
        }

        // GET: RouteStop/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var routeStop = await _context.RouteStop.FindAsync(id);
            if (routeStop == null)
            {
                return NotFound();
            }
            ViewData["BusRouteCode"] = new SelectList(_context.BusRoute, "BusRouteCode", "BusRouteCode", routeStop.BusRouteCode);
            ViewData["BusStopNumber"] = new SelectList(_context.BusStop, "BusStopNumber", "BusStopNumber", routeStop.BusStopNumber);
            return View(routeStop);
        }

        // POST: RouteStop/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RouteStopId,BusRouteCode,BusStopNumber,OffsetMinutes")] RouteStop routeStop)
        {
            if (id != routeStop.RouteStopId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(routeStop);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RouteStopExists(routeStop.RouteStopId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["BusRouteCode"] = new SelectList(_context.BusRoute, "BusRouteCode", "BusRouteCode", routeStop.BusRouteCode);
            ViewData["BusStopNumber"] = new SelectList(_context.BusStop, "BusStopNumber", "BusStopNumber", routeStop.BusStopNumber);
            return View(routeStop);
        }

        // GET: RouteStop/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var routeStop = await _context.RouteStop
                .Include(r => r.BusRouteCodeNavigation)
                .Include(r => r.BusStopNumberNavigation)
                .FirstOrDefaultAsync(m => m.RouteStopId == id);
            if (routeStop == null)
            {
                return NotFound();
            }

            return View(routeStop);
        }

        // POST: RouteStop/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var routeStop = await _context.RouteStop.FindAsync(id);
            _context.RouteStop.Remove(routeStop);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RouteStopExists(int id)
        {
            return _context.RouteStop.Any(e => e.RouteStopId == id);
        }
    }
}
