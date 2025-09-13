//+------------------------------------------------------------------+
//|                                                        wschat.js |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

// from command line arguments drop off two first of them (node.exe and current js-file)
const args = process.argv.slice(2);
const secure = args.length > 0 ? 'https' : 'http'; // secure connection flag
const deflate = args.length > 1 ? args[1] : false; // compression switches on/off by file size limit

// attach dependencies
const fs = require('fs');
const http1 = require(secure);
const WebSocket = require('ws');

// attach key files for secure connections (if https used)
const options = args.length > 0 ?
{
   key : fs.readFileSync(`${args[0]}.key`),
   cert : fs.readFileSync(`${args[0]}.crt`)
} : null;

// create a http-server object for start page
http1.createServer(options, function (req, res)
{
   console.log(req.method, req.url);
   console.log(req.headers);

   if(req.url == '/') req.url = "wschat.htm";

   fs.readFile('./' + req.url, (err, data) =>
   {
      if(!err)
      {
         var dotoffset = req.url.lastIndexOf('.');
         var mimetype = dotoffset == -1 ? 'text/plain' :
         {
            '.htm' : 'text/html',
            '.html' : 'text/html',
            '.css' : 'text/css',
            '.js' : 'text/javascript',
            '.jpg' : 'image/jpeg',
            '.png' : 'image/png',
            '.ico' : 'image/x-icon',
            '.gif' : 'image/gif'
         }[ req.url.substr(dotoffset) ];
         res.setHeader('Content-Type', mimetype == undefined ? 'text/plain' : mimetype);
         res.end(data);
      }
      else
      {
         console.log('File not fount: ' + req.url);
         res.writeHead(404, "Not Found");
         res.end();
      }
  });

}).listen(secure == 'https' ? 443 : 80);

// create a http-server object for websocket-server handshaking
const server = new http1.createServer(options).listen(9000);
server.on('upgrade', function(req, socket, head)
{
   console.log(req.headers); // TODO: aux authorization
});

// keep track of users
const clients = new Map();
var count = 0;

// create a websocket-server object
const wsServer = new WebSocket.Server({ perMessageDeflate: deflate ? {threshold: deflate} : false, server });
wsServer.on('connection', function onConnect(client)
{
   console.log('New user:', ++count);
   client.id = count; 
   client.send('server#Hello, user' + count);
   clients.set(count, client);

   client.on('message', function(message)
   {
      try
      {
         console.log('%d : %s', client.id, message);
         Array.from(clients.values()).forEach(function(elem)
         {
            try
            {
               elem.send('user' + client.id + '#' + message);
            }
            catch(e)
            {
               console.log('Disconnected:', elem.id, e);
            }
         });
      }
      catch(error)
      {
         console.log('Error', error);
      }
   });

   client.on('close', function()
   {
      console.log('User disconnected:', client.id);
      clients.delete(client.id);
   });
});
//+------------------------------------------------------------------+
