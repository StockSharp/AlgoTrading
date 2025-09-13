//+------------------------------------------------------------------+
//|                                   _HPCS_Inter1_MT4_EA_V01_WE.mq4 |
//|                        Copyright 2021, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2021, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
      string ls_string;
      ushort lu_char;
      string ls_Result[];
      
      int li_Filehandle = FileOpen("Third.csv",FILE_READ|FILE_CSV|FILE_ANSI);
      if(li_Filehandle!=INVALID_HANDLE)
      {
      
         ls_string = FileReadString(li_Filehandle);
         Print(ls_string);
         
         lu_char = StringGetCharacter("_",0);
         int li_str = StringSplit(ls_string,lu_char,ls_Result);
         
         for(int i = 0 ; i<li_str ; i++)
         {
            Print(ls_Result[i]," ");
         }
      
      }      
      
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   
  }
//+------------------------------------------------------------------+
