﻿using IoTSharp.Data;
using IoTSharp.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTSharp.Controllers
{
   /// <summary>
   /// 客户管理
   /// </summary>
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CustomersController(ApplicationDbContext context)
        {
            _context = context;
        }
        /// <summary>
        /// 获取指定租户下的所有客户
        /// </summary>
        /// <param name="tenantId">租户</param>
        /// <returns></returns>
        [HttpGet("Tenant/{tenantId}")]
        [Authorize(Roles = nameof(UserRole.NormalUser))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResult), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers(Guid tenantId)
        {
            var f = from c in _context.Customer where c.Tenant.Id == tenantId select c;
            if (!f.Any())
            {
                return NotFound(new ApiResult(ApiCode.NotFoundCustomer, "This tenant does not have any customers"));
            }
            else
            {
                return await f.ToArrayAsync();
            }
        }

       /// <summary>
       /// 返回指定id的客户
       /// </summary>
       /// <param name="id"></param>
       /// <returns></returns>
        [Authorize(Roles = nameof(UserRole.NormalUser))]
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResult), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult<Customer>> GetCustomer(Guid id)
        {
            var customer = await _context.Customer.FindAsync(id);

            if (customer == null)
            {
                return NotFound(new ApiResult(ApiCode.NotFoundCustomer, "This customer was not found"));
            }

            return customer;
        }

     /// <summary>
     /// 修改指定租户为 指定的信息
     /// </summary>
     /// <param name="id"></param>
     /// <param name="customer"></param>
     /// <returns></returns>
        [Authorize(Roles = nameof(UserRole.CustomerAdmin))]
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResult), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> PutCustomer(Guid id, CustomerDto customer)
        {
            if (id != customer.Id)
            {
                return BadRequest();
            }
            customer.Tenant = _context.Tenant.Find(customer.TenantID);
            _context.Entry(customer).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CustomerExists(id))
                {
                    return NotFound(new ApiResult(ApiCode.NotFoundCustomer, "This customer was not found"));
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

      /// <summary>
      /// 为当前客户所在的租户新增客户
      /// </summary>
      /// <param name="customer"></param>
      /// <returns></returns>
        [Authorize(Roles = nameof(UserRole.CustomerAdmin))]
        [HttpPost]
        public async Task<ActionResult<Customer>> PostCustomer(CustomerDto customer)
        {
            if (customer.TenantID != Guid.Empty && (User.IsInRole(nameof(UserRole.SystemAdmin)) || User.IsInRole(nameof(UserRole.TenantAdmin))))
            {
                var tent = _context.Tenant.Find(customer.TenantID);
                customer.Tenant = tent;
            }
            else
            {
                var tid = User.Claims.First(c => c.Type == IoTSharpClaimTypes.Tenant);
                var tidguid = new Guid(tid.Value);
                var tent = _context.Tenant.Find(tidguid);
                customer.Tenant = tent;
            }
            _context.Customer.Add(customer);
            await _context.SaveChangesAsync();
            return await GetCustomer(customer.Id);
        }
        /// <summary>
        /// 删除指定的客户ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // DELETE: api/Customers/5
        [Authorize(Roles = nameof(UserRole.TenantAdmin))]
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResult), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult<Customer>> DeleteCustomer(Guid id)
        {
            var customer = await _context.Customer.FindAsync(id);
            if (customer == null)
            {
                return NotFound(new ApiResult(ApiCode.NotFoundCustomer, "This customer was not found"));
            }

            _context.Customer.Remove(customer);
            await _context.SaveChangesAsync();

            return customer;
        }

        private bool CustomerExists(Guid id)
        {
            return _context.Customer.Any(e => e.Id == id);
        }

        public class TenantDto
        {
            public Guid Id { get; set; }
        }
    }
}