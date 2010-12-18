using Stormbreaker.Models;

namespace Stormbreaker.Web.Routing {
    public class PathData : IPathData {
        /* *******************************************************************
	    * Properties
	    * *******************************************************************/
        public string Action { get; set; }
        public string Controller { get; set; }
        public IDocument CurrentDocument { get; set; }
    }
}