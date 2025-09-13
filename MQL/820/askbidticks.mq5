//+------------------------------------------------------------------------------+
//|                                                              AskBidTicks.mq5 |
//|                                                                    Erdem SEN |
//+------------------------------------------------------------------------------+
#property copyright "2012-2015 Erdem Sen"
#property version   "1.8"
#property link      "http://login.mql5.com/en/users/erdogenes"
//--- description
#property description   "AskBidTicks is a high precission tick data collector for microstructure analysis."
#property description   "It's designed to export real-time tick data into a csv file."
#property description   "It works with local computer time (for Windows)."
//--- a 16 Byte structure to contain system time
struct SYSTEMTIME
  {
   int               YearMonth;
   int               DayofweekDayofmonth;
   int               HourMinute;
   int               SecondMillisecond;
  };
//--- importing Windows system file kernel32
#import  "kernel32.dll"
bool   GetLocalTime(SYSTEMTIME &lt); // function to get high precission local time
#import
//--- an enumeration for delimiters
enum DELIMITER
  {
   CSV_TAB        ='\t',   // Tab
   CSV_COMMA      =',',    // Comma
   CSV_SEMICOLON  =';'     // Semicolon
  };
//--- an enumeration for timestamps
enum TIME_STAMPS
  {
   STANDARD_MODE,          // Standard  (YYYY.MM.DD hh:mm:ss)
   MILLISECOND_MODE,       // Systemtime  (YYYY.MM.DD hh:mm:ss.msc)
   ANALYSIS_MODE,          // Analysis  (only milliseconds)
  };
//--- Input Parameters
input string      FileName    = "Use default";   // Enter a file name
input DELIMITER   Delimiter   = CSV_TAB;         // Choose delimiter
input TIME_STAMPS TimeStamp   = MILLISECOND_MODE;// Choose a format for timestamps
//--- global variables
SYSTEMTIME  st;
int         filehandle;
string      time_stamp,tick_ask,tick_bid;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- set a name for the file
   string filename;
   if(FileName=="Use default")
     {
      filename=TimeToString(TimeLocal(),TIME_DATE);
      filename= StringFormat("%s_%s.csv",filename,_Symbol);
     }
   else filename=FileName+".csv";
//--- set the delimiter and open the file
   short delimiter=(short)Delimiter;
   filehandle=FileOpen(filename,FILE_WRITE|FILE_CSV,delimiter);
//--- show the target file path
   if(filehandle!=INVALID_HANDLE)
     {
      string path=TerminalInfoString(TERMINAL_DATA_PATH)+"\\MQL5\\Files";
      PrintFormat("Exportation has started \n%s\\%s",path,filename);
      //--- write titles of each column into the file
      FileWrite(filehandle,"Time","Symbol","Ask","Bid");
     }
   else Print("Invalid handle, error!",GetLastError());
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTick()
  {
//--- setting timestamps     
   GetLocalTime(st);
   time_stamp=StringFormat("%d.%02d.%02d  %02d:%02d:%02d.%03d",
                           st.YearMonth & 0xFFFF,
                           st.YearMonth>>16,
                           st.DayofweekDayofmonth>>16,
                           st.HourMinute & 0xFFFF,
                           st.HourMinute>>16,
                           st.SecondMillisecond & 0xFFFF,
                           st.SecondMillisecond >>16);
   if(TimeStamp==STANDARD_MODE)
      time_stamp=TimeToString(TimeLocal(),TIME_DATE|TIME_SECONDS);
   else if(TimeStamp==ANALYSIS_MODE)
     {
      static uint first_tick_time=GetTickCount();
      uint        new_tick_time     =  GetTickCount()-first_tick_time;
      time_stamp                    =  IntegerToString(new_tick_time);
     }
//--- copy ticks
   tick_ask = DoubleToString(SymbolInfoDouble(_Symbol,SYMBOL_ASK),_Digits);
   tick_bid = DoubleToString(SymbolInfoDouble(_Symbol,SYMBOL_BID),_Digits);
//--- export to csv and show the stream in toolbox    
   if(filehandle!=INVALID_HANDLE)
     {
      FileWrite(filehandle,time_stamp,_Symbol,tick_ask,tick_bid);
      PrintFormat("%s\t%s\task:%s\tbid:%s",time_stamp,_Symbol,tick_ask,tick_bid);
     }
   else  Print("csv exportation is failed! ",GetLastError());
//---
  }
//+------------------------------------------------------------------------------+
