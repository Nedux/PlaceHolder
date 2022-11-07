﻿using Azure.Messaging.ServiceBus;
using GP3.Common.Constants;
using GP3.Common.Entities;
using GP3.Funcs.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using System;
using System.Net;
using System.Threading.Tasks;

namespace GP3.Funcs.Functions.HTTP
{
    public class AdminHttp
    {
        private readonly ReqAuthService _reqAuthService;

        public AdminHttp(ReqAuthService reqAuthService)
        {
            _reqAuthService = reqAuthService;
        }


        [FunctionName("AdminHttpSendNotif")]
        [OpenApiOperation(operationId: "Send integration notification")]
        [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
        [OpenApiParameter(name: "reason", In = ParameterLocation.Query, Required = true, Type = typeof(IntegrationCallbackReason), Description = "Callback reason")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "plain/text", bodyType: typeof(string), Description = "Callback OK")]
        public async Task<IActionResult> AddIntegration([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = Routes.Admin)] HttpRequest req,
            [ServiceBus(ConnStrings.IntegrationQ, Connection = ConnStrings.IntegrationQConn)] ServiceBusSender sender) =>
            await _reqAuthService.HandledAuthAction(req, async (req, principal) =>
            {
                if (!_reqAuthService.IsAdmin(principal))
                {
                    return new UnauthorizedObjectResult("User is not an admin!");
                }

                if (!Enum.TryParse<IntegrationCallbackReason>(req.Query["reason"], out var reason))
                {
                    return new BadRequestObjectResult("Invalid reason");
                }

                await sender.SendMessageAsync(new ServiceBusMessage(reason.ToString()));
                return new OkObjectResult($"Sent {reason} event");
            });
    }
}
