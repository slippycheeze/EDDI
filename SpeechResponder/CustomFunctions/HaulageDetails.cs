﻿using Cottle.Functions;
using Cottle.Values;
using EddiConfigService;
using EddiSpeechResponder.Service;
using JetBrains.Annotations;
using System.Linq;

namespace EddiSpeechResponder.CustomFunctions
{
    [UsedImplicitly]
    public class HaulageDetails : ICustomFunction
    {
        public string name => "HaulageDetails";
        public FunctionCategory Category => FunctionCategory.Details;
        public string description => Properties.CustomFunctions_Untranslated.HaulageDetails;
        public NativeFunction function => new NativeFunction((values) =>
        {
            var cargo = ConfigService.Instance.cargoMonitorConfiguration?.cargo;
            var result = cargo?.FirstOrDefault(c => c.haulageData.FirstOrDefault(h => h.missionid == (long)values[0].AsNumber) != null)?.haulageData;
            return new ReflectionValue(result ?? new object());
        }, 1);
    }
}
