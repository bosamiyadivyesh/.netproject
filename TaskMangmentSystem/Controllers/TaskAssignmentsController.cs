
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Models;
using TaskManagement.Data;

public class TaskAssignmentsController : Controller
{
    private readonly AppDbContext _context;

    public TaskAssignmentsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: TASKASSIGNMENTS
    public async Task<IActionResult> Index()    
    {
        ViewData["UserId"] = await _context.Users.ToListAsync();
        ViewData["TaskId"] = await _context.Tasks.ToListAsync();
        return View(await _context.TaskAssignments.ToListAsync());
    }

    // GET: TASKASSIGNMENTS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var taskassignment = await _context.TaskAssignments
            .FirstOrDefaultAsync(m => m.Id == id);
        if (taskassignment == null)
        {
            return NotFound();
        }

        return View(taskassignment);
    }

    // GET: TASKASSIGNMENTS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: TASKASSIGNMENTS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaskAssignment taskassignment)
    {
        if (ModelState.IsValid)
        {
            _context.Add(taskassignment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(taskassignment);
    }

    // GET: TASKASSIGNMENTS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var taskassignment = await _context.TaskAssignments.FindAsync(id);
        if (taskassignment == null)
        {
            return NotFound();
        }
        return View(taskassignment);
    }

    // POST: TASKASSIGNMENTS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("Id,TaskId,Task,UserId,User,AssignedAt")] TaskAssignment taskassignment)
    {
        if (id != taskassignment.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(taskassignment);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TaskAssignmentExists(taskassignment.Id))
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
        return View(taskassignment);
    }

    // GET: TASKASSIGNMENTS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var taskassignment = await _context.TaskAssignments
            .FirstOrDefaultAsync(m => m.Id == id);
        if (taskassignment == null)
        {
            return NotFound();
        }

        return View(taskassignment);
    }

    // POST: TASKASSIGNMENTS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var taskassignment = await _context.TaskAssignments.FindAsync(id);
        if (taskassignment != null)
        {
            _context.TaskAssignments.Remove(taskassignment);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool TaskAssignmentExists(int? id)
    {
        return _context.TaskAssignments.Any(e => e.Id == id);
    }
}
