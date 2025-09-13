//+------------------------------------------------------------------+
//|                                                      NEWS_Filter |
//|                                   Copyright 2022, Muhammad Haris |
//|                                             mharis.mt4@gmail.com |
//+------------------------------------------------------------------+

//Parts of the code were taken from MT4 News filter made by Vladimir Gribachev
//This Code can be compiled for both Mt4 and Mt5
//YOU MUST ADD THE ADDRESS "http://calendar.fxstreet.com/"IN THE LIST OF ALLOWED URL IN THE TAB 'ADVISERS'


#property copyright "Copyright 2022, Muhammad Haris"
#property link      "mharis.mt4@gmail.com"
#property version   "1.20"
#property strict
//---

struct sNews
  {
   datetime          dTime;
   string            time;
   string            currency;
   string            importance;
   string            news;
   string            Actual;
   string            forecast;
   string            previus;
  };

input bool               NEWS_FILTER               =true;
input bool               NEWS_IMPOTANCE_LOW        =false;
input bool               NEWS_IMPOTANCE_MEDIUM     =true;
input bool               NEWS_IMPOTANCE_HIGH       =true;
input int                STOP_BEFORE_NEWS          =30;
input int                START_AFTER_NEWS          =30;
input string             Currencies_Check          ="USD,EUR,CAD,AUD,NZD,GBP";
input bool               Check_Specific_News       =false;
input string             Specific_News_Text        ="employment";
input bool               DRAW_NEWS_CHART           = true;
input int                X                         = 10;//Chart X-Axis Position
input int                Y                         = 280;//Chart Y-Axis Position
input string             News_Font                 ="Arial";
input color              Font_Color                =clrRed;
input bool               DRAW_NEWS_LINES           =true;
input color              Line_Color                =clrRed;
input ENUM_LINE_STYLE    Line_Style                =STYLE_DOT;
input int                Line_Width                =1;

int Font_Size=8;
string LANG="en-US";
sNews NEWS_TABLE[],HEADS;
datetime date;
int TIME_CORRECTION,NEWS_ON=0;


//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   if(!MQLInfoInteger(MQL_TESTER) || !MQLInfoInteger(MQL_OPTIMIZATION))
     {
      if(NEWS_FILTER==true && READ_NEWS(NEWS_TABLE) && ArraySize(NEWS_TABLE)>0)
         DRAW_NEWS(NEWS_TABLE);
      //TIME_CORRECTION=((int(TimeCurrent() - TimeGMT()) + 1800) / 3600);
      TIME_CORRECTION=(-TimeGMTOffset());
     }
   EventSetTimer(1);

   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   DEINIT_PANEL();
   EventKillTimer();

  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTimer()
  {
   OnTick();
   if(NEWS_FILTER==false)
      return;
   static int waiting=0;
   if(waiting<=0)
     {
      if(!MQLInfoInteger(MQL_TESTER) || !MQLInfoInteger(MQL_OPTIMIZATION))
        {
         if(READ_NEWS(NEWS_TABLE))
            waiting=100;
         if(ArraySize(NEWS_TABLE)<=0)
            return;
         DRAW_NEWS(NEWS_TABLE);
        }
     }
   else
      waiting--;
   if(ArraySize(NEWS_TABLE)<=0)
      return;

   datetime time=TimeCurrent();
//---
   for(int i=0; i<ArraySize(NEWS_TABLE); i++)
     {
      datetime news_time=NEWS_TABLE[i].dTime+TIME_CORRECTION;
      bool Importance_Check=false;
      if((!NEWS_IMPOTANCE_LOW && NEWS_TABLE[i].importance=="*") ||
         (!NEWS_IMPOTANCE_MEDIUM && NEWS_TABLE[i].importance=="**") ||
         (!NEWS_IMPOTANCE_HIGH && NEWS_TABLE[i].importance=="***"))
         Importance_Check=true;
      if(Importance_Check || StringFind(Currencies_Check,NEWS_TABLE[i].currency,0)==-1 || (Check_Specific_News  && (StringFind(NEWS_TABLE[i].news,Specific_News_Text)==-1)))
         continue;
      if((news_time<=time && (news_time+(datetime)(START_AFTER_NEWS*60))>=time) ||
         (news_time>=time && (news_time-(datetime)(STOP_BEFORE_NEWS*60))<=time))
        {
         NEWS_ON=1;
         Comment("News Time...");
         break;
        }
      else
        {
         NEWS_ON=0;
         Comment("No News");
        }
     }
   return;
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---

  }
//+------------------------------------------------------------------+
void DEL_ROW(sNews &l_a_news[],int row)
  {
   int size=ArraySize(l_a_news)-1;
   for(int i=row; i<size; i++)
     {
      l_a_news[i].Actual=l_a_news[i+1].Actual;
      l_a_news[i].currency=l_a_news[i+1].currency;
      l_a_news[i].dTime=l_a_news[i+1].dTime;
      l_a_news[i].forecast=l_a_news[i+1].forecast;
      l_a_news[i].importance=l_a_news[i+1].importance;
      l_a_news[i].news=l_a_news[i+1].news;
      l_a_news[i].previus=l_a_news[i+1].previus;
      l_a_news[i].time=l_a_news[i+1].time;
     }
   ArrayResize(l_a_news,size);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool READ_NEWS(sNews &l_NewsTable[])
  {
   string cookie=NULL,referer=NULL,headers;
   char post[],result[];
   string tmpStr="";
   string st_date=TimeToString(TimeCurrent(),TIME_DATE),end_date=TimeToString((TimeCurrent()+(datetime)(7*24*60*60)),TIME_DATE);
   StringReplace(st_date,".","");
   StringReplace(end_date,".","");
   string url="http://calendar.fxstreet.com/EventDateWidget/GetMini?culture="+LANG+"&view=range&start="+st_date+"&end="+end_date+"&timezone=UTC"+"&columns=date%2Ctime%2Ccountry%2Ccountrycurrency%2Cevent%2Cconsensus%2Cprevious%2Cvolatility%2Cactual&showcountryname=false&showcurrencyname=true&isfree=true&_=1455009216444";
   ResetLastError();
   WebRequest("GET",url,cookie,referer,10000,post,sizeof(post),result,headers);
   if(ArraySize(result)<=0)
     {
      int er=GetLastError();
      ResetLastError();
      Print("ERROR_TXT IN WebRequest");
      if(er==4060)
         MessageBox("YOU MUST ADD THE ADDRESS '"+"http://calendar.fxstreet.com/"+"' IN THE LIST OF ALLOWED URL IN THE TAB 'ADVISERS'","ERROR_TXT",MB_ICONINFORMATION);
      return false;
     }

   tmpStr=CharArrayToString(result,0,WHOLE_ARRAY,CP_UTF8);
   int handl=FileOpen("News.txt",FILE_WRITE|FILE_TXT);
   FileWrite(handl,tmpStr);
   FileFlush(handl);
   FileClose(handl);
   StringReplace(tmpStr,"&#39;","'");
   StringReplace(tmpStr,"&#163;","");
   StringReplace(tmpStr,"&#165;","");
   StringReplace(tmpStr,"&amp;","&");

   int st=StringFind(tmpStr,"fxst-thevent",0);
   st=StringFind(tmpStr,">",st)+1;
   int end=StringFind(tmpStr,"</th>",st);
   HEADS.news=(st<end ? StringSubstr(tmpStr,st,end-st) :"");
   st=StringFind(tmpStr,"fxst-thvolatility",0);
   st=StringFind(tmpStr,">",st)+1;
   end=StringFind(tmpStr,"</th>",st);
   HEADS.importance=(st<end ? StringSubstr(tmpStr,st,fmin(end-st,8)) :"");
   st=StringFind(tmpStr,"fxst-thactual",0);
   st=StringFind(tmpStr,">",st)+1;
   end=StringFind(tmpStr,"</th>",st);
   HEADS.Actual=(st<end ? StringSubstr(tmpStr,st,fmin(end-st,8)) :"");
   st=StringFind(tmpStr,"fxst-thconsensus",0);
   st=StringFind(tmpStr,">",st)+1;
   end=StringFind(tmpStr,"</th>",st);
   HEADS.forecast=(st<end ? StringSubstr(tmpStr,st,fmin(end-st,8)) :"");
   st=StringFind(tmpStr,"fxst-thprevious",0);
   st=StringFind(tmpStr,">",st)+1;
   end=StringFind(tmpStr,"</th>",st);
   HEADS.previus=(st<end ? StringSubstr(tmpStr,st,end-st) :"");
   HEADS.currency="";
   HEADS.dTime=0;
   HEADS.time="";
   int startLoad=StringFind(tmpStr,"<tbody>",0)+7;
   int endLoad=StringFind(tmpStr,"</tbody>",startLoad);
   if(startLoad>=0 && endLoad>startLoad)
     {
      tmpStr=StringSubstr(tmpStr,startLoad,endLoad-startLoad);
      while(StringReplace(tmpStr,"  "," "));
     }
   else
      return false;
   int begin=-1;
   do
     {
      begin=StringFind(tmpStr,"<span",0);
      if(begin>=0)
        {
         end=StringFind(tmpStr,"</span>",begin)+7;
         tmpStr=StringSubstr(tmpStr,0,begin)+StringSubstr(tmpStr,end);
        }
     }
   while(begin>=0);
   StringReplace(tmpStr,"<strong>",NULL);
   StringReplace(tmpStr,"</strong>",NULL);
   int BackShift=0;
   string arNews[];
   for(uchar tr=1; tr<255; tr++)
     {
      if(StringFind(tmpStr,CharToString(tr),0)>0)
         continue;
      int K=StringReplace(tmpStr,"</tr>",CharToString(tr));
      //ArrayResize(arNews,StringReplace(tmpStr,"</tr>",CharToString(tr)));
      K=StringSplit(tmpStr,tr,arNews);
      ArrayResize(l_NewsTable,K);
      for(int td=0; td<ArraySize(arNews); td++)
        {
         st=StringFind(arNews[td],"fxst-td-date",0);
         if(st>0)
           {
            st=StringFind(arNews[td],">",st)+1;
            end=StringFind(arNews[td],"</td>",st)-1;
            int d=(int)StringToInteger(StringSubstr(arNews[td],end-4,end-st));
            MqlDateTime time;
            TimeCurrent(time);
            if(d<(time.day-5))
              {
               if(time.mon==12)
                 {
                  time.mon=1;
                  time.year++;
                 }
               else
                 {
                  time.mon++;
                 }
              }
            time.day=d;
            time.min=0;
            time.hour=0;
            time.sec=0;
            date=StructToTime(time);
            BackShift++;
            continue;
           }
         st=StringFind(arNews[td],"fxst-evenRow",0);
         if(st<0)
           {
            BackShift++;
            continue;
           }
         int st1=StringFind(arNews[td],"fxst-td-time",st);
         st1=StringFind(arNews[td],">",st1)+1;
         end=StringFind(arNews[td],"</td>",st1);
         l_NewsTable[td-BackShift].time=StringSubstr(arNews[td],st1,end-st1);
         if(StringFind(l_NewsTable[td-BackShift].time,":")>0)
           {
            l_NewsTable[td-BackShift].dTime=StringToTime(TimeToString(date,TIME_DATE)+" "+StringSubstr(arNews[td],st1,end-st1));
           }
         else
           {
            l_NewsTable[td-BackShift].dTime=date;
           }
         st1=StringFind(arNews[td],"fxst-td-currency",st);
         st1=StringFind(arNews[td],">",st1)+1;
         end=StringFind(arNews[td],"</td>",st1);
         l_NewsTable[td-BackShift].currency=(st1<end ? StringSubstr(arNews[td],st1,end-st1) :"");
         st1=StringFind(arNews[td],"fxst-i-vol",st);
         st1=StringFind(arNews[td],">",st1)+1;
         end=StringFind(arNews[td],"</td>",st1);
         StringInit(l_NewsTable[td-BackShift].importance,(int)StringToInteger(StringSubstr(arNews[td],st1,end-st1)),'*');
         st1=StringFind(arNews[td],"fxst-td-event",st);
         int st2=StringFind(arNews[td],"fxst-eventurl",st1);
         st1=StringFind(arNews[td],">",fmax(st1,st2))+1;
         end=StringFind(arNews[td],"</td>",st1);
         int end1=StringFind(arNews[td],"</a>",st1);
         l_NewsTable[td-BackShift].news=StringSubstr(arNews[td],st1,(end1>0 ? fmin(end,end1):end)-st1);
         st1=StringFind(arNews[td],"fxst-td-act",st);
         st1=StringFind(arNews[td],">",st1)+1;
         end=StringFind(arNews[td],"</td>",st1);
         l_NewsTable[td-BackShift].Actual=(end>st1 ? StringSubstr(arNews[td],st1,end-st1) : "");
         st1=StringFind(arNews[td],"fxst-td-cons",st);
         st1=StringFind(arNews[td],">",st1)+1;
         end=StringFind(arNews[td],"</td>",st1);
         l_NewsTable[td-BackShift].forecast=(end>st1 ? StringSubstr(arNews[td],st1,end-st1) : "");
         st1=StringFind(arNews[td],"fxst-td-prev",st);
         st1=StringFind(arNews[td],">",st1)+1;
         end=StringFind(arNews[td],"</td>",st1);
         l_NewsTable[td-BackShift].previus=(end>st1 ? StringSubstr(arNews[td],st1,end-st1) : "");
        }
      break;
     }
   ArrayResize(l_NewsTable,(ArraySize(l_NewsTable)-BackShift));
   return(true);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void DRAW_NEWS(sNews &l_a_news[])
  {
   if(DRAW_NEWS_LINES || DRAW_NEWS_CHART)
     {
      if(NEWS_FILTER==false)
         return;
      for(int i=ArraySize(l_a_news)-1; i>=0; i--)
        {
         StringReplace(l_a_news[i].currency," ","");
         int Currency_check_counter=0;

      datetime t1=(l_a_news[i].dTime+(datetime)(START_AFTER_NEWS*60));
      datetime t2=((TimeCurrent()-(datetime)TIME_CORRECTION));
         
         if(StringFind(Currencies_Check,l_a_news[i].currency)==-1 || t1<t2 || (Check_Specific_News  && (StringFind(l_a_news[i].news,Specific_News_Text)==-1)))
           {
            DEL_ROW(l_a_news,i);
            continue;
           }

         if((!NEWS_IMPOTANCE_LOW && l_a_news[i].importance=="*") ||
            (!NEWS_IMPOTANCE_MEDIUM && l_a_news[i].importance=="**") ||
            (!NEWS_IMPOTANCE_HIGH && l_a_news[i].importance=="***"))
           {
            DEL_ROW(l_a_news,i);
            continue;
           }
         string NAME=(" "+l_a_news[i].currency+" "+l_a_news[i].importance+" "+l_a_news[i].news);
         if(DRAW_NEWS_LINES)
           {
            if(ObjectFind(0,NAME)<0)
              {
               ObjectCreate(0,NAME,OBJ_VLINE,0,l_a_news[i].dTime+TIME_CORRECTION,0);
               ObjectSetInteger(0,NAME,OBJPROP_SELECTABLE,false);
               ObjectSetInteger(0,NAME,OBJPROP_SELECTED,false);
               ObjectSetInteger(0,NAME,OBJPROP_HIDDEN,true);
               ObjectSetInteger(0,NAME,OBJPROP_BACK,false);
               ObjectSetInteger(0,NAME,OBJPROP_COLOR,Line_Color);
               ObjectSetInteger(0,NAME,OBJPROP_STYLE,Line_Style);
               ObjectSetInteger(0,NAME,OBJPROP_WIDTH,Line_Width);
              }
           }
        }
      string NAME;
      int K=0,Z=0;
      if(DRAW_NEWS_CHART)
        {
         for(int l=1; l<=9 && Z<ArraySize(l_a_news); l++)
           {
            for(K=Z; K<ArraySize(l_a_news); K++)
               if(l_a_news[K].currency!="")
                  break;
            Z=K+1;


           NAME="PANEL_NEWS_N"+(string)l;
            if(ObjectFind(0,NAME)<0)
               OBJECT_LABEL(0,NAME,0,X+110,Y-(int)(18*(l+5)),CORNER_LEFT_LOWER,((TimeToString(l_a_news[K].dTime+TIME_CORRECTION,TIME_DATE|TIME_MINUTES)+" "+l_a_news[K].currency+" "+l_a_news[K].importance+" "+l_a_news[K].news)),News_Font,Font_Size,Font_Color,0,ANCHOR_LEFT_UPPER,false,false,true,0);

           }
        }
      return;
     }
  }
//+------------------------------------------------------------------+
void DEINIT_PANEL()
  {
   ObjectsDeleteAll(0);
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool OBJECT_LABEL(const long              CHART_ID=0,
                  const string            NAME        = "",
                  const int               SUB_WINDOW  = 0,
                  const int               X_Axis      = 0,
                  const int               Y_Axis      = 0,
                  const ENUM_BASE_CORNER  CORNER      = CORNER_LEFT_UPPER,
                  const string            TEXT        = "",
                  const string            FONT        = "",
                  const int               FONT_SIZE   = 10,
                  const color             CLR         = color("255,0,0"),
                  const double            ANGLE       = 0.0,
                  const ENUM_ANCHOR_POINT ANCHOR      = ANCHOR_LEFT_UPPER,
                  const bool              BACK        = false,
                  const bool              SELECTION   = false,
                  const bool              HIDDEN      = true,
                  const long              ZORDER      = 0,
                  string                  TOOLTIP     = "\n")
  {
   ResetLastError();
   if(ObjectFind(0,NAME)<0)
     {
      ObjectCreate(CHART_ID,NAME,OBJ_LABEL,SUB_WINDOW,0,0);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_XDISTANCE,X_Axis);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_YDISTANCE,Y_Axis);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_CORNER,CORNER);
      ObjectSetString(CHART_ID,NAME,OBJPROP_TEXT,TEXT);
      ObjectSetString(CHART_ID,NAME,OBJPROP_FONT,FONT);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_FONTSIZE,FONT_SIZE);
      ObjectSetDouble(CHART_ID,NAME,OBJPROP_ANGLE,ANGLE);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_ANCHOR,ANCHOR);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_COLOR,CLR);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_BACK,BACK);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_SELECTABLE,SELECTION);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_SELECTED,SELECTION);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_HIDDEN,HIDDEN);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_ZORDER,ZORDER);
      ObjectSetString(CHART_ID,NAME,OBJPROP_TOOLTIP,TOOLTIP);
     }
   else
     {
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_COLOR,CLR);
      ObjectSetString(CHART_ID,NAME,OBJPROP_TEXT,TEXT);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_XDISTANCE,X);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_YDISTANCE,Y);
     }
   return(true);
   ChartRedraw();
  }
//+------------------------------------------------------------------+
