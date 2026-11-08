using BillMate.DTOs;
using BillMate.Interfaces;
using BillMate.Middleware;
using BillMate.Models;
using BillMate.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BillMate.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BillController : ControllerBase
    {
        private readonly IBillService _billService;
        private readonly ILogger<BillController> _logger;

        public BillController(IBillService billService, ILogger<BillController> logger)
        {
            _billService = billService;
            _logger = (ILogger<BillController>)logger;
        }

        [Route("CreateBill")]
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateBill([FromForm] CreateBillDTO billDTO)
        {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid CreateBillDTO attempt with invalid model state.");
                    return BadRequest(ModelState);
                }
                // Check whether the user is authorized 
                var curUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (curUserId == null)
                    return Unauthorized(new { message = "User is not authorized." });

                var result = await _billService.CreateBill(curUserId, billDTO);
                return Ok(result);
        }



        [Route("MyBills")]
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> MyBills()
        {
            // Check whether the user is authorized
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                _logger.LogWarning("Invalid userId attempt with invalid model state.");
                return Unauthorized("Invalid token.");
            }

            var result = await _billService.MyBills(userId);
            return Ok(result);            
        }


        [Route("Bills")]
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Bills()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                _logger.LogInformation($"Invalid userId attempt with invalid model state.{userId}" );
                return Unauthorized("Invalid token.");
            }
            var result = await _billService.Bills(userId);

            return Ok(result);
        }




        [Route("editBill")]
        [HttpPatch]
        [Authorize]
        public async Task<IActionResult> EditBill([FromBody] EditBillDTO editBillDTO)
        {


            if (editBillDTO == null)
                return Ok(new ResponseDTO { Message = $"Error when edit bill with id: {editBillDTO.BillId}"});
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if(userId == null)
                return Unauthorized(new ResponseDTO { Message = $"Error Unauthorized" });
            var isEdit = await _billService.EditBill(userId, editBillDTO);


            return Ok(isEdit);
        }



        [Route("payBill")]
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> PayBill([FromForm]PayBillDTO dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                _logger.LogInformation($"Invalid userId attempt with invalid model state.{userId}");
                return Unauthorized("Invalid token.");
            }
            var result = await _billService.PayBill(userId, dto);

            return Ok(result);
        }



        [Route("settlement")]
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SettlementBill([FromBody] SettlementDTO dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            return Unauthorized(new { message = "User is not authorized." });

            var result = await _billService.SettlementBill(userId, dto);
            return Ok(result);
        }



        [Route("deleteBill")]
        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> DeleteBill(int billId)
        {

                if (billId == 0)
                    return BadRequest("No billId in this requist");

                // Check whether the user is authorized 
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if(string.IsNullOrEmpty(currentUserId))
                    return Unauthorized(new { message = "User is not authorized." });

                var deleteBill = await _billService.DeleteBill(billId, currentUserId);
                if (deleteBill == null)
                    return BadRequest("Error when delete bill");
                return Ok(deleteBill);
        }



        [Route("archived")]
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ArchivedBills()
        {
            // Check whether the user is authorized
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                _logger.LogWarning("Invalid userId attempt with invalid model state.");
                return Unauthorized("Invalid token.");
            }

            var result = await _billService.ArchivedBills(userId);
            return Ok(result);
        }


    }
}
