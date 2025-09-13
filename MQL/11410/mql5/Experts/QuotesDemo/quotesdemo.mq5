//+------------------------------------------------------------------+
//|                                                   QuotesDemo.mq5 |
//|                   Copyright 2009-2014, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2009-2014, MetaQuotes Software Corp."
#property link      "http://www.mql5.com"
#property version   "1.00"
//---
#property description "This Expert Advisor loads and displays Google Finance quotes using WebRequest.";
//---
#include "QuotesDialog.mqh"
//---
enum GoogleFinanceQuotesTable
  {
   ENUM_INDICES=0,     // World Indices
   ENUM_CURRENCIES,    // Currencies
   ENUM_BONDS,         // Bonds
  };
//---
input GoogleFinanceQuotesTable TableIndex=ENUM_INDICES;  // Google Finance quotes
//+------------------------------------------------------------------+
//| Global Variables                                                 |
//+------------------------------------------------------------------+
CQuotesDialog ExtDialog;
string   ExtCaption="";
//+------------------------------------------------------------------+
//| TestWebRequest                                                   |
//+------------------------------------------------------------------+
bool TestWebRequest()
  {
   string cookie=NULL,headers;
   char post[],result[];
   int res;
//---
   string google_url="https://www.google.com/finance";
//---
   ResetLastError();
   int timeout=5000; //--- timeout less than 1000 (1 sec.) is not sufficient for slow Internet speed
   res=WebRequest("GET",google_url,cookie,NULL,timeout,post,0,result,headers);
   if(res==-1) return(false);
   return(true);
  }
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   if(!TestWebRequest())
     {
      MessageBox("Add address 'https://www.google.com/finance' in Expert Advisors tab of the Options window","Information",MB_ICONINFORMATION);
      return(INIT_FAILED);
     }
//---
   EventSetTimer(10);
//---
  bool res=false;
 switch(TableIndex)
  {
   case ENUM_INDICES:
      ExtCaption="World indices";
      res=ExtDialog.Create(0,ExtCaption,0,10,10,335,430,1);
      break;
   case ENUM_CURRENCIES:
      ExtCaption="Currencies";
      res=ExtDialog.Create(0,ExtCaption,0,10,10,335,180,2);
      break;
   case ENUM_BONDS:
      ExtCaption="Bonds";
      res=ExtDialog.Create(0,ExtCaption,0,10,10,335,160,3);
  }
//---
   if(!res) return(INIT_FAILED);
//--- run application
   ExtDialog.Run();
//--- update quotes
   OnTimer();
//--- succeed
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//--- destroy dialog
   ExtDialog.Destroy(reason);
  }
//+------------------------------------------------------------------+
//| Expert chart event function                                      |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,         // event ID  
                  const long& lparam,   // event parameter of the long type
                  const double& dparam, // event parameter of the double type
                  const string& sparam) // event parameter of the string type
  {
   ExtDialog.ChartEvent(id,lparam,dparam,sparam);
  }
//+------------------------------------------------------------------+
void OnTimer()
  {
   static long last_time=0;
   if (GetTickCount()+5000>last_time) last_time=GetTickCount(); else return;
//---
   ExtDialog.Caption(ExtCaption+" : "+TimeToString(TimeLocal()));
   ExtDialog.UpdateQuotes();
  }
//+------------------------------------------------------------------+
