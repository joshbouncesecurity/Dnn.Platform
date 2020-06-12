﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information

using System.Web.Mvc;

namespace DotNetNuke.Tests.Web.Mvc.Fakes.Filters
{
    public class FakeHandleExceptionRedirectAttribute : FakeRedirectAttribute, IExceptionFilter
    {
        public static bool IsExceptionHandled { get; set; }
        public void OnException(ExceptionContext filterContext)
        {
            filterContext.Result = Result;
            filterContext.ExceptionHandled = IsExceptionHandled;
        }
    }
}
