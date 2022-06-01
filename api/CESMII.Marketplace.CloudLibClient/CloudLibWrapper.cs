﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Opc.Ua.CloudLib.Client;
using CESMII.OpcUa.NodeSetImporter;
using CESMII.Marketplace.Common.Models;

namespace CESMII.Marketplace.CloudLibClient
{
    public class CloudLibWrapper :ICloudLibWrapper
    {
        private readonly UACloudLibClient _client;
        private readonly CloudLibraryConfig _config;
        private readonly ILogger<CloudLibWrapper> _logger;
        //private TokenResponseModel _token;
        private int _maxAuthorizationAttempts = 10;

        public CloudLibWrapper(UACloudLibClient client,
            IConfiguration config, ILogger<CloudLibWrapper> logger)
        {
            _client = client;
            _config = new CloudLibraryConfig();
            config.GetSection("CloudLibConfig").Bind(_config);
            _logger = logger;

            //initialize cloud lib client
            _client = new UACloudLibClient(_config.EndPoint, _config.UserName, _config.Password);
        }

        public async Task<IEnumerable<string>> ResolveNodeSetsAsync(List<ModelNameAndVersion> missingModels)
        {
            var downloadedNodeSets = new List<string>();

            // TODO Is there an API to download matching nodeset directly via URI/PublicationDate? Should we push to add this?
            var namespacesAndIds = await _client.GetNamespaceIdsAsync().ConfigureAwait(false);
            if (namespacesAndIds != null)
            {
                var matchingNamespacesAndIds = namespacesAndIds.Where(nsid => missingModels.Any(m => m.ModelUri == nsid.Item1)).ToList();
                var nodesetWithURIAndDate = new List<(string, DateTime, string)?>();
                foreach (var nsid in matchingNamespacesAndIds)
                {
                    var nodeSet = await _client.DownloadNodesetAsync(nsid.Item2).ConfigureAwait(false);
                    nodesetWithURIAndDate.Add((
                        nodeSet.Nodeset.NamespaceUri?.ToString() ?? nsid.Item1, // TODO cloud lib currently doesn't return the namespace uri: report issue/fix
                        nodeSet.Nodeset.PublicationDate,
                        nodeSet.Nodeset.NodesetXml));
                }

                foreach (var missing in missingModels)
                {
                    // Find exact match or lowest matching version
                    var bestMatch = nodesetWithURIAndDate.FirstOrDefault(n => n?.Item1 == missing.ModelUri && n?.Item2 == missing.PublicationDate);
                    if (bestMatch == null)
                    {
                        bestMatch = nodesetWithURIAndDate.Where(n => n?.Item1 == missing.ModelUri && n?.Item2 >= missing.PublicationDate).OrderBy(m => m?.Item2).FirstOrDefault();
                    }
                    if (bestMatch != null)
                    {
                        downloadedNodeSets.Add(bestMatch?.Item3);
                    }
                }
            }
            return downloadedNodeSets;
        }

        public async Task<List<UANodesetResult>> Search(List<string> keywords, List<string> exclude)
        {
            var result = await _client.GetBasicNodesetInformationAsync(keywords).ConfigureAwait(false);
            //TBD - don't need this. Keep it here for now in case we temporarily need to get more data.
            //foreach (var nodeset in result)
            //{
            //    //get more details
            //}

            //if exclude has values, strip out nodesets where exclude value == nodeset uri
            if (exclude != null && result != null)
            {
                result = result.Where(x => !exclude.Any(y => y.ToLower().Equals(x.NameSpaceUri.ToLower()))).ToList();
            }

            return result;
        }

        public async Task<UANameSpace> GetById(string id)
        {
            var result = await _client.DownloadNodesetAsync(id).ConfigureAwait(false);
            return result;
        }
    }
}
