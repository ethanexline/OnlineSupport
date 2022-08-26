using System;

namespace OnlineSupport
{
    public partial class OnlineSupport : System.Web.UI.Page
    {                

        public static bool operator_online;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["OperatorOnline"] != null)
            {
                operator_online = true;                
            }
        }

       
    }
}