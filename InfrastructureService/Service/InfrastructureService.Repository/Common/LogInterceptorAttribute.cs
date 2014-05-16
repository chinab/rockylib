using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using PostSharp.Aspects;

namespace InfrastructureService.Repository
{
    [Serializable]
    public class LogInterceptorAttribute : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs args)
        {
            string methodArgs;
            try
            {
                methodArgs = JsonConvert.SerializeObject(args.Arguments, Formatting.None);
            }
            catch (Exception ex)
            {
                methodArgs = ex.Message;
            }
            App.LogDebug("PreProceed_{0}:{1}", args.Method.Name, methodArgs);
            base.OnEntry(args);
        }

        public override void OnExit(MethodExecutionArgs args)
        {
            string methodArgs;
            try
            {
                methodArgs = JsonConvert.SerializeObject(args.ReturnValue, Formatting.None);
            }
            catch (Exception ex)
            {
                methodArgs = ex.Message;
            }
            App.LogDebug("PostProceed_{0}:{1}", args.Method.Name, methodArgs);
            base.OnExit(args);
        }

        public override void OnException(MethodExecutionArgs args)
        {
            args.FlowBehavior = FlowBehavior.RethrowException;
            object[] array = new object[3];
            array[0] = args.Method.Name;
            try
            {
                array[1] = JsonConvert.SerializeObject(args.Arguments, Formatting.None);
                array[2] = JsonConvert.SerializeObject(args.ReturnValue, Formatting.None);
            }
            catch (Exception ex)
            {
                array[1] = ex.Message;
            }
            var dbEx = args.Exception as DbEntityValidationException;
            if (dbEx != null)
            {
                StringBuilder msg = new StringBuilder();
                foreach (var validationErrors in dbEx.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        msg.AppendFormat("Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage);
                    }
                }
                Array.Resize(ref array, 4);
                array[3] = msg;
                App.LogError(args.Exception, "PerformError_{0}:{1}\t{2}\r\n{3}", array);
            }
            else
            {
                App.LogError(args.Exception, "PerformError_{0}:{1}\t{2}", array);
            }
            base.OnException(args);
        }
    }
}