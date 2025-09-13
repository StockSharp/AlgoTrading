//+------------------------------------------------------------------+
//|                                   Fundamental Trader DailyFX.mq4 |
//|                                      Fundamental Trader Freeware |
//|                                                                  |
//+------------------------------------------------------------------+
//v0.04 | Fixed Risk/Reward
//v0.04 | Fixed Invalid Lots Issue
//v0.04 | Fixed to download calendar only when event is released, and not every minute of the Day
//v0.04 | Implemented to close position after an X amount of minutes
//v0.04 | Fixed, NZD order direction

#property copyright "Fundamental Trader Freeware"
#property link      ""
//extern string HtmlAdress = "http://www.dailyfx.com/calendar/Dailyfx_Global_Economic_Calendar.csv";
//extern string HtmlAdress = "http://www.dailyfx.com/calendar/Dailyfx_Global_Economic_Calendar.csv?currentWeek=/events-calendar/2008/0921/&direction=none&collector=allInFolderDateDesc&view=week&timezone=GMT&currencyFilter=|&importanceFilter=|";
extern string HtmlAdress = "http://www.dailyfx.com/calendar/Dailyfx_Global_Economic_Calendar.csv?currentcalendar.csv";
extern string GetrightAdress = "c:\progra~1\getright\getright.exe";

extern int xTime=27; //time for EA to wait for news to be released
extern int MagicNumber=815; //order magicnumber
extern bool enable_close_time=false; //set to true to enable the function to close orders after certain amount of time has passed
extern int wait_time=20; //time the EA waits after the orders were executed, to close orders based on a time frame


//Risk and Reward Setup  
extern int risk=20; //20 pip risk
extern int reward=3;//reward ratio multiplier, if risk is 20pip, then reward is 60pip

//external specification of minimim lots to trade and maximum lots to trade when news events are released
extern double lot1=0.01;extern double lot2=0.02;extern double lot3=0.03;extern double lot4=0.04;
extern double lot5=0.05;extern double lot6=0.06;extern double lot7=0.07;extern double lot8=0.08;
extern double lot9=0.09;extern double lot10=0.1;extern double lot11=0.11;extern double lot12=0.12;
extern double lot13=0.13;extern double lot14=0.14;extern double lot15=0.15;extern double lot16=0.16;
extern double lot17=0.17;extern double lot18=0.17;
//end of lots specification

#include <Time.mqh>
#import "kernel32.dll"
int WinExec(string NameEx, int dwFlags);
#import

#import "str2double.dll"
double StringToDouble(string str);
#import



int Max = 0;
datetime LastTimeDownloading = 0;
datetime OrderTime=0;
bool down_flag;

void DownloadCalendar()
{
  Print("Downloading "+HtmlAdress+" to experts\files\html\Calendar.csv");
  //FileDelete("\Html\Calendar.csv");
  if(down_flag!=true){WinExec(GetrightAdress+" /URL:"+HtmlAdress+" /FILE:Calendar.csv /W /O",0);} 
  down_flag=true; 
}

void CloseOrders()//function which closes opened orders after x amount of time has passed!
   {
      int opened_orders=OrdersTotal();
      for (int fe=0;fe<opened_orders;fe++)
            {
            OrderSelect(fe, SELECT_BY_POS, MODE_TRADES); //check opened orders
            if(enable_close_time==true && OrderMagicNumber()==MagicNumber && TimeCurrent()>OrderTime+wait_time*60 && OrderType()==OP_BUY)
               {            
                  double order_bid=MarketInfo(OrderSymbol(),MODE_BID);
                  OrderClose(OrderTicket(),OrderLots(),order_bid,3,Violet);
               }
               
            if(enable_close_time==true && OrderMagicNumber()==MagicNumber && TimeCurrent()>OrderTime+wait_time*60 && OrderType()==OP_SELL)
               {
                  double order_ask=MarketInfo(OrderSymbol(),MODE_ASK);
                  OrderClose(OrderTicket(),OrderLots(),order_ask,3,Violet);
               }               
            }
     return;       
   }



void DownloadCal_prior_to_event()
{
  int file1 = FileOpen("\Html\Calendar.csv",FILE_READ|FILE_CSV,',');
  while (!FileIsEnding(file1))
    {
      string stDate1="";
      while (!FileIsEnding(file1) && stDate1=="")
        stDate1 = FileReadString(file1);       
      string stTime1 = FileReadString(file1);
      string stTimeZone1 = FileReadString(file1);
      string stCurrency1 = FileReadString(file1);
      string stDescription1 = FileReadString(file1);
      string stImportance1 = FileReadString(file1);
      string stActual1 = FileReadString(file1);
      string stForecast1 = FileReadString(file1);
      string stPrevious1 = FileReadString(file1);
      //Alert(stDate1,stTime1,stTimeZone1,stCurrency1,stDescription1,stImportance1,stActual1,stForecast1,stPrevious1);
      datetime Date1 = ToDate(stDate1,stTime1); 
      if(Date1>(TimeCurrent()-(xTime*60)))
         {
            if(TimeCurrent()>Date1 && stTime1!="Time")
               {
                  //Alert("time before event"+stTime1);
                  FileClose(file1);
                  WinExec(GetrightAdress+" /URL:"+HtmlAdress+" /FILE:Calendar.csv /W /O",0);
                  return;
               }
         }   
    }
  //WinExec(GetrightAdress+" /URL:"+HtmlAdress+" /FILE:Calendar.csv /W /O",0); 
 FileClose(file1);  
}

datetime PerviousMonday(datetime d)
{
  datetime res = d - (TimeDayOfWeek(d)-1)*24*60*60;
  return(res);
}


datetime ToDate(string stDate,string stTime) 
{
  string WeekDay = StringSubstr(stDate,0,3);
  int WeekPlus = 0;
  if (WeekDay=="Mon") WeekPlus=0;
  if (WeekDay=="Tue") WeekPlus=1;
  if (WeekDay=="Wed") WeekPlus=2;
  if (WeekDay=="Thu") WeekPlus=3;
  if (WeekDay=="Fri") WeekPlus=4;
  if (WeekDay=="Sat") WeekPlus=5;
  if (WeekDay=="Sun") WeekPlus=-1;
  
  datetime Res = PerviousMonday(GetTimeGMT())+WeekPlus*24*60*60;
  datetime Tm = StrToTime(stTime);
  Res=Res+TimeHour(Tm )*60*60+TimeMinute(Tm )*60+TimeSeconds(Tm )
         -TimeHour(Res)*60*60-TimeMinute(Res)*60-TimeSeconds(Res);  
  if((StringFind(stTime,"12"))>=0 && (StringFind(stTime,"AM"))>=0){Res=Res-(TimeHour(Tm )*60*60)-(TimeMinute(Tm )*60);}
  if (StringFind(stTime,"PM")>=0)       
    Res+=12*60*60;
  Res=Res-GetShiftGMT();
  if((StringFind(stTime,"12"))>=0 && (StringFind(stTime,"PM"))>=0){Res=Res-(TimeHour(Tm )*60*60)-(TimeMinute(Tm )*60);}
  //if(stTime=="12:15 PM" || stTime=="12:30 PM" || stTime=="12:45 PM"){Res=Res-(TimeHour(Tm )*60*60)-(TimeMinute(Tm )*60);}
  return (Res);
}

void GrabNews() 
{
  int file = FileOpen("\Html\Calendar.csv",FILE_READ|FILE_CSV,',');
  if (file==-1||FileSize(file)==0)
        return;
          
  int i=0;
  while (!FileIsEnding(file))
    {
      string stDate="";
      while (!FileIsEnding(file) && stDate=="")
        stDate = FileReadString(file);
        
        
      string stTime = FileReadString(file);
      string stTimeZone = FileReadString(file);
      string stCurrency = FileReadString(file);
      string stDescription = FileReadString(file);
      string stImportance = FileReadString(file);
      string stActual = FileReadString(file);
      string stForecast = FileReadString(file);
      string stPrevious = FileReadString(file);
      //Alert(stDate,stTime,stTimeZone,stCurrency,stDescription,stImportance,stActual,stForecast,stPrevious);
      
                 
      datetime Date = ToDate(stDate,stTime);      
      //Alert(TimeCurrent()+"   "+(TimeCurrent()-(15*60)));
      if(Date>(TimeCurrent()-(xTime*60)))//give  xTime minutes to the EA to wait for news to be released
      
      {
      //Alert(Date+" "+(TimeCurrent()));//-(15*60)));
         Comment("Date: ",stDate,
         "\n","Time: ",stTime,
         "\n","TimeZone: ",stTimeZone,
         "\n","Currency: ",stCurrency,
         "\n","Description: ",stDescription,
         "\n","Importance: ",stImportance,
         "\n","Actual: ",stActual,
         "\n","Forecast: ",stForecast,
         "\n","Previous: ",stPrevious);         
         
//double myvalue = StringToDouble("-$610");
//Alert("My value: " + myvalue);//DoubleToStr(myvalue, 2));
//Alert(Date+"   "+TimeCurrent());
    
         string ordercurrency="";
         if(stCurrency=="EUR" ){ordercurrency="EURUSD";} //trading eurusd
         if(stCurrency=="USD" ){ordercurrency="EURUSD";} //trading eurusd
         if(stCurrency=="JPY" ){ordercurrency="USDJPY";} //trading usdjpy
         if(stCurrency=="GBP" ){ordercurrency="GBPUSD";} //trading usdgbp
         if(stCurrency=="CHF" ){ordercurrency="USDCHF";} //trading usdchf
         if(stCurrency=="AUD" ){ordercurrency="AUDUSD";} //trading audusd
         if(stCurrency=="CAD" ){ordercurrency="USDCAD";} //trading usdcad
         if(stCurrency=="NZD" ){ordercurrency="NZDUSD";} //trading nzdusd
         double atr=iATR(ordercurrency, 0, 14, 0); 
         
//evaluation of SAR as stoploss and takeprofit         
//double SAR=iSAR(ordercurrency,0,0.02,0.2,0);
//double close_price=iClose(ordercurrency,0,0);
//double diff=MathAbs(close_price-SAR);
//double cbid=MarketInfo(ordercurrency,MODE_BID); //buy is ask, sell is bid
//double cask=MarketInfo(ordercurrency,MODE_ASK);
//Alert("currentprice:"+close_price+" "+"Takeprofit: "+(cbid+diff)+" "+"Stoploss: "+(cask-diff));
      
     
//(2) states to enter the MarketInfo
      //(1) when actual data is released, and it is compared to forecast, and make sure not to trade if event contains a symbol before integers, e.g. $56, the "$" causes the EA to stop
      if(StringLen(stActual)>0 && StringLen(stForecast)>0 && stTime!="Time" && stImportance=="High")//&& StrToDouble(stActual)!=0 && StrToDouble(stForecast)!=0) //not zero, takes care of the symbol in front of event
      {
      //int percent_d_AF=MathAbs((MathAbs(StrToDouble(stActual)-StrToDouble(stForecast))/StrToDouble(stForecast))*100);//percent difference actual to forecast
      double percent_d_AF=MathAbs((MathAbs(StringToDouble(stActual)-StringToDouble(stForecast))/StringToDouble(stForecast))*100);//percent difference actual to forecast
                        double lot_p=0;
                        if((percent_d_AF>0 && percent_d_AF<=3)){lot_p=lot1;}//0.1 lot
                        if((percent_d_AF>3 && percent_d_AF<=6)){lot_p=lot2;}//0.2 lots
                        if((percent_d_AF>6 && percent_d_AF<=9)){lot_p=lot3;}//0.3 lots
                        if((percent_d_AF>9 && percent_d_AF<=12)){lot_p=lot4;}//0.4 lots
                        if((percent_d_AF>12 && percent_d_AF<=15)){lot_p=lot5;}//0.5 lots
                        if((percent_d_AF>15 && percent_d_AF<=18)){lot_p=lot6;}//0.6 lots
                        if((percent_d_AF>18 && percent_d_AF<=21)){lot_p=lot7;}//0.7 lots
                        if((percent_d_AF>21 && percent_d_AF<=24)){lot_p=lot8;}//0.8 lots
                        if((percent_d_AF>24 && percent_d_AF<=27)){lot_p=lot9;}//0.9 lots
                        if((percent_d_AF>27 && percent_d_AF<=30)){lot_p=lot10;}//1 lots
                        if((percent_d_AF>30 && percent_d_AF<=40)){lot_p=lot11;}//2 lots
                        if((percent_d_AF>40 && percent_d_AF<=50)){lot_p=lot12;}//3 lots
                        if((percent_d_AF>50 && percent_d_AF<=60)){lot_p=lot13;}//4 lots
                        if((percent_d_AF>60 && percent_d_AF<=70)){lot_p=lot14;}//5 lots
                        if((percent_d_AF>70 && percent_d_AF<=80)){lot_p=lot15;}//6 lots
                        if((percent_d_AF>80 && percent_d_AF<=90)){lot_p=lot16;}//7 lots
                        if((percent_d_AF>90 && percent_d_AF<=100)){lot_p=lot17;}//8 lots
                        if((percent_d_AF>100))                    {lot_p=lot18;}//8 lots
                      
           if(StringToDouble(stActual)>StringToDouble(stForecast))
                {
                Alert("Stronger(actual vs forecast): "+stCurrency+" "+"Time: "+stTime);
                        int total=OrdersTotal();
                           for(int cnt=0;cnt<total;cnt++)
                           {
                           OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES); //check opened orders
                           if(OrderComment()==stCurrency+stActual+stForecast+"BUY" || OrderComment()==stCurrency+stActual+stForecast+"SELL"){FileClose(file);Comment("\n"+"Live Trade Open"+OrderTicket());return(0);}
                           }
                        int historytotal=OrdersHistoryTotal();
                           for(cnt=0;cnt<historytotal;cnt++)
                           {
                           OrderSelect(cnt, SELECT_BY_POS, MODE_HISTORY); //check opened orders
                           if(OrderComment()==stCurrency+stActual+stForecast+"BUY" || OrderComment()==stCurrency+stActual+stForecast+"SELL" || OrderComment()==stCurrency+stActual+stForecast+"BUY[sl]" || OrderComment()==stCurrency+stActual+stForecast+"SELL[sl]" ||OrderComment()==stCurrency+stActual+stForecast+"BUY[tp]" || OrderComment()==stCurrency+stActual+stForecast+"SELL[tp]"){FileClose(file);Comment("\n"+"Trade Executed"+OrderTicket());return(0);}
                           }
                if(stCurrency=="EUR"||stCurrency=="GBP"||stCurrency=="AUD"||stCurrency=="NZD"){double ask=MarketInfo(ordercurrency,MODE_ASK);double point1=MarketInfo(ordercurrency,MODE_POINT); OrderSend(ordercurrency,OP_BUY,lot_p,ask,3,ask-(risk*point1),ask+(reward*risk*point1),stCurrency+stActual+stForecast+"BUY",MagicNumber);OrderTime=TimeCurrent();}
                if(stCurrency=="CHF"||stCurrency=="CAD"||stCurrency=="JPY"||stCurrency=="USD"){double bid=MarketInfo(ordercurrency,MODE_BID); double point2=MarketInfo(ordercurrency,MODE_POINT);OrderSend(ordercurrency,OP_SELL,lot_p,bid,3,bid+(risk*point2),bid-(reward*risk*point2),stCurrency+stActual+stForecast+"SELL",MagicNumber);OrderTime=TimeCurrent();}
                
                }
      
      
        if(StringToDouble(stActual)<StringToDouble(stForecast))
                {
                        int total1=OrdersTotal();
                           for(cnt=0;cnt<total1;cnt++)
                           {
                           OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES); //check opened orders
                           if(OrderComment()==stCurrency+stActual+stForecast+"BUY" || OrderComment()==stCurrency+stActual+stForecast+"SELL"){FileClose(file);Comment("\n"+"Live Trade Open"+OrderTicket());return(0);}
                           }
                        int historytotal1=OrdersHistoryTotal();
                           for(cnt=0;cnt<historytotal1;cnt++)
                           {
                           OrderSelect(cnt, SELECT_BY_POS, MODE_HISTORY); //check opened orders
                           if(OrderComment()==stCurrency+stActual+stForecast+"BUY" || OrderComment()==stCurrency+stActual+stForecast+"SELL" || OrderComment()==stCurrency+stActual+stForecast+"BUY[sl]" || OrderComment()==stCurrency+stActual+stForecast+"SELL[sl]" || OrderComment()==stCurrency+stActual+stForecast+"BUY[tp]" || OrderComment()==stCurrency+stActual+stForecast+"SELL[tp]"){FileClose(file);Comment("\n"+"Trade Executed"+OrderTicket());return(0);}
                           }
                Alert("Weaker(actual vs forecast): "+stCurrency+" "+"Time: "+stTime);
                if(stCurrency=="EUR"||stCurrency=="GBP"||stCurrency=="AUD"||stCurrency=="NZD"){double bidd=MarketInfo(ordercurrency,MODE_BID); double point3=MarketInfo(ordercurrency,MODE_POINT);OrderSend(ordercurrency,OP_SELL,lot_p,bidd,3,bidd+(risk*point3),bidd-(reward*risk*point3),stCurrency+stActual+stForecast+"SELL",MagicNumber);OrderTime=TimeCurrent();}
                if(stCurrency=="CHF"||stCurrency=="CAD"||stCurrency=="JPY"||stCurrency=="USD"){double askk=MarketInfo(ordercurrency,MODE_ASK); double point4=MarketInfo(ordercurrency,MODE_POINT);OrderSend(ordercurrency,OP_BUY,lot_p,askk,3,askk-(risk*point4),askk+(reward*risk*point4),stCurrency+stActual+stForecast+"BUY",MagicNumber);OrderTime=TimeCurrent();}
                
                }
               
     FileClose(file); 
     return(0); }
      
      
      //(2) when actual data is released, and it is compared to the previous(string length of forecast==0)
      if(StringLen(stActual)>0 && StringLen(stForecast)==0 && StringLen(stPrevious)>0 && stTime!="Time" && stImportance=="High")// && StrToDouble(stActual)!=0 && StrToDouble(stPrevious)!=0)
      { 
      //int percent_d_AP=MathAbs((MathAbs(StrToDouble(stActual)-StrToDouble(stPrevious))/StrToDouble(stPrevious))*100);//percent difference actual to previous
      double percent_d_AP=MathAbs((MathAbs(StringToDouble(stActual)-StringToDouble(stPrevious))/StringToDouble(stPrevious))*100);//percent difference actual to previous
                        double lot_pp=0;
                        if((percent_d_AP>0 && percent_d_AP<=3)){lot_pp=lot1;}//0.1 lot
                        if((percent_d_AP>3 && percent_d_AP<=6)){lot_pp=lot2;}//0.2 lots
                        if((percent_d_AP>6 && percent_d_AP<=9)){lot_pp=lot3;}//0.3 lots
                        if((percent_d_AP>9 && percent_d_AP<=12)){lot_pp=lot4;}//0.4 lots
                        if((percent_d_AP>12 && percent_d_AP<=15)){lot_pp=lot5;}//0.5 lots
                        if((percent_d_AP>15 && percent_d_AP<=18)){lot_pp=lot6;}//0.6 lots
                        if((percent_d_AP>18 && percent_d_AP<=21)){lot_pp=lot7;}//0.7 lots
                        if((percent_d_AP>21 && percent_d_AP<=24)){lot_pp=lot8;}//0.8 lots
                        if((percent_d_AP>24 && percent_d_AP<=27)){lot_pp=lot9;}//0.9 lots
                        if((percent_d_AP>27 && percent_d_AP<=30)){lot_pp=lot10;}//1 lots
                        if((percent_d_AP>30 && percent_d_AP<=40)){lot_pp=lot11;}//2 lots
                        if((percent_d_AP>40 && percent_d_AP<=50)){lot_pp=lot12;}//3 lots
                        if((percent_d_AP>50 && percent_d_AP<=60)){lot_pp=lot13;}//4 lots
                        if((percent_d_AP>60 && percent_d_AP<=70)){lot_pp=lot14;}//5 lots
                        if((percent_d_AP>70 && percent_d_AP<=80)){lot_pp=lot15;}//6 lots
                        if((percent_d_AP>80 && percent_d_AP<=90)){lot_pp=lot16;}//7 lots
                        if((percent_d_AP>90 && percent_d_AP<=100)){lot_pp=lot17;}//8 lots
                        if((percent_d_AP>100))                    {lot_pp=lot18;}//8 lots
      
      
        if(StringToDouble(stActual)>StringToDouble(stPrevious))
                {
                Alert("Stronger(actual vs previous): "+stCurrency+" "+"Time: "+stTime);
                        int total2=OrdersTotal();
                           for(cnt=0;cnt<total2;cnt++)
                           {
                           OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES); //check opened orders
                           if(OrderComment()==stCurrency+stActual+stPrevious+"BUY" || OrderComment()==stCurrency+stActual+stPrevious+"SELL"){FileClose(file);Comment("\n"+"Live Trade Open"+OrderTicket());return(0);}
                           }
                        int historytotal2=OrdersHistoryTotal();
                           for(cnt=0;cnt<historytotal2;cnt++)
                           {
                           OrderSelect(cnt, SELECT_BY_POS, MODE_HISTORY); //check opened orders
                           if(OrderComment()==stCurrency+stActual+stPrevious+"BUY" || OrderComment()==stCurrency+stActual+stPrevious+"SELL" || OrderComment()==stCurrency+stActual+stPrevious+"BUY[sl]" || OrderComment()==stCurrency+stActual+stPrevious+"SELL[sl]" || OrderComment()==stCurrency+stActual+stPrevious+"BUY[tp]" || OrderComment()==stCurrency+stActual+stPrevious+"SELL[tp]"){FileClose(file);Comment("\n"+"Trade Executed"+OrderTicket());return(0);}
                           }
                if(stCurrency=="EUR"||stCurrency=="GBP"||stCurrency=="AUD"||stCurrency=="NZD"){double askz=MarketInfo(ordercurrency,MODE_ASK);double point5=MarketInfo(ordercurrency,MODE_POINT);OrderSend(ordercurrency,OP_BUY,lot_pp,askz,3,askz-(risk*point5),askz+(reward*risk*point5),stCurrency+stActual+stPrevious+"BUY",MagicNumber);OrderTime=TimeCurrent();}
                if(stCurrency=="CHF"||stCurrency=="CAD"||stCurrency=="JPY"||stCurrency=="USD"){double bidz=MarketInfo(ordercurrency,MODE_BID); double point6=MarketInfo(ordercurrency,MODE_POINT);OrderSend(ordercurrency,OP_SELL,lot_pp,bidz,3,bidz+(risk*point6),bidz-(reward*risk*point6),stCurrency+stActual+stPrevious+"SELL",MagicNumber);OrderTime=TimeCurrent();}
                }
      
      
     
        if(StringToDouble(stActual)<StringToDouble(stPrevious))
                {
                Alert("Weaker(actual vs previous): "+stCurrency+" "+"Time: "+stTime);
                        int total3=OrdersTotal();
                           for(cnt=0;cnt<total3;cnt++)
                           {
                           OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES); //check opened orders
                           if(OrderComment()==stCurrency+stActual+stPrevious+"BUY" || OrderComment()==stCurrency+stActual+stPrevious+"SELL"){FileClose(file);Comment("\n"+"Live Trade Open"+OrderTicket());return(0);}
                           }
                        int historytotal3=OrdersHistoryTotal();
                           for(cnt=0;cnt<historytotal3;cnt++)
                           {
                           OrderSelect(cnt, SELECT_BY_POS, MODE_HISTORY); //check opened orders
                           if(OrderComment()==stCurrency+stActual+stPrevious+"BUY" || OrderComment()==stCurrency+stActual+stPrevious+"SELL" || OrderComment()==stCurrency+stActual+stPrevious+"BUY[sl]" || OrderComment()==stCurrency+stActual+stPrevious+"SELL[sl]" || OrderComment()==stCurrency+stActual+stPrevious+"BUY[tp]" || OrderComment()==stCurrency+stActual+stPrevious+"SELL[tp]"){FileClose(file);Comment("\n"+"Trade Executed"+OrderTicket());return(0);}
                           }
                if(stCurrency=="EUR"||stCurrency=="GBP"||stCurrency=="AUD"||stCurrency=="NZD"){double bidx=MarketInfo(ordercurrency,MODE_BID); double point7=MarketInfo(ordercurrency,MODE_POINT);OrderSend(ordercurrency,OP_SELL,lot_pp,bidx,3,bidx+(risk*point7),bidx-(reward*risk*point7),stCurrency+stActual+stPrevious+"BUY",MagicNumber);OrderTime=TimeCurrent();}
                if(stCurrency=="CHF"||stCurrency=="CAD"||stCurrency=="JPY"||stCurrency=="USD"){double askx=MarketInfo(ordercurrency,MODE_ASK); double point8=MarketInfo(ordercurrency,MODE_POINT);OrderSend(ordercurrency,OP_BUY,lot_pp,askx,3,askx-(risk*point8),askx+(reward*risk*point8),stCurrency+stActual+stPrevious+"SELL",MagicNumber);OrderTime=TimeCurrent();}
                }
       
     FileClose(file); 
     return(0); }
      
      
      //Alert(stDate,stTime,stTimeZone,stCurrency,stDescription,stImportance,stActual,stForecast,stPrevious);
         
      FileClose(file);
      break;
      }
      //FileClose(file);
      i++;
      

    }
  Max = i;
  //if (file!=-1)
    FileClose(file);}
    
    
    
    
  
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+

   int start()
   {
   if (TimeCurrent()>LastTimeDownloading+1*60)
   {

          DownloadCalendar(); //this function, only downloads the calendar once, to find next event
          DownloadCal_prior_to_event(); //function to download the calendar, starting from when the news event is released!
          LastTimeDownloading = TimeCurrent();
          CloseOrders();  //function to close orders at a certain time after the order is executed
          int file=-1;
          while (file==-1)
              file = FileOpen("\Html\Calendar.csv",FILE_READ|FILE_CSV,',');       
          GrabNews();          
          FileClose(file);
          }
          //FileClose(file);
          return(0);
         
    
}

  


