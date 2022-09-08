﻿using EMarket.BLL.Interfaces.Customer;

using EMarket.Entities.Customer;
using EMarketDTO.Customer;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EMarket.Controllers.Customer
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class Customer_ProfileController : ControllerBase
    {
        ICustomer_Profile_Service _inter;
        private readonly IAntiforgery _antiForgery;
        public Customer_ProfileController(ICustomer_Profile_Service inter, IAntiforgery antiForgery)
        {
            _inter = inter;
            _antiForgery = antiForgery;
        }
        [HttpGet]
        [Route("Get_Customer_Details/{id:int}")]
      
        public Customer_ProfileDTO Get_Customer_Details([FromHeader(Name = "userid")] string userid, int id)
        {
            Customer_ProfileDTO dto = new Customer_ProfileDTO();
            dto.language_id = id;
            dto.user_id = Convert.ToInt64(userid);
            dto.ipAddress = HttpContext.Connection.RemoteIpAddress.ToString();
            return _inter.Get_Customer_Details(dto);
        }


        [Route("Upadate_Customer_Details")]
        [HttpPost]

        public Customer_ProfileDTO Upadate_Customer_Details([FromHeader(Name = "userid")] string userid, [FromBody] Customer_ProfileDTO dto)

        {
            dto.ipAddress = HttpContext.Connection.RemoteIpAddress.ToString();
            dto.user_id = Convert.ToInt64(userid);
            return _inter.Upadate_Customer_Details(dto);
        }

        [Route("getstate")]
        public Customer_ProfileDTO getstate([FromHeader(Name = "userid")] string userid, [FromBody] Customer_ProfileDTO dto)
        {
            dto.ipAddress = HttpContext.Connection.RemoteIpAddress.ToString();
            dto.user_id = Convert.ToInt64(userid);
            return _inter.getstate(dto);
        }
    }
}
