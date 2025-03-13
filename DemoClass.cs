using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamsACSFunctions
{

 



    public class Toneinfo
    {
        public string odatatype { get; set; }
        public int sequenceId { get; set; }
        public string tone { get; set; }
    }

    public class CallData
    {
        public string odatatype { get; set; }
        public Value[] value { get; set; }
    }

    public class Value
    {
        public string odatatype { get; set; }
        public string changeType { get; set; }
        public string resource { get; set; }
        public string resourceUrl { get; set; }
        public Resourcedata resourceData { get; set; }
    }

    public class Resourcedata
    {
        public string odatatype { get; set; }
        public string state { get; set; }
        public Resultinfo resultInfo { get; set; }
        public Meetingcapability meetingCapability { get; set; }
        public Meetingproperties meetingProperties { get; set; }
        public object[] coOrganizers { get; set; }
        public string callChainId { get; set; }
        public Terminationsender terminationSender { get; set; }
        public Toneinfo toneInfo { get; set; }
    }

    public class Resultinfo
    {
        public string odatatype { get; set; }
        public int code { get; set; }
        public int subcode { get; set; }
        public string message { get; set; }
    }

    public class Meetingcapability
    {
        public string odatatype { get; set; }
        public bool allowTranslatedCaptions { get; set; }
        public bool allowTranslatedTranscriptions { get; set; }
        public string[] recorderAllowed { get; set; }
    }

    public class Meetingproperties
    {
        public string odatatype { get; set; }
        public string meetingLabel { get; set; }
    }

    public class Terminationsender
    {
        public string odatatype { get; set; }
        public Phone phone { get; set; }
    }

    public class Phone
    {
        public string odatatype { get; set; }
        public string id { get; set; }
        public string displayName { get; set; }
        public string identityProvider { get; set; }
    }

    internal class DemoClass
    {
    }
}
