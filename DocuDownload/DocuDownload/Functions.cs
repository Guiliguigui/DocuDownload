using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocuWare.Platform.ServerClient;
using DocuWare.Services.Http;
using System.IO;
using System.IO.Compression;

namespace DocuDownload
{
    public static class Functions
    {

        public static FileCabinet GetFileCabinetByName(List<FileCabinet> fileCabinets, string fcName)
        {
            FileCabinet fileCabinet = new FileCabinet();
            foreach (var fc in fileCabinets)
                if (fc.Name == fcName)
                    fileCabinet = fc;
            return fileCabinet;
        }

        public static Dialog GetDialogByName(DialogInfos dialogInfoItems, string dialogName)
        {
            Dialog dialog = new Dialog();
            foreach (var dia in dialogInfoItems.Dialog)
            {
                if (dia.GetDialogFromSelfRelation().DisplayName == dialogName)
                    dialog = dia.GetDialogFromSelfRelation();
            }
            return dialog;
        }

        public static List<string> GetVisiblesFieldsNames(Dialog dialog)
        {
            List<string> fieldsNames = new List<string>();
            foreach (var field in dialog.Fields)
            {
                if (field.Visible)
                    fieldsNames.Add(field.DBFieldName.ToString());
            }
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

        public static string ExistFileIncrement(string pathfile)
        {
            string pathfile_current = pathfile;
            int count = 0;
            while (File.Exists(pathfile_current))
            {
                count++;
                pathfile_current = Path.GetDirectoryName(pathfile)
                                 + Path.DirectorySeparatorChar
                                 + Path.GetFileNameWithoutExtension(pathfile)
                                 + " (" + count.ToString() + ")"
                                 + Path.GetExtension(pathfile);
            }
            return pathfile_current;
        }

        public class FileDownloadResult
        {
            public string ContentType { get; set; }
            public string FileName { get; set; }
            public long? ContentLength { get; set; }
            public System.IO.Stream Stream { get; set; }
        }

        public static FileDownloadResult DownloadDocumentContent(this Document document)
        {
            if (document.FileDownloadRelationLink == null)
                document = document.GetDocumentFromSelfRelation();

            var downloadResponse = document.PostToFileDownloadRelationForStreamAsync(
                new FileDownload()
                {
                    TargetFileType = FileDownloadType.Auto
                }).Result;

            var contentHeaders = downloadResponse.ContentHeaders;
            return new FileDownloadResult()
            {
                Stream = downloadResponse.Content,
                ContentLength = contentHeaders.ContentLength,
                ContentType = contentHeaders.ContentType.MediaType,
                FileName = downloadResponse.GetFileName()
            };
        }

        public static string DownloadSingleDocumentContent(ServiceConnection connection, string path, Document document)
        {
            var downloadedFile = DownloadDocumentContent(document);
            string pathfile = ExistFileIncrement(path + downloadedFile.FileName);

            using (var file = File.Create(pathfile))
            using (var stream = downloadedFile.Stream)
                stream.CopyTo(file);

            return pathfile;
        }

        public static string DownloadZip(ServiceConnection connection, string path, string zipName, List<Document> list)
        {
            string temppath = path + @"tmp\";
            Directory.CreateDirectory(temppath);

            foreach (Document document in list)
            {
                DownloadSingleDocumentContent(connection, temppath, document);
            }

            string zipPath = ExistFileIncrement(path + zipName + ".zip");
            ZipFile.CreateFromDirectory(temppath, zipPath);

            Directory.Delete(temppath, true);

            return zipPath;
        }

    }
}
