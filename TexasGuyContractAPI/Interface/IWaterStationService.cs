using System.Collections.Generic;
using System.Threading.Tasks;
using TexasGuyContractIdentity.Models;

namespace TexasGuyContractAPI.Interface
{
    public interface IWaterStationService
    {
        Task CheckAndLogWaterStationsAsync(int stationId);
    }
}