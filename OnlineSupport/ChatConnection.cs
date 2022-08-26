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
                            Connection.Send(connectionId, "QUEUEUPDATE" + waiting_users.Count().ToString());
                            Connection.Send(connectionId, "No user is connected");
                        });
                        //return Connection.Send(connectionId, "No user is connected");
                    }
                    else
                    {
                        return Connection.Broadcast("Support is now online.");
                    }
                }

            }
            else
            {
                return Connection.Send(connectionId, "Support is currently offline. Please send an email to cdonohue@autoplusap.com for urgent inquiries.");
            }



            if (string.IsNullOrEmpty(group_ids[1]))
            {
                group_ids[1] = connectionId;
                return Task.Factory.StartNew(() =>
                {
                    Connection.Send(connectionId, "You are now connected to Support.");
                    Connection.Send(operator_id, "New user has joined");
                    Connection.Send(operator_id, "QUEUEUPDATE" + waiting_users.Count().ToString());
                });
            }
            else
            {
                if (operator_id != connectionId)
                {
                    waiting_users.Enqueue(connectionId);
                    return Task.Factory.StartNew(() =>
                    {
                        Connection.Send(operator_id, "QUEUEUPDATE" + waiting_users.Count().ToString());
                        Connection.Send(connectionId, "Support is assisting another user. You are currently " + waiting_users.Count.ToString() + " in line. Please wait...");
                    });
                    //return Connection.Send(connectionId, "Support is assisting another user. You are currently " + waiting_users.Count.ToString() + " in line. Please wait...");
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
                return Connection.Send(connectionId, "Support is currently offline. Please send an email to cdonohue@autoplusap.com for urgent inquiries.");

            if (!string.IsNullOrEmpty(group_ids[0]) && !string.IsNullOrEmpty(group_ids[1]) && !group_ids.Contains(connectionId))
                return Connection.Send(connectionId, "Support is assisting another user. Please wait...");

            if (string.IsNullOrEmpty(group_ids[1]))
            {
                return base.OnReceived(request, connectionId, "");
            }

            if (connectionId == operator_id)
                data = "Support: " + data;
            else
                data = "User: " + data;
            sb.AppendLine(data);
            return Task.Factory.StartNew(() =>
            {
                Connection.Send(group_ids[0], data);
                /*******
                 * To force end the chat with user, support can enter "EXIT"
                 *******/
                if (data.Equals("Support: EXIT"))
                {
                    Connection.Send(group_ids[1], "You have been disconnected. To get back in support queue, please refresh the page.");
                    Connection.Send(group_ids[0], "QUEUEUPDATE" + waiting_users.Count().ToString());
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
                    Connection.Send(group_ids[0], "QUEUEUPDATE" + waiting_users.Count().ToString());
                    base.OnDisconnected(request, connectionId);
                });
            }

            if (string.IsNullOrEmpty(group_ids[1]))
            {
                return Task.Factory.StartNew(() =>
                {
                    Connection.Send(group_ids[0], "QUEUEUPDATE" + waiting_users.Count().ToString());
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
                return Connection.Broadcast("Support is currently offline. Please send an email to cdonohue@autoplusap.com for urgent inquiries.");
            }
            else
            {
                group_ids[1] = string.Empty;
                if (waiting_users.Count > 0)
                {
                    group_ids[1] = waiting_users.Dequeue();
                    return Task.Factory.StartNew(() =>
                    {
                        foreach (string user in waiting_users)
                        {
                            Connection.Send(user, "Position in line changed to: " + CustomQ<string>.Position(waiting_users, user).ToString());
                        }
                        Connection.Send(group_ids[1], "You are now connected to Support");
                        Connection.Send(group_ids[0], string.Format("================User has left================<br/>New User has joined"));
                        Connection.Send(group_ids[0], "QUEUEUPDATE" + waiting_users.Count().ToString());
                    });
                }
                else
                {
                    return Connection.Send(group_ids[0], string.Format("==============User has left===============<br/>There are no more users in queue"));
                }
            }
        }

        public void SaveChatToFile(string message)
        {
            Debug.Write(message);
        }
    }
}