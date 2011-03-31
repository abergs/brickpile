using System.Web.Mvc;
using Dashboard.Web;
using Dashboard.Web.Routing;
using StructureMap;

namespace Dashboard {
    public class DashboardAreaRegistration : AreaRegistration {
        /* *******************************************************************
	    * Properties
	    * *******************************************************************/
        public override string AreaName { get { return "Dashboard"; } }
        /* *******************************************************************
	    * Methods
	    * *******************************************************************/
        public override void RegisterArea(AreaRegistrationContext context) {
            
            ViewEngines.Engines.Add(new RazorViewEngine()
                                        {
                                            AreaPartialViewLocationFormats = new[] { "~/Views/{1}/{0}.cshtml" }
                                        });


            var dashboardRoute = new ContentRoute(
                ObjectFactory.GetInstance<DashboardPathResolver>(),
                ObjectFactory.GetInstance<DashboardVirtualPathResolver>(),
                null);

            context.Routes.Add("Dashboard", dashboardRoute);

            context.MapRoute(
                "Dashboard_default",
                "dashboard/{controller}/{action}/{id}",
                new { controller = "dashboard", action = "index", id = UrlParameter.Optional }
            );
        }
    }
}