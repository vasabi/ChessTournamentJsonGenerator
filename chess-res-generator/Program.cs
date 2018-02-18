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
    class Program
    {
        static void Main(string[] args)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            string resultFolder = Path.Combine(baseDir, @"Result-" + DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss"));
            if (!Directory.Exists(resultFolder))
                resultFolder = Directory.CreateDirectory(resultFolder).FullName;

            string tournamentsFolder = Path.Combine(resultFolder, @"Tournaments_separated");
            if (!Directory.Exists(tournamentsFolder))
                tournamentsFolder = Directory.CreateDirectory(tournamentsFolder).FullName;

            StreamWriter outputBigFile = new StreamWriter(Path.Combine(resultFolder, "tournamentsMongo.json"));

            Console.WriteLine("1 - Вытащить турниры с ресурса, 2 - Собрать большой джейсон из маленьких");
            var cs = Console.ReadLine();

            switch (cs)
            {
                case "1":
                    Crawler cr = new Crawler();
                    cr.GetAllTournaments(outputBigFile, tournamentsFolder);
                    break;
                case "2":
                    var j = 1;
                    var trns = Directory.GetFiles(tournamentsFolder, "*.json", SearchOption.AllDirectories);
                    outputBigFile.WriteLine("[");
                    foreach (var file in trns)
                    {
                        var i = 1;
                        var content = File.ReadAllLines(file);
                        foreach (var line in content)
                        {
                            if (i == content.Length && j!=trns.Length)
                                outputBigFile.WriteLine("  " + line + ",");
                            else
                                outputBigFile.WriteLine("  " + line);
                            i++;
                        }
                        j++;
                    }
                    outputBigFile.WriteLine("]");
                    outputBigFile.Close();
                    break;
            }

            Console.WriteLine("Thats All Folks");
            Console.ReadKey();
        }
    }
}
