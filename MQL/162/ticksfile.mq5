//+------------------------------------------------------------------+
//|                                                    TicksFile.mq5 |
//|                                                    2010, Avatara |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "2010, Avatara"                   //  10.08.10
#property link      "avatara@bigmir.net"
#property version   "1.00"
input bool diskret=false;        // work at bar opening? 
input string Filler=";";         // fields separtor in file
bool File=true;
MqlDateTime newT,oldT,oTimes;
datetime newTT,oldTT,oTimess;
string FName,Fillers;
int hF,per,count=0;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   oldTT=TimeCurrent()+51;TimeToStruct(oldTT,oldT);
   oldTT-=oldT.hour; oldTT-=oldT.min;
   per=PeriodSeconds();
   if(Filler==" ") Fillers="\t";
   else Fillers=Filler;
   StringConcatenate(FName,"T_",Symbol(),"_M",
                     IntegerToString(per/60),"_",
                     IntegerToString(oldT.year),"_",
                     IntegerToString(oldT.mon),"_",
                     IntegerToString(oldT.day),"_",
                     IntegerToString(oldT.hour),"x",
                     IntegerToString(oldT.min),
                     ".csv");
   if(File)
     {
      hF=FileOpen(FName,FILE_WRITE|FILE_CSV,Fillers);
      if(hF<-1) Print("Error open ",FName," ",GetLastError());
      FileWrite(hF,"day","mon","year","hour","min","S",
                "close","high","low","open","spread","tick_volume","t","ask",
                "bid","last","volume","N","H","M","close","high","low",
                "open","spread","tick_volume");
      Print("Start record ",FName);
     }
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   if(File)
     {
      FileClose(hF);
      Print(count," records in file ",FName," !");
     }
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   bool poz=true;
   MqlRates rates[2];
   MqlTick tick;
//------------------
   if(!SymbolInfoTick(Symbol(),tick))
     {
      Print("Failed to get Symbol info! M=",per/60);
      return;
     }
   newTT=tick.time;
   if(newTT<oldTT+per)
      if(diskret)return;
//=== else ===================================
   int copied=CopyRates(Symbol(),0,0,2,rates);
   if(copied<=0)
     {
      Print("Error copying price data ",GetLastError());
      return;
     }
   else oldTT=rates[1].time;  // go ahead !
   oTimess=rates[0].time;     // true time of the previous tick
   TimeToStruct(oTimess,oTimes);
   TimeToStruct(oldTT,oldT);
   TimeToStruct(newTT,newT);
   if(File) FileWrite(hF,oTimes.day,//Completed bar time
      oTimes.mon,
      oTimes.year,
      oTimes.hour,
      oTimes.min,
      "I",// Completed bar data
      rates[0].close,
      rates[0].high,
      rates[0].low,
      rates[0].open,
      rates[0].spread,
      rates[0].tick_volume,
      "T",// Arrived tick data
      tick.ask,
      tick.bid,
      tick.last,
      tick.volume,
      "N=",// Current (uncompleted) bar data
      newT.hour, // hour
      newT.min,  // minute
      rates[1].close,
      rates[1].high,
      rates[1].low,
      rates[1].open,
      rates[1].spread,
      rates[1].tick_volume);
   count++;
   return;
  }
//+------------------------------------------------------------------+
