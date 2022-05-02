﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;

namespace Core.Api.Model
{
    public class VersionErrorProvider : IErrorResponseProvider
    {
        public IActionResult CreateResponse(ErrorResponseContext context)
        {

            ResponseObject<object> responseObject = new ResponseObject<object>
            {
                Status = new ResponseStatus
                {
                    Code = $"{context.StatusCode}",
                    Message = $"{context.ErrorCode} - {context.Message}"
                }
            };

            return new BadRequestObjectResult(responseObject);
        }
    }

}
