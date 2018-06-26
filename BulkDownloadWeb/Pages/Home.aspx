<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Home.aspx.cs" Inherits="BulkDownloadWeb.Pages.Home" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <title></title>
    <script src="../Scripts/jquery-1.7.1.min.js"></script>  
</head>
<body style="font-family:Arial,helvetica,sans-serif;">
    <form id="form1" runat="server">
        <asp:Panel ID="pnlMain" runat="server">
            <div>Your file (s) have been successfully downloaded.</div>
            <input type="button" value="Close" onclick="closeWindow()" />
        </asp:Panel>
          <iframe id="ifrm" runat="server" style="border:0" />        
    </form>
    <script type="text/javascript">

        function closeWindow() {
            alert("done");
            window.parent.postMessage("CloseCustomActionDialogNoRefresh", "*");
        }
        </script> 
</body>
</html>
