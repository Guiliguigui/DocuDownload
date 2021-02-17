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
        public IActionResult Connect(string docuwareURL = "http://localhost/docuware/platform", 
                                         string login = "admin", 
                                         string password = "admin")
        {
            if (Functions.GetConnection(docuwareURL, login, password) == null)
                return NotFound();
            //save session ?
            return Ok();
        }

        [HttpGet]
        public IActionResult Organizations(string docuwareURI = "http://localhost/docuware/platform",
                                         string login = "admin",
                                         string password = "admin")
        {
            return Json(Functions.GetAllOrganizationNames(Functions.GetConnection(docuwareURI, login, password)));
        }

        [HttpGet]
        public IActionResult FileCabinets(string docuwareURI = "http://localhost/docuware/platform",
                                         string login = "admin",
                                         string password = "admin",
                                         string organization = null)
        {
            ServiceConnection conn = Functions.GetConnection(docuwareURI, login, password);
            Organization org = Functions.GetOrganizationByName(conn, organization) ?? conn.Organizations[0];
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
            Organization org = Functions.GetOrganizationByName(conn, organization) ?? conn.Organizations[0];
            FileCabinet fc = Functions.GetFileCabinetByName(org, filecabinet);
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
            Organization org = Functions.GetOrganizationByName(conn, organization) ?? conn.Organizations[0];
            FileCabinet fc = Functions.GetFileCabinetByName(org, filecabinet);
            Dialog dia = Functions.GetDialogByName(fc, dialog);
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

            Uri uri = new Uri(extraction.DocuwareURI);
            ServiceConnection conn = ServiceConnection.Create(uri, extraction.UserLogin, extraction.UserPassword);

            Organization org = Functions.GetOrganizationByName(conn, extraction.Organization) ?? conn.Organizations[0];

            FileCabinet fileCabinet = Functions.GetFileCabinetByName(org, extraction.FileCabinetName);

            Dialog dialog = Functions.GetDialogByName(fileCabinet, extraction.DialogName);

            List<Document> documents = Functions.SearchDocuments(dialog, extraction.Fields);
            if (documents.Count == 0)
                return NotFound();

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

            return Ok();
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
                return NotFound();
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
                return NotFound();
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

                return Ok();
            }
            else
            {
                return NotFound();
            }
        }
    }
}
