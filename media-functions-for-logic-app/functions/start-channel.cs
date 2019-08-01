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
    public static class start_channel
    {
        // Field for service context.
        private static CloudMediaContext _context = null;

        [FunctionName("start-channel")]
        public static async Task<object> Run([HttpTrigger(WebHookType = "genericJson")]HttpRequestMessage req, TraceWriter log)

        {
            log.Info($"Webhook was triggered!");
            string channelStatus = null;
            string channelName = null;
            try{

            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);

            log.Info(jsonContent);

            if (data.channelName == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, new
                {
                    error = "Please pass channelName in the input object"
                });
            }
            
            channelName = data.channelName;
            }catch (Exception )
            {

            }

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

                var channels= new System.Collections.Generic.List <String>();
                _context.Channels.ToList().ForEach(e=>channels.Add("{'ChannelName':'"+e.Name+"','State':'"+e.State.ToString()+"'}"));
                log.Info("Channels: "+String.Join(", ", channels.ToArray()));
                channels.Clear();

                if (channelName != null)
                {
                    var channel = _context.Channels.Where(c => c.Name == channelName).FirstOrDefault();
                    if (channel == null)
                    {
                        return req.CreateResponse(HttpStatusCode.BadRequest, new
                        {
                            error = string.Format("Channel {0} not found", channelName)
                        });

                    }
                    try{channel.Start();}catch(Exception){}
                    log.Info("Channel "+channelName+" is non state: "+channel.State.ToString());
                }
                else {
                    log.Info("All channels are Started");
                    _context.Channels.AsParallel().ForAll(e => {try{e.Start();}catch(Exception){}});
                }
                    _context.Channels.ToList().ForEach(e=>channels.Add("{'ChannelName':'"+e.Name+"','State':'"+e.State.ToString()+"'}")); 
                    channelStatus="["+String.Join(", ", channels.ToArray())+"]";
                    log.Info("Channels: "+channelStatus);
              
                
                
            }
            catch (Exception ex)
            {
                string message = ex.Message + ((ex.InnerException != null) ? Environment.NewLine + MediaServicesHelper.GetErrorMessage(ex) : "");
                log.Info($"ERROR: Exception {message}");
                return req.CreateResponse(HttpStatusCode.InternalServerError, new { error = message });
            }
            log.Info("Channel Status: "+channelStatus);
            return req.CreateResponse(HttpStatusCode.OK, new
            {
                success = true,
                channels= channelStatus
            });
        }
    }
}
