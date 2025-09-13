//+------------------------------------------------------------------+
//|                                                      wspubsub.js |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

// from command line arguments drop off two first of them (node.exe and current js-file)
const args = process.argv.slice(2);
const secure = args.length > 0 ? 'https' : 'http'; // secure connection flag

// attach dependencies
const fs = require('fs');
const http1 = require(secure);
const WebSocket = require('ws');
const crypto = require('crypto');

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

   if(req.url == '/')
   {
      req.url = "wspubsub.htm";
   }

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
         console.log('File not fount:' + req.url);
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
const publishers = new Map();
const subscribers = new Map();
var count = 0;

// create a websocket-server object
const wsServer = new WebSocket.Server({ server });
wsServer.on('connection', function onConnect(client)
{
   console.log('New user:', ++count, client.protocol);
   if(client.protocol.startsWith('X-MQL5-publisher'))
   {
      const parts = client.protocol.split('-');
      if(parts.length != 5)
      {
         console.log('Incomplete publisher ID');
         client.send('{"origin":"Server", "msg":"Incomplete publisher ID"}');
         client.close();
         return;
      }

      client.id = parts[3];
      client.key = parts[4];

      if(client.id == "Server")
      {
         console.log('Login "Server" is reserved');
         client.send('{"origin":"Server", "msg":"Login \'Server\' is reserved"}');
         return;
      }

      if(publishers.get(client.id))
      {
         console.log('Publisher is already connected');
         client.send('{"origin":"Server", "msg":"Publisher is already connected: ' + client.id + '"}');
         return;
      }

      publishers.set(client.id, client);
      client.send('{"origin":"Server", "msg":"Hello, publisher ' + client.id + '"}');

      client.on('message', function(message)
      {
         try
         {
            console.log('%s : %s', client.id, message);
            
            if(subscribers.get(client.id))
               subscribers.get(client.id).forEach(function(elem)
            {
               try
               {
                  elem.send('{"origin":"publisher ' + client.id + '", "msg":' + message + '}');
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
         console.log('Publisher disconnected:', client.id);
         if(subscribers.get(client.id))
            subscribers.get(client.id).forEach(function(elem)
         {
            elem.close();
         });
         publishers.delete(client.id);
      });

   }
   else if(client.protocol.startsWith('X-MQL5-subscriber'))
   {
      const parts = client.protocol.split('-');
      if(parts.length != 6)
      {
         console.log('Incomplete subscriber ID');
         client.send('{"origin":"Server", "msg":"Incomplete subscriber ID"}');
         client.close();
         return;
      }

      client.id = parts[3];
      client.pub_id = parts[4];
      client.access = parts[5];

      if(client.id == "Server")
      {
         console.log('Login "Server" is reserved');
         client.send('{"origin":"Server", "msg":"Login \'Server\' is reserved"}');
         client.close();
         return;
      }

      const id = client.pub_id;
      var p = publishers.get(id);
      if(p)
      {
         const check = crypto.createHash('sha256').update(id + ':' + p.key + ':' + client.id).digest('hex');
         if(check != client.access)
         {
            console.log(`Bad credentials: '${client.access}' vs '${check}'`);
            client.send('{"origin":"Server", "msg":"Bad credentials, subscriber ' + client.id + '"}');
            client.close();
            return;
         }

         var list = subscribers.get(id);
         if(list == undefined)
         {
            list = [];
         }
         else
         {
            const found = list.find(function(el) { return el.id === client.id; });
            if(found)
            {
               console.log(`Subscriber '${client.id}' is already connected to '${id}'`);
               client.send('{"origin":"Server", "msg":"Subscriber is already connected: ' + client.id + '"}');
               client.close();
               return;
            }
         }
         list.push(client);
         subscribers.set(id, list);
         client.send('{"origin":"Server", "msg":"Hello, subscriber ' + client.id + '"}');
         p.send('{"origin":"Server", "msg":"New subscriber ' + client.id + '"}');
      }
      else
      {
         console.log('Auto-closing subscriber', client.id);
         client.send('{"origin":"Server", "msg":"Publisher not found"}');
         client.close();
         return;
      }
      
      client.on('close', function()
      {
         console.log('Subscriber disconnected:', client.id);
         const list = subscribers.get(client.pub_id);
         if(list)
         {
            if(list.length > 1)
            {
               const filtered = list.filter(function(el) { return el !== client; });
               subscribers.set(client.pub_id, filtered);
            }
            else
            {
               subscribers.delete(client.pub_id);
            }
         }
      });
   }
   else
   {
      console.log('No X-MQL5-headers, disconnecting');
      client.close();
   }
});
//+------------------------------------------------------------------+
