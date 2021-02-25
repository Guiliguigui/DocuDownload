using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DocuDownload.Models;
using DocuWare.Platform.ServerClient;
using Newtonsoft.Json;
using System.IO;
using System.Linq;

namespace DocuDownload.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Application Web pour télécharger une archive .zip contenant un ensemble de documents provenants d'une armoire Docuware.";

            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public IActionResult TryConnection(string docuwareURL = "http://localhost/docuware/platform", 
                                         string login = "admin", 
                                         string password = "admin")
        {
            if (Functions.GetConnection(docuwareURL, login, password) == null)
                return NotFound("Invalid credentials or URI.");
            return Ok("Connection is valid.");
        }

        [HttpGet]
        public IActionResult Organizations(string docuwareURI = "http://localhost/docuware/platform",
                                         string login = "admin",
                                         string password = "admin")
        {
            ServiceConnection conn = Functions.GetConnection(docuwareURI, login, password);
            if (conn == null) return NotFound("Invalid credentials or URI.");

            return Json(Functions.GetAllOrganizationNames(conn));
        }

        [HttpGet]
        public IActionResult FileCabinets(string docuwareURI = "http://localhost/docuware/platform",
                                         string login = "admin",
                                         string password = "admin",
                                         string organization = null)
        {
            ServiceConnection conn = Functions.GetConnection(docuwareURI, login, password);
            if (conn == null) return NotFound("Invalid credentials or URI.");

            Organization org;
            if(organization != null)
            {
                org = Functions.GetOrganizationByName(conn, organization);
                if (org == null) return NotFound("Organization not found.");
            }
            else
                org = conn.Organizations[0];

            return Json(Functions.GetAllFileCabinetNames(org));
        }

        [HttpGet]
        public IActionResult Dialogs(string docuwareURI = "http://localhost/docuware/platform",
                                         string login = "admin",
                                         string password = "admin",
                                         string organization = null,
                                         string filecabinet = "Blandine company")
        {
            ServiceConnection conn = Functions.GetConnection(docuwareURI, login, password);
            if (conn == null) return NotFound("Invalid credentials or URI.");

            Organization org;
            if (organization != null)
            {
                org = Functions.GetOrganizationByName(conn, organization);
                if (org == null) return NotFound("Organization not found.");
            }
            else
                org = conn.Organizations[0];

            FileCabinet fc = Functions.GetFileCabinetByName(org, filecabinet);
            if (fc == null) return NotFound("FileCabinet not found.");

            return Json(Functions.GetAllDialogNames(fc));
        }

        [HttpGet]
        public IActionResult Fields(string docuwareURI = "http://localhost/docuware/platform",
                                         string login = "admin",
                                         string password = "admin",
                                         string organization = null,
                                         string filecabinet = "Blandine company",
                                         string dialog = "ZipDownload")
        {
            ServiceConnection conn = Functions.GetConnection(docuwareURI, login, password);
            if (conn == null) return NotFound("Invalid credentials or URI.");

            Organization org;
            if (organization != null)
            {
                org = Functions.GetOrganizationByName(conn, organization);
                if (org == null) return NotFound("Organization not found.");
            }
            else
                org = conn.Organizations[0];

            FileCabinet fc = Functions.GetFileCabinetByName(org, filecabinet);
            if (fc == null) return NotFound("FileCabinet not found.");

            Dialog dia = Functions.GetDialogByName(fc, dialog);
            if (dia == null) return NotFound("Dialog not found.");

            return Json(Functions.GetVisiblesFieldsInfos(dia));
        }

        [HttpGet]
        public IActionResult DownloadZip(string docuwareURL = "http://localhost/docuware/platform",         //fonction de test de téléchargement
                                         string login = "admin",
                                         string password = "admin",
                                         string fcName = "Blandine company",
                                         string dialogName = "ZipDownload")
        {
            string zipName = fcName + " - " + DateTimeOffset.Now.Date.ToString("dd.MM.yyyy") + " - Downloaded by " + login + ".zip";

            Uri uri = new Uri(docuwareURL);
            ServiceConnection conn = ServiceConnection.Create(uri, login, password);

            Organization org = conn.Organizations[0];

            FileCabinet fileCabinet = Functions.GetFileCabinetByName(org, fcName);
            
            Dialog dialog = Functions.GetDialogByName(fileCabinet, dialogName);
            
            List<Document> documents = Functions.SearchDocuments(dialog);

            List<string> hierarchy = new List<string>()
            {
                "CHAPITRE",
                "SOUS_CHAPITRE",
                "TYPE_DOCUMENT"
            };

            var zipStream = Functions.GetZipStream(documents, hierarchy);

            return File(zipStream, "application/octet-stream", zipName);
        }

        [HttpGet]
        public IActionResult DownloadZipExtraction([FromBody] Extraction extraction)
        {
            string zipName = extraction.FileCabinetName + " - " + DateTimeOffset.Now.Date.ToString("dd.MM.yyyy") + " - Downloaded by " + extraction.UserLogin + ".zip";

            ServiceConnection conn = Functions.GetConnection(extraction.DocuwareURI, extraction.UserLogin, extraction.UserPassword);
            if (conn == null) return NotFound("Invalid credentials or URI.");

            Organization org;
            if (extraction.Organization != null)
            {
                org = Functions.GetOrganizationByName(conn, extraction.Organization);
                if (org == null) return NotFound("Organization not found.");
            }
            else
                org = conn.Organizations[0];

            FileCabinet fc = Functions.GetFileCabinetByName(org, extraction.FileCabinetName);
            if (fc == null) return NotFound("FileCabinet not found.");

            Dialog dia = Functions.GetDialogByName(fc, extraction.DialogName);
            if (dia == null) return NotFound("Dialog not found.");

            List<string> fieldsNames = Functions.GetVisiblesFieldsInfos(dia).Select(p => p.Key).ToList();
            List<string> exFieldsNames = extraction.Fields.Select(p => p.Key).ToList();
            List<string> differences= exFieldsNames.Where(m => !fieldsNames.Contains(m)).ToList();
            if(differences.Count > 0)
            {
                return NotFound("Field(s) not matching : \n" + String.Join("\n", differences));
            }
            
            List<Document> documents = Functions.SearchDocuments(dia, extraction.Fields);
            if (documents.Count == 0)
                return NotFound("There is none documents that match this research.");

            var zipStream = Functions.GetZipStream(documents, extraction.Hierarchy);

            return File(zipStream, "application/octet-stream", zipName);
        }

        [HttpPost]
        public IActionResult SaveExtraction([FromBody] Extraction extraction)
        {
            extraction.UserPassword = null;
            string directory = @"..\DocuDownload\Extractions\" + extraction.UserLogin + @"\";
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            System.IO.File.WriteAllText(directory + extraction.Name + ".json", JsonConvert.SerializeObject(extraction));

            return Ok("Extraction saved.");
        }

        [HttpGet]
        public IActionResult GetUserExtractions(string user)
        {
            string directory = @"..\DocuDownload\Extractions\" + user + @"\";


            if (Directory.Exists(directory))
            {
                return Json(Directory.GetFiles(directory).Select(p => Path.GetFileNameWithoutExtension(p)).ToList());
            }
            else
            {
                return NotFound("Extraction not Found.");
            }
        }

        [HttpGet]
        public IActionResult GetExtraction(string user, string extractionName)
        {
            string file = @"..\DocuDownload\Extractions\" + user + @"\" + extractionName + ".json";

            if(System.IO.File.Exists(file))
            {
                Extraction extraction = JsonConvert.DeserializeObject<Extraction>(System.IO.File.ReadAllText(file));
                return Json(extraction);
            }
            else
            {
                return NotFound("Extraction not Found.");
            }
        }

        [HttpDelete]
        public IActionResult DeleteExtraction(string user, string extractionName)
        {
            string file = @"..\DocuDownload\Extractions\" + user + @"\" + extractionName + ".json";

            if (System.IO.File.Exists(file))
            {
                System.IO.File.Delete(file);

                string directory = System.IO.Path.GetDirectoryName(file);
                if (System.IO.Directory.GetFiles(directory).Length == 0)
                    System.IO.Directory.Delete(directory);

                return Ok("Extraction deleted.");
            }
            else
            {
                return NotFound("Extraction not Found.");
            }
        }
    }
}
