using System.Web.Mvc;
using System.Web.Routing;

namespace Ketchup.Mvc.Testing
{
    public class RouteTestBase
    {
        private UrlHelper _url;

        protected UrlHelper Url
        {
            get { return _url ?? (_url = RouteTestingExtensions.GetUrlHelper()); }
        }


        protected RouteData Post(string url)
        {
            return RouteTestingExtensions.Post(this, url);
        }

        protected RouteData Get(string url)
        {
            return url.Route();
        }
    }
}