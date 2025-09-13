//+------------------------------------------------------------------+
//|                                                         news.mq4 |
//|                                             Copyright © 2016 Tor |
//|                                              http://einvestor.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2016 Tor"
#property link      "http://einvestor.ru/"
#property version   "1.0"
#property description "This Expert Advisor loads the News from the site Investing.com without using .dll"
#property strict

input  int AfterNewsStop=5; // Indent after News, minuts
input  int BeforeNewsStop=5; // Indent before News, minuts
input bool NewsLight= false; // Enable light news
input bool NewsMedium=false; // Enable medium news
input bool NewsHard=true; // Enable hard news
input int  offset=3;     // Your Time Zone, GMT (for news)
input string NewsSymb="USD,EUR,GBP,CHF,CAD,AUD,NZD,JPY"; //Currency to display the news (empty - only the current currencies) 
input bool  DrawLines=true;       // Draw lines on the chart
input bool  Next           = false;      // Draw only the future of news line
input bool  Signal         = false;      // Signals on the upcoming news

color highc          = clrRed;     // Colour important news
color mediumc        = clrBlue;    // Colour medium news
color lowc           = clrLime;    // The color of weak news
int   Style          = 2;          // Line style
int   Upd            = 86400;      // Period news updates in seconds

bool  Vhigh          = false;
bool  Vmedium        = false;
bool  Vlow           = false;
int   MinBefore=0;
int   MinAfter=0;

int NomNews=0;
string NewsArr[4][1000];
int Now=0;
datetime LastUpd;
string str1;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {

   if(StringLen(NewsSymb)>1)str1=NewsSymb;
   else str1=Symbol();

   Vhigh=NewsHard;
   Vmedium=NewsMedium;
   Vlow=NewsLight;
   
   MinBefore=BeforeNewsStop;
   MinAfter=AfterNewsStop;

//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   ObjectsDeleteAll(0,OBJ_VLINE);
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---

   double CheckNews=0;
   if(AfterNewsStop>0)
     {
      if(TimeCurrent()-LastUpd>=Upd){Comment("News Loading...");Print("News Loading...");UpdateNews();LastUpd=TimeCurrent();Comment("");}
      WindowRedraw();
      //---Draw a line on the chart news--------------------------------------------
      if(DrawLines)
        {
         for(int i=0;i<NomNews;i++)
           {
            string Name=StringSubstr(TimeToStr(TimeNewsFunck(i),TIME_MINUTES)+"_"+NewsArr[1][i]+"_"+NewsArr[3][i],0,63);
            if(NewsArr[3][i]!="")if(ObjectFind(Name)==0)continue;
            if(StringFind(str1,NewsArr[1][i])<0)continue;
            if(TimeNewsFunck(i)<TimeCurrent() && Next)continue;

            color clrf = clrNONE;
            if(Vhigh && StringFind(NewsArr[2][i],"High")>=0)clrf=highc;
            if(Vmedium && StringFind(NewsArr[2][i],"Moderate")>=0)clrf=mediumc;
            if(Vlow && StringFind(NewsArr[2][i],"Low")>=0)clrf=lowc;

            if(clrf==clrNONE)continue;

            if(NewsArr[3][i]!="")
              {
               ObjectCreate(Name,0,OBJ_VLINE,TimeNewsFunck(i),0);
               ObjectSet(Name,OBJPROP_COLOR,clrf);
               ObjectSet(Name,OBJPROP_STYLE,Style);
               ObjectSetInteger(0,Name,OBJPROP_BACK,true);
              }
           }
        }
      //---------------event Processing------------------------------------
      int i;
      CheckNews=0;
      for(i=0;i<NomNews;i++)
        {
         int power=0;
         if(Vhigh && StringFind(NewsArr[2][i],"High")>=0)power=1;
         if(Vmedium && StringFind(NewsArr[2][i],"Moderate")>=0)power=2;
         if(Vlow && StringFind(NewsArr[2][i],"Low")>=0)power=3;
         if(power==0)continue;
         if(TimeCurrent()+MinBefore*60>TimeNewsFunck(i) && TimeCurrent()-MinAfter*60<TimeNewsFunck(i) && StringFind(str1,NewsArr[1][i])>=0)
           {
            CheckNews=1;
            break;
           }
         else CheckNews=0;

        }
      if(CheckNews==1 && i!=Now && Signal) { Alert("In ",(int)(TimeNewsFunck(i)-TimeCurrent())/60," minutes released news ",NewsArr[1][i],"_",NewsArr[3][i]);Now=i;}
/***  ***/
     }

   if(CheckNews>0)
     {
      /////  We are doing here if we are in the framework of the news
      Comment("News time");

     }else{
      // We are out of scope of the news release (No News)
      Comment("No news");

     }

  }
//+------------------------------------------------------------------+
//////////////////////////////////////////////////////////////////////////////////
// Download CBOE page source code in a text variable
// And returns the result
//////////////////////////////////////////////////////////////////////////////////
string ReadCBOE()
  {

   string cookie=NULL,headers;
   char post[],result[];     string TXT="";
   int res;
//--- to work with the server, you must add the URL "https://www.google.com/finance"  
//--- the list of allowed URL (Main menu-> Tools-> Settings tab "Advisors"): 
   string google_url="http://ec.forexprostools.com/?columns=exc_currency,exc_importance&importance=1,2,3&calType=week&timeZone=15&lang=1";
//--- 
   ResetLastError();
//--- download html-pages
   int timeout=5000; //--- timeout less than 1,000 (1 sec.) is insufficient at a low speed of the Internet
   res=WebRequest("GET",google_url,cookie,NULL,timeout,post,0,result,headers);
//--- error checking
   if(res==-1)
     {
      Print("WebRequest error, err.code  =",GetLastError());
      MessageBox("You must add the address ' "+google_url+"' in the list of allowed URL tab 'Advisors' "," Error ",MB_ICONINFORMATION);
      //--- You must add the address ' "+ google url"' in the list of allowed URL tab 'Advisors' "," Error "
     }
   else
     {
      //--- successful download
      //PrintFormat("File successfully downloaded, the file size in bytes  =%d.",ArraySize(result)); 
      //--- save the data in the file
      int filehandle=FileOpen("news-log.html",FILE_WRITE|FILE_BIN);
      //--- проверка ошибки 
      if(filehandle!=INVALID_HANDLE)
        {
         //---save the contents of the array result [] in file 
         FileWriteArray(filehandle,result,0,ArraySize(result));
         //--- close file 
         FileClose(filehandle);

         int filehandle2=FileOpen("news-log.html",FILE_READ|FILE_BIN);
         TXT=FileReadString(filehandle2,ArraySize(result));
         FileClose(filehandle2);
        }else{
         Print("Error in FileOpen. Error code =",GetLastError());
        }
     }

   return(TXT);
  }
//+------------------------------------------------------------------+
datetime TimeNewsFunck(int nomf)
  {
   string s=NewsArr[0][nomf];
   string time=StringConcatenate(StringSubstr(s,0,4),".",StringSubstr(s,5,2),".",StringSubstr(s,8,2)," ",StringSubstr(s,11,2),":",StringSubstr(s,14,4));
   return((datetime)(StringToTime(time) + offset*3600));
  }
//////////////////////////////////////////////////////////////////////////////////
void UpdateNews()
  {
   string TEXT=ReadCBOE();
   int sh = StringFind(TEXT,"pageStartAt>")+12;
   int sh2= StringFind(TEXT,"</tbody>");
   TEXT=StringSubstr(TEXT,sh,sh2-sh);

   sh=0;
   while(!IsStopped())
     {
      sh = StringFind(TEXT,"event_timestamp",sh)+17;
      sh2= StringFind(TEXT,"onclick",sh)-2;
      if(sh<17 || sh2<0)break;
      NewsArr[0][NomNews]=StringSubstr(TEXT,sh,sh2-sh);

      sh = StringFind(TEXT,"flagCur",sh)+10;
      sh2= sh+3;
      if(sh<10 || sh2<3)break;
      NewsArr[1][NomNews]=StringSubstr(TEXT,sh,sh2-sh);
      if(StringFind(str1,NewsArr[1][NomNews])<0)continue;

      sh = StringFind(TEXT,"title",sh)+7;
      sh2= StringFind(TEXT,"Volatility",sh)-1;
      if(sh<7 || sh2<0)break;
      NewsArr[2][NomNews]=StringSubstr(TEXT,sh,sh2-sh);
      if(StringFind(NewsArr[2][NomNews],"High")>=0 && !Vhigh)continue;
      if(StringFind(NewsArr[2][NomNews],"Moderate")>=0 && !Vmedium)continue;
      if(StringFind(NewsArr[2][NomNews],"Low")>=0 && !Vlow)continue;

      sh=StringFind(TEXT,"left event",sh)+12;
      int sh1=StringFind(TEXT,"Speaks",sh);
      sh2=StringFind(TEXT,"<",sh);
      if(sh<12 || sh2<0)break;
      if(sh1<0 || sh1>sh2)NewsArr[3][NomNews]=StringSubstr(TEXT,sh,sh2-sh);
      else NewsArr[3][NomNews]=StringSubstr(TEXT,sh,sh1-sh);

      NomNews++;
      if(NomNews==300)break;
     }
  }
//+------------------------------------------------------------------+
