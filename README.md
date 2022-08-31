# OnlineSupport
A complete working example of a 1 on 1 support chat (currently only supports 1 "operator") using .NET and SignalR.

- While Operator has an ongoing chat session with a user, subsequent visitors are placed into a queue. 
- Operator can see all users in queue, switching between or killing sessions at will, and can immediately end current session by entering message "EXIT". 
- Upon ending a session, next user in queue is automatically entered into session with Operator. 
- Users can see their current position in the queue, which is updated in real time.
- Users receive automatic updates when they're queued, re-queued (after a session switch), or their session is remotely ended
- Operator sees all this as a continuous chat feed.

