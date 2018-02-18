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

namespace chess_res_generator
{
    class Tournament
    {
        //public string _id { get; set; }
        public string privateId { get; set; }
        public string name { get; set; }
        public string startDate { get; set; }
        public int? roundsCount { get; set; }
        public string[] tieBreaks { get; set; }
        public Player[] players { get; set; }
        public Round[] rounds { get; set; }
    }

    class Player
    {
        public string playerId { get; set; }
        public int? number { get; set; }
        public string name { get; set; }
        public string nationalId { get; set; }
        public int? nationalRating { get; set; }
        public int? age { get; set; }
        public string sex { get; set; }
        public string region { get; set; }
        public string club { get; set; }
    }

    class Round
    {
        public int? number { get; set; }
        public Board[] boards { get; set; }
        public List<Result> results { get; set; }
    }

    class Board
    {
        public int? number { get; set; }
        public White white { get; set; }
        public Black black { get; set; }
    }

    class White
    {
        public string playerId { get; set; }
        public string result { get; set; }
        public double? resultPoints { get; set; }
    }

    class Black
    {
        public string playerId { get; set; }
        public string result { get; set; }
        public double? resultPoints { get; set; }
    }

    class Result
    {
        public string playerId { get; set; }
        public double? points { get; set; }
        public double?[] tieBreaks { get; set; }
    }
}
