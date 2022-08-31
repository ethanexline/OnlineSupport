using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;
using System.Text;
using System.Diagnostics;


namespace OnlineSupport
{
    public class ChatConnection : PersistentConnection
    {
        //static string group_name = "OnlineSupport";  
        /// <summary>
        /// operator_id is set when operator is online
        /// </summary>
        static string operator_id = string.Empty;
        /// <summary>
        /// sb is for storing message,and when user left the chat,save it to DB or file
        /// </summary>
        static StringBuilder sb = new StringBuilder();
        /// <summary>
        /// operator_added_to_group is set when operator is online
        /// </summary>
        static bool operator_added_to_group = false;
        /// <summary>
        /// max 2 ids for 1-1 chat,other users will be stored in queue object (in memory)
        /// group_ids[0] is for operator
        /// group_ids[1] is for user
        /// </summary>
        static string[] group_ids = { string.Empty, string.Empty };
        /// <summary>
        /// waiting_users is a queue object,contains users connectionId
        /// </summary>
        static Queue<string> waiting_users = new Queue<string>();


        protected override Task OnConnected(IRequest request, string connectionId)
        {

            if (OnlineSupport.operator_online)
            {

                if (!operator_added_to_group)
                {

                    operator_id = connectionId;
                    group_ids[0] = connectionId;

                    operator_added_to_group = true;

                    if (string.IsNullOrWhiteSpace(group_ids[1]))
                    {
                        return Task.Factory.StartNew(() =>
                        {
                            Connection.Send(connectionId, "SUPPORTVIEW");
                            Connection.Send(connectionId, string.Format("No user is connected<br/>"));
                        });
                    }
                    else
                    {
                        return Connection.Broadcast(string.Format("Support is now online.<br/>"));
                    }
                }

            }
            else
            {
                return Connection.Send(connectionId, string.Format("Support is currently offline. Please send an email to cdonohue@autoplusap.com for urgent inquiries.<br/>"));
            }



            if (string.IsNullOrEmpty(group_ids[1]))
            {
                group_ids[1] = connectionId;
                return Task.Factory.StartNew(() =>
                {
                    Connection.Send(connectionId, "QUEUEFIN");
                    Connection.Send(connectionId, string.Format("You are now connected to Support.<br/>"));
                    Connection.Send(operator_id, string.Format("New user has joined<br/>"));
                });
            }
            else
            {
                if (operator_id != connectionId)
                {
                    waiting_users.Enqueue(connectionId);
                    return Task.Factory.StartNew(() =>
                    {
                        Connection.Send(operator_id, "QUEUEADD" + connectionId);
                        Connection.Send(connectionId, "QUEUEPOS" + waiting_users.Count.ToString());
                        Connection.Send(connectionId, string.Format("Support is assisting another user. Please wait...<br/>"));
                    });
                }
                else
                {
                    return base.OnConnected(request, connectionId);
                }
            }


        }

        protected override Task OnReceived(IRequest request, string connectionId, string data)
        {
            if (!OnlineSupport.operator_online)
                return Connection.Send(connectionId, string.Format("Support is currently offline. Please send an email to cdonohue@autoplusap.com for urgent inquiries.<br/>"));

            if (!string.IsNullOrEmpty(group_ids[0]) && !string.IsNullOrEmpty(group_ids[1]) && !group_ids.Contains(connectionId))
                return Connection.Send(connectionId, string.Format("Support is assisting another user. Please wait...</br>"));

            if (string.IsNullOrEmpty(group_ids[1]))
            {
                return base.OnReceived(request, connectionId, "");
            }

            if (data.StartsWith("ENDSESSION"))
            {
                string end = data.Substring(10);
                return Task.Factory.StartNew(() =>
                {
                    Connection.Send(end, "QUEUEFIN");
                    Connection.Send(end, string.Format("You have been disconnected. To get back in support queue, please refresh the page.<br/>"));
                    OnDisconnected(request, end);
                });
            }
            else if (data.StartsWith("CHGSESSION"))
            {
                string old = group_ids[1];
                string change = data.Substring(10);

                waiting_users = CustomQ<string>.RemoveItem(waiting_users, change);
                group_ids[1] = change;
                waiting_users.Enqueue(old);

                return Task.Factory.StartNew(() =>
                {
                    foreach (string user in waiting_users)
                    {
                        Connection.Send(user, string.Format("QUEUEPOS" + CustomQ<string>.Position(waiting_users, user).ToString()));
                    }
                    Connection.Send(operator_id, "QUEUEADD" + old);
                    Connection.Send(old, string.Format("Support has re-queued you. Please wait...<br/>"));
                    
                    Connection.Send(change, "QUEUEFIN");
                    Connection.Send(change, string.Format("You are now connected to Support.<br/>"));
                    Connection.Send(operator_id, string.Format("User " + change + " connected<br/>"));
                });
            }

            if (connectionId == operator_id)
                data = string.Format("<b>Support</b>: " + data + "<br/>");
            else
                data = string.Format("User: " + data + "<br/>");
            sb.AppendLine(data);
            return Task.Factory.StartNew(() =>
            {
                Connection.Send(group_ids[0], data);
                /*******
                 * To force end the chat with user, support can enter "EXIT"
                 *******/
                if (data.Equals("<b>Support</b>: EXIT<br/>"))
                {
                    Connection.Send(group_ids[1], string.Format("You have been disconnected. To get back in support queue, please refresh the page.<br/>"));
                    Connection.Send(group_ids[1], "QUEUEFIN");
                    OnDisconnected(request, group_ids[1]);
                }
                else
                    Connection.Send(group_ids[1], data);
            });
        }

        protected override Task OnDisconnected(IRequest request, string connectionId)
        {
            if (!string.IsNullOrEmpty(group_ids[0]) && !string.IsNullOrEmpty(group_ids[1]) && !group_ids.Contains(connectionId))
            {
                waiting_users = CustomQ<string>.RemoveItem(waiting_users, connectionId);
                return Task.Factory.StartNew(() =>
                {
                    foreach (string user in waiting_users)
                    {
                        Connection.Send(user, string.Format("QUEUEPOS" + CustomQ<string>.Position(waiting_users, user).ToString()));
                    }
                    Connection.Send(group_ids[0], "QUEUEDEL" + connectionId);
                    base.OnDisconnected(request, connectionId);
                });
            }

            if (string.IsNullOrEmpty(group_ids[1]))
            {
                return Task.Factory.StartNew(() =>
                {
                    Connection.Send(group_ids[0], "QUEUEDEL" + connectionId);
                    base.OnDisconnected(request, connectionId);
                });
            }

            sb.Append("====chat ends====\n");
            string message = sb.ToString();
            sb.Clear();
            SaveChatToFile(message);

            if (connectionId == operator_id)
            {
                OnlineSupport.operator_online = false;
                operator_added_to_group = false;
                group_ids[0] = string.Empty;
                group_ids[1] = string.Empty;
                waiting_users.Clear();
                return Connection.Broadcast(string.Format("Support is currently offline. Please send an email to cdonohue@autoplusap.com for urgent inquiries.<br/>"));
            }
            else
            {
                string toRemove = group_ids[1];
                group_ids[1] = string.Empty;
                if (waiting_users.Count > 0)
                {
                    group_ids[1] = waiting_users.Dequeue();
                    return Task.Factory.StartNew(() =>
                    {
                        foreach (string user in waiting_users)
                        {
                            Connection.Send(user, string.Format("QUEUEPOS" + CustomQ<string>.Position(waiting_users, user).ToString()));
                        }
                        Connection.Send(group_ids[1], string.Format("You are now connected to Support<br/>"));
                        Connection.Send(group_ids[1], "QUEUEFIN");
                        Connection.Send(group_ids[0], string.Format("================User has left================<br/>New User has joined<br/>"));
                        Connection.Send(group_ids[0], "QUEUEDEL" + group_ids[1]);
                    });
                }
                else
                {
                    return Task.Factory.StartNew(() =>
                    {
                        Connection.Send(group_ids[0], string.Format("=================User has left===============<br/>There are no more users in queue<br/>"));
                        Connection.Send(group_ids[0], "QUEUEDEL" + toRemove);
                    });
                }
            }
        }

        public void SaveChatToFile(string message)
        {
            Debug.Write(message);
        }
    }
}