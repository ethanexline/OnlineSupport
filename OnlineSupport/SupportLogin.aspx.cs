using System;

namespace OnlineSupport
{
    public partial class OperatorLogin : System.Web.UI.Page
    {
        //public static bool operator_online = false;
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void Login_btn_Click(object sender, EventArgs e)
        {
            Session["OperatorOnline"] = true;
            //operator_online = true;
            Response.Redirect("OnlineSupport.aspx",true);
            //Server.Transfer("onlinesupport.aspx");
        }
    }
}