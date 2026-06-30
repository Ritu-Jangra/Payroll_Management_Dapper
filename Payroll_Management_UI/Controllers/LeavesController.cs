namespace Payroll_Management_UI.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using System.Net;
    using System.Net.Http.Json;
    using System.Threading;
    using static ApiErrorMessageHelper;

    public class LeavesController : Controller
    {
        private readonly HttpClient _httpClient;

        public LeavesController(IHttpClientFactory factory)
        {
            _httpClient = factory.CreateClient("ApiClient");
        }

        [HttpGet]
        public async Task<IActionResult> MyLeaves(CancellationToken cancellationToken)
        {
            try
            {
                List<LeaveViewModel>? leaves = await GetMyLeavesAsync(cancellationToken);

                if (leaves == null)
                    return View();

                return View(leaves);
            }
            catch
            {
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> LeaveRequests(CancellationToken cancellationToken)
        {
            try
            {
                List<LeaveViewModel>? leaves = await GetTeamsLeavesAsync(cancellationToken);

                if (leaves == null)
                    return View();

                return View(leaves);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return View();
            }
        }

        [HttpGet]
        public ActionResult ApplyLeave()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyLeave(ApplyLeaveViewModel applyLeaveViewModel, CancellationToken cancellationToken)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(applyLeaveViewModel);

                HttpResponseMessage response = await _httpClient.PostAsJsonAsync("v1/Leaves", applyLeaveViewModel, cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    TempData["Error"] = "Failed to apply leave.";
                    HttpContext.Session.Clear();
                    return View();
                }

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = await GetApiErrorMessage(response, cancellationToken);
                    return View();
                }

                TempData["Success"] = "Leave applied successfully.";
                return RedirectToAction(nameof(MyLeaves));
            }
            catch
            {
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> UpdateLeave(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                List<LeaveViewModel>? leaves = await GetMyLeavesAsync(cancellationToken);

                if (leaves == null)
                    return View();

                LeaveViewModel? leaveView = leaves?.FirstOrDefault(x => x.LeaveId == id);

                return View(leaveView);
            }
            catch
            {
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdateLeave(Guid id, LeaveViewModel leaveViewModel, CancellationToken cancellationToken)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(leaveViewModel);

                HttpResponseMessage response = await _httpClient.PutAsJsonAsync($"v1/Leaves/{id}", leaveViewModel, cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    TempData["Error"] = "Failed to update leave.";
                    HttpContext.Session.Clear();
                    return View();
                }

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = await GetApiErrorMessage(response, cancellationToken);
                    return View();
                }

                TempData["Success"] = "Leave updated successfully!";
                return RedirectToAction(nameof(MyLeaves));
            }
            catch
            {
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> DeleteLeave(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                List<LeaveViewModel>? leaves = await GetMyLeavesAsync(cancellationToken);

                if (leaves == null)
                    return View();

                LeaveViewModel? leaveView = leaves?.FirstOrDefault(x => x.LeaveId == id);

                return View(leaveView);
            }
            catch
            {
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName(nameof(DeleteLeave))]
        public async Task<IActionResult> DeleteLeaveAsync(Guid id, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return View();

            HttpResponseMessage response = await _httpClient.DeleteAsync($"v1/Leaves/{id}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                TempData["Error"] = "Failed to delete leave.";
                HttpContext.Session.Clear();
                return View();
            }

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = await GetApiErrorMessage(response, cancellationToken);
                return View();
            }

            TempData["Success"] = "Leave deleted successfully!";
            return RedirectToAction(nameof(MyLeaves));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ApproveLeave(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                List<LeaveViewModel>? leaves = await GetTeamsLeavesAsync(cancellationToken);

                if (leaves == null)
                    return View();

                LeaveViewModel? leaveView = leaves?.FirstOrDefault(x => x.LeaveId == id);

                HttpResponseMessage response = await _httpClient.PutAsJsonAsync($"v2/Leaves/{id}/approve", leaveView, cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    TempData["Error"] = "Failed to approve leave.";
                    HttpContext.Session.Clear();
                    return View();
                }

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = await GetApiErrorMessage(response, cancellationToken);
                    return View();
                }

                TempData["Success"] = "Leave Approved successfully!";
                return RedirectToAction(nameof(LeaveRequests));
            }
            catch
            {
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RejectLeave(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                List<LeaveViewModel>? leaves = await GetTeamsLeavesAsync(cancellationToken);

                if (leaves == null)
                    return View();

                LeaveViewModel? leaveView = leaves?.FirstOrDefault(x => x.LeaveId == id);

                HttpResponseMessage response = await _httpClient.PutAsJsonAsync($"v2/Leaves/{id}/reject", leaveView, cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    TempData["Error"] = "Failed to reject leave.";
                    HttpContext.Session.Clear();
                    return View();
                }

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = await GetApiErrorMessage(response, cancellationToken);
                    return View();
                }

                TempData["Success"] = "Leave Rejected successfully!";
                return RedirectToAction(nameof(LeaveRequests));
            }
            catch
            {
                return View();
            }
        }

        #region Private Helper

        private async Task<List<LeaveViewModel>?> GetMyLeavesAsync(CancellationToken ct)
        {
            HttpResponseMessage response = await _httpClient.GetAsync("v1/Leaves/my", ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                TempData["Error"] = "Failed to get your applied leave.";
                HttpContext.Session.Clear();
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = await GetApiErrorMessage(response, ct);
                return null;
            }

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<LeaveViewModel>>(ct);
        }

        private async Task<List<LeaveViewModel>?> GetTeamsLeavesAsync(CancellationToken ct)
        {
            HttpResponseMessage response = await _httpClient.GetAsync("v2/Leaves/team", ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                TempData["Error"] = "Failed to get teams leave requests.";
                HttpContext.Session.Clear();
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = await GetApiErrorMessage(response, ct);
                return null;
            }

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<LeaveViewModel>>(ct);
        }

        #endregion Private Helper
    }
}
