using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ADOTestConsole
{
    class Program
    {
        private static readonly string azureDevOpsOrganizationUrl = ConfigurationManager.AppSettings["ADOUrl"];
        private static readonly string azureDevOpsProject = ConfigurationManager.AppSettings["Project"];
        private static readonly string personalAccessToken = ConfigurationManager.AppSettings["PersonalAccessToken"];
        
        static void Main(string[] args)
        {
            string base64PAT = Convert.ToBase64String(
                Encoding.ASCII.GetBytes(
                    string.Format("{0}:{1}", "", personalAccessToken)));
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    AuthenticationHeaderValue authHeader = new AuthenticationHeaderValue("Basic", base64PAT);
                    httpClient.DefaultRequestHeaders.Authorization = authHeader;
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    Console.WriteLine($"*** Retrieving test runs for project {azureDevOpsProject} ***");
                    using (HttpResponseMessage response = httpClient.GetAsync($"{azureDevOpsOrganizationUrl}/{azureDevOpsProject}/_apis/test/runs?includeRunDetails=true&api-version=5.1").Result)
                    {
                        response.EnsureSuccessStatusCode();
                        string responseBody = response.Content.ReadAsStringAsync().Result;
                        dynamic testRuns = JsonConvert.DeserializeObject(responseBody);
                        Console.WriteLine($"Test runs found: {testRuns.count}");
                        if (testRuns.count > 0)
                        {
                            foreach (dynamic testRun in testRuns.value)
                            {
                                Console.WriteLine($"Test Run ID: {testRun.id}");
                                Console.WriteLine($"Test Run Name: {testRun.name}");
                                Console.WriteLine($"Test Plan ID: {testRun.plan.id}");
                                Console.WriteLine($"Total Tests: {testRun.totalTests}");
                                Console.WriteLine($"Passed Tests: {testRun.passedTests}");
                                Console.WriteLine($"Completed Date: {testRun.completedDate}");
                                using (HttpClient resultsClient = new HttpClient())
                                {
                                    resultsClient.DefaultRequestHeaders.Authorization = authHeader;
                                    resultsClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                                    using (HttpResponseMessage resultsResponse = resultsClient.GetAsync($"{azureDevOpsOrganizationUrl}/{azureDevOpsProject}/_apis/test/Runs/{testRun.id}/results?api-version=5.1").Result)
                                    {
                                        string resultsBody = resultsResponse.Content.ReadAsStringAsync().Result;
                                        dynamic testResults = JsonConvert.DeserializeObject(resultsBody);
                                        Console.WriteLine($"Test results found: {testResults.count}");
                                        if (testResults.count > 0)
                                        {
                                            Console.WriteLine("Results");
                                            Console.WriteLine("=======");
                                            foreach (dynamic testResult in testResults.value)
                                            {
                                                Console.WriteLine($"Test Result ID: {testResult.id}");
                                                Console.WriteLine($"Outcome: {testResult.outcome}");
                                                Console.WriteLine($"Completed Date: {testResult.completedDate}");
                                                Console.WriteLine($"Test Case: {testResult.testCase.name}");
                                            }
                                        }
                                    }
                                }
                                Console.WriteLine();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0}: {1}", ex.GetType(), ex.Message);
            }
        }
    }
}
