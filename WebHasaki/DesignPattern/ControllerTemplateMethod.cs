using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebHasaki.DesignPattern
{
    public abstract class ControllerTemplateMethod : Controller
    {
        protected abstract void PrintRoutes();
        protected abstract void PrintDIs();

        protected void PrintInformation()
        {
            System.Diagnostics.Debug.WriteLine("=== START DEBUG INFORMATION ===");
            PrintRoutes();
            PrintDIs();
            System.Diagnostics.Debug.WriteLine("=== END DEBUG INFORMATION ===");
        }

        protected virtual void LogAction(string actionName)
        {
            System.Diagnostics.Debug.WriteLine($"[ACTION] {GetType().Name}.{actionName}()");
        }
    }
}