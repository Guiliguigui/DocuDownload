using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocuWare.Platform.ServerClient;
using DocuWare.Services.Http;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace DocuDownload
{
    public static class Functions
    {
        public static List<string> GetAllOrganizationNames(ServiceConnection connection)
        {
            return connection.Organizations.Select(p => p.Name).ToList();
        }

        public static List<string> GetAllFileCabinetNames(Organization organization)
        {
            return organization.GetFileCabinetsFromFilecabinetsRelation().FileCabinet.Select(p => p.Name).ToList();
        }

        public static List<string> GetAllDialogNames(FileCabinet fileCabinet)
        {
            return fileCabinet.GetDialogInfosFromSearchesRelation().Dialog.Select(p => p.GetDialogFromSelfRelation().DisplayName).ToList();
        }
        
        public static FileCabinet GetFileCabinetByName(List<FileCabinet> fileCabinets, string fcName)
        {
            FileCabinet fileCabinet = fileCabinets.Where(i => i.Name == fcName).FirstOrDefault();
            if (fileCabinet == null)
                throw new Exception("File cabinet not found.");
            return fileCabinet;
        }

        public static Dialog GetDialogByName(DialogInfos dialogInfos, string dialogName)
        {
            Dialog dialog = null;
            foreach (var dia in dialogInfos.Dialog)
                if (dia.GetDialogFromSelfRelation().DisplayName == dialogName)
                    dialog = dia.GetDialogFromSelfRelation();
            if (dialog == null)
                throw new Exception("Dialog not found.");
            return dialog;
        }

        public static List<string> GetVisiblesFieldsNames(Dialog dialog) //à modifier pour avoir ne nom dans la db, le type et le label de chaque champ
        {
            List<string> fieldsNames = dialog.Fields.Where(p => p.Visible).Select(p => p.DlgLabel.ToString()).ToList();
            //foreach (var field in dialog.Fields)
            //{
            //    if (field.Visible)
            //        fieldsNames.Add(field.DlgLabel.ToString());
            //        //fieldsNames.Add(field.DBFieldName.ToString());
            //        //fieldsNames.Add(field.DWFieldType.ToString());
            //}
            return fieldsNames;
        }

        public static List<Document> SearchDocuments(Dialog dialog, Dictionary<string, string> conditions = null, DialogExpressionOperation operation = DialogExpressionOperation.And, int count = 1000000, SortDirection sortDirection = SortDirection.Desc)
        {
            if (conditions == null)
                conditions = new Dictionary<string, string>();

            var q = new DialogExpression()
            {
                Operation = operation,
                Condition = new List<DialogExpressionCondition>(),
                Count = count,
                SortOrder = new List<SortedField>
                {
                    SortedField.Create("DWSTOREDATETIME", sortDirection)
                }
            };

            foreach (KeyValuePair<string, string> entry in conditions)
            {
                q.Condition.Add(DialogExpressionCondition.Create(entry.Key, entry.Value));
            }

            var queryResult = dialog.GetDocumentsResult(q);
            List<Document> result = new List<Document>();

            foreach (var d in queryResult.Items)
            {
                if (d.ContentType != "application/octet-stream")
                    result.Add(d);
            }

            return result;
        }

        public class ZipItem
        {
            public string Name { get; set; }
            public Stream Content { get; set; }
            public ZipItem(string name, Stream content)
            {
                this.Name = name;
                this.Content = content;
            }
        }

        public static ZipItem DownloadDocumentContent(this Document document)
        {
            if (document.FileDownloadRelationLink == null)
                document = document.GetDocumentFromSelfRelation();

            var downloadResponse = document.PostToFileDownloadRelationForStreamAsync(
                new FileDownload()
                {
                    TargetFileType = FileDownloadType.Auto
                }).Result;

            var contentHeaders = downloadResponse.ContentHeaders;

            return new ZipItem(downloadResponse.GetFileName(), downloadResponse.Content);
        }

        public static Stream Zip(List<ZipItem> zipItems)
        {
            var zipStream = new MemoryStream();

            using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
            {
                foreach (var zipItem in zipItems)
                {
                    var entry = zip.CreateEntry(zipItem.Name);
                    using (var entryStream = entry.Open())
                    {
                        zipItem.Content.CopyTo(entryStream);
                    }
                }
            }
            zipStream.Position = 0;
            return zipStream;
        }

        public static string ExistFileIncrement(string fileName, List<ZipItem> zipItems)
        {
            string fileName_current = fileName;
            int count = 0;
            while (zipItems.Where(p => p.Name == fileName_current).Count() > 0)
            {
                count++;
                fileName_current = Path.GetFileNameWithoutExtension(fileName)
                                 + " (" + count.ToString() + ")"
                                 + Path.GetExtension(fileName);
            }
            return fileName_current;
        }

        public static Stream GetZipStream(ServiceConnection connection, List<Document> documents)
        {
            List<ZipItem> zipItems = new List<ZipItem>();

            foreach (Document document in documents)
            {
                ZipItem zipItem = DownloadDocumentContent(document);
                zipItem.Name = ExistFileIncrement(zipItem.Name, zipItems);
                zipItems.Add(zipItem);
            }
            
            return Zip(zipItems);
        }
    }
}
