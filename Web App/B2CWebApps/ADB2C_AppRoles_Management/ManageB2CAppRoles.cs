using ADB2C_AppRoles_Management.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Text;

namespace ADB2C_AppRoles_Management
{
    public class ManageB2CAppRoles
    {
        private readonly ILogger<ManageB2CAppRoles> _logger;

        public ManageB2CAppRoles(ILogger<ManageB2CAppRoles> logger)
        {
            _logger = logger;
        }

        [Function("ManageB2CAppRoles")]
        public async Task<ActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
           
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string objectId = req.Query["objectId"]; // User
            string clientId = req.Query["clientId"]; // App
            string tenantId = req.Query["tenantId"]; // Tenant
            string scope = req.Query["scope"];       // "roles", "roles groups" or "groups"

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            objectId = objectId ?? data?.objectId;
            clientId = clientId ?? data?.clientId;
            tenantId = tenantId ?? data?.tenantId;
            scope = scope ?? data?.scope;
            // default just roles
            if (string.IsNullOrWhiteSpace(scope))
            {
                scope = "roles";
            }
            else
            {
                scope = scope.ToLowerInvariant();
            }
            _logger.LogInformation($"Params: objectId={objectId}, clientId={clientId}, tenantId={tenantId}, scope={scope}");

            //////////////////////////////////////////////////////////////////////////////////////////////////////
            // If you use Cert based auth, you will receive a client cert
            //////////////////////////////////////////////////////////////////////////////////////////////////////
            var cert = req.HttpContext.Connection.ClientCertificate;
            _logger.LogInformation($"Incoming cert: {cert}");
            if (cert != null)
            {
                var b2cCertSubject = System.Environment.GetEnvironmentVariable($"B2C_{tenantId}_CertSubject"); //
                var b2cCertThumbprint = System.Environment.GetEnvironmentVariable($"B2C_{tenantId}_CertThumbprint");
                if (!(cert.Subject.Equals(b2cCertSubject) && cert.Thumbprint.Equals(b2cCertThumbprint)))
                {
                    var respContent = new
                    {
                        version = "1.0.0",
                        status = (int)HttpStatusCode.BadRequest,
                        userMessage = "Technical error - cert..."
                    };
                    var json = JsonConvert.SerializeObject(respContent);
                    _logger.LogInformation(json);

                    //return new HttpResponseMessage(HttpStatusCode.Conflict)
                    //{
                    //    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                    //};
                    return (ActionResult)new OkObjectResult(new OutputClaimsModel()
                    {
                        Groups = new List<string>(),
                        Roles = new List<string>(),

                    });
                }
            }

            //////////////////////////////////////////////////////////////////////////////////////////////////////
            // get the B2C client credentials for this tenant
            //////////////////////////////////////////////////////////////////////////////////////////////////////
            var b2cClientId = System.Environment.GetEnvironmentVariable($"B2C_ClientId"); //
            var b2cClientSecret = System.Environment.GetEnvironmentVariable($"B2C_ClientSecret");

            _logger.LogInformation($"ClientId={b2cClientId}, ClientSecret= {b2cClientSecret}");

            //////////////////////////////////////////////////////////////////////////////////////////////////////
            // Authenticate via the Client Credentials flow
            //////////////////////////////////////////////////////////////////////////////////////////////////////
            string accessToken = GetCachedAccessToken(tenantId);
            if (null == accessToken)
            {
                HttpClient client = new HttpClient();
                var dict = new Dictionary<string, string>();
                dict.Add("grant_type", "client_credentials");
                dict.Add("client_id", b2cClientId);
                dict.Add("client_secret", b2cClientSecret);
                dict.Add("resource", "https://graph.microsoft.com");
                dict.Add("scope", "User.Read.All AppRoleAssignment.ReadWrite.All");

                var urlTokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/token?api-version=1.0";
                _logger.LogInformation(urlTokenEndpoint);

                HttpResponseMessage resp = client.PostAsync(urlTokenEndpoint, new FormUrlEncodedContent(dict)).Result;
                var contents = await resp.Content.ReadAsStringAsync();
                client.Dispose();
                _logger.LogInformation("HttpStatusCode=" + resp.StatusCode.ToString() + " - " + contents);

                // If the client creds failed, return error
                if (resp.StatusCode != HttpStatusCode.OK)
                {
                    var respContent = new { version = "1.0.0", status = (int)HttpStatusCode.BadRequest, userMessage = "Technical error..." };
                    var json = JsonConvert.SerializeObject(respContent);
                    _logger.LogInformation(json);
                    //return new HttpResponseMessage(HttpStatusCode.Conflict)
                    //{
                    //    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                    //};
                    return (ActionResult)new OkObjectResult(new OutputClaimsModel()
                    {
                        Groups = new List<string>(),
                        Roles = new List<string>(),

                    });
                }
                accessToken = JObject.Parse(contents)["access_token"].ToString();
                CacheAccessToken(tenantId, accessToken);
            }
            _logger.LogInformation(accessToken);

            //////////////////////////////////////////////////////////////////////////////////////////////////////
            // GraphAPI query for user's group membership
            //////////////////////////////////////////////////////////////////////////////////////////////////////
            var groupsList = new List<string>();
            var groupIDList = new List<string>();
            if (scope.Contains("groups"))
            {
                HttpClient httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var url = $"https://graph.microsoft.com/v1.0/users/{objectId}/memberOf?$select=id,displayName";
                _logger.LogInformation(url);
                var res = await httpClient.GetAsync(url);
                _logger.LogInformation("HttpStatusCode=" + res.StatusCode.ToString());
                if (res.IsSuccessStatusCode)
                {
                    var respData = await res.Content.ReadAsStringAsync();
                    var groupArray = (JArray)JObject.Parse(respData)["value"];
                    foreach (JObject g in groupArray)
                    {
                        var name = g["displayName"].Value<string>();
                        var id = g["id"].Value<string>();
                        if (name != null)
                        {
                            groupsList.Add(name??string.Empty);
                            groupIDList.Add(id);
                        }
                        else
                        {
                            groupsList.Add("");
                        }
                    }
                }
                httpClient.Dispose();
            }

            //////////////////////////////////////////////////////////////////////////////////////////////////////
            // GraphAPI query for user AppRoleAssignments
            //////////////////////////////////////////////////////////////////////////////////////////////////////
            JObject userData = null;
            bool hasAssignments = false;
            var roleNames = new List<string>();

            if (scope.Contains("roles"))
            {
                HttpClient httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var url = $"https://graph.microsoft.com/beta/users/{objectId}/appRoleAssignments?$select=appRoleId,resourceId,resourceDisplayName";
                _logger.LogInformation(url);
                var res = await httpClient.GetAsync(url);
                _logger.LogInformation("HttpStatusCode=" + res.StatusCode.ToString());
                if (res.IsSuccessStatusCode)
                {
                    var respData = await res.Content.ReadAsStringAsync();
                    _logger.LogInformation(respData);
                    userData = JObject.Parse(respData);
                    foreach (var item in userData["value"])
                    {
                        hasAssignments = true;
                        break;
                    }
                }
                _logger.LogInformation($"rolesSection = {userData["value"]}");
                httpClient.Dispose();
            }
            if (groupIDList.Count > 0)
            {
                HttpClient httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                foreach(var groupId in groupIDList)
                {
                    var url = $"https://graph.microsoft.com/beta/groups/{groupId}/appRoleAssignments?$select=appRoleId,resourceId,resourceDisplayName";
                    var res = await httpClient.GetAsync(url);
                    if (res.IsSuccessStatusCode)
                    {
                        var respData = await res.Content.ReadAsStringAsync();
                        _logger.LogInformation(respData);
                        var groupData= JObject.Parse(respData);                        
                        // Append groupData to userData
                        if (userData == null)
                        {
                            userData = new JObject();
                            userData["value"] = new JArray();
                        }
                        foreach (var item in groupData["value"])
                        {
                            ((JArray)userData["value"]).Add(item);
                        }
                        foreach (var item in userData["value"])
                        {
                            hasAssignments = true;
                            break;
                        }
                    }
                    _logger.LogInformation($"Group -{groupId}  = {userData["value"]}");
                }
                
                httpClient.Dispose();
            }

            if (hasAssignments)
            {
                HttpClient httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var url = $"https://graph.microsoft.com/beta/servicePrincipals?$filter=appId eq '{clientId}'&$select=id,appRoles";
                _logger.LogInformation(url);
                var res = httpClient.GetAsync(url).Result;
                JArray appRolesArray = null;
                string spId = "";
                if (res.IsSuccessStatusCode)
                {
                    var respData = res.Content.ReadAsStringAsync().Result;
                    var spoData = JObject.Parse(respData);
                    foreach (var item in spoData["value"])
                    {
                        spId = item["id"].ToString();
                        appRolesArray = (JArray)item["appRoles"];
                        foreach (var itemUser in userData["value"])
                        {
                            if (spId == itemUser["resourceId"].ToString())
                            {
                                var appRoleId = itemUser["appRoleId"].ToString();
                                foreach (var role in appRolesArray)
                                {
                                    if (appRoleId == role["id"].ToString())
                                    {
                                        roleNames.Add(role["value"].ToString());
                                    }
                                }
                            }
                        }
                    }
                }
                httpClient.Dispose();
            }


            //return new HttpResponseMessage(HttpStatusCode.OK)
            //{
            //    Content = new StringContent(jsonToReturn, System.Text.Encoding.UTF8, "application/json")
            //};

            var jsonToReturn = JsonConvert.SerializeObject((ActionResult)new OkObjectResult(new OutputClaimsModel()
            {
                Groups = groupsList,
                Roles = roleNames.Distinct().ToList()

            }));
            _logger.LogInformation(jsonToReturn);
            return (ActionResult)new OkObjectResult(new OutputClaimsModel()
            {
                Groups  = groupsList,
                Roles   = roleNames.Distinct().ToList()

            });
         //   var jsonToReturn = JsonConvert.SerializeObject(new { roles = roleNames, groups = groupsList });
          
        }

        public static string GetCachedAccessToken(string tenantId, int secondsRemaining = 60) // access_token needs to be valid for N seconds more
        {
            string accessToken = Environment.GetEnvironmentVariable($"B2C_{tenantId}_AppRoles_AccessToken");
            if (accessToken != null)
            {
                DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                string b64 = accessToken.Split(".")[1];
                while ((b64.Length % 4) != 0)
                    b64 += "=";
                JObject jwtClaims = JObject.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(b64)));
                DateTime expiryTime = epoch.AddSeconds(int.Parse(jwtClaims["exp"].ToString()));
                if (DateTime.UtcNow >= expiryTime.AddSeconds(-secondsRemaining))
                    accessToken = null; // invalidate
            }
            return accessToken;
        }
        public static void CacheAccessToken(string tenantId, string accesToken)
        {
            Environment.SetEnvironmentVariable($"B2C_{tenantId}_AppRoles_AccessToken", accesToken);
        }
    }
    
}
