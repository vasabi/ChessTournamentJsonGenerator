using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using System.Xml.XPath;
using System.Xml;
using CsQuery.HtmlParser;
using CsQuery;

namespace chess_res_generator
{
    class Program
    {
        static void Main(string[] args)
        {
            string urlPageWithListOfYears = "TurnierSuche.aspx?lan=11&jahr=99999";

            CQ htmlPageWithListOfYears = GetPage(urlPageWithListOfYears).Result
                ["#_ctl0_F7 > div:nth-child(3) > table > tbody > tr:nth-child(6) > td.CR > a"];
            //["#_ctl0_F7 > div:nth-child(3) > table > tbody > * > td.CR > a"]

            foreach (IDomObject years in htmlPageWithListOfYears)
            {
                Console.WriteLine(years.GetAttribute("href"));
                CQ htmlPageWithListOfTourmamentsForOneYear = GetPage(years.GetAttribute("href")).Result;
                CQ tournamentUrlAndNameTd = htmlPageWithListOfTourmamentsForOneYear
                    ["#_ctl0_F7 > div:nth-child(3) > table > tbody > tr:nth-child(3) > td:nth-child(1) > a"];
                //["#_ctl0_F7 > div:nth-child(3) > table > tbody > tr:nth-child(n+2) > td:nth-child(1) > a"];
                CQ tournamentDateTd = htmlPageWithListOfTourmamentsForOneYear
                    //["#_ctl0_F7 > div:nth-child(3) > table > tbody > tr:nth-child(n+2) > td:nth-child(2)"];
                    ["#_ctl0_F7 > div:nth-child(3) > table > tbody > tr:nth-child(3) > td:nth-child(2)"];
                CQ tournamentRoundCountTd = htmlPageWithListOfTourmamentsForOneYear
                    //["#_ctl0_F7 > div:nth-child(3) > table > tbody > tr:nth-child(n+2) > td:nth-child(8)"];
                    ["#_ctl0_F7 > div:nth-child(3) > table > tbody > tr:nth-child(3) > td:nth-child(8)"];
                CQ tournamentPlayerCountTd = htmlPageWithListOfTourmamentsForOneYear
                    //["#_ctl0_F7 > div:nth-child(3) > table > tbody > tr:nth-child(n+2) > td:nth-child(9)"];
                    ["#_ctl0_F7 > div:nth-child(3) > table > tbody > tr:nth-child(3) > td:nth-child(9)"];
                foreach (IDomObject item in tournamentUrlAndNameTd)
                {
                    var tournamentUrl = item.GetAttribute("href");
                    var tournamentName = item.InnerText;
                    Console.WriteLine(tournamentUrl);
                    Console.WriteLine(tournamentName);

                    #region GetCutTournamentPage
                    CQ htmlPageTournamentCut = GetPage(tournamentUrl).Result;
                    var viewState = htmlPageTournamentCut["#__VIEWSTATE"][0].GetAttribute("value");
                    var viewStateGenerator = htmlPageTournamentCut["#__VIEWSTATEGENERATOR"][0].GetAttribute("value");
                    var eventValidation = htmlPageTournamentCut["#__EVENTVALIDATION"][0].GetAttribute("value");
                    var paremeters = new Dictionary<string, string> {{"__EVENTTARGET","linkbutton_show"},
                                                                    {"__EVENTARGUMENT",""},{"__VIEWSTATE",viewState},
                                                                    {"__VIEWSTATEGENERATOR",viewStateGenerator},
                                                                    {"__EVENTVALIDATION",eventValidation}};
                    Console.WriteLine("{0}{3}{1}{3}{2}{3}", viewState, viewStateGenerator, eventValidation, Environment.NewLine);
                    #endregion

                    #region GetFullTournamentPage
                    CQ htmlPageTournamentFull = GetPage(tournamentUrl, paremeters).Result;
                    CQ playerUrlTd = htmlPageTournamentFull
                        ["#_ctl0_F7 > div:nth-child(2) > table > tbody > tr:nth-child(n+1) > td:nth-child(3) > a"];
                    foreach (IDomObject player in playerUrlTd)
                    {
                        var playerUrl = player.GetAttribute("href");
                        Console.WriteLine("{0}{2}", playerUrl, Environment.NewLine);
                        CQ htmlPlayerInfo = GetPage(playerUrl).Result;
                        var playerName = htmlPlayerInfo
                            ["#_ctl0_F7 > div:nth-child(2) > table:nth-child(2) > tbody > tr:nth-child(1) > td:nth-child(2)"][0].InnerText;
                        var playerNumber = htmlPlayerInfo
                            ["#_ctl0_F7 > div:nth-child(2) > table:nth-child(2) > tbody > tr:nth-child(3) > td:nth-child(2)"][0].InnerText;
                        var playerNationalRating = htmlPlayerInfo
                            ["#_ctl0_F7 > div:nth-child(2) > table:nth-child(2) > tbody > tr:nth-child(5) > td:nth-child(2)"][0].InnerText;
                        var playerAge = htmlPlayerInfo
                            ["#_ctl0_F7 > div:nth-child(2) > table:nth-child(2) > tbody > tr:nth-child(14) > td:nth-child(2)"][0].InnerText;
                        var playerFIDEId = htmlPlayerInfo //для генерации своего id и проверки уникальности игрока
                            ["#_ctl0_F7 > div:nth-child(2) > table:nth-child(2) > tbody > tr:nth-child(13) > td:nth-child(2)"][0].InnerText;
                        // не хватает: id, natId, sex
                    }
                    #endregion


                }
                foreach (IDomObject item in tournamentDateTd)
                {
                    Console.WriteLine(item.InnerText);
                    var tournamentDate = item.InnerText;
                }
                foreach (IDomObject item in tournamentRoundCountTd)
                {
                    Console.WriteLine(item.InnerText);
                    var tournamentRoundCount = item.InnerText;
                }
                foreach (IDomObject item in tournamentPlayerCountTd)
                {
                    Console.WriteLine(item.InnerText);
                    var tournamentPlayerCount = item.InnerText;
                }
                // не хватает id, pId
            }





            Console.ReadKey();
        }

        static async Task<CQ> GetPage(string url)
        {
            string page = url;

            HttpClientHandler handler = new HttpClientHandler();
            HttpClient client = new HttpClient(handler);
            client.BaseAddress = new Uri("http://chess-results.com/");
            HttpResponseMessage response = await client.GetAsync(page);
            HttpContent content = response.Content;

            string resultString = await content.ReadAsStringAsync();

            CQ html = CQ.Create(resultString);

            return html;
        }

        static async Task<CQ> GetPage(string url, Dictionary<string, string> parameters)
        {
            string page = url;
            var encodedContent = new FormUrlEncodedContent(parameters);

            HttpClientHandler handler = new HttpClientHandler();
            HttpClient client = new HttpClient(handler);
            client.BaseAddress = new Uri("http://chess-results.com/");
            HttpResponseMessage response = await client.PostAsync(url, encodedContent).ConfigureAwait(false);
            HttpContent content = response.Content;

            string resultString = await content.ReadAsStringAsync();

            CQ html = CQ.Create(resultString);

            return html;
        }
    }
}
