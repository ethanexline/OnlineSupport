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
        /// operator_id is set when support is online
        /// </summary>
        static string operator_id = string.Empty;
        /// <summary>
        /// sb stores all messages that transpire in a chat, to be saved on disconnect
        /// </summary>
        static StringBuilder sb = new StringBuilder();
        /// <summary>
        /// operator_added_to_group is set when support is online
        /// </summary>
        static bool operator_added_to_group = false;
        /// <summary>
        /// holds currently chatting members, all other users will be stored in queue 
        /// group_ids[0] is for support
        /// group_ids[1] is for user
        /// </summary>
        static string[] group_ids = { string.Empty, string.Empty };
        /// <summary>
        /// contains users waiting for active session (subsequent to the first user)
        /// </summary>
        static Queue<string> waiting_users = new Queue<string>();

        
        protected override Task OnConnected(IRequest request, string connectionId)
        {

            if (OnlineSupport.operator_online)
            {
                //if support login was done and there wasn't already a session in the support role
                if (!operator_added_to_group)
                {
                    operator_id = connectionId;
                    group_ids[0] = connectionId;

                    operator_added_to_group = true;

                    if (string.IsNullOrWhiteSpace(group_ids[1]))
                    {
                        if (!(waiting_users.Count < 1))
                        {
                            group_ids[1] = waiting_users.Dequeue();
                            return Task.Factory.StartNew(() =>
                            {
                                Connection.Broadcast(string.Format("Support is now online.<br/>"), new string[] { group_ids[0] });

                                foreach (string user in waiting_users)
                                {
                                    Connection.Send(operator_id, "QUEUEADD" + user);
                                    Connection.Send(user, string.Format("QUEUEPOS" + CustomQ<string>.Position(waiting_users, user).ToString()));
                                }

                                Connection.Send(group_ids[1], "QUEUEFIN");
                                Connection.Send(group_ids[1], string.Format("You are now connected to Support.<br/>"));
                                
                                Connection.Send(operator_id, "SUPPORTVIEW");
                                Connection.Send(operator_id, string.Format("New user has joined<br/>"));
                            });
                        }
                        else
                        {
                            return Task.Factory.StartNew(() =>
                            {
                                Connection.Send(operator_id, "SUPPORTVIEW");
                                Connection.Send(operator_id, string.Format("No user is connected<br/>"));
                            });
                        }
                    }
                    //if support disconnects and reconnects with a session still in the active position
                    else
                    {
                        if (!(waiting_users.Count < 1))
                        {
                            return Task.Factory.StartNew(() =>
                            {
                                Connection.Broadcast(string.Format("Support is now online.<br/>"), new string[] { group_ids[0] });

                                foreach (string user in waiting_users)
                                {
                                    Connection.Send(operator_id, "QUEUEADD" + user);
                                    Connection.Send(user, string.Format("QUEUEPOS" + CustomQ<string>.Position(waiting_users, user).ToString()));
                                }

                                Connection.Send(group_ids[1], string.Format("You are now connected to Support.<br/>"));

                                Connection.Send(operator_id, "SUPPORTVIEW");
                                Connection.Send(operator_id, string.Format("New user has joined<br/>"));
                            });
                        }
                        else
                        {
                            return Task.Factory.StartNew(() =>
                            {
                                Connection.Send(operator_id, "SUPPORTVIEW");
                                Connection.Send(operator_id, string.Format("No user is connected<br/>"));
                            });
                        }
                    }
                }

            }
            //if site is visited the regular way and no session in support role
            else
            {
                waiting_users.Enqueue(connectionId);
                return Task.Factory.StartNew(() =>
                {
                    Connection.Send(connectionId, "QUEUEPOS" + waiting_users.Count.ToString());
                    Connection.Send(connectionId, string.Format("Support is currently offline. Please send an email to 'whatever' for urgent inquiries.<br/>"));
                });
            }

            //if there's already a session in support role and no session in active position
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
            //since both roles already have active sessions, insert session into queue and inform them thusly
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
                return Connection.Send(connectionId, string.Format("Support is currently offline. Please send an email to 'whatever' for urgent inquiries.<br/>"));

            if (!string.IsNullOrEmpty(group_ids[0]) && !string.IsNullOrEmpty(group_ids[1]) && !group_ids.Contains(connectionId))
                return Connection.Send(connectionId, string.Format("Support is assisting another user. Please wait...</br>"));

            if (string.IsNullOrEmpty(group_ids[1]))
            {
                return base.OnReceived(request, connectionId, "");
            }

            /*******
             * these are system messages sent by the links that represent each user and session that are created when a user first enters the queue
             * ENDSESSION comes from clicking the X, and the user's connectionID is appended to the message so data.Substring(10) represents the 
             * connectionID of the user whose session we're ending
             *******/
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
            /*******
             * CHGSESSION comes from clicking the user, and the user's connectionID is appended to the message so data.Substring(10) represents the 
             * connectionID of the user whose session we're making the active session. The current active session is placed at the back of the queue,
             * the passed in one is placed in the active position, and every other user receives a queue position update
             *******/
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

            //if we've gotten this far, data is going to actually end up displayed in the feed and needs to be formatted thusly
            if (connectionId == operator_id)
                data = string.Format("<b>Support</b>: " + data + "<br/>");
            else
                data = string.Format("User: " + data + "<br/>");
            sb.AppendLine(data);
            return Task.Factory.StartNew(() =>
            {
                //support will always get the message
                Connection.Send(group_ids[0], data);

                /*******
                 * support entering "EXIT" immediately kills current active session 
                 *******/
                if (data.Equals("<b>Support</b>: EXIT<br/>"))
                {
                    Connection.Send(group_ids[1], string.Format("You have been disconnected. To get back in support queue, please refresh the page.<br/>"));
                    Connection.Send(group_ids[1], "QUEUEFIN");
                    OnDisconnected(request, group_ids[1]);
                }
                //otherwise, just send the message
                else
                    Connection.Send(group_ids[1], data);
            });
        }

        protected override Task OnDisconnected(IRequest request, string connectionId)
        {
            //if both active session positions are filled and the passed in connection is neither
            //e.g. a user in the queue closes the window or is remotely disconnected by support
            if (!string.IsNullOrEmpty(group_ids[0]) && !string.IsNullOrEmpty(group_ids[1]) && !group_ids.Contains(connectionId))
            {
                waiting_users = CustomQ<string>.RemoveItem(waiting_users, connectionId);
                if (!string.IsNullOrEmpty(group_ids[0]))
                {
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
                else
                {
                    return base.OnDisconnected(request, connectionId);
                }
            }

            //if there's no active session and nothing in queue, just make sure that session doesn't appear in support's queue list
            if (string.IsNullOrEmpty(group_ids[1]))
            {
                if (!string.IsNullOrEmpty(group_ids[0]))
                {
                    return Task.Factory.StartNew(() =>
                    {
                        Connection.Send(group_ids[0], "QUEUEDEL" + connectionId);
                        base.OnDisconnected(request, connectionId);
                    });
                }
                else
                {
                    return base.OnDisconnected(request, connectionId);
                }
            }

            sb.Append("====chat ends====\n");
            string message = sb.ToString();
            sb.Clear();
            SaveChatToFile(message);

            //if disconnecting user is support, inform all users that support is no longer online and do administration
            if (connectionId == operator_id)
            {
                OnlineSupport.operator_online = false;
                operator_added_to_group = false;
                group_ids[0] = string.Empty;

                return Connection.Broadcast(string.Format("Support is currently offline. Please send an email to 'whatever' for urgent inquiries.<br/>"));
            }
            else
            {
                string toRemove = group_ids[1];
                group_ids[1] = string.Empty;

                //if queue isn't empty, update queue positions and queue listing and give the next user in the queue the active position
                if (waiting_users.Count > 0)
                {
                    group_ids[1] = waiting_users.Dequeue();
                    if (!string.IsNullOrEmpty(group_ids[0]))
                    {
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
                        return base.OnDisconnected(request, connectionId);
                    }
                }
                //if queue is empty, update listing for support
                else
                {
                    if (!string.IsNullOrEmpty(group_ids[0]))
                    {
                        return Task.Factory.StartNew(() =>
                        {
                            Connection.Send(group_ids[0], string.Format("=================User has left===============<br/>There are no more users in queue<br/>"));
                            Connection.Send(group_ids[0], "QUEUEDEL" + toRemove);
                        });
                    }
                    else
                    {
                        return base.OnDisconnected(request, connectionId);
                    }
                }
            }
        }

        public void SaveChatToFile(string message)
        {
            Debug.Write(message);
        }
    }
}