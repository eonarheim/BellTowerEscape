using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using AutoMapper;
using BellTowerEscape.Server;
using BellTowerEscape.Utility;

namespace BellTowerEscape
{

    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // Configure AutoMapper
            Mapper.CreateMap<Elevator, ElevatorLite>().IgnoreAllNonExisting();
            Mapper.CreateMap<Meeple, MeepleLite>().IgnoreAllNonExisting();
            Mapper.CreateMap<Floor, FloorLite>().ForMember(
                dest => dest.NumberOfMeeple, 
                opt => opt.MapFrom(src => src.Meeples.Count)).ForMember(
                dest => dest.GoingDown,
                opt => opt.MapFrom(src => src.Meeples.Where(m => m.Destination < src.Number).Count())).ForMember(
                dest => dest.GoingUp,
                opt => opt.MapFrom(src => src.Meeples.Where(m => m.Destination > src.Number).Count()));
            
            Mapper.AssertConfigurationIsValid();


        }
    }
}
