//+------------------------------------------------------------------+
//|                                                MT4MailStatus.mq4 |
//|                                                            Moggy |
//|                                             moggylew@hotmail.com |
//+------------------------------------------------------------------+
#property copyright "Moggy"
#property link      "moggylew@hotmail.com"
#property version   "1.00"
#property strict

extern long sendmininterval = 3600;//interval(S)(>60)

datetime lastreporttime = 0;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   printf("OnInit called to init timer");
   if(!EventSetTimer(2))
   {
       printf("OnInit EventSetTimer error:" + (string)GetLastError());
   }
   if(sendmininterval < 60)
   {
       printf("OnInit Failed,sendmininterval should more than 60 seconds");
       return INIT_FAILED;
   }
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
//--- destroy timer
   EventKillTimer();
}
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{

}
//+------------------------------------------------------------------+
//| Timer function                                                   |
//+------------------------------------------------------------------+
void OnTimer()
{
//---
    datetime nowtime = TimeCurrent();
    //send report
    if(IsEnoughTimePassed(lastreporttime,nowtime,sendmininterval) && (OrdersTotal() > 0))
    {
        string mailcontent = "";
        mailcontent += "" + (string)nowtime + "\n";
        mailcontent += "###Orders begin###\n";
        for(int ordidx = 0; ordidx < OrdersTotal(); ordidx++)
        {
            if(OrderSelect(ordidx,SELECT_BY_POS,MODE_TRADES)==false) break;
            double ordask = MarketInfo(OrderSymbol(),MODE_ASK);
            double ordbid = MarketInfo(OrderSymbol(),MODE_BID);
            int ordtype = OrderType();
            string typestr = "";
            switch(ordtype)
            {
                case OP_BUY:
                typestr = " buy ";
                break;
                case OP_SELL:
                typestr = " sell ";
                break;
                case OP_BUYLIMIT:
                typestr = " buylimit ";
                break;
                case OP_SELLLIMIT:
                typestr = " selllimit ";
                break;
                case OP_BUYSTOP:
                typestr = " buystop ";
                break;
                case OP_SELLSTOP:
                typestr = " sellstop ";
                break;
                default:
                break;
            }
            mailcontent += "Ticket:" + (string)OrderTicket() + " " + OrderSymbol() + typestr + " at " + (string)OrderOpenPrice() + "\nSL:" + (string)OrderStopLoss() + " TP:" + (string)OrderTakeProfit() + "\nAsk:" + (string)ordask + " Bid:" + (string)ordbid + "\nLots:" + (string)OrderLots() + " profit:" + (string)OrderProfit() + "\n\n";
        }
        mailcontent += "###Orders end###\n";
        if(SendMail("Status Reports", mailcontent))
        {
            lastreporttime = nowtime;
        }else
        {
            WriteToLogFile("SendMail failed error code:" + (string)GetLastError());
        }
    }

   printf("OnTimer is here~");
}
//+------------------------------------------------------------------+
//| Tester function                                                  |
//+------------------------------------------------------------------+
double OnTester()
{
   double ret=0.0;
   printf("OnTester called");
   return(ret);
}
//+------------------------------------------------------------------+
//| ChartEvent function                                              |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,
                  const long &lparam,
                  const double &dparam,
                  const string &sparam)
{

}
//+------------------------------------------------------------------+
//| judge if enough time passed                                      |
//+------------------------------------------------------------------+
bool IsEnoughTimePassed(datetime oldtime, datetime newtime, long timepassed)
{
    if ((newtime > oldtime) && (newtime - oldtime > timepassed)) {
        return true;
    } else {
        return false;
    }
}
//+------------------------------------------------------------------+
//| Write usefull log to file                                        |
//+------------------------------------------------------------------+
void WriteToLogFile(string logstring)
{
    int handle;
    string filedatestr = "" + (string)TimeYear(TimeCurrent()) + "_" + (string)TimeMonth(TimeCurrent()) + "_" + (string)TimeDay(TimeCurrent()) + "";
    handle = FileOpen("mylog_" + filedatestr + ".txt", FILE_READ|FILE_WRITE);
    if(handle > 0)
    {
        FileSeek(handle, 0, SEEK_END);
        FileWrite(handle,logstring);
        FileClose(handle);
    }
}

