<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="SupportLogin.aspx.cs" Inherits="OnlineSupport.OperatorLogin" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <h1>Login for Support</h1>
        <p>For real world application you will have to use login mechanism here. 
            <br />To make this demo simple I have used this button to set session value and go to online support page</p>
        <asp:Button ID="Login_btn" runat="server" Text="Login" OnClick="Login_btn_Click" />
    </div>
    </form>
</body>
</html>
