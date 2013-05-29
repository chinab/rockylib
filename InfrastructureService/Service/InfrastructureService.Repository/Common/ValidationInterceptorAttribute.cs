using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PostSharp.Aspects;
using Microsoft.Practices.EnterpriseLibrary.Validation;
using InfrastructureService.Model;

namespace InfrastructureService.Repository
{
    [Serializable]
    public class ValidationAspectAttribute : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs args)
        {
            var repository = args.Instance as RepositoryBase;
            if (repository != null)
            {
                var array = args.Arguments;
                if (array.Count == 0)
                {
                    return;
                }
                var header = array[0] as HeaderEntity;
                if (header == null)
                {
                    throw new InvalidOperationException("方法首参数必须继承HeaderEntity");
                }
                repository.VerifyHeader(header);
            }

            StringBuilder faultReason = null;
            foreach (var param in args.Arguments)
            {
                var validateResults = Validation.Validate(param);
                if (validateResults.IsValid)
                {
                    continue;
                }

                if (faultReason == null)
                {
                    faultReason = new StringBuilder("Validate fault: ");
                }
                foreach (var result in validateResults)
                {
                    faultReason.AppendFormat("{0}:{1},", result.Key, result.Message);
                }
                faultReason.Length--;
            }
            if (faultReason != null)
            {
                throw new ArgumentException(faultReason.ToString());
            }
            base.OnEntry(args);
        }
    }
}