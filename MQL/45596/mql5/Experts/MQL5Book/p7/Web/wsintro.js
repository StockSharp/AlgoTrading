//+------------------------------------------------------------------+
//|                                                       wsintro.js |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

// attach ("#include") 'ws'-module which provides WebSockets functionality
const WebSocket = require('ws');

// choose network port number and create WebSocket server running on it
const port = 9000;
const wss = new WebSocket.Server({ port: port });

// display feedback for ongoing actions in console (command line window)
console.log('listening on port: ' + port);

// define in-place event handler for new connections,
// function doesn't have to have a name, because it's used in-place
wss.on('connection', function(channel)
{
   // for every connection assign event handler for new messages:
   // the event handler will not be triggered, until a message arrives
   channel.on('message', function(message)
   {
      // show message in console for debug purpose
      console.log('message: ' + message);
      // just send out the same message back to correspondent
      channel.send('echo: ' + message);
   });

   console.log('new client connected!');
   // send "hello" notification to new client
   channel.send('connected!');
});
//+------------------------------------------------------------------+
