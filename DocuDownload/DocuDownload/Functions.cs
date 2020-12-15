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
    static class Functions
    {
        /// <summary> 
        /// Récupère la liste des Organisations.
        /// </summary> 
        /// <param name="connection">Connexion à la plateforme DocuWare</param> 
        /// <returns>Liste des Organisations</returns>
        public static List<string> GetAllOrganizationNames(ServiceConnection connection)
        {
            return connection.Organizations.Select(p => p.Name).ToList();
        }

        /// <summary> 
        /// Récupère une organisation d'une connexion avec son nom.
        /// </summary> 
        /// <param name="connection">Connexion</param> 
        /// <param name="orgName">Nom d'une organisation</param> 
        /// <returns>Organisation</returns>
        /// <exception>Lorsque l'organisation n'existe pas/exception>
        public static Organization GetOrganizationByName(ServiceConnection connection, string orgName)
        {
            Organization organization = connection.Organizations.Where(i => i.Name == orgName).FirstOrDefault();
            if (organization == null)
                throw new Exception("Organization not found.");
            return organization;
        }

        /// <summary> 
        /// Récupère la liste des Armoires d'une Organisation.
        /// </summary> 
        /// <param name="organization">Organisation</param> 
        /// <returns>Liste des Armoires</returns>
        public static List<string> GetAllFileCabinetNames(Organization organization)
        {
            return organization.GetFileCabinetsFromFilecabinetsRelation().FileCabinet.Select(p => p.Name).ToList();
        }

        /// <summary> 
        /// Récupère une armoire d'une organisation avec son nom.
        /// </summary> 
        /// <param name="organization">Organisation</param> 
        /// <param name="fileCabinetName">Nom d'une armoire</param> 
        /// <returns>Armoire</returns>
        /// <exception>Lorsque l'armoire n'existe pas</exception>
        public static FileCabinet GetFileCabinetByName(Organization organization, string fileCabinetName)
        {
            List<FileCabinet> fileCabinets = organization.GetFileCabinetsFromFilecabinetsRelation().FileCabinet;
            FileCabinet fileCabinet = fileCabinets.Where(i => i.Name == fileCabinetName).FirstOrDefault();
            if (fileCabinet == null)
                throw new Exception("File cabinet not found.");
            return fileCabinet;
        }

        /// <summary> 
        /// Récupère la liste des Boites de recherche d'une Armoire.
        /// </summary> 
        /// <param name="fileCabinet">Armoire</param> 
        /// <returns>Liste des Boites de recherche</returns>
        public static List<string> GetAllDialogNames(FileCabinet fileCabinet)
        {
            return fileCabinet.GetDialogInfosFromSearchesRelation().Dialog.Select(p => p.GetDialogFromSelfRelation().DisplayName).ToList();
        }

        /// <summary> 
        /// Récupère une boite de recherche avec son nom.
        /// </summary> 
        /// <param name="fileCabinet">Armoire</param> 
        /// <param name="dialogName">Nom d'une boite de recherche</param> 
        /// <returns>Boite de recherche</returns>
        /// <exception>Lorsque la boite de recherche n'existe pas/exception>
        public static Dialog GetDialogByName(FileCabinet fileCabinet, string dialogName)
        {
            DialogInfos dialogInfoItems = fileCabinet.GetDialogInfosFromSearchesRelation();
            Dialog dialog = null;
            foreach (var dia in dialogInfoItems.Dialog)
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

        public static List<Document> SearchDocuments(Dialog dialog, Dictionary<string, string> conditions = null, int count = 1000000, SortDirection sortDirection = SortDirection.Desc)
        //à modifier pour recherche avec plusieurs données pour un champ
        {
            if (conditions == null)
                conditions = new Dictionary<string, string>();

            var q = new DialogExpression()
            {
                Operation = DialogExpressionOperation.And,
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

        /// <summary> 
        /// Classe représentant un élément d'une archive zip.
        /// </summary> 
        private class ZipItem
        {
            public string Name { get; set; }
            public Stream Content { get; set; }
            public ZipItem(string name, Stream content)
            {
                this.Name = name;
                this.Content = content;
            }
        }

        /// <summary> 
        /// Transforme un document DocuWare en élément d'archive zip.
        /// </summary> 
        /// <param name="document">Document DocuWare</param> 
        /// <returns>Elément de l'archive zip</returns>
        private static ZipItem DownloadDocumentContent(this Document document)
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

        /// <summary> 
        /// Crée le flux de l'archive zip à partir des éléments la composant.
        /// </summary> 
        /// <param name="zipItems">Liste des éléments de l'archive</param> 
        /// <returns>Stream du fichier zip</returns>
        private static Stream Zip(List<ZipItem> zipItems)
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

        /// <summary> 
        /// Renomme un document lorsqu'un autre document avec le même nom existe dans l'archive.
        /// </summary> 
        /// <param name="filePath">Chemin relatif du document dans l'archive</param> 
        /// <param name="zipItems">Liste des fichiers de l'archive</param> 
        /// <returns>Nouveau chemin relatif du document</returns>
        private static string ExistFileIncrement(string filePath, List<ZipItem> zipItems)
        {
            string fileName_current = filePath;
            int count = 0;
            while (zipItems.Where(p => p.Name == fileName_current).Count() > 0)
            {
                count++;
                if (Path.GetDirectoryName(filePath) == "")
                    fileName_current = Path.GetFileNameWithoutExtension(filePath)
                                     + " (" + count.ToString() + ")"
                                     + Path.GetExtension(filePath);
                else
                    fileName_current = Path.GetDirectoryName(filePath)
                                     + Path.DirectorySeparatorChar
                                     + Path.GetFileNameWithoutExtension(filePath)
                                     + " (" + count.ToString() + ")"
                                     + Path.GetExtension(filePath);
            }
            return fileName_current;
        }

        /// <summary> 
        /// Crée le flux de l'archive zip corespondant à la liste de documents avec la hierarchie de fichiers choisie.
        /// </summary> 
        /// <param name="documents">Liste des documents</param> 
        /// <param name="hierarchy">Liste correspondant au classement des documents en dossiers et sous dossiers</param> 
        /// <returns>Stream du fichier zip</returns>
        public static Stream GetZipStream(List<Document> documents, List<string> hierarchy = null)
        {
            List<ZipItem> zipItems = new List<ZipItem>();

            foreach (Document document in documents)
            {
                string documentPath = "";
                if (hierarchy != null)
                {
                    List<DocumentIndexField> fields = document.Fields;
                    foreach (string fieldName in hierarchy)
                    {
                        object item = fields.Where(p => p.FieldName == fieldName).Select(p => p.Item).FirstOrDefault();
                        if (item == null)
                            documentPath += @"Unnamed\";
                        else
                            documentPath += item.ToString() + @"\";
                    }
                }
                ZipItem zipItem = DownloadDocumentContent(document);
                zipItem.Name = ExistFileIncrement(documentPath + zipItem.Name, zipItems);
                zipItems.Add(zipItem);
            }

            return Zip(zipItems);
        }

    }
}