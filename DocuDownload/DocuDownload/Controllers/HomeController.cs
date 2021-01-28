using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DocuDownload.Models;
using DocuWare.Platform.ServerClient;
using Newtonsoft.Json;

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
        public IActionResult Connect(string docuwareURL = "http://localhost/docuware/platform", string login = "admin", string password = "admin")
        {
            if (Functions.GetConnection(docuwareURL, login, password) == null)
                return NotFound();
            else
                return Ok();
        }

        [HttpGet]
        public IActionResult DownloadZip(string docuwareURI = "http://localhost/docuware/platform",
                                         string login = "admin",
                                         string password = "admin",
                                         string fcName = "Blandine company",
                                         string dialogName = "ZipDownload")
        {
            string zipName = fcName + " - " + DateTimeOffset.Now.Date.ToString("dd.MM.yyyy") + " - Downloaded by " + login + ".zip";

            Uri uri = new Uri(docuwareURI);
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
            string directory = @"..\DocuDownload\Extractions\" + extraction.UserLogin + @"\";
            if (!System.IO.Directory.Exists(directory))
                System.IO.Directory.CreateDirectory(directory);
            System.IO.File.WriteAllText(directory + extraction.Name + ".json", JsonConvert.SerializeObject(extraction));

            return Ok();
        }

        [HttpGet]
        public IActionResult GetExtraction(string user, string name)
        {
            string file = @"..\DocuDownload\Extractions\" + user + @"\" + name + ".json";

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
        public IActionResult DeleteExtraction(string user, string name)
        {
            string file = @"..\DocuDownload\Extractions\" + user + @"\" + name + ".json";

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
