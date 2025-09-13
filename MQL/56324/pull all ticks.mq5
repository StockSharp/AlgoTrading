//+------------------------------------------------------------------+
//|                                                      ProjectName |
//|                                      Copyright 2020, CompanyName |
//|                                       http://www.companyname.net |
//+------------------------------------------------------------------+
#property version   "1.00"
#define MANAGER_FOLDER "pat"
#define MANAGER_STATUS_FILE "pat\\status.txt"
input bool limitDate=true;//Date limit ?
input datetime oldestLimit=D'2000.01.01';//time to stop
input uint tick_packets=300000;//Tick packets size one tick is 60bytes so adjust according to your ram/3 , if you have chrome open /6

//+------------------------------------------------------------------+
//|Init                                                              |
//+------------------------------------------------------------------+
int OnInit()
  {
   MANAGER.reset();
   EventSetMillisecondTimer(100);
   return(INIT_SUCCEEDED);
  }

//+------------------------------------------------------------------+
//|Timer                                                             |
//+------------------------------------------------------------------+
void OnTimer()
  {
//--- if synchronized
   if(SymbolIsSynchronized(_Symbol)&&TerminalInfoInteger(TERMINAL_CONNECTED)==1)
     {
      EventKillTimer();
      //--- load the system here
      if(MANAGER.load(MANAGER_FOLDER,MANAGER_STATUS_FILE))
        {
         //--- system loaded so we are scanning a symbol here
         Comment("System loaded and we are processing "+MANAGER.m_current);
         //--- tick load

         //--- find the oldest tick available in the broker first
         int attempts=0;
         int ping=-1;
         datetime cursor=flatten(TimeTradeServer());
         long cursorMSC=((long)cursor)*1000;
         long jump=2592000000;//60*60*24*30*1000;

         MqlTick receiver[];
         long oldest=LONG_MAX;
         Comment("PleaseWait");
         while(attempts<5)
           {
            ping=CopyTicks(_Symbol,receiver,COPY_TICKS_ALL,cursorMSC,1);
            if(ping==1)
              {
               if(receiver[0].time_msc==oldest)
                 {
                  attempts++;
                 }
               else
                 {
                  attempts=0;
                 }
               if(receiver[0].time_msc<oldest)
                 {
                  oldest=receiver[0].time_msc;
                 }
               cursorMSC-=jump;
               if(limitDate&&receiver[0].time<=oldestLimit)
                 {
                  break;
                 }
              }
            else
              {
               attempts++;
              }

            Sleep(44);
            Comment("Oldest Tick : "+TimeToString((datetime)(oldest/1000),TIME_DATE|TIME_MINUTES|TIME_SECONDS)+"\nCursor("+TimeToString((datetime)(cursorMSC/1000),TIME_DATE|TIME_MINUTES|TIME_SECONDS)+")\nAttempts("+IntegerToString(attempts)+")\nPlease wait for response...");
           }
         //--- at this point we have the oldest tick
         //--- start requesting ticks from the oldest to the newest
         if(oldest!=LONG_MAX)
           {
            ArrayFree(receiver);
            datetime newest_tick=0;
            //--- receive the time of the last tick for this symbol stored in symbol_time
            datetime most_recent_candle=(datetime)SymbolInfoInteger(_Symbol,SYMBOL_TIME);
            while(newest_tick<most_recent_candle)
              {
               //--- request a new batch starting from the oldest time with the ticks limit specified
               int pulled=CopyTicks(_Symbol,receiver,COPY_TICKS_ALL,oldest,tick_packets);
               if(pulled>0)
                 {
                  //--- if we pull a new batch update our downloaded times
                  newest_tick=receiver[pulled-1].time;
                  oldest=receiver[pulled-1].time_msc;
                  ArrayFree(receiver);
                 }
               //--- timeout server requests , alter it if you want
               Sleep(44);
               Comment("Pulled up to "+TimeToString(newest_tick,TIME_DATE|TIME_MINUTES|TIME_SECONDS)+" so far");
              }
           }
         else
           {
            Alert("Please close the terminal \n head over to the ticks folder \n and delete the empty folders");
            ExpertRemove();
           }
         //--- update the manager and move on
         MANAGER.manage(MANAGER_FOLDER,MANAGER_STATUS_FILE);
        }
      else
        {
         //--- grab the market watch symbols to start download
         Comment("Grabbing MW and starting");
         MANAGER.grab_symbols();
         MANAGER.manage(MANAGER_FOLDER,MANAGER_STATUS_FILE);
        }
     }
  }

//+------------------------------------------------------------------+
//|Deinit                                                            |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   EventKillTimer();
  }
void OnTick() {}

//+------------------------------------------------------------------+
//|milliseconds to datetime                                          |
//+------------------------------------------------------------------+
datetime mscToTime(long msc) {datetime result=(datetime)(msc/(long)1000);return(result);}
//+------------------------------------------------------------------+
//|flatten time                                                      |
//+------------------------------------------------------------------+
datetime flatten(datetime _time)
  {
   MqlDateTime mqt;
   if(TimeToStruct(_time,mqt))
     {
      mqt.day=1;
      mqt.hour=0;
      mqt.min=0;
      mqt.sec=0;
      _time=StructToTime(mqt);
     }
   return(_time);
  }

//+------------------------------------------------------------------+
//| Download Manager                                                 |
//+------------------------------------------------------------------+
struct CDownloadManager
  {
   bool              m_started,m_finished;
   string            m_symbols[],m_current;
   int               m_index;
                     CDownloadManager(void) {reset();}
                    ~CDownloadManager(void) {reset();}
   //+------------------------------------------------------------------+
   //| reset                                                            |
   //+------------------------------------------------------------------+
   void              reset()
     {
      m_index=0;
      m_started=false;
      m_finished=false;
      ArrayFree(m_symbols);
      m_current=NULL;
     }
   //+------------------------------------------------------------------+
   //| grab symbols from the market watch                               |
   //+------------------------------------------------------------------+
   void              grab_symbols()
     {
      //---  only from the mw !
      int s=SymbolsTotal(true);
      ArrayResize(m_symbols,s,0);
      for(int i=0;i<ArraySize(m_symbols);i++)
        {
         m_symbols[i]=SymbolName(i,true);
        }
     }
   //+------------------------------------------------------------------+
   //| Manage the download of symbols process                           |
   //+------------------------------------------------------------------+
   void              manage(string folder,string filename)
     {
      //--- essentially this starts or navigates to the next symbol
      if(ArraySize(m_symbols)>0)
        {
         //--- if not started
         if(!m_started)
           {
            m_started=true;
            //--- go to first symbol
            m_current=m_symbols[0];
            m_index=1;
            save(folder,filename);
            if(_Symbol!=m_current)
              {
               ChartSetSymbolPeriod(ChartID(),m_current,_Period);
              }
            else
              {
               //--- otherwise find a new period to renavigate to
               ENUM_TIMEFRAMES new_period=PERIOD_M1;
               for(int p=0;p<ArraySize(TFS);p++)
                 {
                  if(_Period!=TFS[p])
                    {
                     new_period=TFS[p];
                     break;
                    }
                 }
               ChartSetSymbolPeriod(ChartID(),m_current,new_period);
              }
            return;
           }
         //--- if started
         else
           {
            //--- advance to the next index in our list of symbols
            m_index++;
            if(m_index<=ArraySize(m_symbols))
              {
               m_current=m_symbols[m_index-1];
               save(folder,filename);
               if(_Symbol!=m_current)
                 {
                  //--- navigate to that symbol
                  ChartSetSymbolPeriod(ChartID(),m_current,_Period);
                 }
               return;
              }
            //--- if no more symbols to scan the proccess is finished
            else
              {
               m_finished=true;
               //--- delete the manager file
               FileDelete(folder+"\\"+filename);
               Print("Finished");
               ExpertRemove();
               return;
              }
           }
        }
      else
        {
         Print("Please grab symbols first");
        }
      //if set ends here
     }
   //+------------------------------------------------------------------+
   //| save                |
   //+------------------------------------------------------------------+
   void              save(string folder,string filename)
     {
      string location=folder+"\\"+filename;
      if(FileIsExist(location))
        {
         FileDelete(location);
        }
      //--- open file to write as binary
      int f=FileOpen(location,FILE_WRITE|FILE_BIN);
      if(f!=INVALID_HANDLE)
        {
         //--- the state of the manager
         FileWriteInteger(f,((char)m_started),CHAR_VALUE);
         FileWriteInteger(f,((char)m_finished),CHAR_VALUE);
         //--- the number of symbols to download
         FileWriteInteger(f,ArraySize(m_symbols),INT_VALUE);
         //--- the current downloaded index
         FileWriteInteger(f,m_index,INT_VALUE);
         //--- write all the symbols in the file
         for(int i=0;i<ArraySize(m_symbols);i++)
           {
            writeStringToFile(f,m_symbols[i]);
           }
         //--- write the current symbol
         writeStringToFile(f,m_current);
         FileClose(f);
        }
     }
   //+------------------------------------------------------------------+
   //| load                |
   //+------------------------------------------------------------------+
   bool              load(string folder,string filename)
     {
      reset();
      //--- if the file exists
      string location=folder+"\\"+filename;
      if(FileIsExist(location))
        {
         //--- open the file for reading as binary
         int f=FileOpen(location,FILE_READ|FILE_BIN);
         if(f!=INVALID_HANDLE)
           {
            //--- read the state of the manager
            m_started=(bool)FileReadInteger(f,CHAR_VALUE);
            m_finished=(bool)FileReadInteger(f,CHAR_VALUE);
            //--- read the number of symbols in the manager
            int total=(int)FileReadInteger(f,INT_VALUE);
            //--- read the current index proccessed
            m_index=(int)FileReadInteger(f,INT_VALUE);
            //--- if symbols exist
            if(total>0)
              {
               //--- presize the array to load them
               ArrayResize(m_symbols,total,0);
               //--- load each symbol
               for(int i=0;i<ArraySize(m_symbols);i++)
                 {
                  m_symbols[i]=readStringFromFile(f);
                 }
               //--- load the current symbol
               m_current=readStringFromFile(f);
              }
            FileClose(f);
            if(!m_finished&&total>0)
              {
               return(true);
              }
           }
        }
      return(false);
     }
  };
CDownloadManager MANAGER;

ENUM_TIMEFRAMES TFS[]= {PERIOD_M1,PERIOD_M5,PERIOD_M15,PERIOD_M30,PERIOD_H1,PERIOD_H4,PERIOD_D1};
int TFSmins[]= {1,5,15,30,60,240,1440};

//+------------------------------------------------------------------+
//|write string to file                                      |
//+------------------------------------------------------------------+
void writeStringToFile(int f,string thestring)
  {
//save symbol string
   char sysave[];
   int charstotal=StringToCharArray(thestring,sysave,0,StringLen(thestring),CP_ACP);
   FileWriteInteger(f,charstotal,INT_VALUE);
   for(int i=0;i<charstotal;i++)
     {
      FileWriteInteger(f,sysave[i],CHAR_VALUE);
     }
  }
//+------------------------------------------------------------------+
//|read string from file                                            |
//+------------------------------------------------------------------+
string readStringFromFile(int f)
  {
   string result="";
//load symbol string
   char syload[];
   int charstotal=(int)FileReadInteger(f,INT_VALUE);
   if(charstotal>0)
     {
      ArrayResize(syload,charstotal,0);
      for(int i=0;i<charstotal;i++)
        {
         syload[i]=(char)FileReadInteger(f,CHAR_VALUE);
        }
      result=CharArrayToString(syload,0,charstotal,CP_ACP);
     }
   return(result);
  }
//+------------------------------------------------------------------+
