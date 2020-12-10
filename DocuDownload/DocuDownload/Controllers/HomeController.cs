using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DocuDownload.Models;
using DocuWare.Platform.ServerClient;

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

        public IActionResult DownloadZip(string docuwareURL = "http://localhost/docuware/platform",
                                         string login = "admin",
                                         string password = "admin",
                                         string fcName = "Blandine company",
                                         string dialogName = "ZipDownload")
        {
            string zipName = fcName + " - " + DateTimeOffset.Now.Date.ToString("dd.MM.yyy") + " - Downloaded by " + login + ".zip";

            Uri uri = new Uri(docuwareURL);
            ServiceConnection conn = ServiceConnection.Create(uri, login, password);

            Organization org = conn.Organizations[0];
            List<FileCabinet> fileCabinets = org.GetFileCabinetsFromFilecabinetsRelation().FileCabinet;
            FileCabinet fileCabinet = Functions.GetFileCabinetByName(fileCabinets, fcName);

            DialogInfos dialogInfoItems = fileCabinet.GetDialogInfosFromSearchesRelation();
            Dialog dialog = Functions.GetDialogByName(dialogInfoItems, dialogName);
            
            List<Document> documents = Functions.SearchDocuments(dialog);

            var zipStream = Functions.GetZipStream(conn, documents);

            return File(zipStream, "application/octet-stream", zipName);
        }
    }
}
