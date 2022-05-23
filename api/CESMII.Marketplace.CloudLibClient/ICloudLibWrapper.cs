using System;
using System.Collections.Generic;

using Opc.Ua.CloudLib.Client;
using CESMII.OpcUa.NodeSetImporter;

namespace CESMII.Marketplace.CloudLibClient
{
    public interface ICloudLibWrapper
    {
        Task<IEnumerable<string>> ResolveNodeSetsAsync(List<ModelNameAndVersion> missingModels);
        Task<List<UANodesetResult>> Search(List<string> keywords);
        Task<UANameSpace> GetById(string id);
    }
}
