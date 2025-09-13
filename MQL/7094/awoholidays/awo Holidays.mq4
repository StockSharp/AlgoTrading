//+------------------------------------------------------------------+
//|                                                 awo Holidays.mq4 |
//|                                                              AWo |
//|                                                     a-wo@mail.ru |
//+------------------------------------------------------------------+
#property copyright "AWo"
#property link      "a-wo@mail.ru"

extern int Numbers_of_Lines=3;
extern bool Del_Lines_If_Deinit=true;
extern string FileName="holidays.csv";
extern color clrWork=LightBlue;
extern color clrWE=Blue;
extern color clrHLD=DarkOrange;

int tm24h=86400, tm1h=3600;

//+------------------------------------------------------------------+
//| Ф ДАТА ДНЯ, poz - позиция дня >0 в прошлом, <0 в будущем, ( -2 - послезавтра, 1 - вчера )
//+------------------------------------------------------------------+
datetime Data( int poz ) { return ( TimeCurrent( )-tm24h*poz ); }
string DataString( int poz ) { return ( TimeToStr(Data(poz),TIME_DATE) ); }

   
//+------------------------------------------------------------------+
//| Ф Выходные, poz - позиция дня >0 в прошлом, <0 в будущем, ( -2 - послезавтра, 1 - вчера )
//+------------------------------------------------------------------+

string isWeekEnd( int poz )
      {
      int N; string DOW;
       N=TimeDayOfWeek(Data(poz));
   if( N==0 ) DOW="sunday";
   if( N==1 || N==2 || N==3 || N==4 || N==5 ) DOW="workday";
   if( N==6 ) DOW="saturday";
      return(DOW);
      }

//+------------------------------------------------------------------+
//| Ф CТАТУС ДНЯ, poz - позиция дня >0 в прошлом, <0 в будущем, ( -2 - послезавтра, 1 - вчера )
//+------------------------------------------------------------------+

string isFiesta( int poz )
      {
   string sDT, sCountry, sSymb, sHoliday, tomorrow="";
   int handle= FileOpen(FileName,FILE_CSV|FILE_READ,';');

   while (FileIsEnding(handle)==false)
            {
         sDT=        FileReadString(handle,0);
         sCountry=   FileReadString(handle,0);
         sSymb=      FileReadString(handle,0);
         sHoliday=   FileReadString(handle,0);

         if (sDT==DataString(poz) && StringFind(Symbol(),sSymb,0)!=-1) break;
            }
               FileClose(handle) ;
      if (sDT==DataString(poz)) tomorrow = sHoliday+" in "+sCountry;
      return (tomorrow);
      }



//+------------------------------------------------------------------+
//+------------------------------------------------------------------+

void start()
  {
  
   string nmSwapLine="SWAP "+DataString(-1)+" "+isWeekEnd(-1)+" "+isFiesta(-1);
   string nmDayLine="DAY "+DataString(-1)+" "+isWeekEnd(-1)+" "+isFiesta(-1);

   ObjectCreate(nmSwapLine,OBJ_TREND,0,TimeCurrent( )+tm24h-tm1h*3,WindowPriceMax(),TimeCurrent( )+tm24h-tm1h*3,WindowPriceMin(),0,0);
   ObjectCreate(nmDayLine,OBJ_TREND,0,TimeCurrent( )+tm24h,WindowPriceMax(),TimeCurrent( )+tm24h,WindowPriceMin(),0,0);

                                    ObjectSet(nmSwapLine,OBJPROP_WIDTH,2);
                                    ObjectSet(nmSwapLine,OBJPROP_TIMEFRAMES,OBJ_PERIOD_M1|OBJ_PERIOD_M5|OBJ_PERIOD_M15|OBJ_PERIOD_M30|OBJ_PERIOD_H1|OBJ_PERIOD_H4);
   if ( isWeekEnd(-1)=="workday" )  ObjectSet(nmSwapLine,OBJPROP_COLOR,clrWork);
   else                             ObjectSet(nmSwapLine,OBJPROP_COLOR,clrWE);
   if ( isFiesta(-1)!="" )          ObjectSet(nmSwapLine,OBJPROP_COLOR,clrHLD);

                                    ObjectSet(nmDayLine,OBJPROP_STYLE,STYLE_DOT);
                                    ObjectSet(nmDayLine,OBJPROP_TIMEFRAMES,OBJ_PERIOD_M1|OBJ_PERIOD_M5|OBJ_PERIOD_M15|OBJ_PERIOD_M30|OBJ_PERIOD_H1);
   if ( isWeekEnd(-1)=="workday" )  ObjectSet(nmDayLine,OBJPROP_COLOR,clrWork);
   else                             ObjectSet(nmDayLine,OBJPROP_COLOR,clrWE);
   if ( isFiesta(-1)!="" )          ObjectSet(nmDayLine,OBJPROP_COLOR,clrHLD);


    
  Comment( "\n","Вчера      ",        isWeekEnd(1)+  " ("+isFiesta(1)+ ")",
           "\n","Сегодня   ",      isWeekEnd(0)+  " ("+isFiesta(0)+ ")",
           "\n","Завтра     ",       isWeekEnd(-1)+ " ("+isFiesta(-1)+")",
           "\n","пЗавтра    ",      isWeekEnd(-2)+ " ("+isFiesta(-2)+")",
           "\n","ппЗавтра   ",     isWeekEnd(-3)+ " ("+isFiesta(-3)+")"    );

  }

void deinit() {   if ( Del_Lines_If_Deinit==true ) ObjectsDeleteAll(0,OBJ_TREND);  }
      

