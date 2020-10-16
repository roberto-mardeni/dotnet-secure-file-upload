﻿using SecureFileUpload.FileUtilities;
using SecureFileUpload.Models;
using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;

namespace SecureFileUpload
{
    public partial class FileUpload : System.Web.UI.Page
    {
        private enum FileStorageProvider
        {
            Local,
            AzureStorageBlobs
        }

        private IFileStorage localFileStorage;
        private IFileStorage azureFileStorage;
        private IVirusScanner virusScanner;

        public FileUpload()
        {
            this.localFileStorage = new LocalFileStorage(Server.MapPath("App_Data"));
            this.azureFileStorage = new AzureFileStorage();
            this.virusScanner = new CloudmersiveVirusScanner();
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            UpdateFileList();
            ResetForm(FileStorageProvider.Local);
            ResetForm(FileStorageProvider.AzureStorageBlobs );
        }

        private void UpdateFileList()
        {
            var items = new List<ListItem>();
            localFileStorage.GetFiles().ForEach(f => items.Add(new ListItem { Text = f, Value = f }));
            dlFilesLocal.DataSource = items;
            dlFilesLocal.DataBind();

            items = new List<ListItem>();
            azureFileStorage.GetFiles().ForEach(f => items.Add(new ListItem { Text = f, Value = f }));
            dlFilesAzure.DataSource = items;
            dlFilesAzure.DataBind();
        }

        protected void dlFiles_ItemCommand(object source, DataListCommandEventArgs e)
        {
            if (e.CommandName == "DeleteFile")
            {
                string file = e.CommandArgument as string;
                if (((DataList)source).ID == "dlFilesLocal")
                {
                    localFileStorage.DeleteFile(file);
                    ShowResult(FileStorageProvider.Local, $"Deleted {file}");
                }
                if (((DataList)source).ID == "dlFilesAzure")
                {
                    azureFileStorage.DeleteFile(file);
                    ShowResult(FileStorageProvider.AzureStorageBlobs, $"Deleted {file}");
                }
            }
            UpdateFileList();
        }

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            FileStorageProvider provider = FileStorageProvider.Local;
            System.Web.UI.WebControls.FileUpload fileUploadControl = fuCsvFileLocal;

            if (((Button)sender).ID.Equals("btnSubmitAzure"))
            {
                provider = FileStorageProvider.AzureStorageBlobs;
                fileUploadControl = fuCsvFileAzure;
            }

            if (fileUploadControl.PostedFile != null)
            {
                if (fileUploadControl.PostedFile.ContentLength > 0)
                {
                    var memoryStream = StreamUtility.CopyToMemoryStream(fileUploadControl.PostedFile.InputStream);

                    var scanResult = virusScanner.ScanStream(StreamUtility.CopyToMemoryStream(memoryStream));
                    if (scanResult.IsSafe)
                    {
                        var parseErrors = CsvFile.Validate(StreamUtility.CopyToMemoryStream(memoryStream));

                        if (parseErrors.Count > 0)
                        {
                            ShowError(provider, "Errors found in file: <br />" + string.Join("<br />", parseErrors));
                        }
                        else
                        {
                            try
                            {
                                switch (provider)
                                {
                                    case FileStorageProvider.AzureStorageBlobs:
                                        azureFileStorage.SavePostedFile(fileUploadControl.PostedFile.FileName, StreamUtility.CopyToMemoryStream(memoryStream));
                                        break;
                                    case FileStorageProvider.Local:
                                        localFileStorage.SavePostedFile(fileUploadControl.PostedFile.FileName, StreamUtility.CopyToMemoryStream(memoryStream));
                                        break;
                                }
                                ShowResult(provider, "The file has been uploaded.");
                                UpdateFileList();
                            }
                            catch (Exception ex)
                            {
                                ShowError(provider, ex.Message);
                            }
                        }
                    }
                    else
                    {
                        ShowError(provider, $"Virus scan found issues: {scanResult.Message}");
                    }
                }
                else
                {
                    ShowError(provider, "Empty file uploaded.");
                }
            }
            else
            {
                ShowResult(provider, "Please select a file to upload.");
            }
        }

        private void ResetForm(FileStorageProvider provider)
        {
            switch (provider)
            {
                case FileStorageProvider.Local:
                    pnlResultLocal.Visible = false;
                    pnlErrorLocal.Visible = false;
                    break;
                case FileStorageProvider.AzureStorageBlobs:
                    pnlResultAzure.Visible = false;
                    pnlErrorAzure.Visible = false;
                    break;
            }
        }

        private void ShowResult(FileStorageProvider provider, string resultMessage)
        {
            switch (provider)
            {
                case FileStorageProvider.Local:
                    pnlResultLocal.Visible = true;
                    pnlErrorLocal.Visible = false;
                    litResultsLocal.Text = resultMessage;
                    break;
                case FileStorageProvider.AzureStorageBlobs:
                    pnlResultAzure.Visible = true;
                    pnlErrorAzure.Visible = false;
                    litResultsAzure.Text = resultMessage;
                    break;
            }
        }

        private void ShowError(FileStorageProvider provider, string errorMessage)
        {
            switch (provider)
            {
                case FileStorageProvider.Local:
                    pnlResultLocal.Visible = false;
                    pnlErrorLocal.Visible = true;
                    litErrorLocal.Text = errorMessage;
                    break;
                case FileStorageProvider.AzureStorageBlobs:
                    pnlResultAzure.Visible = false;
                    pnlErrorAzure.Visible = true;
                    litErrorAzure.Text = errorMessage;
                    break;
            }

        }
    }
}