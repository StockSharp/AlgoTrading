//+------------------------------------------------------------------+
//|                                                  OutputSound.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include "PRTF.mqh"

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // file "new.txt" was created but will produce neither a sound, nor an error
   PRTF(PlaySound("new.txt"));
   // this file is missing
   PRTF(PlaySound("abracadabra.wav"));
   // lets measure the time neede to execute the function
   const uint start = GetTickCount();
   // this is a standard file, it should play ok
   PRTF(PlaySound("request.wav"));
   PRTF(GetTickCount() - start);
   /*
      output:
      
      PlaySound(new.txt)=true / ok
      PlaySound(abracadabra.wav)=false / FILE_NOT_EXIST(5019)
      PlaySound(request.wav)=true / ok
      GetTickCount()-start=0 / ok
   */
}
//+------------------------------------------------------------------+
