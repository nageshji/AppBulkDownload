<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="BulkDownloadWeb.Pages.Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">

    <title></title>
    <script type="text/javascript" src="../Scripts/DialogEvents.js"></script>

</head>
<body style="font-family:Arial,helvetica,sans-serif;">
    <form id="form1" runat="server">
        <asp:HiddenField ID="ctoken" runat="server" />
         <asp:HiddenField ID="hweb" runat="server" />
         <asp:Panel ID="pnlMain" runat="server">
            <table>
                <tr id="trfolders">
                    <td>
                        <asp:Label ID="lblFolders" runat="server">Preserve folder structure in Zip file:</asp:Label></td>
                    <td>
                        <asp:CheckBox ID="chkFolders" runat="server" /></td>
                </tr>
                <tr id="trzipfile">
                    <td>
                        <asp:Label ID="Label1" runat="server">Enter the Zip file Name:</asp:Label></td>
                    <td>
                        <asp:TextBox ID="txtZipFileName" runat="server"></asp:TextBox>.zip</td>
                </tr>
                <tr style="column-span: all">
                    <td id="tdDownload">
                        <asp:Button ID="btnDownload" runat="server" Text="Download" OnClick="btnDownload_Click" Width="90px" OnClientClick="hideControls()" /></td>
                    <td id="tdclose" style="display: none">
                        <input type="button" value="Close" onclick="closeWindow()" style="width: 90px" /></td>
                </tr>
            </table>
            <asp:Label runat="server" ID="lblLog" />
        </asp:Panel>
    </form>
    <script type="text/javascript">
        function closeWindow() {
            window.parent.postMessage("CloseCustomActionDialogNoRefresh", "*");
        }
        function hideControls() {
            document.getElementById("trfolders").style.display = "none";
            document.getElementById("trzipfile").style.display = "none";
            document.getElementById("tdDownload").style.display = "none";
            document.getElementById("tdclose").style.display = "block";
        }
    </script>
</body>
</html>
