//+------------------------------------------------------------------+
//|                                                        access.js |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

const args = process.argv.slice(2);

const input = args.length > 0 ? args[0] : 'PUB_ID_001:PUB_KEY_FFF:SUB_ID_100';
console.log('Hashing "', input, '"');

const crypto = require('crypto');

console.log(crypto.createHash('sha256').update(input).digest('hex'));

/*

Command:
>node access.js PUB_ID_001:PUB_KEY_FFF:SUB_ID_100

Output:
Hashing " PUB_ID_001:PUB_KEY_FFF:SUB_ID_100 "
fd3f7a105eae8c2d9afce0a7a4e11bf267a40f04b7c216dd01cf78c7165a2a5a

*/
//+------------------------------------------------------------------+
