using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DocuDownload.Models
{
    public class Extraction
    {
        public Extraction(string name, 
                          string docuwareURI, 
                          string userLogin, 
                          string userPassword, 
                          string organization, 
                          string fileCabinetName, 
                          string dialogName, 
                          List<string> hierarchy, 
                          Dictionary<string, string> fields)
        {
            Name = name;
            DocuwareURI = docuwareURI;
            UserLogin = userLogin;
            UserPassword = userPassword;
            Organization = organization;
            FileCabinetName = fileCabinetName;
            DialogName = dialogName;
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
        public string FileCabinetName { get; set; }
        public string DialogName { get; set; }
        public List<string> Hierarchy { get; set; }
        public Dictionary<string, string> Fields { get; set; }
    }
}
