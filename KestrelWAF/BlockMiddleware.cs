﻿using MicroRuleEngine;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KestrelWAF
{
    public class BlockMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<BlockMiddleware> _logger;
        private readonly MaxMindDb _geo;
        private readonly Func<WebRequest, bool> _compiledRule;

        public BlockMiddleware(RequestDelegate next, ILogger<BlockMiddleware> logger, MaxMindDb geo, IOptions<Rule> ruleset)
        {
            _next = next;
            _logger = logger;
            _geo = geo;
            _compiledRule = new MRE().CompileRule<WebRequest>(ruleset.Value);
        }

        public async Task Invoke(HttpContext context)
        {
            var wr = new WebRequest(context.Request, _geo);

            if (_compiledRule(wr))
            {
                _logger.LogWarning($"Forbidden request from {wr.RemoteIp}");
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            await _next.Invoke(context);
        }
    }
}
