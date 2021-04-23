using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
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

        public IActionResult Extraction()
        {
            return View();
        }
        
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public IActionResult _LoginPartial(string docuwareURI, string userLogin, string userPassword)
        {
            if (Functions.GetConnection(docuwareURI, userLogin, userPassword) != null)
            {
                HttpContext.Session.SetString("isConnected", "true");
                HttpContext.Session.SetString("docuwareURI", docuwareURI);
                HttpContext.Session.SetString("userLogin", userLogin);
                HttpContext.Session.SetString("userPassword", userPassword);
            }
            else
            {
                HttpContext.Session.SetString("isConnected", "false");
                HttpContext.Session.SetString("docuwareURI", "");
                HttpContext.Session.SetString("userLogin", "");
                HttpContext.Session.SetString("userPassword", "");
            }

            return PartialView("_LoginPartial");
        }

        [HttpPost]
        public IActionResult _FormPartial()
        {
            if (HttpContext.Session.GetString("isConnected") == "true")
                return PartialView("_FormPartial");
            else
                return Ok("User not connected");
        }

        [HttpPost]
        public IActionResult DocuWareInfos(string docuwareURI, string userLogin, string userPassword)
        {
            var docuwareInfos = Functions.GetDocuWareInfos(docuwareURI, userLogin, userPassword);
            if (docuwareInfos == null) return Ok("Invalid credentials or URI.");

            return Json(docuwareInfos);
        }

        [HttpGet]
        public IActionResult DownloadZip(string docuwareURI = "http://localhost/docuware/platform",         //fonction de test de téléchargement
                                         string userLogin = "admin",
                                         string userPassword = "admin",
                                         string fileCabinet = "Blandine company",
                                         string dialog = "ZipDownload")
        {
            string zipName = fileCabinet + " - " + DateTimeOffset.Now.Date.ToString("dd.MM.yyyy") + " - Downloaded by " + userLogin + ".zip";

            Uri uri = new Uri(docuwareURI);
            ServiceConnection conn = ServiceConnection.Create(uri, userLogin, userPassword);

            Organization org = conn.Organizations[0];

            FileCabinet fc = Functions.GetFileCabinetByName(org, fileCabinet);
            
            Dialog dia = Functions.GetDialogByName(fc, dialog);
            
            List<Document> documents = Functions.SearchDocuments(dia);

            List<string> hierarchy = new List<string>()
            {
                "CHAPITRE",
                "SOUS_CHAPITRE",
                "TYPE_DOCUMENT"
            };

            var zipStream = Functions.GetZipStream(documents, hierarchy);

            return File(zipStream, "application/octet-stream", zipName);
        }

        [HttpPost]
        public IActionResult DownloadZipExtraction(Extraction extraction)
        {
            if (!ModelState.IsValid)
                return BadRequest("Extraction Format Invalid");

            string zipName = extraction.FileCabinet + " - " + DateTimeOffset.Now.Date.ToString("dd.MM.yyyy") + " - Downloaded by " + extraction.UserLogin + ".zip";

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

            FileCabinet fc = Functions.GetFileCabinetByName(org, extraction.FileCabinet);
            if (fc == null) return NotFound("FileCabinet not found.");

            Dialog dia = Functions.GetDialogByName(fc, extraction.Dialog);
            if (dia == null) return NotFound("Dialog not found.");

            List<string> fieldsNames = Functions.GetVisiblesFieldsInfos(dia).Select(p => p.Key).ToList();
            List<string> exFieldsNames = extraction.Fields.Select(p => p.Key).ToList();
            List<string> differences= exFieldsNames.Where(m => !fieldsNames.Contains(m)).ToList();
            if(differences.Count > 0)
            {
                return NotFound("Field(s) not matching : \n" + string.Join("\n", differences));
            }
            
            List<Document> documents = Functions.SearchDocuments(dia, extraction.Fields);
            if (documents.Count == 0)
                return NotFound("There is no documents that match this search.");

            var zipStream = Functions.GetZipStream(documents, extraction.Hierarchy);

            return File(zipStream, "application/octet-stream", zipName);
        }

        [HttpPost]
        public IActionResult SaveExtraction(Extraction extraction)
        {
            extraction.UserPassword = null;
            string directory = @"..\DocuDownload\Extractions\" + extraction.UserLogin + @"\";
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            System.IO.File.WriteAllText(directory + extraction.Name + ".json", JsonConvert.SerializeObject(extraction));

            return Ok("Extraction saved.");
        }

        [HttpPost]
        public IActionResult GetUserExtractions(string userLogin)
        {
            string directory = @"..\DocuDownload\Extractions\" + userLogin + @"\";


            if (Directory.Exists(directory))
            {
                return Json(Directory.GetFiles(directory).Select(p => Path.GetFileNameWithoutExtension(p)).ToList());
            }
            else
            {
                return NotFound("Extraction not Found.");
            }
        }

        [HttpPost]
        public IActionResult GetExtraction(string userLogin, string Name)
        {
            string file = @"..\DocuDownload\Extractions\" + userLogin + @"\" + Name + ".json";

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
        public IActionResult DeleteExtraction(string userLogin, string Name)
        {
            string file = @"..\DocuDownload\Extractions\" + userLogin + @"\" + Name + ".json";

            if (System.IO.File.Exists(file))
            {
                System.IO.File.Delete(file);

                string directory = Path.GetDirectoryName(file);
                if (Directory.GetFiles(directory).Length == 0)
                    Directory.Delete(directory);

                return Ok("Extraction deleted.");
            }
            else
            {
                return NotFound("Extraction not Found.");
            }
        }
    }
}
