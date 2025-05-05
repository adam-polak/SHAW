using Newtonsoft.Json;
using StarFederation.Datastar.DependencyInjection;

namespace SHAW.Controllers.Util;

public static class SignalUtil
{
    public static async Task<T> GetModelFromSignal<T>(IDatastarSignalsReaderService reader)
    {
        string signal = await reader.ReadSignalsAsync();

        T? model;
        model = JsonConvert.DeserializeObject<T>(signal);
        if (model == null)
        {
            throw new Exception("Failed to convert signal to user");
        }

        return model;
    }
}