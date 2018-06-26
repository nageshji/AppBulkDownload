using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace BulkDownloadWeb.Pages
{
    public partial class Home : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string hosturl = Request.QueryString["SPHostUrl"];
            string listid = Request.QueryString["SPListId"];
            var contextToken = TokenHelper.GetContextTokenFromRequest(Page.Request);
            ifrm.Src = "Default.aspx?SPHostUrl=" + hosturl + "&ct=" + contextToken + "&SPListId=" + listid + "&ListURLDir=" + Request.QueryString["ListURLDir"] + "&SelectedItemId=" + Request.QueryString["SelectedItemId"];
        }

        protected void ifrm_Load(object sender, EventArgs e)
        {
            Response.Write("file is downloaded");
        }   
    }
}