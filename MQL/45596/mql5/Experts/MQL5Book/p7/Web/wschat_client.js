//+------------------------------------------------------------------+
//|                                                 wschat_client.js |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

const proto = window.location.protocol.startsWith('http') ?
              window.location.protocol.replace('http', 'ws') : 'ws:';
const ws = new WebSocket(proto + '//' + window.location.hostname + ':9000');

ws.onopen = function()
{
   console.log('Connected');
};

ws.onmessage = function(message)
{
   console.log('Message: %s', message.data);
   var parts = message.data.split('#');
   document.getElementById('origin').innerText = parts[0]; 
   document.getElementById('echo').value = parts[1];
   if(parts[0] == 'server' && parts[1].startsWith('Hello,'))
   {
      document.getElementById('user').innerText = parts[1].substring(6);
   } 
};

const button = document.querySelectorAll('button'); // get all button-tags

// submit button
button[0].addEventListener('click', (event) =>
{
   const x = document.getElementById('message').value;
   if(x) ws.send(x);
});

// close button
button[1].addEventListener('click', (event) =>
{
   ws.close();
   document.getElementById('user').innerText = 'disconnected';
   Array.from(document.getElementsByTagName('button')).forEach((e) =>
   {
      e.disabled = true;
   });
});
//+------------------------------------------------------------------+
