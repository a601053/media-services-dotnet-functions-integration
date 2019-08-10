/*

Azure Media Services REST API v2 Function
 
This function starts a channel.

Input:
{
    "channelName" : (optional)"the name of the existing channel, EX:c06lpvmix02",
}

Output:
{
    "success" : true,
    "channels" : [{"ChannelName":"c06lpvmix01","State":"Running"}, {"ChannelName":c06lpvmix02","State":"Stopped"}]
}

*/


using System;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.MediaServices.Client;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Linq;

namespace media_functions_for_logic_app
{
    
    public static class list_program
    {
        // Field for service context.
        private static CloudMediaContext _context = null;

        [FunctionName("list-program")]
        public static async Task<object> Run([HttpTrigger(WebHookType = "genericJson")]HttpRequestMessage req, TraceWriter log)

        {
            log.Info($"Webhook was triggered!");
            string channelStatus = null;

            MediaServicesCredentials amsCredentials = new MediaServicesCredentials();
            log.Info($"Using Azure Media Service Rest API Endpoint : {amsCredentials.AmsRestApiEndpoint}");
            
            try
            {
                AzureAdTokenCredentials tokenCredentials = new AzureAdTokenCredentials(amsCredentials.AmsAadTenantDomain,
                                             new AzureAdClientSymmetricKey(amsCredentials.AmsClientId, amsCredentials.AmsClientSecret),
                                             AzureEnvironments.AzureCloudEnvironment);

                AzureAdTokenProvider tokenProvider = new AzureAdTokenProvider(tokenCredentials);

                _context = new CloudMediaContext(amsCredentials.AmsRestApiEndpoint, tokenProvider);
                
                log.Info("Context object created.");

                var channelsStarted= new System.Collections.Generic.List <String>();
                var channelsStoped= new System.Collections.Generic.List <String>();
                _context.Channels.ToList().ForEach(e=>channelsStarted.Add("{'ChannelName':'"+e.Name+"','State':'"+e.State.ToString()+"'}"));
                /* 
                _context.Channels.ToList().ForEach(
                    e=> e.Programs.ToList().ForEach(
                        p=>channelsStarted.Add("{ 'ChannelName':'"+   p.Channel.Name
                                                +"','ChannelState':'"+ p.Channel.State.ToString()
                                                +"','Program':'"+p.Name
                                                +"','ProgramState':'"+p.State.ToString()
                                                +"','Locators':'"+p.Asset.GetHlsUri().AbsoluteUri
                                                +"'}")));
                */
                //_context.Channels.ToList().ForEach(e=> channelsStoped.Add("{'ChannelName':'"+e.Name+"','State':'"+e.State.ToString()+"'}")   );
                _context.Channels.ToList().ForEach(e=> e.Programs.Where(p => p.Asset!=null && p.Asset.Locators!=null && p.Asset.Locators.Count>0).ToList().ForEach(p=> log.Info("{'ChannelName':'"+   p.Channel.Name+"','State':'--- : "+p.Channel.Name+" --- Locators: "+p.Asset.GetHlsUri().AbsoluteUri)  ));
                
       
                channelStatus="["+String.Join(", ", channelsStarted.ToArray())+"]";
                //log.Info("Channels: "+channelStatus);
                
            }
            catch (Exception ex)
            {
                string message = ex.Message + ((ex.InnerException != null) ? Environment.NewLine + MediaServicesHelper.GetErrorMessage(ex) : "");
                log.Info($"ERROR: Exception {message}");
                return req.CreateResponse(HttpStatusCode.InternalServerError, new { error = message });
            }
            //log.Info("Channel Status: "+channelStatus);
            return req.CreateResponse(HttpStatusCode.OK, new
            {
                success = true,
                channels= JsonConvert.DeserializeObject( channelStatus)
            });
        }
    }
}
