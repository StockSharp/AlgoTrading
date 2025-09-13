//+------------------------------------------------------------------+
//|                                                    SaveTicks.mq5 |
//|                                               Alexey Volchanskiy |
//|                                            http://mql.gnomio.com |
//+------------------------------------------------------------------+
#property copyright "Alexey Volchanskiy"
#property link      "http://mql.gnomio.com"
#property version   "1.02"
#property description "SaveTicks save tick data for multiple symbols in files"
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum ESelectSymbols
  {
   EAllSymbols,        //All symbols
   EMarketWatchSymbols,//MarketWatch symbols
   ELoadFromFile       //Load list of symbols from file
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum EFormatRecording
  {
   ECsv,   //CSV
   EBin,   //Binary
   EAll    //CSV+Binary
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum EFormatTime
  {
   EServerTime,    //Server time
   ELocalTime      //Local windows time
  };

input int               TimerMsInterval = 500;                      //Recording interval 
input ESelectSymbols    SelectSymbols   = EMarketWatchSymbols;      //The symbols chosen as...
input string            SymbolsFileName = "InputSymbolList.txt";    //Name of file with all symbol names  
input EFormatRecording  FormatRecording = ECsv;                     //Format recording
input EFormatTime       FormatTime      = EServerTime;              //Time format

string SymbolList[];
int FileSymbolCsv[];
int FileSymbolBin[];
int FileSymbolList;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OnInit()
  {
   if(CreateSymbolList()==-1)
      ExpertRemove();
   EventSetMillisecondTimer(TimerMsInterval);
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   EventKillTimer();
   for(int n=0; n<ArraySize(SymbolList); n++)
     {
      FileClose(FileSymbolCsv[n]);
      FileClose(FileSymbolBin[n]);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTimer()
  {
   static string dt;
//static SYSTEMTIME st;
   static MqlTick mt;

   if(FormatTime==ELocalTime)
      dt=TimeToString(TimeLocal(),TIME_DATE|TIME_MINUTES|TIME_SECONDS);
   else
      dt=TimeToString(TimeCurrent(),TIME_DATE|TIME_MINUTES|TIME_SECONDS);
   for(int n=0; n<ArraySize(SymbolList); n++)
     {
      if(!SymbolInfoTick(SymbolList[n],mt))
         return;
      if(FormatRecording==ECsv || FormatRecording==EAll)
        {
         if(FileSymbolCsv[n]==INVALID_HANDLE)
            return;
         FileWrite(FileSymbolCsv[n],dt,mt.bid,mt.ask);
        }
      if(FormatRecording==EBin || FormatRecording==EAll)
        {
         if(FileSymbolBin[n]==INVALID_HANDLE)
            return;
         if(FormatTime==ELocalTime)
           {
            FileWriteLong(FileSymbolBin[n],TimeLocal());
            FileWriteStruct(FileSymbolBin[n],mt);
           }
         else
           {
            FileWriteLong(FileSymbolBin[n],TimeCurrent());
            FileWriteStruct(FileSymbolBin[n],mt);
           }
        }
     }

  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int CreateSymbolList()
  {
   int stotal=0;
   if(SelectSymbols==ELoadFromFile)
      stotal=LoadSymbolList(SymbolsFileName);
   else
     {
      stotal=SymbolsTotal((bool)SelectSymbols);
      ArrayResize(SymbolList,stotal);
     }
   ArrayResize(FileSymbolCsv,stotal);
   ArrayResize(FileSymbolBin,stotal);
   for(int n=0; n<stotal; n++)
     {
      if(SelectSymbols != ELoadFromFile)
         SymbolList[n] = SymbolName(n, (bool)SelectSymbols);
      string fname=SymbolList[n]+"_"+MQLInfoString(MQL_PROGRAM_NAME);
      if(FormatRecording==ECsv || FormatRecording==EAll)
        {
         FileSymbolCsv[n]=FileOpen(fname+".csv",FILE_WRITE|FILE_READ|FILE_SHARE_READ|FILE_CSV|FILE_ANSI,',');
         if(FileSymbolCsv[n]==INVALID_HANDLE)
           {
            Alert("Can't open file "+fname,"Error of opening file");
            return -1;
           }
         FileSeek(FileSymbolCsv[n],0,SEEK_END);
        }
      if(FormatRecording==EBin || FormatRecording==EAll)
        {
         FileSymbolBin[n]=FileOpen(fname+".bin",FILE_WRITE|FILE_READ|FILE_SHARE_READ|FILE_BIN);
         if(FileSymbolBin[n]==INVALID_HANDLE)
           {
            Alert("Can't open file "+fname,"Error of opening file");
            return -1;
           }
         FileSeek(FileSymbolBin[n],0,SEEK_END);
        }
     }
   return SaveSymbolList();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int SaveSymbolList()
  {
   string fname="AllSymbols_"+MQLInfoString(MQL_PROGRAM_NAME)+".txt";
   int file= FileOpen(fname,FILE_WRITE|FILE_TXT);
   if(file == INVALID_HANDLE)
     {
      Alert("Can't open file "+fname,"Error of opening file");
      return -1;
     }
   FileWrite(file,IntegerToString(ArraySize(SymbolList)));
   for(int n=0; n<ArraySize(SymbolList); n++)
      FileWrite(file,SymbolList[n]);
   FileClose(file);
   return ArraySize(SymbolList);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int LoadSymbolList(string fname)
  {
   int file=-1;
   file=FileOpen(fname,FILE_READ|FILE_TXT);
   if(file==INVALID_HANDLE)
     {
      Alert("Can't open file "+fname);
      return -1;
     }
   int size=(int)StringToInteger(FileReadString(file));
   Print("size=",size);
   if(size>0)
      ArrayResize(SymbolList,size);
   else
     {
      Alert("First string <size> is incorrect in file "+fname);
      return -1;
     }
   for(int n=0; n<ArraySize(SymbolList); n++)
     {
      SymbolList[n]=FileReadString(file);
      Print(SymbolList[n]);
     }
   FileClose(file);
   return size;
  }
//+------------------------------------------------------------------+
