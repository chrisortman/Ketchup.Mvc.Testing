using System;
using System.Collections.Specialized;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Ketchup.Web.Routing;
using NSubstitute;
using Shouldly;

namespace Ketchup.Mvc.Testing
{
    /// <summary>
    /// Used to simplify testing routes.
    /// </summary>
    public static class RouteTestingExtensions
    {
        /// <summary>
        /// Returns the corresponding route for the URL.  Returns null if no route was found.
        /// </summary>
        /// <param name="url">The app relative url to test.</param>
        /// <returns>A matching <see cref="RouteData" />, or null.</returns>
        public static RouteData Route(this string url)
        {
            var context = FakeHttpContext(url);
            return RouteTable.Routes.GetRouteData(context);
        }

        public static RouteData Post(this object testClass, string url)
        {
            var context = FakeHttpContext(url, httpMethod: "POST");
            return RouteTable.Routes.GetRouteData(context);
        }

        /// <summary>
        /// Asserts that the route matches the expression specified.  Checks controller, action, and any method arguments
        /// into the action as route values.
        /// </summary>
        /// <typeparam name="TController">The controller.</typeparam>
        /// <param name="routeData">The routeData to check</param>
        /// <param name="action">The action to call on TController.</param>
        /// <param name="skipParameterChecking"></param>
        public static RouteData ShouldMapTo<TController>(this RouteData routeData, Expression<Func<TController, ActionResult>> paction, bool skipParameterChecking = false)
            where TController : Controller
        {
            //routeData.ShouldNotBeNull("The URL did not match any route");
            //Assert.IsNotNull(routeData, "The URL did not match any route");
            routeData.ShouldNotBe(null);

            //check controller
            routeData.ShouldMapTo<TController>();

            //check action
            var methodCall = (MethodCallExpression)paction.Body;
            string actualAction = routeData.Values.GetValue("action").ToString();
            string expectedAction = methodCall.Method.Name;
            //actualAction.AssertSameStringAs(expectedAction);
            //Assert.AreEqual(expectedAction, actualAction,true);
            string action = actualAction.ToLower();
            action.ShouldBe(expectedAction.ToLower());
            //check parameters
            if(skipParameterChecking == false)
            {
                for(int i = 0; i < methodCall.Arguments.Count; i++)
                {
                    string name = methodCall.Method.GetParameters()[i].Name;
                    object value = null;

                    switch(methodCall.Arguments[i].NodeType)
                    {
                        case ExpressionType.Constant:
                            value = ((ConstantExpression)methodCall.Arguments[i]).Value;
                            break;

                        case ExpressionType.MemberAccess:
                            value = Expression.Lambda(methodCall.Arguments[i]).Compile().DynamicInvoke();
                            break;

                        case ExpressionType.Convert:
                            var unaryExpression = (UnaryExpression)methodCall.Arguments[i];
                            switch(unaryExpression.Operand.NodeType)
                            {
                                case ExpressionType.Constant:
                                    value = ((ConstantExpression)unaryExpression.Operand).Value;
                                    break;
                                default:
                                    value = null;
                                    break;
                            }
                            break;
                    }

                    value = (value == null ? value : value.ToString());
                    //routeData.Values.GetValue(name).ShouldEqual(value,"Value for parameter did not match");
                    // Assert.AreEqual(value, routeData.Values.GetValue(name), "Value for parameter did not match");
                    routeData.Values.GetValue(name).ShouldBe(value);
                }
            }

            return routeData;
        }

        /// <summary>
        /// Converts the URL to matching RouteData and verifies that it will match a route with the values specified by the expression.
        /// </summary>
        /// <typeparam name="TController">The type of controller</typeparam>
        /// <param name="relativeUrl">The ~/ based url</param>
        /// <param name="action">The expression that defines what action gets called (and with which parameters)</param>
        /// <returns></returns>
        public static RouteData ShouldMapTo<TController>(this string relativeUrl,
                                                         Expression<Func<TController, ActionResult>> action,
                                                            bool skipParameterChecking = false)
            where TController : Controller
        {
            var routeData = relativeUrl.Route();
            if(routeData == null)
            {
                throw new Exception(String.Format("Url {0} did not match any routes", relativeUrl));                
            }
            return routeData.ShouldMapTo(action, skipParameterChecking);
        }

        //public static RouteData ShouldMapToWebFormsPage(this string relativeUrl, string webFormsVirtualPath = null)
        //{
        //    var route = relativeUrl.Route();

        //    if(webFormsVirtualPath == null)
        //    {
        //        webFormsVirtualPath = relativeUrl;
        //    }

        //    Assert.IsInstanceOfType(route.RouteHandler, typeof(PageRouteHandler));
        //    Assert.AreEqual(webFormsVirtualPath,
        //                    ((PageRouteHandler)route.RouteHandler).VirtualPath, true);
        //    return route;
        //}

        /// <summary>
        /// Verifies the <see cref="RouteData">routeData</see> maps to the controller type specified.
        /// </summary>
        /// <typeparam name="TController"></typeparam>
        /// <param name="routeData"></param>
        /// <returns></returns>
        public static RouteData ShouldMapTo<TController>(this RouteData routeData) where TController : Controller
        {
            //strip out the word 'Controller' from the type
            string expected = typeof(TController).Name.Replace("Controller", "");

            //get the key (case insensitive)
            string actual = routeData.Values.GetValue("controller").ToString();


            //expected.AssertSameStringAs(actual);
            actual.ShouldBe(expected);
            return routeData;
        }

        /// <summary>
        /// Verifies the <see cref="RouteData">routeData</see> will instruct the routing engine to ignore the route.
        /// </summary>
        /// <param name="relativeUrl"></param>
        /// <returns></returns>
        public static RouteData ShouldBeIgnored(this string relativeUrl)
        {
            RouteData routeData = relativeUrl.Route();

            routeData.RouteHandler.ShouldBeTypeOf<StopRoutingHandler>();

            return routeData;
        }

        //public static RouteData ShouldRedirectTo(this string relativeUrl, string newUrl)
        //{
        //    var routeData = relativeUrl.Route();
        //    Assert.IsInstanceOfType(routeData.RouteHandler, typeof(RedirectRouteHandler),
        //                            "Expected RedirectRouteHandler but wasn't");

        //    Assert.AreEqual(newUrl, ((RedirectRouteHandler)routeData.RouteHandler).RedirectToUrl, true);

        //    return routeData;
        //}

        /// <summary>
        /// Gets a value from the <see cref="RouteValueDictionary" /> by key.  Does a
        /// case-insensitive search on the keys.
        /// </summary>
        /// <param name="routeValues"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static object GetValue(this RouteValueDictionary routeValues, string key)
        {
            foreach(var routeValueKey in routeValues.Keys)
            {
                if(string.Equals(routeValueKey, key, StringComparison.InvariantCultureIgnoreCase))
                {
                    return routeValues[routeValueKey] as string;
                }
            }

            return null;
        }

         public static RouteData ShouldMapToWebFormsPage(this string relativeUrl, string webFormsVirtualPath = null)
        {
            var route = relativeUrl.Route();

            if(webFormsVirtualPath == null)
            {
                webFormsVirtualPath = relativeUrl;
            }

            route.RouteHandler.ShouldBeTypeOf(typeof(PageRouteHandler));
             ((PageRouteHandler) route.RouteHandler).VirtualPath.ShouldBe(webFormsVirtualPath);
            return route;
        }

        public static RouteData ShouldRedirectTo(this string relativeUrl, string newUrl)
        {
            var routeData = relativeUrl.Route();
            routeData.RouteHandler.ShouldBeTypeOf(typeof(RedirectRouteHandler));

            ((RedirectRouteHandler) routeData.RouteHandler).RedirectToUrl.ShouldBe(newUrl);

            return routeData;
        }

        private static HttpContextBase FakeHttpContext(string requestUrl, string appPath = "/", string httpMethod = "GET")
        {
            var serverVariables = new NameValueCollection();
            var querystring = new NameValueCollection();
            if(requestUrl.IndexOf("?") > -1)
            {
                var parts = requestUrl.Split('?');
                requestUrl = parts[0];
                querystring = HttpUtility.ParseQueryString(parts[1]);
            }
            var request = Substitute.For<HttpRequestBase>();
            request.AppRelativeCurrentExecutionFilePath.Returns(requestUrl);
            request.PathInfo.Returns(String.Empty);
            request.ApplicationPath.Returns(appPath);
            request.ServerVariables.Returns(serverVariables);
            request.QueryString.Returns(querystring);
            request.HttpMethod.Returns(httpMethod);

            var response = Substitute.For<HttpResponseBase>();
            response.ApplyAppPathModifier(Arg.Any<string>()).Returns(x => x.Arg<string>());

            var context = Substitute.For<HttpContextBase>();
            context.Request.Returns(request);
            context.Response.Returns(response);

            return context;
        }

        internal static UrlHelper GetUrlHelper(string appPath = "/", RouteCollection routes = null)
        {
            if(routes == null)
            {
                routes = RouteTable.Routes;
            }

            HttpContextBase httpContext = FakeHttpContext(requestUrl: "~/", appPath: appPath);
            RouteData routeData = new RouteData();
            routeData.Values.Add("controller", "home");
            routeData.Values.Add("action", "index");
            RequestContext requestContext = new RequestContext(httpContext, routeData);

            httpContext.Request.RequestContext.Returns(requestContext);

            UrlHelper helper = new UrlHelper(requestContext, routes);
            return helper;
        }
    }

}