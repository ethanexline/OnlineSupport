<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="OnlineSupport.aspx.cs" Inherits="OnlineSupport.OnlineSupport" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <script src="Scripts/jquery-1.6.4.min.js"></script>
    <script src="Scripts/jquery.signalR-1.0.0-rc2.min.js"></script>
    <script type="text/javascript">        
        $(function () {
            var conn = $.connection();
            var img1 = "online.png";
            var img2 = "offline.png";
            $('#message').keypress(function (event) {
                if (event.keyCode == 13) {
                    event.preventDefault();
                    $("#send").click();
                }
            });

            conn.received(function (data) {
                if (data.substr(0, 11) == "QUEUEUPDATE") {
                    document.getElementById("supportHeader").style.display = "block";
                    document.getElementById("queueLength").innerText = "Users currently in queue: " + data.slice(11);
                }
                else {
                    $("#message_list").append(data + "<br/>");
                    playSound();
                    document.getElementById("message_list").lastElementChild.scrollIntoView();
                    if (data == "Support is currently offline. Please send an email to cdonohue@autoplusap.com for urgent inquiries.") {
                        $("#Status_div").html("<img alt=\"offline\" src=\"offline.png\" style=\"border:none;\" />");
                    }
                    else if (data == "Support is now online.") {
                        location.reload(true);
                        $("#Status_div").html("<img alt=\"online\" src=\"online.png\" />");
                        return false;
                    }
                    else {
                        $("#Status_div").html("<img alt=\"online\" src=\"online.png\" />");
                    }
                }
            });

            conn.start()
                .promise()
                .done(function () {
                    $("#Wait_div").css("visibility", "hidden")
                    $("#send").click(function () {
                        if ($("#message").val().length == 0)
                            return;
                        conn.send($("#message").val());
                        $('#message').val('').focus();
                    })
                });
        });
    </script>  
     <script type="text/javascript">
         var snd = "sound.wav";
         function playSound() {
             document.getElementById("sound").innerHTML = "<embed src='sound.wav' hidden=true autostart=true loop=false>";
         }
    </script>
    <style type="text/css">
        .heading
        {
             font-family:'Segoe UI',sans-serif; font-size:25px;
        }
        .btn
        {
            height:30px; background-color:#efefef; border:solid 1px silver;
        }
    </style>
</head>
<body style="margin:20px; font-family:'Segoe UI',sans-serif;">
    <form id="form1" runat="server">       
    <div>
        <h1 id="supportHeader" style="display: none;">Support Mode</h1>
        <div id="Wait_div" style="visibility:visible;">
            <span class="heading" >Please wait....</span>
        </div>
        <div style="display: flex; justify-content: space-between; width:560px; align-items: center; font-size: large;">
            <div id="Status_div">           
            </div>

            <label id="queueLength"></label>
        </div>
        
        <div id="message_list" style="border:solid 1px silver; padding:5px; min-height:400px; width:560px; max-height:400px; overflow:auto;">            
        </div>
        <br />
        <div>
            <input id="message" name="message" type="text" style="width:500px; border:solid 1px silver; height:25px;" />&nbsp;&nbsp;
            <input id="send" name="send" value="Send" type="button"  class="btn"/>
        </div>
        
    </div>
    <div id="sound"></div>
    </form>
</body>
</html>
