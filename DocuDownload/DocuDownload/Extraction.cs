using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DocuDownload
{
    public class Extraction
    {
        public Extraction(string name, 
                          string docuwareURI, 
                          string userLogin, 
                          string userPassword, 
                          string organization, 
                          string fileCabinet, 
                          string dialog, 
                          List<string> hierarchy, 
                          Dictionary<string, string> fields)
        {
            Name = name;
            DocuwareURI = docuwareURI;
            UserLogin = userLogin;
            UserPassword = userPassword;
            Organization = organization;
            FileCabinet = fileCabinet;
            Dialog = dialog;
            Hierarchy = hierarchy;
            Fields = fields;
        }

        public Extraction()
        {
        }

        public string Name { get; set; }
        public string DocuwareURI { get; set; }
        public string UserLogin { get; set; }
        public string UserPassword { get; set; }
        public string Organization { get; set; }
        public string FileCabinet { get; set; }
        public string Dialog { get; set; }
        public List<string> Hierarchy { get; set; }
        public Dictionary<string, string> Fields { get; set; }
    }
}
