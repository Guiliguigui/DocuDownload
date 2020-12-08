using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DocuWare.Platform.ServerClient;

namespace DocuDownload.Controllers
{
    [Route("api/zip")]
    [ApiController]
    public class ZipDownloadController : ControllerBase
    {
        [HttpGet()]
        public IActionResult DownloadZip()
        {
            string login = "admin";
            string password = "admin";
            string fcName = "Blandine company";
            string dialogName = "ZipDownload";
            string zipName = DateTimeOffset.Now.Date.ToString("dd.MM.yyy HH.mm") + " - Downloaded by " + login + ".zip";

            Uri uri = new Uri("http://localhost/docuware/platform");
            ServiceConnection conn = ServiceConnection.Create(uri, login, password);

            Organization org = conn.Organizations[0];
            List<FileCabinet> fileCabinets = org.GetFileCabinetsFromFilecabinetsRelation().FileCabinet;
            FileCabinet fileCabinet = Functions.GetFileCabinetByName(fileCabinets, fcName);

            DialogInfos dialogInfoItems = fileCabinet.GetDialogInfosFromSearchesRelation();
            Dialog dialog = Functions.GetDialogByName(dialogInfoItems, dialogName);

            Console.WriteLine("Dialog: " + dialog.GetDialogFromSelfRelation().DisplayName);
            //List<string> fieldList = Examples.GetVisiblesFieldsNames(dialog);

            List<Document> documents = Functions.SearchDocuments(dialog);

            var zipStream = Functions.GetZipStream(conn, documents);

            return File(zipStream, "application/octet-stream");
        }
    }
}