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
using System.Text.RegularExpressions;
using System.IO;
using System.Collections;
using System.Globalization;
using Newtonsoft.Json;

namespace chess_res_generator
{
    class Crawler
    {
        public void GetAllTournaments(StreamWriter file, string tournamentsFolder)
        {
            string urlPageWithListOfYears = "TurnierSuche.aspx?lan=11&jahr=99999";

            try
            {
                var htmlPageWithListOfYears = GetPage(urlPageWithListOfYears).Result;

                CQ listOfYearsTd = htmlPageWithListOfYears
                    ["#_ctl0_F7 > div:nth-child(n) > table > tbody > tr:nth-child(2) > td.CR > a"];

                int trnCount = int.Parse(htmlPageWithListOfYears
                    ["#_ctl0_F7 > div:nth-child(n) > table > tbody > tr.CRg1b:last-child > td.CR:contains(\"Все турниры\") ~ td.CRr:last-child"][0]
                    .InnerText, NumberStyles.Integer);

                ArrayList tournamentsList = new ArrayList();
                var ti = 0;
                List<string> tournamentIdsList = new List<string>();
                List<string> tournamentPrivateIdsList = new List<string>();
                List<string> tournamentUrlsList = new List<string>();
                List<string> tournamentNamesList = new List<string>();
                List<string> tournamentDatesList = new List<string>();
                List<string> tournamentRoundsCountList = new List<string>();
                List<string> tournamentPlayersCountList = new List<string>();
                ArrayList currentTournamentPlayerInfoList = new ArrayList();
                ArrayList currentTournamentRoundsList = new ArrayList();

                tournamentsList.Add(tournamentIdsList);
                tournamentsList.Add(tournamentPrivateIdsList);
                tournamentsList.Add(tournamentUrlsList);
                tournamentsList.Add(tournamentNamesList);
                tournamentsList.Add(tournamentDatesList);
                tournamentsList.Add(tournamentRoundsCountList);
                tournamentsList.Add(tournamentPlayersCountList);
                tournamentsList.Add(currentTournamentPlayerInfoList);
                tournamentsList.Add(currentTournamentRoundsList);

                List<Tournament> trns = new List<Tournament>();

                foreach (IDomObject years in listOfYearsTd)
                {
                    Console.WriteLine(years.GetAttribute("href"));
                    CQ htmlPageWithListOfTourmamentsForOneYear = GetPage(years.GetAttribute("href")).Result;

                    var htmlTableTournamentsHeaderChildrens = htmlPageWithListOfTourmamentsForOneYear
                                    ["#_ctl0_F7 > div:nth-child(3) > table > tbody > tr.CRg1b"].Children().ToList();

                    var forElementTournamentDateIndexTd = htmlTableTournamentsHeaderChildrens.FindIndex
                        (e => { var a = WebUtility.HtmlDecode(e.InnerText).Contains("с"); return a; }) + 1;
                    var forElementTournamentRoundCountIndexTd = htmlTableTournamentsHeaderChildrens.FindLastIndex
                        (e => { var a = WebUtility.HtmlDecode(e.InnerText).Contains("Тур"); return a; }) + 1;
                    var forElementTournamentPlayerCountIndexTd = htmlTableTournamentsHeaderChildrens.FindIndex
                        (e => { var a = WebUtility.HtmlDecode(e.InnerText).Contains("n"); return a; }) + 1;

                    CQ tournamentDateTd = htmlPageWithListOfTourmamentsForOneYear
                        ["#_ctl0_F7 > div:nth-child(3) > table > tbody > tr.CRg1b ~ tr:nth-child(n) > td:nth-child(" +
                        forElementTournamentDateIndexTd + ")"];
                    CQ tournamentRoundCountTd = htmlPageWithListOfTourmamentsForOneYear
                        ["#_ctl0_F7 > div:nth-child(3) > table > tbody > tr.CRg1b ~ tr:nth-child(n) > td:nth-child(" +
                        forElementTournamentRoundCountIndexTd + ")"];
                    CQ tournamentPlayerCountTd = htmlPageWithListOfTourmamentsForOneYear
                        ["#_ctl0_F7 > div:nth-child(3) > table > tbody > tr.CRg1b ~ tr:nth-child(n) > td:nth-child(" +
                        forElementTournamentPlayerCountIndexTd + ")"];

                    foreach (IDomObject item in tournamentDateTd)
                    {
                        try
                        {
                            var startDate = DateTime.Parse(WebUtility.HtmlDecode(item.InnerText.Trim()), null, DateTimeStyles.AssumeLocal)
                                .ToString("yyyy-MM-ddTHH:mm:sszzz");
                            tournamentDatesList.Add(startDate);
                        }
                        catch
                        {
                            var startDate = item.InnerText;
                            tournamentDatesList.Add(startDate);
                        }

                    }
                    foreach (IDomObject item in tournamentRoundCountTd)
                    {
                        tournamentRoundsCountList.Add(WebUtility.HtmlDecode(item.InnerText.Trim()));
                    }
                    foreach (IDomObject item in tournamentPlayerCountTd)
                    {
                        tournamentPlayersCountList.Add(WebUtility.HtmlDecode(item.InnerText.Trim()));
                    }

                    CQ tournamentUrlAndNameTd = htmlPageWithListOfTourmamentsForOneYear
                        ["#_ctl0_F7 > div:nth-child(3) > table > tbody > tr:nth-child(n+1) > td.CRnowrap > a"];

                    foreach (IDomObject item in tournamentUrlAndNameTd)
                    {
                        ArrayList playerInfoList = new ArrayList();
                        List<List<List<string>>> roundsList = new List<List<List<string>>>();
                        currentTournamentPlayerInfoList.Add(playerInfoList);
                        currentTournamentRoundsList.Add(roundsList);

                        tournamentUrlsList.Add(item.GetAttribute("href"));
                        tournamentIdsList.Add(Math.Abs(tournamentUrlsList[ti].GetHashCode()).ToString("0000000000") + "-chessres");
                        tournamentPrivateIdsList.Add("_" + tournamentIdsList[ti]);
                        tournamentNamesList.Add(WebUtility.HtmlDecode(item.InnerText.Trim()));
                        Console.WriteLine(tournamentUrlsList[ti]);
                        Console.WriteLine(tournamentNamesList[ti]);

                        #region GetCutTournamentPage
                        CQ htmlPageTournamentCut = GetPage(tournamentUrlsList[ti]).Result;
                        CQ htmlPageTournamentFull;
                        try
                        {
                            var viewState = htmlPageTournamentCut["#__VIEWSTATE"][0].GetAttribute("value");
                            var viewStateGenerator = htmlPageTournamentCut["#__VIEWSTATEGENERATOR"][0].GetAttribute("value");
                            var eventValidation = htmlPageTournamentCut["#__EVENTVALIDATION"][0].GetAttribute("value");
                            var paremeters = new Dictionary<string, string> {{"__EVENTTARGET","linkbutton_show"},
                                                                    {"__EVENTARGUMENT",""},{"__VIEWSTATE",viewState},
                                                                    {"__VIEWSTATEGENERATOR",viewStateGenerator},
                                                                    {"__EVENTVALIDATION",eventValidation}};
                            Console.WriteLine("{0}{3}{1}{3}{2}{3}", viewState, viewStateGenerator, eventValidation, Environment.NewLine);
                            htmlPageTournamentFull = GetPage(tournamentUrlsList[ti], paremeters).Result;
                        }
                        catch { htmlPageTournamentFull = htmlPageTournamentCut; }
                        #endregion

                        string playersListUrl = string.Empty;
                        try
                        {
                            playersListUrl = htmlPageTournamentFull
                                ["#_ctl0_F7 > div:nth-child(n) > table > tbody > tr:nth-child(n) > td.CR > a:contains(\"Стартовый список\")"]
                                [0].GetAttribute("href");
                        }
                        catch { playersListUrl = tournamentUrlsList[ti]; Console.WriteLine(playersListUrl); }

                        CQ htmlPlayerList = GetPage(playersListUrl).Result;
                        CQ playerUrlTd;

                        try
                        {
                            playerUrlTd = htmlPlayerList
                                ["#_ctl0_F7 > div:nth-child(2) > table > tbody > tr:nth-child(n+1) > td:nth-child(n) > a.CRdb"];
                        }
                        catch
                        {
                            playerUrlTd = htmlPageTournamentFull
                               ["#_ctl0_F7 > div:nth-child(2) > table > tbody > tr:nth-child(n+1) > td:nth-child(n) > a.CRdb"];
                        }

                        #region GetPlayerInfo
                        var pi = 0;
                        List<string> playerIdsList = new List<string>();
                        List<string> playerUrlsList = new List<string>();
                        List<string> playerNamesList = new List<string>();
                        List<string> playerNumbersList = new List<string>();
                        List<string> playerNationalIdsList = new List<string>();
                        List<string> playerNationalRatingsList = new List<string>();
                        List<string> playerAgesList = new List<string>();
                        List<string> playerSexList = new List<string>();
                        List<string> playerFIDEIdsList = new List<string>();
                        List<string> playerFinalPointsList = new List<string>();

                        playerInfoList.Add(playerIdsList);
                        playerInfoList.Add(playerUrlsList);
                        playerInfoList.Add(playerNamesList);
                        playerInfoList.Add(playerNumbersList);
                        playerInfoList.Add(playerNationalIdsList);
                        playerInfoList.Add(playerNationalRatingsList);
                        playerInfoList.Add(playerAgesList);
                        playerInfoList.Add(playerSexList);
                        playerInfoList.Add(playerFIDEIdsList);
                        playerInfoList.Add(playerFinalPointsList);

                        Player[] plrs = new Player[playerUrlTd.Count()];

                        foreach (IDomObject player in playerUrlTd)
                        {
                            Random rnd = new Random(Guid.NewGuid().ToString().GetHashCode());
                            string[] sex = { "m", "f" };
                            int whatSex = rnd.Next(sex.Length);

                            playerUrlsList.Add(player.GetAttribute("href"));
                            playerSexList.Add(sex[whatSex]);
                            Console.WriteLine("{0}{1}", playerUrlsList[pi], Environment.NewLine);
                            CQ htmlPlayerInfo = GetPage(playerUrlsList[pi]).Result;
                            string playerName = null;
                            string playerNumber = null;
                            string playerNationalRating = null;
                            string playerAge = null;
                            string playerFIDEId = null;
                            string playerFinalPoints = null;

                            try
                            {
                                playerName = WebUtility.HtmlDecode(htmlPlayerInfo
                                    ["#_ctl0_F7 > div:nth-child(n) > table:nth-child(n) > tbody > tr:nth-child(n) > td:contains(\"Имя\") + td"][0].InnerText).Trim();
                            }
                            catch
                            {
                                try
                                {
                                    playerName = WebUtility.HtmlDecode(player.InnerText).Trim();
                                }
                                catch { };
                            }
                            playerNamesList.Add(playerName);

                            try
                            {
                                playerNumber = WebUtility.HtmlDecode(htmlPlayerInfo
                                    ["#_ctl0_F7 > div:nth-child(n) > table:nth-child(n) > tbody > tr:nth-child(n) > td:contains(\"Стартовое место\") + td"][0].InnerText).Trim();
                            }
                            catch { }
                            playerNumbersList.Add(playerNumber);

                            try
                            {
                                playerNationalRating = WebUtility.HtmlDecode(htmlPlayerInfo
                                    ["#_ctl0_F7 > div:nth-child(n) > table:nth-child(n) > tbody > tr:nth-child(n) > td:contains(\"Нац.рейтинг\") + td"][0].InnerText).Trim();
                            }
                            catch { }
                            playerNationalRatingsList.Add(playerNationalRating);

                            try
                            {
                                playerAge = WebUtility.HtmlDecode(htmlPlayerInfo
                                ["#_ctl0_F7 > div:nth-child(n) > table:nth-child(n) > tbody > tr:nth-child(n) > td:contains(\"Год рождения\") + td"][0].InnerText).Trim();
                            }
                            catch { }
                            playerAgesList.Add(playerAge);

                            try
                            {
                                playerFIDEId = WebUtility.HtmlDecode(htmlPlayerInfo //для генерации своего id и проверки уникальности игрока
                                ["#_ctl0_F7 > div:nth-child(n) > table:nth-child(n) > tbody > tr:nth-child(n) > td:contains(\"код FIDE\") + td"][0].InnerText).Trim();
                            }
                            catch { }
                            playerFIDEIdsList.Add(playerFIDEId);

                            try
                            {
                                playerFinalPoints = WebUtility.HtmlDecode(htmlPlayerInfo
                                ["#_ctl0_F7 > div:nth-child(n) > table:nth-child(n) > tbody > tr:nth-child(n) > td:contains(\"Очки\") + td"][0].InnerText).Trim();
                            }
                            catch { }
                            playerFinalPointsList.Add(playerFinalPoints);

                            playerNationalIdsList.Add(Math.Abs((playerFIDEId + playerAge + playerName).GetHashCode()).ToString("0000000000"));
                            playerIdsList.Add(playerNationalIdsList[pi] + "-chessres");

                            Console.WriteLine("{0} {1} {2} {3} {4} {5} {6}", playerName, playerNumber, playerNationalRating, playerAge, playerFIDEId, playerFinalPoints, Environment.NewLine);

                            Player p = new Player();

                            //try { p.playerId = playerIdsList[pi]; }
                            //catch { p.playerId = null; }
                            p.playerId = null;
                            try { p.number = pi + 1; }
                            catch { p.number = null; }
                            try { p.name = playerNamesList[pi]; }
                            catch { p.name = null; }
                            //try { p.nationalId = playerNationalIdsList[pi]; }
                            //catch { p.nationalId = null; }
                            p.nationalId = null;
                            try { p.nationalRating = int.Parse(playerNationalRatingsList[pi], NumberStyles.Integer); }
                            catch { p.nationalId = null; }
                            try { p.age = int.Parse(playerAgesList[pi], NumberStyles.Integer); }
                            catch { p.age = null; }
                            try { p.sex = playerSexList[pi]; }
                            catch { p.sex = null; }

                            plrs[pi] = p;
                            pi++;
                        }
                        #endregion

                        var isRoundsParsed = true;
                        var ri = 0;
                        CQ roundResultUrlTd = htmlPageTournamentFull
                            ["#_ctl0_F7 > div:nth-child(n) > table > tbody > tr:nth-child(n) > td:contains(\"Пары по доскам\") + td.CR > a:contains(\"Тур\")"];

                        Round[] rnds = new Round[roundResultUrlTd.Count()];
                        if (roundResultUrlTd.Count() == 0)
                            isRoundsParsed = false;

                        foreach (IDomObject round in roundResultUrlTd)
                        {
                            var roundResultUrl = round.GetAttribute("href");
                            Console.WriteLine("{0}{1}", roundResultUrl, Environment.NewLine);
                            CQ htmlRoundResult = GetPage(roundResultUrl).Result;
                            List<string> firstPlayersNumbers = new List<string>();
                            List<string> firstPlayersNames = new List<string>();
                            List<string> firstPlayersPoints = new List<string>();
                            List<string> firstPlayersPointsPre = new List<string>();
                            List<string> firstPlayersResult = new List<string>();
                            List<string> roundResults = new List<string>();
                            List<string> secondPlayersResult = new List<string>();
                            List<string> secondPlayersNames = new List<string>();
                            List<string> secondPlayersPoints = new List<string>();
                            List<string> secondPlayersPointsPre = new List<string>();
                            List<string> secondPlayersNumbers = new List<string>();
                            List<List<string>> roundResultList = new List<List<string>>();

                            roundResultList.Add(firstPlayersNumbers);
                            roundResultList.Add(firstPlayersNames);
                            roundResultList.Add(firstPlayersPoints);
                            roundResultList.Add(firstPlayersPointsPre);
                            roundResultList.Add(firstPlayersResult);
                            roundResultList.Add(roundResults);
                            roundResultList.Add(secondPlayersResult);
                            roundResultList.Add(secondPlayersPointsPre);
                            roundResultList.Add(secondPlayersPoints);
                            roundResultList.Add(secondPlayersNames);
                            roundResultList.Add(secondPlayersNumbers);
                            try
                            {
                                var htmlTableHeaderChildrens = htmlRoundResult
                                    ["#_ctl0_F7 > div > h2:contains(\"Пары/Результаты\") ~ table > tbody > tr.CRg1b"].Children().ToList();

                                var forElementFirstPlayerNameIndexTd = htmlTableHeaderChildrens.FindIndex
                                    (e => { var a = WebUtility.HtmlDecode(e.InnerText).Contains("Имя"); return a; }) + 1;
                                var forElementSecondPlayerNameIndexTd = htmlTableHeaderChildrens.FindLastIndex
                                    (e => { var a = WebUtility.HtmlDecode(e.InnerText).Contains("Имя"); return a; }) + 1;
                                var forElementFirstPlayerPointsIndexTd = htmlTableHeaderChildrens.FindIndex
                                    (e => { var a = WebUtility.HtmlDecode(e.InnerText).Contains("Очки"); return a; }) + 1;
                                var forElementSecondPlayerPointsIndexTd = htmlTableHeaderChildrens.FindLastIndex
                                    (e => { var a = WebUtility.HtmlDecode(e.InnerText).Contains("Очки"); return a; }) + 1;
                                var forElementRoundResultIndexTd = htmlTableHeaderChildrens.FindIndex
                                    (e => { var a = WebUtility.HtmlDecode(e.InnerText).Contains("Результат"); return a; }) + 1;

                                Console.WriteLine("{0} {1}{2}{3} {4}{2}{5}", forElementFirstPlayerNameIndexTd, forElementSecondPlayerNameIndexTd
                                    , Environment.NewLine, forElementFirstPlayerPointsIndexTd, forElementSecondPlayerPointsIndexTd
                                    , forElementRoundResultIndexTd);

                                CQ firstPlayersNamesTd = htmlRoundResult
                                    ["#_ctl0_F7 > div > h2:contains(\"Пары/Результаты\") ~ table > tbody > tr:nth-child(n+2) > td:nth-child("
                                    + forElementFirstPlayerNameIndexTd + ")"];
                                CQ secondPlayersNamesTd = htmlRoundResult
                                    ["#_ctl0_F7 > div > h2:contains(\"Пары/Результаты\") ~ table > tbody > tr:nth-child(n+2) > td:nth-child("
                                    + forElementSecondPlayerNameIndexTd + ")"];
                                CQ firsPlayersPointsTd = htmlRoundResult
                                    ["#_ctl0_F7 > div > h2:contains(\"Пары/Результаты\") ~ table > tbody > tr:nth-child(n+2) > td:nth-child("
                                    + forElementFirstPlayerPointsIndexTd + ")"];
                                CQ secondPlayersPointsTd = htmlRoundResult
                                    ["#_ctl0_F7 > div > h2:contains(\"Пары/Результаты\") ~ table > tbody > tr:nth-child(n+2) > td:nth-child("
                                    + forElementSecondPlayerPointsIndexTd + ")"];
                                CQ roundResultsTd = htmlRoundResult
                                    ["#_ctl0_F7 > div > h2:contains(\"Пары/Результаты\") ~ table > tbody > tr:nth-child(n+2) > td:nth-child("
                                    + forElementRoundResultIndexTd + ")"];

                                var replasementPattern = @"(<.*?>)(.*)(</.*>)";

                                try
                                {
                                    foreach (IDomObject firstPlayerName in firstPlayersNamesTd)
                                    {
                                        var name = Regex.Replace(WebUtility.HtmlDecode(firstPlayerName.InnerHTML), replasementPattern, "$2").Trim();
                                        firstPlayersNames.Add(name);
                                        var number = playerNamesList.FindIndex(e => { var a = e.Contains(name); return a; });
                                        if (number == -1)
                                            secondPlayersNumbers.Add(null);
                                        else
                                            firstPlayersNumbers.Add((number + 1).ToString());
                                    }
                                }
                                catch { firstPlayersNames.Add(null); }
                                try
                                {
                                    foreach (IDomObject secondPlayerName in secondPlayersNamesTd)
                                    {
                                        var name = Regex.Replace(WebUtility.HtmlDecode(secondPlayerName.InnerHTML), replasementPattern, "$2").Trim();
                                        secondPlayersNames.Add(name);
                                        var number = playerNamesList.FindIndex(e => { var a = e.Contains(name); return a; });
                                        if (number == -1)
                                            secondPlayersNumbers.Add(null);
                                        else
                                            secondPlayersNumbers.Add((number + 1).ToString());
                                    }
                                }
                                catch { secondPlayersNames.Add(null); }
                                try
                                {
                                    foreach (IDomObject firstPlayerPointsPre in firsPlayersPointsTd)
                                    {
                                        firstPlayersPointsPre.Add(WebUtility.HtmlDecode(normalizePoints(WebUtility.HtmlDecode(firstPlayerPointsPre.InnerText))));
                                    }
                                }
                                catch { firstPlayersPointsPre.Add(null); }
                                try
                                {
                                    foreach (IDomObject secondPlayerPointsPre in secondPlayersPointsTd)
                                    {
                                        secondPlayersPointsPre.Add(WebUtility.HtmlDecode(normalizePoints(WebUtility.HtmlDecode(secondPlayerPointsPre.InnerText))));
                                    }
                                }
                                catch { secondPlayersPointsPre.Add(null); }
                                try
                                {
                                    foreach (IDomObject roundResult in roundResultsTd)
                                    {
                                        var results = WebUtility.HtmlDecode(roundResult.InnerText);
                                        roundResults.Add(results);
                                        var separatedResults = parseResult(results);
                                        firstPlayersResult.Add(separatedResults[0]);
                                        firstPlayersPoints.Add(separatedResults[1]);
                                        secondPlayersResult.Add(separatedResults[2]);
                                        secondPlayersPoints.Add(separatedResults[3]);
                                    }
                                }
                                catch
                                {
                                    roundResults.Add(null);
                                    firstPlayersResult.Add(null);
                                    firstPlayersPoints.Add(null);
                                    secondPlayersResult.Add(null);
                                    secondPlayersPoints.Add(null);
                                }

                                if (firstPlayersNames.All(e => e == null) && secondPlayersNames.All(e => e == null))
                                    isRoundsParsed = false;

                                Board[] brds = new Board[roundResults.Count];
                                List<Result> rslts = new List<Result>();

                                for (var i = 0; i < brds.Count(); i++)
                                {
                                    White w = new White();
                                    Black b = new Black();
                                    Result re1 = new Result();
                                    Result re2 = new Result();
                                    Result re = new Result();

                                    try { w.playerNum = int.Parse(firstPlayersNumbers[i], NumberStyles.Integer); }
                                    catch { w.playerNum = null; };
                                    try { b.playerNum = int.Parse(secondPlayersNumbers[i], NumberStyles.Integer); }
                                    catch { b.playerNum = null; };
                                    try { w.result = firstPlayersResult[i]; }
                                    catch { w.result = null; }
                                    try { b.result = secondPlayersResult[i]; }
                                    catch { b.result = null; }
                                    try { w.resultPoints = Double.Parse(firstPlayersPoints[i], CultureInfo.InvariantCulture); }
                                    catch { w.resultPoints = null; }
                                    try { b.resultPoints = Double.Parse(secondPlayersPoints[i], CultureInfo.InvariantCulture); }
                                    catch { b.resultPoints = null; }

                                    re1.playerNum = w.playerNum;
                                    try { re1.points = w.resultPoints + Double.Parse(firstPlayersPointsPre[i], new NumberFormatInfo() { NumberDecimalSeparator = "," }); }
                                    catch { re1.points = null; }

                                    re2.playerNum = b.playerNum;
                                    try { re2.points = b.resultPoints + Double.Parse(secondPlayersPointsPre[i], new NumberFormatInfo() { NumberDecimalSeparator = "," }); }
                                    catch { re2.points = null; }

                                    double? buchholz1 = 0.0;
                                    double? buchholz2 = 0.0;
                                    double? buchholzCut11 = 0.0;
                                    double? buchholzCut12 = 0.0;
                                    double? winsCount1 = 0;
                                    double? winsCount2 = 0;
                                    List<double?> foes1Points = new List<double?>();
                                    List<double?> foes2Points = new List<double?>();
                                    List<string> foes1 = new List<string>();
                                    List<string> foes2 = new List<string>();

                                    try
                                    {
                                        if (re2.points != 0)
                                            foes1Points.Add(re2.points);
                                        if (re1.points != null)
                                            foes2Points.Add(re1.points);
                                        if (firstPlayersResult[i].Equals("1"))
                                            winsCount1++;
                                        if (secondPlayersResult[i].Equals("1"))
                                            winsCount2++;
                                        foreach (var oneOfPreviousRounds in roundsList)
                                        {
                                            for (var cnt = 0; cnt < roundResults.Count; cnt++)
                                            {
                                                try
                                                {
                                                    if (oneOfPreviousRounds[0][cnt].Equals(re1.playerNum.ToString()))
                                                    {
                                                        if (oneOfPreviousRounds[4][cnt].Equals("1"))
                                                            winsCount1++;
                                                        foes1.Add(oneOfPreviousRounds[10][cnt]);
                                                    }
                                                }
                                                catch { }
                                                try
                                                {
                                                    if (oneOfPreviousRounds[10][cnt].Equals(re1.playerNum.ToString()))
                                                    {
                                                        if (oneOfPreviousRounds[6][cnt].Equals("1"))
                                                            winsCount1++;
                                                        foes1.Add(oneOfPreviousRounds[0][cnt]);
                                                    }
                                                }
                                                catch { }
                                                try
                                                {
                                                    if (oneOfPreviousRounds[0][cnt].Equals(re2.playerNum.ToString()))
                                                    {
                                                        if (oneOfPreviousRounds[4][cnt].Equals("1"))
                                                            winsCount2++;
                                                        foes2.Add(oneOfPreviousRounds[10][cnt]);
                                                    }
                                                }
                                                catch { }
                                                try
                                                {
                                                    if (oneOfPreviousRounds[10][cnt].Equals(re2.playerNum.ToString()))
                                                    {
                                                        if (oneOfPreviousRounds[6][cnt].Equals("1"))
                                                            winsCount2++;
                                                        foes2.Add(oneOfPreviousRounds[0][cnt]);
                                                    }
                                                }
                                                catch { }
                                            }

                                        }

                                        try
                                        {
                                            for (var cnt = 0; cnt < foes1.Count; cnt++)
                                            {
                                                if (foes1.Skip(cnt + 1).Where(e => e == foes1[cnt]).Count() != 0)
                                                {
                                                    foes1.RemoveAt(cnt);
                                                    cnt--;
                                                }
                                            }
                                        }
                                        catch { }

                                        try
                                        {
                                            for (var cnt = 0; cnt < foes2.Count; cnt++)
                                            {
                                                if (foes2.Skip(cnt + 1).Where(e => e == foes2[cnt]).Count() != 0)
                                                {
                                                    foes2.RemoveAt(cnt);
                                                    cnt--;
                                                }
                                            }
                                        }
                                        catch { }

                                        foreach (var foe in foes1)
                                        {
                                            for (var cnt = 0; cnt < roundResults.Count; cnt++)
                                            {
                                                try
                                                {
                                                    var points1 = Double.Parse(roundResultList[3][cnt], new NumberFormatInfo() { NumberDecimalSeparator = "," })
                                                                + Double.Parse(roundResultList[2][cnt], new NumberFormatInfo() { NumberDecimalSeparator = "," });
                                                    var points2 = Double.Parse(roundResultList[7][cnt], new NumberFormatInfo() { NumberDecimalSeparator = "," })
                                                                + Double.Parse(roundResultList[8][cnt], new NumberFormatInfo() { NumberDecimalSeparator = "," });
                                                    if (roundResultList[0][cnt].Equals(foe))
                                                    {
                                                        foes1Points.Add(points1);
                                                    }
                                                    if (roundResultList[10][cnt].Equals(foe))
                                                    {
                                                        foes1Points.Add(points2);
                                                    }
                                                }
                                                catch { }
                                            }
                                        }
                                        buchholz1 = foes1Points.Sum();
                                        try
                                        {
                                            buchholzCut11 = buchholz1 - foes1Points.Where(e => e != 0).Min();
                                        }
                                        catch { }

                                        foreach (var foe in foes2)
                                        {
                                            for (var cnt = 0; cnt < roundResults.Count; cnt++)
                                            {
                                                try
                                                {
                                                    var points1 = Double.Parse(roundResultList[3][cnt], new NumberFormatInfo() { NumberDecimalSeparator = "," })
                                                                + Double.Parse(roundResultList[2][cnt], new NumberFormatInfo() { NumberDecimalSeparator = "," });
                                                    var points2 = Double.Parse(roundResultList[7][cnt], new NumberFormatInfo() { NumberDecimalSeparator = "," })
                                                                + Double.Parse(roundResultList[8][cnt], new NumberFormatInfo() { NumberDecimalSeparator = "," });
                                                    if (roundResultList[0][cnt].Equals(foe))
                                                    {
                                                        foes2Points.Add(points1);
                                                    }
                                                    if (roundResultList[10][cnt].Equals(foe))
                                                    {
                                                        foes2Points.Add(points2);
                                                    }
                                                }
                                                catch { }
                                            }
                                        }
                                        buchholz2 = foes2Points.Sum();
                                        try
                                        {
                                            buchholzCut12 = buchholz2 - foes2Points.Where(e => e != 0).Min();
                                        }
                                        catch { }
                                    }
                                    catch { }

                                    re1.tieBreaks = new double?[] { buchholz1, buchholzCut11, winsCount1 };
                                    re2.tieBreaks = new double?[] { buchholz2, buchholzCut12, winsCount2 };
                                    for (var k=0; k<3;k++)
                                    {
                                        if (re1.tieBreaks[k] == null)
                                            re1.tieBreaks[k] = 0.0;
                                    }
                                    for (var k = 0; k < 3; k++)
                                    {
                                        if (re2.tieBreaks[k] == null)
                                            re2.tieBreaks[k] = 0.0;
                                    }

                                    Board brd = new Board();
                                    try { brd.number = i + 1; }
                                    catch { brd.number = null; }

                                    brd.white = w;
                                    brd.black = b;
                                    if (w.playerNum == null)
                                        brd.white = null;
                                    if (b.playerNum == null)
                                        brd.black = null;

                                    brds[i] = brd;
                                    if (re1.playerNum != null)
                                        rslts.Add(re1);
                                    if (re2.playerNum != null)
                                        rslts.Add(re2);
                                }

                                roundsList.Add(roundResultList);

                                Round r = new Round();
                                try { r.number = ri + 1; }
                                catch { r.number = null; }
                                r.boards = brds;
                                r.results = rslts
                                    .OrderByDescending(e => e.points)
                                    .ThenByDescending(e => e.tieBreaks[0])
                                    .ThenByDescending(e => e.tieBreaks[1])
                                    .ThenByDescending(e => e.tieBreaks[2]).ToList();

                                rnds[ri] = r;
                                ri++;
                            }
                            catch { }
                        }

                        if (isRoundsParsed == true)
                        {
                            Tournament trn = new Tournament();

                            //try { trn._id = tournamentIdsList[ti]; }
                            //catch { trn._id = null; }
                            try { trn.privateId = tournamentPrivateIdsList[ti]; }
                            catch { trn.privateId = null; }
                            try { trn.name = tournamentNamesList[ti]; }
                            catch { trn.name = null; }
                            try { trn.startDate = tournamentDatesList[ti]; }
                            catch { trn.startDate = null; }
                            try { trn.roundsCount = int.Parse(tournamentRoundsCountList[ti], NumberStyles.Integer); }
                            catch { trn.roundsCount = null; }
                            trn.tieBreaks = new string[] { "buchholz", "buchholzCut1", "winsCount" };

                            trn.players = plrs;
                            trn.rounds = rnds;

                            trns.Add(trn);
                            StreamWriter trnFile = new StreamWriter(Path.Combine(tournamentsFolder, (ti + 1) + "-tournament.json"));
                            trnFile.Write(JsonConvert.SerializeObject(trn, Newtonsoft.Json.Formatting.Indented));
                            trnFile.Close();
                        }

                        ti++;
                    }
                }

                file.Write(JsonConvert.SerializeObject(trns, Newtonsoft.Json.Formatting.Indented));
                file.Close();
            }
            catch { }
        }

        static async Task<CQ> GetPage(string url)
        {
            string page = url;
            string resultString = string.Empty;

            HttpClientHandler handler = new HttpClientHandler();
            HttpClient client = new HttpClient(handler);
            client.BaseAddress = new Uri("http://chess-results.com/");
            try
            {
                HttpResponseMessage response = await client.GetAsync(page);
                HttpContent content = response.Content;

                resultString = await content.ReadAsStringAsync();
            }
            catch { }

            CQ html = CQ.Create(resultString);


            return html;
        }

        static async Task<CQ> GetPage(string url, Dictionary<string, string> parameters)
        {
            string page = url;
            string resultString = string.Empty;
            var encodedContent = new FormUrlEncodedContent(parameters);

            HttpClientHandler handler = new HttpClientHandler();
            HttpClient client = new HttpClient(handler);
            client.BaseAddress = new Uri("http://chess-results.com/");
            try
            {
                HttpResponseMessage response = await client.PostAsync(url, encodedContent).ConfigureAwait(false);
                HttpContent content = response.Content;

                resultString = await content.ReadAsStringAsync();
            }
            catch { }

            CQ html = CQ.Create(resultString);

            return html;
        }

        static string normalizePoints(string inputString)
        {
            double value;
            if (inputString == string.Empty)
                return inputString;
            else
            {
                if (inputString[inputString.Length - 1] == '½')
                {
                    value = int.Parse(inputString.Substring(0, inputString.Length - 1), NumberStyles.Integer) + 0.5;
                }
                else
                    value = int.Parse(inputString, NumberStyles.Integer);
                return value.ToString();
            }
        }

        static string[] parseResult(string inputString)
        {
            string[] playersResult = new string[4];
            switch (inputString)
            {
                case "0":
                    playersResult[0] = "Z";
                    playersResult[1] = "0";
                    playersResult[2] = null;
                    playersResult[3] = null;
                    break;
                case "1":
                    playersResult[0] = "U";
                    playersResult[1] = "1";
                    playersResult[2] = null;
                    playersResult[3] = null;
                    break;
                case "½":
                    playersResult[0] = "H";
                    playersResult[1] = "0.5";
                    playersResult[2] = null;
                    playersResult[3] = null;
                    break;
                case "":
                    playersResult[0] = null;
                    playersResult[1] = null;
                    playersResult[2] = null;
                    playersResult[3] = null;
                    break;
                case "½ - ½":
                    playersResult[0] = "=";
                    playersResult[1] = "0.5";
                    playersResult[2] = "=";
                    playersResult[3] = "0.5";
                    break;
                case "1 - 0":
                    playersResult[0] = "1";
                    playersResult[1] = "1";
                    playersResult[2] = "0";
                    playersResult[3] = "0";
                    break;
                case "0 - 1":
                    playersResult[0] = "0";
                    playersResult[1] = "0";
                    playersResult[2] = "1";
                    playersResult[3] = "1";
                    break;
                case "0 - 0":
                    playersResult[0] = "D";
                    playersResult[1] = "0";
                    playersResult[2] = "D";
                    playersResult[3] = "0";
                    break;
                case "- - +":
                    playersResult[0] = "-";
                    playersResult[1] = "0";
                    playersResult[2] = "+";
                    playersResult[3] = "1";
                    break;
                case "+ - -":
                    playersResult[0] = "+";
                    playersResult[1] = "1";
                    playersResult[2] = "-";
                    playersResult[3] = "0";
                    break;
                //case "+ - +":
                //    playersResult[0] = "W";
                //    playersResult[1] = "0";
                //    playersResult[2] = "W";
                //    playersResult[3] = "0";
                //    break;
                //case "- - -":
                //    playersResult[0] = "L";
                //    playersResult[1] = "0";
                //    playersResult[2] = "L";
                //    playersResult[3] = "0";
                //    break;
                default:
                    playersResult[0] = "W";
                    playersResult[1] = "0";
                    playersResult[2] = "L";
                    playersResult[3] = "0";
                    break;
            }
            return playersResult;
        }
    }
}

