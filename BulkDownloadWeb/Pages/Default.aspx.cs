#define DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.SharePoint.Client;
using Microsoft.IdentityModel.S2S.Tokens;
using System.Net;
using System.IO;
using System.Xml;

namespace BulkDownloadWeb.Pages
{
    public partial class Default : System.Web.UI.Page
    {
        //SharePointContextToken contextToken;
        string accessToken;
        string tmpDirLocation;
        string hostListId;
        string libName;
        int foldercount;

        // The Page_load method fetches the context token and the access token. 
        // The access token is used by all of the data retrieval methods.
        protected void Page_Load(object sender, EventArgs e)
        {
            //TokenHelper.TrustAllCertificates();
            if (string.IsNullOrEmpty(accessToken)) GetAccessToken();
        }

        /// <summary>
        /// Gets the access token from cache or from context token
        /// </summary>
        private void GetAccessToken()
        {
            var contextTokenString = string.Empty;
            var hostWebUrl = string.Empty;
            if (!IsPostBack)
            {
                contextTokenString = TokenHelper.GetContextTokenFromRequest(Page.Request);
                hostWebUrl = Page.Request["SPHostUrl"];
                //Session.Add("SPContextToken", contextTokenString);
                ctoken.Value = contextTokenString; //TokenHelper.GetContextTokenFromRequest(Page.Request);
                hweb.Value = hostWebUrl;
                //Session.Add("SPHostUrl", hostWebUrl);
            }

            contextTokenString = ctoken.Value; //Session["SPContextToken"] == null ? string.Empty : Session["SPContextToken"].ToString();
            if (string.IsNullOrEmpty(contextTokenString))
            {
                //hide everything, then return
#if (DEBUG)
                lblLog.Text = "NULL context token";
#else
                pnlMain.Visible = false;
#endif
                return;
            }
            var hostWeb = new Uri(hweb.Value == null ? string.Empty : hweb.Value.ToString());
            
            //get the context token details
            SharePointContextToken tokenContent = TokenHelper.ReadAndValidateContextToken(contextTokenString, Request.Url.Authority);

            //now look to see if we have cached an access token for this yet
            if (Session[tokenContent.CacheKey] != null)
            {
                AccessTokenInfo ati = (AccessTokenInfo)Session[tokenContent.CacheKey];

                //check the expiration
                if (DateTime.Now < ati.Expires)
                    accessToken = ati.AccessToken;
            }

            if (string.IsNullOrEmpty(accessToken))
            {
                //get an access token from the refresh token
                accessToken = TokenHelper.GetAccessToken(TokenHelper.ReadAndValidateContextToken(contextTokenString, Request.Url.Authority), hostWeb.Authority).AccessToken;

                //create a new AccessTokenInformation item and set the
                //expiration of the access token to 30 minutes
                //and put it in session state for next time
                Session.Add(tokenContent.CacheKey, new AccessTokenInfo(accessToken, DateTime.Now.AddMinutes(30)));
            }
        }

        protected void btnDownload_Click(object sender, EventArgs e)
        {
            try
            {
                chkFolders.Visible = false;
                btnDownload.Visible = false;
                if (string.IsNullOrEmpty(accessToken)) GetAccessToken();
                
                DownFilesAsZip(accessToken);
            }
            catch (Exception ex)
            {
                #if (DEBUG)
                                lblLog.Text += "Error occured.<br/>" + ex.Message + "<br/>" + ex.StackTrace;
                #else
                                lblLog.Text += "Error occured.<br/>" + ex.Message;
                #endif
            }
            
        }

        private void DownFilesAsZip(string actoken)
        {
            hostListId = Page.Request["SPListId"];
            var seletectedIds = Page.Request["SelectedItemId"];
            libName = Page.Request["ListURLDir"];
            
            if (string.IsNullOrEmpty(seletectedIds))
            {
                return;
            }

            string strTempDirectory = Path.GetTempPath();
            string strTempFolder = Path.GetRandomFileName();
            tmpDirLocation = strTempDirectory + strTempFolder + "\\";

            if (!Directory.Exists(tmpDirLocation))
                System.IO.Directory.CreateDirectory(tmpDirLocation);
            if (chkFolders.Checked)
            {
                Directory.CreateDirectory(tmpDirLocation + libName);
                foldercount = 0;
            }
               
            string[] sItemIds = seletectedIds.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            int[] itemsIDs = new int[sItemIds.Length];
            for (int i = 0; i < sItemIds.Length; i++)
            {
                itemsIDs[i] = Convert.ToInt32(sItemIds[i]);
            }

            if (!string.IsNullOrEmpty(hostListId) && itemsIDs.Length > 0)
            {
                try
                {

                    using (var clientContext = getClientContext(actoken))
                    {
                        List library = clientContext.Web.Lists.GetById(new Guid(hostListId.ToString())); //.GetByTitle("Documents");               
                        //library = clientContext.Web.Lists.GetByTitle("Documents");

                        string strItems = "";
                        foreach (int id in itemsIDs)
                        {
                            strItems += "<Value Type='Number'>" + id.ToString() + "</Value>";
                        }

                        string strQuery = @"<View Scope='RecursiveAll'>
                                                <Query>
                                                    <Where><In>
                                                        <FieldRef Name='ID'/>
                                                            <Values>" + strItems + @"</Values>
                                                    </In></Where>
                                                </Query>
                                            </View>";

                        //var query = CamlQuery.CreateAllItemsQuery(100);
                        var camlQuery = new CamlQuery();
                        camlQuery.ViewXml = strQuery;

                        var result = library.GetItems(camlQuery);

                        //clientContext.Load(clientContext.Web.Lists.GetByTitle("Documents"), web => web.Title);

                        clientContext.Load(result, items => items.Include(
                                            item => item["ID"],
                                            item => item["Title"],
                                            item => item["FileRef"],
                                            item => item["FileLeafRef"],
                                            item => item["FileDirRef"]

                                       ));
                        clientContext.ExecuteQuery();
                        int count = 0;
                        if (result.Count > 0)
                        {
                            foreach (var item in result)
                            {
                                if (sItemIds.Contains(item["ID"].ToString()))
                                {
                                    count++;
                                    var strfileref = item["FileRef"].ToString();
                                    var strfileleafref = item["FileLeafRef"].ToString();
                                    if (!strfileleafref.Contains('.'))
                                    {
                                        var strFileDirRef = item["FileDirRef"].ToString();
                                        AddFolder(strFileDirRef, strfileref, strfileleafref);
                                    }
                                    else
                                    {
                                        AddFile(strfileref, item.File, clientContext);
                                    }
                                }
                            }

                            if (count > 0)
                            {
                                //Compress the files
                                string strZippedFile;
                                if(!string.IsNullOrEmpty(txtZipFileName.Text))
                                {
                                    strZippedFile = txtZipFileName.Text + ".zip";
                                }
                                else
                                {
                                    strZippedFile = libName + ".zip";
                                }                                  
                                
                                 //strTempFolder + ".zip";

                                Caltex.Utilities.ZipUtilities.ZipFiles(tmpDirLocation, strZippedFile, string.Empty);

                                //Force the browser to download the file
                                string FileName = libName + ".zip";

                                PushFileToDownload(tmpDirLocation + strZippedFile, FileName);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {    
                    lblLog.Text += "Error occured.<br/>" + ex.Message + "<br/>" + ex.StackTrace;    
                    lblLog.Text += "Error occured.<br/>" + ex.Message;    
                }
                finally
                {

                }
            }
        }


        bool WriteToFile(byte[] bytFile, string QualifiedFileName)
        {
            FileStream fs = new FileStream(QualifiedFileName, FileMode.OpenOrCreate, FileAccess.Write);
            fs.Write(bytFile, 0, bytFile.Length);
            fs.Close();
            return true;
        }

        void PushFileToDownload(string FilePath, string FileName)
        {
            FileInfo fInfo = new FileInfo(FilePath);
            Response.ContentType = "application/octet-stream";
            Response.AppendHeader("Content-Disposition", "inline; filename=" + fInfo.Name);
            Response.AddHeader("Content-Length", fInfo.Length.ToString());
            Response.WriteFile(FilePath);
            Response.Flush();
        }

        private ClientContext getClientContext()
        {
            if (string.IsNullOrEmpty(accessToken)) GetAccessToken();
            return getClientContext(accessToken);
        }

        private ClientContext getClientContext(string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new UnauthorizedAccessException("You don't have access to this resource");
            }
            var contextToken = TokenHelper.GetContextTokenFromRequest(Page.Request);
            var hostWeb = Page.Request["SPHostUrl"];
            var ClientContext = TokenHelper.GetClientContextWithAccessToken(hostWeb, accessToken);
            return ClientContext;
        }

        private void AddFile(string strfileref, Microsoft.SharePoint.Client.File file, ClientContext clientContext)
        {
            //var clientContext = getClientContext();
            ClientResult<Stream> data = file.OpenBinaryStream();
            clientContext.Load(file);
            clientContext.ExecuteQuery();


            //FileInformation fileInfo = Microsoft.SharePoint.Client.File.OpenBinaryDirect(clientContext, strfileref);
            if (data != null)
            {
                byte[] bytFile;
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    data.Value.CopyTo(memoryStream);
                    bytFile = memoryStream.GetBuffer();
                    memoryStream.Flush();
                }

                if (chkFolders.Checked)
                {
                    string QualifiedFileName = tmpDirLocation + strfileref.Substring(strfileref.IndexOf(libName));
                    string filePath = QualifiedFileName.TrimEnd(file.Name.ToCharArray());
                    if (!Directory.Exists(filePath)) Directory.CreateDirectory(filePath);
                    WriteToFile(bytFile, QualifiedFileName);
                }
                else
                {
                    string QualifiedFileName = tmpDirLocation + "\\" + file.Name;
                    QualifiedFileName = checkFileName(QualifiedFileName, file.Name);   
                    WriteToFile(bytFile, QualifiedFileName);
                }
            }

        }

        private string checkFileName(string QualifiedFileName, string filename)
        {
            if (System.IO.File.Exists(QualifiedFileName))
            {
                string[] fullfilename;
                fullfilename = filename.Split('.');
                string fileextn = Path.GetExtension(QualifiedFileName);
                QualifiedFileName = tmpDirLocation + fullfilename[0] + "(1)" + fileextn;
                return checkFileName(QualifiedFileName, filename);
            }
            
          return QualifiedFileName;
                       
        }


        private void AddFolder(string strFileDirRef, string strfileref, string strfileleafref)
        {
            string filePath = strfileref.Substring(strfileref.IndexOf(libName));

            //Remove the filename 
            string DirectoryPath = filePath;

            DirectoryPath = DirectoryPath.Replace("/", "\\");

            //create the directory path
            if (!string.IsNullOrEmpty(DirectoryPath) && chkFolders.Checked)
            {
                Directory.CreateDirectory(tmpDirLocation + DirectoryPath);
                foldercount++;
            }
                

            var clientContext = getClientContext();
            Microsoft.SharePoint.Client.List library = clientContext.Web.Lists.GetById(new Guid(hostListId.ToString()));

            CamlQuery camlQuery = new CamlQuery();
            camlQuery = new CamlQuery();
            string strQuery = @"<View Scope='RecursiveAll'>
                            <Query>
                                <Where>
                                    <Eq>
                                        <FieldRef Name='FileDirRef'/>
                                        <Value Type='Text'>/" + @strfileref +
                                        @"</Value>
                                    </Eq>
                                </Where>
                            </Query>                        
                        </View>";
            camlQuery.ViewXml = strQuery;
            Microsoft.SharePoint.Client.ListItemCollection listItems = library.GetItems(camlQuery);
            clientContext.Load(listItems);
            clientContext.ExecuteQuery();

            if (listItems.Count > 0)
            {
                foreach (var item in listItems)
                {
                    var fileref = item["FileRef"].ToString();
                    var fileleafref = item["FileLeafRef"].ToString();
                    if (!fileleafref.Contains('.'))
                    {
                        var FileDirRef = item["FileDirRef"].ToString();
                        AddFolder(FileDirRef, fileref, fileleafref);
                    }
                    else
                        AddFile(fileref, item.File, clientContext);
                }
            }

        }
    }
}