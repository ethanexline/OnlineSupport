# OnlineSupport
A complete working example of a 1 on 1 support chat (currently only supports 1 "operator") using .NET and SignalR.

- While Operator has an ongoing chat session with a user, subsequent visitors are placed into a queue. 
- Operator can see how many users are currently in queue and can automatically end current session by entering message "EXIT". 
- Upon ending a session, next user in queue is automatically entered into session with Operator. 
- Operator sees all this as a continuous chat feed.

