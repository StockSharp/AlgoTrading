//+------------------------------------------------------------------+
//|                                             AutoPendingByRSI.mq4 |
//|                                                            Moggy |
//|                                             moggylew@hotmail.com |
//+------------------------------------------------------------------+
#property copyright "Moggy"
#property link      "moggylew@hotmail.com"
#property version   "1.00"
#property strict

#define MAXSYMBOLCNT 100
#define SMART_BY_IND_MAGIC 20140812
#define INVALID_PIPS -14567894
#define INVALID_SYMBOLS "invalid"
#define INVALID_TICKET -1
#define INVALID_MAX_PRICE -123456
#define INVALID_MIN_PRICE 123456
#define MAX_MODIFY_TRY_TIME 100
#define MATCHTIMEBEFOREPENDING 5//should continuous match for times before send pending orders

//TESTING
//symbol,pendingpips,weekendpluspips,cancelpips,trailingsl,takeprofit
//if trailingsl > takeprofit,the trailing is disabled in fact~

extern string tradesymbol1 = "EURUSDm_802_2802_1602_200_860,USDCHFm_506_2606_1506_120_600";
extern string tradesymbol2 = "USDJPYm_460_2802_1302_100_320";
extern string tradesymbol3 = "";
extern string tradesymbol4 = "";
extern string tradesymbol5 = "";
extern string tradesymbol6 = "";

/*use this if testmode is true
extern string tradesymbol1 = "EURUSDm_802_2802_1602_200_860";
extern string tradesymbol2 = "";
extern string tradesymbol3 = "";
extern string tradesymbol4 = "";
extern string tradesymbol5 = "";
extern string tradesymbol6 = "";*/

//TESTING
extern bool testmode = false;//true:for backtest
extern bool weekenddelete = false;//true:delete false:modify

//trading end time in friday
extern int weekendbeginday = 5;
extern string weekendbegintime = "20:30";//friday 20:30
extern int weekendendday = 7;
extern string weekendendtime = "23:30";//sunday 20:30

//for mail motification
//symbolsstr1 + symbolsstr2 do not exeed MAXSYMBOLCNT
string symbolsstr1 = "AUDJPYm,AUDCADm,AUDCHFm,AUDNZDm,AUDSGDm,AUDUSDm,CADCHFm,CADHKDm,CADJPYm,CHFJPYm,CHFPLNm,CHFSGDm,EURAUDm,EURBRLm,EURCADm,EURCHFm,EURDKKm,EURGBPm,EURHKDm,EURHUFm,EURJPYm,EURMXNm";
string symbolsstr2 = "GBPCHFm,GBPJPYm,GBPNZDm,GBPUSDm,HKDJPYm,HUFJPYm,MXNJPYm,NZDCADm,NZDCHFm,NZDJPYm,NZDSGDm,NZDUSDm,SGDJPYm,USDBRLm,USDCADm,USDCHFm,USDCZKm,USDDKKm,USDHKDm,USDHUFm,USDJPYm,USDMXNm";
string symbolsstr3 = "EURNOKm,EURNZDm,EURPLNm,EURRUBm,EURSEKm,EURSGDm,EURTRYm,EURUSDm,EURZARm,GBPAUDm,GBPCADm,USDNOKm,USDPLNm,USDRONm,USDRUBm,USDSEKm,USDSGDm,USDTRYm,USDZARm,XAGUSDm,XAUUSDm,ZARJPYm";
long sendmininterval = 3600;

int slippage = 0;

//rsi
extern int rsitimeframe = PERIOD_H4;
extern int rsiperiod  = 14;
extern ENUM_APPLIED_PRICE rsiappliedprice = PRICE_CLOSE;
extern int rsishift = 0;
extern double rsiupperline = 70;
extern double rsilowerline = 30;

string allsymbols[MAXSYMBOLCNT];
datetime alllastsendtime[MAXSYMBOLCNT];
int reporthour = -1;
int symbolscnt = 0;

//trade symbols,pendingpips,weekendpluspips,cancelpips
string alltradesymbols[MAXSYMBOLCNT];
double allpendingpips[MAXSYMBOLCNT];
double allweekendpluspips[MAXSYMBOLCNT];
double allcancelpips[MAXSYMBOLCNT];
double alltrailingsl[MAXSYMBOLCNT];
double alltakeprofit[MAXSYMBOLCNT];
int tradesymbolscnt = 0;

int matchtimesbeforepending[MAXSYMBOLCNT];

bool isInWeekendMode = false;
int trytimes = 0;
bool needretry = true;
int magicweekendexpiration = 1577836800;//2020-1-1 0:0:0 intvalue:1577836800

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   isInWeekendMode = IsTimeAtWeekEndMode();//judge if start at weedend mode
//--- create timer
   WriteToLogFile("OnInit called to init timer");
   //TESTING
   if(!testmode)
   {
       if(!EventSetTimer(2))
       {
           WriteToLogFile("OnInit EventSetTimer error:" + (string)GetLastError());
       }
   }

   //split the symbols
   ushort u_sep = StringGetCharacter(",",0);
   string allsymbols1[MAXSYMBOLCNT];
   string allsymbols2[MAXSYMBOLCNT];
   string allsymbols3[MAXSYMBOLCNT];
   int symbolscnt1 = StringSplit(symbolsstr1,u_sep,allsymbols1);
   int symbolscnt2 = StringSplit(symbolsstr2,u_sep,allsymbols2);
   int symbolscnt3 = StringSplit(symbolsstr3,u_sep,allsymbols3);
   symbolscnt = symbolscnt1 + symbolscnt2 + symbolscnt3;
   if(symbolscnt > MAXSYMBOLCNT)
   {
       return (INIT_FAILED);
   }
   int symbolidx = 0;
   for(int idx1 = 0; idx1 < symbolscnt1; idx1++)
   {
       allsymbols[symbolidx] = allsymbols1[idx1];
       //WriteToLogFile("allsymbols1[" + idx1 + "]:" + allsymbols1[idx1]);
       symbolidx++;
   }
   for(int idx2 = 0; idx2 < symbolscnt2; idx2++)
   {
       allsymbols[symbolidx] = allsymbols2[idx2];
       //WriteToLogFile("allsymbols2[" + idx2 + "]:" + allsymbols2[idx2]);
       symbolidx++;
   }
   for(int idx3 = 0; idx3 < symbolscnt3; idx3++)
   {
       allsymbols[symbolidx] = allsymbols3[idx3];
       //WriteToLogFile("allsymbols3[" + idx3 + "]:" + allsymbols3[idx3]);
       symbolidx++;
   }
   for(int idx = 0; idx < symbolscnt; idx++)
   {
       alllastsendtime[idx] = 0;
       //WriteToLogFile("allsymbols[" + idx + "]:" + allsymbols[idx]);
   }

   //init trade data
   int tradeconfigidx = 0;
   string tradedatatemp[MAXSYMBOLCNT];
   ushort u_sep_data = StringGetCharacter("_",0);
   int tradedatacnttemp;
   int tempidx;
   for(tempidx = 0;tempidx<MAXSYMBOLCNT;tempidx++)
   {
       alltradesymbols[tempidx] = INVALID_SYMBOLS;
       allpendingpips[tempidx] = INVALID_PIPS;
       allweekendpluspips[tempidx] = INVALID_PIPS;
       allcancelpips[tempidx] = INVALID_PIPS;
       alltrailingsl[tempidx] = INVALID_PIPS;
       alltakeprofit[tempidx] = INVALID_PIPS;
       
       matchtimesbeforepending[tempidx] = 0;
   }
   tradedatacnttemp = StringSplit(tradesymbol1,u_sep,tradedatatemp);
   for(tempidx = 0;tempidx<tradedatacnttemp;tempidx++)
   {
       string tradeconfig[6];
       StringSplit(tradedatatemp[tempidx],u_sep_data,tradeconfig);
       alltradesymbols[tradeconfigidx] = tradeconfig[0];
       allpendingpips[tradeconfigidx] = StringToDouble(tradeconfig[1]);
       allweekendpluspips[tradeconfigidx] = StringToDouble(tradeconfig[2]);
       allcancelpips[tradeconfigidx] = StringToDouble(tradeconfig[3]);
       alltrailingsl[tradeconfigidx] = StringToDouble(tradeconfig[4]);
       alltakeprofit[tradeconfigidx] = StringToDouble(tradeconfig[5]);
       tradeconfigidx++;
   }
   tradedatacnttemp = StringSplit(tradesymbol2,u_sep,tradedatatemp);
   for(tempidx = 0;tempidx<tradedatacnttemp;tempidx++)
   {
       string tradeconfig[6];
       StringSplit(tradedatatemp[tempidx],u_sep_data,tradeconfig);
       alltradesymbols[tradeconfigidx] = tradeconfig[0];
       allpendingpips[tradeconfigidx] = StringToDouble(tradeconfig[1]);
       allweekendpluspips[tradeconfigidx] = StringToDouble(tradeconfig[2]);
       allcancelpips[tradeconfigidx] = StringToDouble(tradeconfig[3]);
       alltrailingsl[tradeconfigidx] = StringToDouble(tradeconfig[4]);
       alltakeprofit[tradeconfigidx] = StringToDouble(tradeconfig[5]);
       tradeconfigidx++;
   }
   tradedatacnttemp = StringSplit(tradesymbol3,u_sep,tradedatatemp);
   for(tempidx = 0;tempidx<tradedatacnttemp;tempidx++)
   {
       string tradeconfig[6];
       StringSplit(tradedatatemp[tempidx],u_sep_data,tradeconfig);
       alltradesymbols[tradeconfigidx] = tradeconfig[0];
       allpendingpips[tradeconfigidx] = StringToDouble(tradeconfig[1]);
       allweekendpluspips[tradeconfigidx] = StringToDouble(tradeconfig[2]);
       allcancelpips[tradeconfigidx] = StringToDouble(tradeconfig[3]);
       alltrailingsl[tradeconfigidx] = StringToDouble(tradeconfig[4]);
       alltakeprofit[tradeconfigidx] = StringToDouble(tradeconfig[5]);
       tradeconfigidx++;
   }
   tradedatacnttemp = StringSplit(tradesymbol4,u_sep,tradedatatemp);
   for(tempidx = 0;tempidx<tradedatacnttemp;tempidx++)
   {
       string tradeconfig[6];
       StringSplit(tradedatatemp[tempidx],u_sep_data,tradeconfig);
       alltradesymbols[tradeconfigidx] = tradeconfig[0];
       allpendingpips[tradeconfigidx] = StringToDouble(tradeconfig[1]);
       allweekendpluspips[tradeconfigidx] = StringToDouble(tradeconfig[2]);
       allcancelpips[tradeconfigidx] = StringToDouble(tradeconfig[3]);
       alltrailingsl[tradeconfigidx] = StringToDouble(tradeconfig[4]);
       alltakeprofit[tradeconfigidx] = StringToDouble(tradeconfig[5]);
       tradeconfigidx++;
   }
   tradedatacnttemp = StringSplit(tradesymbol5,u_sep,tradedatatemp);
   for(tempidx = 0;tempidx<tradedatacnttemp;tempidx++)
   {
       string tradeconfig[6];
       StringSplit(tradedatatemp[tempidx],u_sep_data,tradeconfig);
       alltradesymbols[tradeconfigidx] = tradeconfig[0];
       allpendingpips[tradeconfigidx] = StringToDouble(tradeconfig[1]);
       allweekendpluspips[tradeconfigidx] = StringToDouble(tradeconfig[2]);
       allcancelpips[tradeconfigidx] = StringToDouble(tradeconfig[3]);
       alltrailingsl[tradeconfigidx] = StringToDouble(tradeconfig[4]);
       alltakeprofit[tradeconfigidx] = StringToDouble(tradeconfig[5]);
       tradeconfigidx++;
   }
   tradedatacnttemp = StringSplit(tradesymbol6,u_sep,tradedatatemp);
   for(tempidx = 0;tempidx<tradedatacnttemp;tempidx++)
   {
       string tradeconfig[6];
       StringSplit(tradedatatemp[tempidx],u_sep_data,tradeconfig);
       alltradesymbols[tradeconfigidx] = tradeconfig[0];
       allpendingpips[tradeconfigidx] = StringToDouble(tradeconfig[1]);
       allweekendpluspips[tradeconfigidx] = StringToDouble(tradeconfig[2]);
       allcancelpips[tradeconfigidx] = StringToDouble(tradeconfig[3]);
       alltrailingsl[tradeconfigidx] = StringToDouble(tradeconfig[4]);
       alltakeprofit[tradeconfigidx] = StringToDouble(tradeconfig[5]);
       tradeconfigidx++;
   }
   tradesymbolscnt = tradeconfigidx;

   for(int test = 0;test<tradesymbolscnt;test++)
   {
       if("" == alltradesymbols[test] || allpendingpips[test] < 30 || allpendingpips[test] > allcancelpips[test])
       {
           return INIT_FAILED;
       }
       WriteToLogFile("alltradesymbols[" + (string)test + "]:" + (string)alltradesymbols[test] + " allpendingpips[" + (string)test + "]:" + (string)allpendingpips[test] + " allweekendpluspips[" + (string)test + "]:" + (string)allweekendpluspips[test] + " allcancelpips[" + (string)test + "]:" + (string)allcancelpips[test]);
   }
   
//---
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
//---
    //TESTING
    if(testmode)
    {
        CheckForHandlePendingOrders();
        CheckForHandleOpenedOrders();
        if(((AccountEquity()/AccountBalance()) < 0.95))
        {
            WriteToWarningFile();
        }
    }
}
//+------------------------------------------------------------------+
//| Timer function                                                   |
//+------------------------------------------------------------------+
void OnTimer()
{
//---
   printf("OnTimer is here~");

   if(!testmode)
   {
       CheckForSendMail();
       CheckForHandlePendingOrders();
       CheckForHandleOpenedOrders();
   }
}
//+------------------------------------------------------------------+
//| Tester function                                                  |
//+------------------------------------------------------------------+
double OnTester()
  {
//---
   double ret=0.0;
//---
printf("OnTester called");
//---
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
//---
   
  }
//+------------------------------------------------------------------+

//customize function
double GetRSIvalue(string localsymbol)
{
    return iRSI(localsymbol,rsitimeframe,rsiperiod,rsiappliedprice,rsishift);
}

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
    string baseinfo = TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS);
    baseinfo = baseinfo + " Balance:" + (string)AccountBalance() + " Equity:" + (string)AccountEquity() + "\nMargin:" + (string)AccountMargin() + " FreeMargin:" + (string)AccountFreeMargin() + "\n";
    if(handle > 0)
    {
        FileSeek(handle, 0, SEEK_END);
        FileWrite(handle,baseinfo,logstring);
        FileClose(handle);
    }
}

//+------------------------------------------------------------------+
//| Write usefull info to warning file                               |
//+------------------------------------------------------------------+
void WriteToWarningFile()
{
   int handle;
   datetime curr = TimeCurrent();
   int curryear = TimeYear(curr);
   int currmonth = TimeMonth(curr);
   int currday = TimeDay(curr);
   int currhour = TimeHour(curr);
   int currmin = TimeMinute(curr);
   int currsec = TimeSeconds(curr);

   string filewarning = "" + Symbol() + "_warning" + "";
   double localequity = AccountEquity();
   double localbalance = AccountBalance();
   if(0 == currmin && currsec < 4)
   {
       handle = FileOpen("mylog_" + filewarning + ".txt", FILE_READ|FILE_WRITE);
       string recordstr = "" + (string)curryear + "." + (string)currmonth + "." + (string)currday + " " + (string)currhour + ":" + (string)currmin + ":" + (string)currsec + "\t" + (string)localequity + "\t" + (string)localbalance + "\t" + (string)(localequity/localbalance);
       if(handle > 0)
       {
           FileSeek(handle, 0, SEEK_END);
           FileWrite(handle,recordstr);
           FileClose(handle);
       }
   }
}

void CheckForSendMail()
{
    datetime nowtime = TimeCurrent();
    //send hour report
    if((reporthour != TimeHour(nowtime)) && (10 > TimeMinute(nowtime)) && (OrdersTotal() > 0))
    {
        string currorderssymbol[MAXSYMBOLCNT];
        string hourmailcontent = "";
        //initialize the currorderssymbol
        for(int idx = 0; idx < MAXSYMBOLCNT; idx++)
        {
            currorderssymbol[idx] = "";
        }
        hourmailcontent += "" + (string)nowtime + "\n";
        hourmailcontent += "###Orders begin###\n";
        //construct the unique symbols array
        for(int ordidx = OrdersTotal() - 1; ordidx >= 0; ordidx--)
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
            hourmailcontent += "Ticket:" + (string)OrderTicket() + " " + OrderSymbol() + typestr + " at " + (string)OrderOpenPrice() + "\nSL:" + (string)OrderStopLoss() + " TP:" + (string)OrderTakeProfit() + "\nAsk:" + (string)ordask + " Bid:" + (string)ordbid + "\nLots:" + (string)OrderLots() + " profit:" + (string)OrderProfit() + "\n\n";
            for(int insertidx = 0; insertidx < MAXSYMBOLCNT; insertidx++)
            {
                if(OrderSymbol() == currorderssymbol[insertidx])
                {
                    //already exist
                    break;
                }else if(currorderssymbol[insertidx] == "")
                {
                    //not exist
                    currorderssymbol[insertidx] = OrderSymbol();
                    break;
                }
            }
        }
        hourmailcontent += "###Orders end###\n\n";
        hourmailcontent += "***instru begin***\n";
        for(int idx = 0; (idx < MAXSYMBOLCNT) && (currorderssymbol[idx] != ""); idx++)
        {
            double orderrsivalue = GetRSIvalue(currorderssymbol[idx]);
            hourmailcontent += "" + currorderssymbol[idx] + "\nRSI:" + (string)orderrsivalue + "\n";
        }
        hourmailcontent += "***instru end***";
        if(SendMail("Hour Reports", hourmailcontent))
        {
            reporthour = TimeHour(nowtime);
        }
    }
    
    for(int idx = 0; idx < symbolscnt;idx++)
    {
        string localsymbol = allsymbols[idx];
        double currrsivalue = GetRSIvalue(localsymbol);
        
        if(IsEnoughTimePassed(alllastsendtime[idx],nowtime,sendmininterval))
        {
            if (currrsivalue > rsiupperline)
            {
                if(SendMail("Sell " + localsymbol, "" + TimeToString(nowtime,TIME_DATE|TIME_MINUTES|TIME_SECONDS) + "\nBid:"
                                    + (string)MarketInfo(localsymbol,MODE_BID) + " Ask:" + (string)MarketInfo(localsymbol,MODE_ASK)
                                    + "\nRSI:" + (string)currrsivalue))
                                    {
                                        //WriteToLogFile("send sell for " + localsymbol);
                                        alllastsendtime[idx] = nowtime;
                                    }
                                    else{
                                        //WriteToLogFile("send sell for " + localsymbol + " error:" + GetLastError());
                                    }
            }
            if (currrsivalue < rsilowerline)
            {
                if(SendMail("Buy " + localsymbol, "" + TimeToString(nowtime,TIME_DATE|TIME_MINUTES|TIME_SECONDS) + "\nBid:"
                                    + (string)MarketInfo(localsymbol,MODE_BID) + " Ask:" + (string)MarketInfo(localsymbol,MODE_ASK)
                                    + "\nRSI:" + (string)currrsivalue))
                                    {
                                        //WriteToLogFile("send buy for " + localsymbol);
                                        alllastsendtime[idx] = nowtime;
                                    }
                                    else{
                                        //WriteToLogFile("send buy for " + localsymbol + " error:" + GetLastError());
                                    }
            }
        }
   }
}

void CheckForHandlePendingOrders()
{
    //in weekend mode
    if(IsTimeAtWeekEndMode())
    {
        if(!isInWeekendMode)//newly to weekend
        {
            isInWeekendMode = true;
            trytimes = 0;//reset trytimes
            needretry = true;
        }
        //in weekend,try to modify again if need
        if(needretry && (trytimes < MAX_MODIFY_TRY_TIME)){
            ModifyToWeekendMode();
            trytimes++;
        }
        return;
    }else
    {
        if(isInWeekendMode)//newly from weekend
        {
            isInWeekendMode = false;
            trytimes = 0;//reset trytimes
            needretry = true;
        }
        if(needretry && (trytimes < MAX_MODIFY_TRY_TIME)){
            ModifyFromWeekendMode();
            trytimes++;
            return;
        }
    }
    
    //check for open pending
    for(int idx = 0; idx < tradesymbolscnt;idx++)
    {
        string localsymbol = alltradesymbols[idx];
        double currrsivalue = GetRSIvalue(localsymbol);
        
        if (currrsivalue > rsiupperline)
        {
            matchtimesbeforepending[idx]++;
            DeleteOppsiteIfAny(localsymbol,OP_BUYLIMIT);
            if(!ExistThisSymbolOrder(localsymbol) && (MATCHTIMEBEFOREPENDING < matchtimesbeforepending[idx]))
            {
                matchtimesbeforepending[idx] = 0;//reset to zero
                double proplots = GetProperLots();
                double pendingpips = GetPendingPips(localsymbol);
                double symbolpoint = MarketInfo(localsymbol, MODE_POINT);
                double symbolbid = MarketInfo(localsymbol, MODE_BID);
                if(!OrderSend(localsymbol,OP_SELLLIMIT,proplots,symbolbid + pendingpips*symbolpoint,slippage,0,0,"selllimit",SMART_BY_IND_MAGIC,0,Green))
                {
                    WriteToLogFile("open OP_SELLLIMIT failed symbol " + localsymbol + " at " + (string)(symbolbid + pendingpips*symbolpoint) + " symbolbid:" + (string)symbolbid + " errorcode:" + (string)GetLastError());
                }else
                {
                }
            }
        }else if (currrsivalue < rsilowerline)
        {
            matchtimesbeforepending[idx]++;
            DeleteOppsiteIfAny(localsymbol,OP_SELLLIMIT);
            if(!ExistThisSymbolOrder(localsymbol) && (MATCHTIMEBEFOREPENDING < matchtimesbeforepending[idx]))
            {
                matchtimesbeforepending[idx] = 0;//reset to zero
                double proplots = GetProperLots();
                double pendingpips = GetPendingPips(localsymbol);
                double symbolpoint = MarketInfo(localsymbol, MODE_POINT);
                double symbolask = MarketInfo(localsymbol, MODE_ASK);
                if(!OrderSend(localsymbol,OP_BUYLIMIT,proplots,symbolask - pendingpips*symbolpoint,slippage,0,0,"buylimit",SMART_BY_IND_MAGIC,0,Blue))
                {
                    WriteToLogFile("open OP_BUYLIMIT failed symbol " + localsymbol + " at " + (string)(symbolask - pendingpips*symbolpoint) + " symbolask:" + (string)symbolask + " errorcode:" + (string)GetLastError());
                }else
                {
                }                
            }
        }else
        {
            matchtimesbeforepending[idx] = 0;//not continuous,reset to zero
        }
    }
    RefreshRates();
    
    //*TESTING check for open another pending(martingale)
    for(int findmaxminidx = 0; findmaxminidx < tradesymbolscnt;findmaxminidx++)
    {
        //find each symbol's opened max buy or min sell
        string findmaxminsymbol = alltradesymbols[findmaxminidx];
        double minbuyprice = INVALID_MIN_PRICE;
        double maxsellprice = INVALID_MAX_PRICE;
        double minbuylots = 0;
        double maxselllots = 0;
        double currbid = MarketInfo(findmaxminsymbol, MODE_BID);
        double currask = MarketInfo(findmaxminsymbol, MODE_ASK);
        double currpoint = MarketInfo(findmaxminsymbol, MODE_POINT);
        double symbolpendingpips = GetPendingPips(findmaxminsymbol);
        //find the buy/sell orders
        for(int openedidx = OrdersTotal()-1;openedidx >= 0;openedidx--)
        {
            if(OrderSelect(openedidx,SELECT_BY_POS,MODE_TRADES)==false) break;
            int openedtype = OrderType();
            if((findmaxminsymbol == OrderSymbol()) && (OrderMagicNumber() == SMART_BY_IND_MAGIC))//it's opened by EA
            {
               if((OP_BUY == openedtype) || (OP_BUYLIMIT == openedtype))//buy/buylimit order
               {
                   //if(OrderOpenPrice() - currbid > symbolpendingpips*currpoint)//loss too much
                   //{
                   if(minbuyprice > OrderOpenPrice())
                   {
                       minbuyprice = OrderOpenPrice();
                       minbuylots = OrderLots();//could multiple lots
                   }
               }else if((OP_SELL == openedtype) || (OP_SELLLIMIT == openedtype))
               {
                   //if(currask - OrderOpenPrice() > symbolpendingpips*currpoint)//loss too much
                   //{
                   if(maxsellprice < OrderOpenPrice())
                   {
                       maxsellprice = OrderOpenPrice();
                       maxselllots = OrderLots();//could multiple lots
                   }
                   //}
               }
            }
        }
        if((INVALID_MIN_PRICE > minbuyprice) && (minbuyprice - currbid > symbolpendingpips*currpoint))//the min buy loss too much
        {
            double findminbuyrsivalue = GetRSIvalue(findmaxminsymbol);
            if (findminbuyrsivalue < rsiupperline)//if do not judge,the SELLLIMIT signal,will close the following orders
            {
                if(!OrderSend(findmaxminsymbol,OP_BUYLIMIT,minbuylots,currask - symbolpendingpips*currpoint,slippage,0,0,"buylimitanother",SMART_BY_IND_MAGIC,0,Blue))
                {
                    WriteToLogFile("open OP_BUYLIMIT another failed symbol " + findmaxminsymbol + " at " + (string)(currask - symbolpendingpips*currpoint) + " ask:" + (string)currask + " errorcode:" + (string)GetLastError());
                }else
                {
                    WriteToLogFile("open OP_BUYLIMIT another success symbol " + findmaxminsymbol + " at " + (string)(currask - symbolpendingpips*currpoint) + " ask:" + (string)currask + " minbuyprice:" + (string)minbuyprice);
                }
                break;//once a time
            }
        }
        if((INVALID_MAX_PRICE < maxsellprice) && (currask - maxsellprice > symbolpendingpips*currpoint))//the max sell loss too much
        {
            double findmaxsellrsivalue = GetRSIvalue(findmaxminsymbol);
            if (findmaxsellrsivalue > rsilowerline)//if do not judge,the SELLLIMIT signal,will close the following orders
            {
                if(!OrderSend(findmaxminsymbol,OP_SELLLIMIT,maxselllots,currbid + symbolpendingpips*currpoint,slippage,0,0,"selllimitanother",SMART_BY_IND_MAGIC,0,Green))
                {
                    WriteToLogFile("open OP_SELLLIMIT another failed symbol " + findmaxminsymbol + " at " + (string)(currbid + symbolpendingpips*currpoint) + " bid:" + (string)currbid + " errorcode:" + (string)GetLastError());
                }else
                {
                    WriteToLogFile("open OP_SELLLIMIT another success symbol " + findmaxminsymbol + " at " + (string)(currbid + symbolpendingpips*currpoint) + " bid:" + (string)currbid + " maxsellprice:" + (string)maxsellprice);
                }
                break;//once a time 
            }
        }
    }
    RefreshRates();
    //*/
    
   //check for close if too far
   for(int findidx = OrdersTotal()-1;findidx >= 0;findidx--)
   {
       if(OrderSelect(findidx,SELECT_BY_POS,MODE_TRADES)==false) break;
       string ordersymbol = OrderSymbol();
       int ordertype = OrderType();
       int orderticket = OrderTicket();
       double openprice = OrderOpenPrice();
       double symbolpoint = MarketInfo(ordersymbol, MODE_POINT);
       double symbolask = MarketInfo(ordersymbol, MODE_ASK);
       double symbolbid = MarketInfo(ordersymbol, MODE_BID);
       if((SymbolSmartTradeAllowed(ordersymbol)) && (OrderMagicNumber() == SMART_BY_IND_MAGIC))
       {
           if(OP_BUYLIMIT == ordertype
           && (symbolask - openprice > GetCanclePips(ordersymbol)*symbolpoint)
           )
           {
               if(!OrderDelete(orderticket))
               {
                   WriteToLogFile("try to close buylimit ticket " + (string)orderticket + " failed" + " errorcode:" + (string)GetLastError());
               }else
               {
                   WriteToLogFile("close buylimit ticket " + (string)orderticket + " success" + " openprice:" + (string)openprice + " symbolask:" + (string)symbolask);
               }
           }else if(OP_SELLLIMIT == ordertype
           && (openprice - symbolbid > GetCanclePips(ordersymbol)*symbolpoint)
           )
           {
               if(!OrderDelete(orderticket))
               {
                   WriteToLogFile("try to close selllimit ticket " + (string)orderticket + " failed" + " errorcode:" + (string)GetLastError());
               }else
               {
                   WriteToLogFile("close selllimit ticket " + (string)orderticket + " success" + " openprice:" + (string)openprice + " symbolbid:" + (string)symbolbid);
               }
           }
       }
   }
}

//handle opened orders
void CheckForHandleOpenedOrders()
{
    for(int idx = OrdersTotal() - 1;idx >= 0;idx--)
    {
        if(OrderSelect(idx,SELECT_BY_POS,MODE_TRADES)==false) break;
        int ordertype = OrderType();
        string localsymbol = OrderSymbol();
        int orderticket = OrderTicket();
        double openprice = OrderOpenPrice();
        double symbolpoint = MarketInfo(localsymbol, MODE_POINT);
        double symbolask = MarketInfo(localsymbol, MODE_ASK);
        double symbolbid = MarketInfo(localsymbol, MODE_BID);
        if(OrderMagicNumber() == SMART_BY_IND_MAGIC)//open by ea
        {
            double trailingsl = GetTrailingSLPips(localsymbol);
            double takeprofit = GetTakeProfitPips(localsymbol);
            double ordersl = OrderStopLoss();
            if(OP_BUY == ordertype)
            {
                if(symbolbid > (openprice + takeprofit*symbolpoint))//take profit
                {
                    if(!OrderClose(orderticket, OrderLots(), symbolbid, slippage, CLR_NONE))
                    {
                        WriteToLogFile("close buy ticket:" + (string)orderticket + " tp:" + (string)(openprice + takeprofit*symbolpoint) + " bid:" + (string)symbolbid + " failed error:" + (string)GetLastError());
                    }else
                    {
                        WriteToLogFile("close buy ticket:" + (string)orderticket + " tp:" + (string)(openprice + takeprofit*symbolpoint) + " bid:" + (string)symbolbid + " success.");
                    }
                }else if((symbolbid - openprice > trailingsl*symbolpoint)
                && (ordersl < symbolbid - trailingsl*symbolpoint)
                )
                {
                    if(!OrderModify(orderticket,openprice,symbolbid - trailingsl*symbolpoint,0/*openprice + takeprofit*symbolpoint*/,0,Green))
                    {
                        WriteToLogFile("modify buy ticket:" + (string)orderticket + " originsl:" + (string)ordersl + " newsl:" + (string)(symbolbid - trailingsl*symbolpoint) + " tp:" + (string)(openprice + takeprofit*symbolpoint) + " failed code:" + (string)GetLastError());
                    }else
                    {
                        WriteToLogFile("modify buy ticket:" + (string)orderticket + " originsl:" + (string)ordersl + " newsl:" + (string)(symbolbid - trailingsl*symbolpoint) + " tp:" + (string)(openprice + takeprofit*symbolpoint) + " success");
                    }
                }
            }else if(OP_SELL == ordertype)
            {
                if(symbolask < (openprice - takeprofit*symbolpoint))//take profit
                {
                    if(!OrderClose(orderticket, OrderLots(), symbolask, slippage, CLR_NONE))
                    {
                        WriteToLogFile("close sell ticket:" + (string)orderticket + " tp:" + (string)(openprice - takeprofit*symbolpoint) + " ask:" + (string)symbolask + " failed error:" + (string)GetLastError());
                    }else
                    {
                        WriteToLogFile("close sell ticket:" + (string)orderticket + " tp:" + (string)(openprice - takeprofit*symbolpoint) + " ask:" + (string)symbolask + " success.");
                    }
                }else if((openprice - symbolask > trailingsl*symbolpoint)
                && ((ordersl > symbolask + trailingsl*symbolpoint) || (0 == ordersl))
                )
                {
                    if(!OrderModify(orderticket,openprice,symbolask + trailingsl*symbolpoint,0/*openprice - takeprofit*symbolpoint*/,0,Red))
                    {
                        WriteToLogFile("modify sell ticket:" + (string)orderticket + " originsl:" + (string)ordersl + " newsl:" + (string)(symbolask + trailingsl*symbolpoint) + " tp:" + (string)(openprice - takeprofit*symbolpoint) + " failed code:" + (string)GetLastError());
                    }else
                    {
                        WriteToLogFile("modify sell ticket:" + (string)orderticket + " originsl:" + (string)ordersl + " newsl:" + (string)(symbolask + trailingsl*symbolpoint) + " tp:" + (string)(openprice - takeprofit*symbolpoint) + " success");
                    }
                }
            }
        }
    }
}

bool IsTimeAtWeekEndMode()
{
    int curr = 0;
    int weekendstart = 0;
    int weekendend = 0;
    
    //minutes of week
    curr = (TimeDayOfWeek(TimeCurrent()) - 1)*24*60 + TimeHour(TimeCurrent())*60 + TimeMinute(TimeCurrent());
    //WriteToLogFile("IsTimeAtWeekEndMode dayofweek:" + (string)TimeDayOfWeek(TimeCurrent()) + " hour:" + (string)TimeHour(TimeCurrent()) + " minute:" + (string)TimeHour(TimeCurrent()));
    weekendstart = (weekendbeginday - 1)*24*60 + TimeHour(StrToTime(weekendbegintime))*60 + TimeMinute(StrToTime(weekendbegintime));
    weekendend = (weekendendday - 1)*24*60 + TimeHour(StrToTime(weekendendtime))*60 + TimeMinute(StrToTime(weekendendtime));
    if((curr > weekendstart) && (curr < weekendend))
    {
        return (true);
    }

    return (false);
}

double GetProperLots()
{
    double lots = (MathFloor(AccountEquity()/200) * 0.01);
    if(lots < 0.01)
    {
        lots = 0.01;
    }
    return lots;
}

bool ExistThisSymbolOrder(string symbol)
{
    for(int idx = OrdersTotal() - 1;idx >= 0;idx--)
    {
        if(OrderSelect(idx,SELECT_BY_POS,MODE_TRADES)==false) break;
        if((OrderSymbol() == symbol) && (OrderMagicNumber() == SMART_BY_IND_MAGIC))
        {
            return true;
        }
    }
    return false;
}

void DeleteOppsiteIfAny(string symbol,int opertype)
{
    for(int idx = OrdersTotal() - 1;idx >= 0;idx--)
    {
        if(OrderSelect(idx,SELECT_BY_POS,MODE_TRADES)==false) break;
        if((OrderSymbol() == symbol) && (OrderMagicNumber() == SMART_BY_IND_MAGIC) && opertype == OrderType())
        {
            int orderticket = OrderTicket();
            if(!OrderDelete(orderticket))
            {
                WriteToLogFile("try to delete oppsite order " + (string)orderticket + " failed errorcode:" + (string)GetLastError());
            }else
            {
                WriteToLogFile("try to delete oppsite order " + (string)orderticket + " sucess");
            }
        }
    }
}

double GetPendingPips(string symbol)
{
    for(int idx = 0;idx<tradesymbolscnt;idx++)
    {
        if(INVALID_SYMBOLS != symbol && symbol == alltradesymbols[idx] && allpendingpips[idx] > 0)
        {
            return allpendingpips[idx];
        }
    }
    return INVALID_PIPS;
}

double GetWeekendPlusPips(string symbol)
{
    for(int idx = 0;idx<tradesymbolscnt;idx++)
    {
        if(INVALID_SYMBOLS != symbol && symbol == alltradesymbols[idx] && allweekendpluspips[idx] > 0)
        {
            return allweekendpluspips[idx];
        }
    }
    return INVALID_PIPS;
}

double GetCanclePips(string symbol)
{
    for(int idx = 0;idx<tradesymbolscnt;idx++)
    {
        if(INVALID_SYMBOLS != symbol && symbol == alltradesymbols[idx] && allcancelpips[idx] > 0)
        {
            return allcancelpips[idx];
        }
    }
    return INVALID_PIPS;
}

double GetTrailingSLPips(string symbol)
{
    for(int idx = 0;idx<tradesymbolscnt;idx++)
    {
        if(INVALID_SYMBOLS != symbol && symbol == alltradesymbols[idx] && alltrailingsl[idx] > 0)
        {
            return alltrailingsl[idx];
        }
    }
    //if not found configuration
    double trailinglevel = MarketInfo(symbol, MODE_STOPLEVEL) * 10;
    WriteToLogFile("GetTrailingSLPips configuration not found symbol:" + symbol + " return trailingsl:" + (string)trailinglevel);
    return trailinglevel;
}

double GetTakeProfitPips(string symbol)
{
    for(int idx = 0;idx<tradesymbolscnt;idx++)
    {
        if(INVALID_SYMBOLS != symbol && symbol == alltradesymbols[idx] && alltakeprofit[idx] > 0)
        {
            return alltakeprofit[idx];
        }
    }
    //if not found configuration
    double takeprofitlevel = MarketInfo(symbol, MODE_STOPLEVEL) * 20;
    WriteToLogFile("GetTakeProfitPips configuration not found symbol:" + symbol + " return takeprofit:" + (string)takeprofitlevel);
    return takeprofitlevel;
}

bool SymbolSmartTradeAllowed(string symbol)
{
    for(int findidx = 0;findidx < tradesymbolscnt;findidx++)
    {
        if(INVALID_SYMBOLS != symbol && symbol == alltradesymbols[findidx])
        {
            return true;
        }
    }
    return false;
}

//called only once
void ModifyToWeekendMode()
{
    //for mail notification
    bool someonefailed = false;
    string failedstring = "";
    
    //modify orders,to weekend mode
    for(int weekendinidx = OrdersTotal()-1;weekendinidx >= 0;weekendinidx--)
    {
        if(OrderSelect(weekendinidx,SELECT_BY_POS,MODE_TRADES)==false) break;
        int weekendticket = OrderTicket();
        int weekendtype = OrderType();
        double weekendprice = OrderOpenPrice();
        string weekendsymbol = OrderSymbol();
        double weekendpoint = MarketInfo(weekendsymbol,MODE_POINT);
        double weekendpluspips = GetWeekendPlusPips(weekendsymbol);
        bool handleresult;
        if((OP_BUYLIMIT == weekendtype)
        && (OrderMagicNumber() == SMART_BY_IND_MAGIC)
        && (OrderExpiration() != magicweekendexpiration)//not in weekend mode
        )
        {
            if(weekenddelete)//delete
            {
                handleresult = OrderDelete(weekendticket);
            }else//modify
            {
                handleresult = OrderModify(weekendticket,weekendprice - weekendpluspips * weekendpoint,0,0,magicweekendexpiration,Green);
            }
            if(!handleresult)
            {
                someonefailed = true;
                if(weekenddelete)//delete
                {
                    failedstring += "buy limit ticket:" + (string)weekendticket + " delete failed errorcode:" + (string)GetLastError() + "\n";
                }else//modify
                {
                    failedstring += "buy limit ticket:" + (string)weekendticket + " " + weekendsymbol + " from " + (string)weekendprice + " to " + (string)(weekendprice - weekendpluspips * weekendpoint) + " failed errorcode:" + (string)GetLastError() + "\n";
                }
            }else//modify success
            {
            }
        }else if((OP_SELLLIMIT == weekendtype)
        && (OrderMagicNumber() == SMART_BY_IND_MAGIC)
        && (OrderExpiration() != magicweekendexpiration)//not in weekend mode
        )
        {
            if(weekenddelete)//delete
            {
                handleresult = OrderDelete(weekendticket);
            }else//modify
            {
                handleresult = OrderModify(weekendticket,weekendprice + weekendpluspips * weekendpoint,0,0,magicweekendexpiration,Green);
            }
            if(!handleresult)
            {
                someonefailed = true;
                if(weekenddelete)//delete
                {
                    failedstring += "sell limit ticket:" + (string)weekendticket + " delete failed errorcode:" + (string)GetLastError() + "\n";
                }else//modify
                {
                    failedstring += "sell limit ticket:" + (string)weekendticket + " " + weekendsymbol + " from " + (string)weekendprice + " to " + (string)(weekendprice + weekendpluspips * weekendpoint) + " failed errorcode:" + (string)GetLastError() + "\n";
                }
            }else//modify success
            {
            }
        }
        RefreshRates();
    }
    if(someonefailed && (!testmode))
    {
        if(SendMail("FAILED TO WEEKEND",failedstring))
        {
            WriteToLogFile("FAILED TO WEEKEND send success");
        }else
        {
            WriteToLogFile("FAILED TO WEEKEND send failed");
        }
    }
    if(!someonefailed)
    {
        needretry = false;//not try again
    }
}

//called only once
void ModifyFromWeekendMode()
{
    //for mail notification
    bool someonefailed = false;
    string failedstring = "";
    
    //modify orders,from weekend mode
    for(int weekendinidx = OrdersTotal()-1;weekendinidx >= 0;weekendinidx--)
    {
        if(OrderSelect(weekendinidx,SELECT_BY_POS,MODE_TRADES)==false) break;
        int weekendticket = OrderTicket();
        int weekendtype = OrderType();
        double weekendprice = OrderOpenPrice();
        string weekendsymbol = OrderSymbol();
        double weekendpoint = MarketInfo(weekendsymbol,MODE_POINT);
        double weekendpluspips = GetWeekendPlusPips(weekendsymbol);
        if((OP_BUYLIMIT == weekendtype)
        && (OrderMagicNumber() == SMART_BY_IND_MAGIC)
        && (OrderExpiration() == magicweekendexpiration)//from weekend mode
        )
        {
            double nowask = MarketInfo(weekendsymbol,MODE_ASK);
            double stoplevel = MarketInfo(weekendsymbol, MODE_STOPLEVEL)*weekendpoint;
            if(nowask - stoplevel > weekendprice + weekendpluspips * weekendpoint)
            {
                if(!OrderModify(weekendticket,weekendprice + weekendpluspips * weekendpoint,0,0,0,Green))
                {
                    someonefailed = true;
                    failedstring += "buy limit ticket:" + (string)weekendticket + " " + weekendsymbol + " from " + (string)weekendprice + " to " + (string)(weekendprice + weekendpluspips * weekendpoint) + " failed errorcode:" + (string)GetLastError() + "\n";
                    WriteToLogFile("ModifyFromWeekendMode OP_BUYLIMIT ticket:" + (string)weekendticket + " to " + (string)(weekendprice + weekendpluspips * weekendpoint) + " failed errorcode:" + (string)GetLastError());
                }else//modify success
                {
                    //RemoveFailedInfoFile(FILE_PREFIX_FROMWEEKEND,weekendticket);
                }
            }else//no need to modify,reset expiration
            {
                if(!OrderModify(weekendticket,weekendprice,0,0,0,Green))
                {
                    someonefailed = true;
                    failedstring += "buy limit ticket:" + (string)weekendticket + " " + weekendsymbol + " reset expiration failed\n";
                    WriteToLogFile("ModifyFromWeekendMode OP_BUYLIMIT ticket:" + (string)weekendticket + " " + weekendsymbol + " reset expiration failed errorcode:" + (string)GetLastError());
                }
            }
        }else if((OP_SELLLIMIT == weekendtype)
        && (OrderMagicNumber() == SMART_BY_IND_MAGIC)
        && (OrderExpiration() == magicweekendexpiration)//from weekend mode
        )
        {
            double nowbid = MarketInfo(weekendsymbol,MODE_BID);
            double stoplevel = MarketInfo(weekendsymbol, MODE_STOPLEVEL)*weekendpoint;
            if(nowbid + stoplevel < weekendprice - weekendpluspips * weekendpoint)
            {
                if(!OrderModify(weekendticket,weekendprice - weekendpluspips * weekendpoint,0,0,0,Green))
                {
                    someonefailed = true;
                    failedstring += "sell limit ticket:" + (string)weekendticket + " " + weekendsymbol + " from " + (string)weekendprice + " to " + (string)(weekendprice - weekendpluspips * weekendpoint) + " failed errorcode:" + (string)GetLastError() + "\n";
                    WriteToLogFile("ModifyFromWeekendMode OP_SELLLIMIT ticket:" + (string)weekendticket + " to " + (string)(weekendprice - weekendpluspips * weekendpoint) + " failed errorcode:" + (string)GetLastError());
                }else//modify success
                {
                }
            }else//no need to modify,reset expiration
            {
                if(!OrderModify(weekendticket,weekendprice,0,0,0,Green))
                {
                    someonefailed = true;
                    failedstring += "sell limit ticket:" + (string)weekendticket + " " + weekendsymbol + " reset expiration failed\n";
                    WriteToLogFile("ModifyFromWeekendMode OP_SELLLIMIT ticket:" + (string)weekendticket + " " + weekendsymbol + " reset expiration failed errorcode:" + (string)GetLastError());
                }
            }
        }
        RefreshRates();
    }
    if(someonefailed && (!testmode))
    {
        if(SendMail("FAILED FROM WEEKEND",failedstring))
        {
            WriteToLogFile("FAILED FROM WEEKEND send success");
        }else
        {
            WriteToLogFile("FAILED FROM WEEKEND send failed");
        }
    }
    if(!someonefailed)
    {
        needretry = false;//not try again
    }
}