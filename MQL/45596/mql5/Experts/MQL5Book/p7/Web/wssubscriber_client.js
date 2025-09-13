//+------------------------------------------------------------------+
//|                                           wssubscriber_client.js |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

const proto = window.location.protocol.startsWith('http') ?
              window.location.protocol.replace('http', 'ws') : 'ws:';
var ws;

const button = document.querySelectorAll('button');
const user = document.getElementById('user');

function disconnect()
{
   user.innerText = 'disconnected';
   button[0].disabled = false;
   button[1].disabled = true;
}

button[0].addEventListener('click', (event) =>
{
   if(event.target.innerText == 'Connect')
   {
      ws = new WebSocket(proto + '//' + window.location.hostname + ':9000',
         'X-MQL5-subscriber-'
         + document.getElementById('sub_id').value + "-"
         + document.getElementById('pub_id').value + "-"
         + document.getElementById('sub_key').value);

      ws.onopen = function()
      {
         console.log('Connected');
      };
      
      ws.onclose = function()
      {
         console.log('Disconnected');
         disconnect();
      }
      
      ws.onmessage = function(message)
      {
         console.log('Message: %s', message.data);
         const obj = ((text) =>
         {
            try
            {
               return JSON.parse(text);
            }
            catch(e)
            {
               console.log(e.message); return null;
            }
         })(message.data);

         if(obj)
         {
            document.getElementById('origin').innerText = obj.origin; 
            document.getElementById('echo').value = obj.msg;
            if(obj.origin == 'Server' && obj.msg && obj.msg.startsWith('Hello,'))
            {
               user.innerText = obj.msg.substring(6);
            }
            
            console.log(obj);
         }
         else
         {
            document.getElementById('origin').innerText = 'not a valid json'; 
            document.getElementById('echo').value = message.data;
         }
      };
      
      ws.onerror = function(e)
      {
         console.log(e);
         disconnect();
      }
      
      user.innerText = 'connecting...';
      button[0].disabled = true;   
      button[1].disabled = false;
   }
});

button[1].addEventListener('click', (event) =>
{
   if(ws) ws.close();
   disconnect();
});
//+------------------------------------------------------------------+
