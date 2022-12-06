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
            $('#message').keypress(function (event) {
                if (event.keyCode == 13) {
                    event.preventDefault();
                    $("#send").click();
                }
            });

            /*******
             * System messages and definitions:
             * SUPPORTVIEW - indicates user is in Support role and should have the header and queue area displayed
             * QUEUEPOS - indicates user is in queue and contains current queue position
             * QUEUEFIN - indicates user has left the queue, whether by disconnection or becoming the active session, and hides queue position label
             * QUEUEADD - sent to Support role to indicate a new user has entered the queue; builds and places links in listing for passed-in ConnectionID accordingly
             * QUEUEDEL - sent to Support role to indicate a user has left the queue; deletes links in listing for passed-in ConnectionID
             *******/
            conn.received(function (data) {
                if (data.substr(0, 11) == "SUPPORTVIEW") {
                    document.getElementById("supportHeader").style.display = "block";
                    document.getElementById("queueArea").style.display = "flex";
                }
                else if (data.substr(0, 8) == "QUEUEPOS") {
                    document.getElementById("queuePosition").style.display = "block";
                    document.getElementById("queuePosition").innerText = "Current Position: " + data.slice(8);
                }
                else if (data.substr(0, 8) == "QUEUEFIN") {
                    document.getElementById("queuePosition").style.display = "none";
                }
                else if (data.substr(0, 8) == "QUEUEADD") {
                    var newUser = document.createElement('li');
                    newUser.id = data.slice(8);
                    var userLink = document.createElement('a');
                    var userLink2 = document.createElement('a');
                    userLink.title = "End this session";
                    userLink2.title = "Make this the active session";
                    userLink.addEventListener('click', function (e) {
                        document.getElementById("user_list").removeChild(document.getElementById(data.slice(8)));
                        conn.send("ENDSESSION" + data.slice(8));
                    });
                    userLink2.addEventListener('click', function (e) {
                        document.getElementById("user_list").removeChild(document.getElementById(data.slice(8)));
                        conn.send("CHGSESSION" + data.slice(8));
                    });
                    userLink.innerHTML = "<img src='/images/delete.png' />";
                    userLink2.innerText = "User " + data.slice(8);
                    newUser.appendChild(userLink);
                    newUser.appendChild(userLink2);
                    document.getElementById("user_list").appendChild(newUser);
                }
                else if (data.substr(0, 8) == "QUEUEDEL") {
                    document.getElementById("user_list").removeChild(document.getElementById(data.slice(8)));
                }
                else {
                    $("#message_list").append(data + "<br/>");
                    playSound();
                    document.getElementById("message_list").lastElementChild.scrollIntoView();
                    console.log(data.startsWith("Support is currently offline."));
                    if (data.startsWith("Support is currently offline.")) {
                        document.getElementById("queuePosition").style.display = "none";
                        document.getElementById("Status_div").innerHTML = "<img alt=\"offline\" src=\"offline.png\" />";
                    }
                    else if (data == "Support is now online.") {
                        location.reload(true);
                        document.getElementById("Status_div").innerHTML = "<img alt=\"online\" src=\"online.png\" />";
                        return false;
                    }
                    else {
                        document.getElementById("Status_div").innerHTML = "<img alt=\"online\" src=\"online.png\" />";
                    }
                }
            });

            conn.start()
                .promise()
                .done(function () {
                    $("#Wait_div").css("visibility", "hidden");
                    $("#send").click(function () {
                        if ($("#message").val().length == 0)
                            return;
                        conn.send($("#message").val());
                        $('#message').val('').focus();
                    });
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

        #user_list 
        {
            display: flex;
            flex-direction: column;
            list-style: none;
            padding-left: 0px;
            margin-top: 0px;
        }

        #user_list li
        {
            display: flex;
            font-size: large;
        }

        #user_list li a:last-child:hover
        {
            color: blue;
        }

        #user_list a
        {
            cursor: pointer;
        }

        img[src='/images/delete.png'] 
        {
            height: 25px;
            padding-right: 15px;
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

            <label id="queuePosition" style="display: none;"></label>
        </div>
        
        <div style="display: flex;">
            <div id="message_list" style="border: solid 1px silver; padding: 5px; min-height: 400px; width: 560px; max-height: 400px; overflow: auto; margin-right: 50px;">            
            </div>

            <div id="queueArea" style="display: none; flex-direction: column; max-height: 412px; width: max-content;">
                <h2 style="text-decoration: underline;">Users in queue:</h2>

                <ul id="user_list" style="overflow-y: auto;">
                </ul>
            </div>
        </div>
        
        <br />
        <div>
            <input id="message" name="message" type="text" style="width:500px; border:solid 1px silver; height:25px;" />&nbsp;&nbsp;
            <input id="send" name="send" value="Send" type="button" class="btn"/>
        </div>
        
    </div>
    <div id="sound"></div>
    </form>
</body>
</html>
