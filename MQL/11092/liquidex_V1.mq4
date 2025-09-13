//+------------------------------------------------------------------+
//|                                                        "X3MA_V1" |
//|                               Copyright © 2013,TFXKENYA SBGInter |
//|                             info@tfxkenya.com  info@sbginter.com |
//+------------------------------------------------------------------+


#property copyright "Liquidex"
#property link      "info@tfxkenya.com"

#property show_inputs

#include <stdlib.mqh>

extern int RangeFilter = 20;
extern int MagicNumber = 293875;
extern string TradeComment = "Liquidex";
extern int StopLoss = 30;
extern double MinLots = 0.01;
extern double MaxLots = 100000.0;
extern double Risk = 25.0;
extern double MaxSpreadWithCommission = 15.0;
extern int TrailingLimit = 15;
extern int TrailingDistance = 15;
extern int MAPeriod = 3;
extern int MAShift = 0;
extern int MAMethod = MODE_LWMA;
extern double DefaultCommisionPoints = 0;

extern string RangeFilterNote = "H-S 0-0: 250 0-1: 300 1-0: 80 1-1: 250";
extern string MAMethodNote = "SMA: 0 EMA: 1 SMMA: 2 LWMA 3";

int digits = 0;
double point = 0.0;
int lotsDigits;
double minLots;
double maxLots;

double risk;
double maxSpreadWithCommission;
double trailingLimit;
double trailingDistance;
double rangeFilter;

int slippage = 3;
bool gotCommissionPointsFromTrade;
double commissionPoints;
double spreadHistory[30];
int spreadHistoryCount = 0;
double last_history_check;

int init()
{
   ArrayInitialize(spreadHistory, 0);
   
   digits = Digits;
   point = Point;
   Print("Digits: " + digits + " Point: " + DoubleToStr(point, digits));
   
   double serverLotsStep = MarketInfo(Symbol(), MODE_LOTSTEP);
   lotsDigits = MathLog(serverLotsStep) / MathLog(0.1);
   minLots = MathMax(MinLots, MarketInfo(Symbol(), MODE_MINLOT));
   maxLots = MathMin(MaxLots, MarketInfo(Symbol(), MODE_MAXLOT));
   Print("LotsDigits: " + lotsDigits + " MinLots: " + DoubleToStr(minLots, lotsDigits) + " MaxLots: " + DoubleToStr(maxLots, lotsDigits));
   
   risk = Risk / 100;
   maxSpreadWithCommission = NormalizeDouble(MaxSpreadWithCommission * point, digits + 1);
   
   trailingLimit = NormalizeDouble(TrailingLimit * point, digits);
   trailingDistance = NormalizeDouble(TrailingDistance * point, digits);
   rangeFilter = NormalizeDouble(point * RangeFilter, digits);
   
   gotCommissionPointsFromTrade = false;
   commissionPoints = NormalizeDouble(DefaultCommisionPoints * point, digits + 1);



   
   return (0);
}

int start()
{
   int errorCode;
   string errorMessage;
   
   int ticket;
   double openPrice;
   bool modified;
   double orderOpenPrice;
   double orderStopLoss;
   double orderTakeProfit;
   double stopLoss;
   double high = iHigh(NULL, 0, 0);
   double low = iLow(NULL, 0, 0);
   double open = iOpen(NULL, 0, 0);
   //double maLow =  iMA(NULL, 0, MAPeriod, MAShift, MAMethod, PRICE_LOW, 0);
   //double maHigh =  iMA(NULL, 0, MAPeriod, MAShift, MAMethod, PRICE_HIGH, 0);
   double ma = iMA(NULL, 0, MAPeriod, MAShift, MAMethod, PRICE_CLOSE, 0);
   //double maRange = maLow - maHigh;
   if (!gotCommissionPointsFromTrade)
   {
      for (int i = OrdersHistoryTotal() - 1; i >= 0; i--)
      {
         if (!OrderSelect(i, SELECT_BY_POS, MODE_HISTORY))
         {
            continue;
         }
         
         if (OrderProfit() == 0.0)
         {
            continue;
         }
         if (OrderClosePrice() == OrderOpenPrice())
         {
            continue;
         }
         if (OrderSymbol() != Symbol())
         {
            continue;
         }
         
         gotCommissionPointsFromTrade = true;
         double pipRate = MathAbs(OrderProfit() / (OrderClosePrice() - OrderOpenPrice()));
         commissionPoints = (-OrderCommission()) / pipRate;
         Print("CommissionPoints: " + DoubleToStr(commissionPoints, digits));
         
         break;
      }
   }
   double spread = Ask - Bid;
   ArrayCopy(spreadHistory, spreadHistory, 0, 1, 29);
   spreadHistory[29] = spread;
   if (spreadHistoryCount < 30)
   {
      spreadHistoryCount++;
   }
   double spreadHistorySum = 0;
   i = 29;
   for (int count_8 = 0; count_8 < spreadHistoryCount; count_8++)
   {
      spreadHistorySum += spreadHistory[i];
      i--;
   }
   double spreadAverage = spreadHistorySum / spreadHistoryCount;
   double askWithCommission = NormalizeDouble(Ask + commissionPoints, digits);
   double bidWithCommission = NormalizeDouble(Bid - commissionPoints, digits);
   double spreadAverageWithCommission = NormalizeDouble(spreadAverage + commissionPoints, digits + 1);
   
   double range = high - low;
   
   if (range > rangeFilter)
   { 
      if (Bid < ma && open>Bid)
      {
         int direction = +1; // SELL
      }
      else if (Bid > ma && open<Bid)
      {
         direction = -1; // BUY
      }
   }    

   for(i=OrdersHistoryTotal()-1;i>=0;i--)
   {
      if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==True)
      {
         if(OrderSymbol() == Symbol() && OrderMagicNumber() ==MagicNumber && OrderType()<=1)
         {
            last_history_check=GetHLineValue("last_history_check_"+Symbol()+strtf(Period())+"_"+MagicNumber);
            if(OrderCloseTime()<last_history_check) break;    
            else if(OrderCloseTime()>last_history_check)
            {
               int tck;
               double money2 = (AccountBalance() * AccountLeverage()) * risk;
               double lots2 = NormalizeDouble(money2 / MarketInfo(Symbol(), MODE_LOTSIZE), lotsDigits);
               lots2 = MathMax(minLots, lots2);
               lots2 = MathMin(maxLots, lots2);
               if(direction<0 && range > rangeFilter && Bid > ma && open<Bid) //BUYSTOP
               {
                  openPrice = NormalizeDouble(Ask + trailingLimit, digits);
                  stopLoss = NormalizeDouble(openPrice - StopLoss * Point, digits);
                  tck = OrderSend(Symbol(), OP_BUYSTOP, lots2, openPrice, slippage, stopLoss, 0, TradeComment, MagicNumber, 0, Lime);
                  if (tck <= 0)
                  {
                     errorCode = GetLastError();
                     errorMessage = ErrorDescription(errorCode);
         
                     Print("BUY Send Error Code: " + errorCode + " Message: " + errorMessage + " LT: " + DoubleToStr(lots2, lotsDigits) + " OP: " + DoubleToStr(openPrice, digits) + " SL: "  + DoubleToStr(stopLoss, digits) + " Bid: " + DoubleToStr(Bid, digits) + " Ask: " + DoubleToStr(Ask, digits));
                  }
               }
               else if(direction>0 && range > rangeFilter && Bid < ma && open>Bid) //SELLSTOP
               {
                  openPrice = NormalizeDouble(Bid - trailingLimit, digits);
                  stopLoss = NormalizeDouble(openPrice + StopLoss * Point, digits);
                  tck = OrderSend(Symbol(), OP_SELLSTOP, lots2, openPrice, slippage, stopLoss, 0, TradeComment, MagicNumber, 0, Orange);
                  if (tck <= 0)
                  {
                     errorCode = GetLastError();
                     errorMessage = ErrorDescription(errorCode);
         
                     Print("SELL Send Error Code: " + errorCode + " Message: " + errorMessage + " LT: " + DoubleToStr(lots2, lotsDigits) + " OP: " + DoubleToStr(openPrice, digits) + " SL: "  + DoubleToStr(stopLoss, digits) + " Bid: " + DoubleToStr(Bid, digits) + " Ask: " + DoubleToStr(Ask, digits));
                  }
               }
               DrawLine("last_history_check_"+Symbol()+strtf(Period())+"_"+MagicNumber,TimeCurrent());
            }
         }           
      }
   }
    

   
   int openTrades = 0;
   for (i = 0; i < OrdersTotal(); i++)
   {
      if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
      {
         continue;
      }
      
      if (OrderMagicNumber() != MagicNumber)
      {
         continue;
      }
      
      int type = OrderType();
      if (type == OP_BUYLIMIT || type == OP_SELLLIMIT)
      {
         continue;
      }
      
      if (OrderSymbol() != Symbol())
      {
         continue;
      }
      
      openTrades++;
      
      switch (type)
      {
      case OP_BUY:
         if (TrailingDistance >= 0)
         {
            orderStopLoss = NormalizeDouble(OrderStopLoss(), digits);
            
            stopLoss = NormalizeDouble(Bid - trailingDistance, digits);
            
            if (!(orderStopLoss == 0 || stopLoss > orderStopLoss))
            {
               break;
            }
            
            modified = OrderModify(OrderTicket(), OrderOpenPrice(), stopLoss, OrderTakeProfit(), 0, Lime);
            if (!modified)
            {
               errorCode = GetLastError();
               errorMessage = ErrorDescription(errorCode);
            
               Print("BUY Modify Error Code: " + errorCode + " Message: " + errorMessage + " OP: " + DoubleToStr(openPrice, digits) + " SL: "  + DoubleToStr(stopLoss, digits) + " Bid: " + DoubleToStr(Bid, digits) + " Ask: " + DoubleToStr(Ask, digits));
            }
         }
         break;
         
      case OP_SELL:
         if (TrailingDistance >= 0)
         {
            orderStopLoss = NormalizeDouble(OrderStopLoss(), digits);
            
            stopLoss = NormalizeDouble(Ask + trailingDistance, digits);
            
            if (!(orderStopLoss == 0 || stopLoss < orderStopLoss))
            {
               break;
            }
            
            modified = OrderModify(OrderTicket(), OrderOpenPrice(), stopLoss, OrderTakeProfit(), 0, Orange);
            if (!modified)
            {
               errorCode = GetLastError();
               errorMessage = ErrorDescription(errorCode);
            
               Print("SELL Modify Error Code: " + errorCode + " Message: " + errorMessage + " OP: " + DoubleToStr(openPrice, digits) + " SL: "  + DoubleToStr(stopLoss, digits) + " Bid: " + DoubleToStr(Bid, digits) + " Ask: " + DoubleToStr(Ask, digits));
            }
         }
         break;
         
      case OP_BUYSTOP:
         orderOpenPrice = NormalizeDouble(OrderOpenPrice(), digits);
         
         openPrice = NormalizeDouble(Ask + trailingLimit, digits);
         
         if (!(openPrice < orderOpenPrice))
         {
            break;
         }
         
         stopLoss = NormalizeDouble(openPrice - StopLoss * Point, digits);
         modified = OrderModify(OrderTicket(), openPrice, stopLoss, OrderTakeProfit(), 0, Lime);
         if (!modified)
         {
            errorCode = GetLastError();
            errorMessage = ErrorDescription(errorCode);
            
            Print("BUYSTOP Modify Error Code: " + errorCode + " Message: " + errorMessage + " OP: " + DoubleToStr(openPrice, digits) + " SL: "  + DoubleToStr(stopLoss, digits) + " Bid: " + DoubleToStr(Bid, digits) + " Ask: " + DoubleToStr(Ask, digits));
         }
         break;
         
      case OP_SELLSTOP:
         orderOpenPrice = NormalizeDouble(OrderOpenPrice(), digits);
         
         openPrice = NormalizeDouble(Bid - trailingLimit, digits);
         
         if (!(openPrice > orderOpenPrice))
         {
            break;
         }
         
         stopLoss = NormalizeDouble(openPrice + StopLoss * Point, digits);
         modified = OrderModify(OrderTicket(), openPrice, stopLoss, OrderTakeProfit(), 0, Orange);
         if (!modified)
         {
            errorCode = GetLastError();
            errorMessage = ErrorDescription(errorCode);
            
            Print("SELLSTOP Modify Error Code: " + errorCode + " Message: " + errorMessage + " OP: " + DoubleToStr(openPrice, digits) + " SL: "  + DoubleToStr(stopLoss, digits) + " Bid: " + DoubleToStr(Bid, digits) + " Ask: " + DoubleToStr(Ask, digits));
         }
         break;
      }
   }
   

   
   if (openTrades == 0 && direction != 0 && spreadAverageWithCommission <= maxSpreadWithCommission)
   {
      double money = (AccountBalance() * AccountLeverage()) * risk;
      double lots = NormalizeDouble(money / MarketInfo(Symbol(), MODE_LOTSIZE), lotsDigits);
      lots = MathMax(minLots, lots);
      lots = MathMin(maxLots, lots);
       
      if (direction < 0)
      {
         openPrice = Ask; //NormalizeDouble(Ask + trailingLimit, digits);
         stopLoss = NormalizeDouble(openPrice - StopLoss * Point, digits);
         ticket = OrderSend(Symbol(), OP_BUY, lots, openPrice, slippage, stopLoss, 0, TradeComment, MagicNumber, 0, Lime);
         if (ticket <= 0)
         {
            errorCode = GetLastError();
            errorMessage = ErrorDescription(errorCode);
            
            Print("BUY Send Error Code: " + errorCode + " Message: " + errorMessage + " LT: " + DoubleToStr(lots, lotsDigits) + " OP: " + DoubleToStr(openPrice, digits) + " SL: "  + DoubleToStr(stopLoss, digits) + " Bid: " + DoubleToStr(Bid, digits) + " Ask: " + DoubleToStr(Ask, digits));
         }
      }
      else
      {
         openPrice = Bid; //NormalizeDouble(Bid - trailingLimit, digits);
         stopLoss = NormalizeDouble(openPrice + StopLoss * Point, digits);
         ticket = OrderSend(Symbol(), OP_SELL, lots, openPrice, slippage, stopLoss, 0, TradeComment, MagicNumber, 0, Orange);
         if (ticket <= 0)
         {
            errorCode = GetLastError();
            errorMessage = ErrorDescription(errorCode);
            
            Print("SELL Send Error Code: " + errorCode + " Message: " + errorMessage + " LT: " + DoubleToStr(lots, lotsDigits) + " OP: " + DoubleToStr(openPrice, digits) + " SL: "  + DoubleToStr(stopLoss, digits) + " Bid: " + DoubleToStr(Bid, digits) + " Ask: " + DoubleToStr(Ask, digits));
         }
      }
   }


   
   string message = "Copyrighted by www.sbginer.com | www.tfxkenya.com"+"\nContact us : info@sbginter.com"+"\nLicence type: Opensource"+"\nAccount Name : "+AccountName()+"\nAccount Number : "+AccountNumber()+"\nAvgSpread:" + DoubleToStr(spreadAverage, digits) + "\nBalance : " + DoubleToStr(AccountBalance(),2) +" $ "+ "\nEquity : " + DoubleToStr(AccountEquity(),2)+ " $ " +" \nCommission rate:" + DoubleToStr(commissionPoints, digits + 1) + " \nReal avg. spread:" + DoubleToStr(spreadAverageWithCommission, digits + 1);
   if (spreadAverageWithCommission > maxSpreadWithCommission)
   {
      message = message 
         + "\n" 
      + "Robot is OFF :: Real avg. spread is too high for this scalping strategy ( " + DoubleToStr(spreadAverageWithCommission, digits + 1) + " > " + DoubleToStr(maxSpreadWithCommission, digits + 1) + " )";
   }
   Comment(message);
   if (openTrades != 0 || direction != 0)
   {
      PrintLineLine(message);
   }
   
   return(0);
}

void PrintLineLine(string text)
{
   int start;
   int position = -1;
   while (position < StringLen(text))
   {
      start = position + 1;
      position = StringFind(text, "\n", start);
      if (position == -1)
      {
         Print(StringSubstr(text, start));
         return;
      }
      Print(StringSubstr(text, start, position - start));
   }
}

void DrawLine(string sName, double dPrice,color cLineClr=CLR_NONE)
{
    int iWidth=1;
    string sObjName = sName;

    if(ObjectFind(sObjName) == -1){
        // create object 
        ObjectCreate(sObjName,OBJ_HLINE, 0, 0,0);
    }

    ObjectSet(sObjName,OBJPROP_PRICE1,dPrice);
    ObjectSet(sObjName, OBJPROP_COLOR, cLineClr);
    ObjectSet(sObjName, OBJPROP_WIDTH, iWidth);
}

double GetHLineValue(string name)
{

   if (ObjectFind(name) == -1)
      return(-1);
   else
      return(ObjectGet(name,OBJPROP_PRICE1));
}


string strtf(int tf)
{
   switch(tf)
   {
      case PERIOD_M1: return("M1");
      case PERIOD_M5: return("M5");
      case PERIOD_M15: return("M15");
      case PERIOD_M30: return("M30");
      case PERIOD_H1: return("H1");
      case PERIOD_H4: return("H4");
      case PERIOD_D1: return("D1");
      case PERIOD_W1: return("W1");
      case PERIOD_MN1: return("MN1");
      default:return("Unknown timeframe");
   }
}