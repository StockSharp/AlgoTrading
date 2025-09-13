//+------------------------------------------------------------------+
//|                                                      ProjectName |
//|                                      Copyright 2012, CompanyName |
//|                                       http://www.companyname.net |
//+------------------------------------------------------------------+
#property copyright ""
#property link      ""
#include <stdlib.mqh>
#include <WinUser32.mqh>
extern double stoploss=40;
extern double takeprofit=200;
//---
extern string Closefunctions="Closefunctions";
extern double Percentofbalance = 1;     //close all orders if profit reached the x percent of balance or
extern double ProfitAmount = 12;        //close all orders if profit reached the x profit amount in acc currency, for example 12 euros.
extern double Orderprofit = 4;          //close market order if profit reached x profit amount, for example 4 euros   
//---
extern string distance="trailing starts after reaching x pips";
extern double trailingstart=30;
//---
extern string trailstop="Trailing stop pips - distance from current price";
extern double trailingstop=20;
//---
extern string toBreakEven=" breakeven after reaching x pips";
extern double breakevengain=25;
//---
extern string breakevenpips="x pips locked in profit";
extern double breakeven=10;
double PipValue;
//--- for stealth mode
extern string SM="Stealth mode for Stoploss and Takeprofit values";
extern bool STEALTH=false;
double buystoplossprice;
double buytakeprofitprice;
double sellstoplossprice;
double selltakeprofitprice;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int init()
  {
   return(INIT_SUCCEEDED);
  }
//--- Expert start
int start()
  {
   OnTick();
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTick()
  {
   double NDigits=Digits;
   if(NDigits==3 || NDigits==5)
     {PipValue=10;}
   else {PipValue=1;}

   if(STEALTH==false)
     {
      SetSLTP();         //function for setting SL and TP for orders
      Movebreakeven();   //moving to "breakeven" pips after reaching "breakevengain" pips
      TrailingStop();    //trailing stop after reaching trailingstart pips 
      CloseOnProfit();   //close order on actual pair if orderprofit reached x amount of acc currency
      CloseAllOrders();  //close all opened orders/buy and sell/ if profit on account reached x percent of balance, or profit reached the x profit amount in acc currency, for example 12 euros.
     }
   else
     {
      SetSLTPstealth();
      CloseonStealthSLTPbuy();
      CloseonStealthSLTPsell();
      Movebreakevenstealth();
      TrailingStopstealth();
      CloseOnProfit();
      CloseAllOrders();

     }

  }
/* In stealth mode we only change and control the values of :      double buystoplossprice,
//                                                                 double buytakeprofitprice,
//                                                                 double sellstoplossprice,
//                                                                 double selltakeprofitprice */
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SetSLTP()
  {
//---
   for(int i=OrdersTotal()-1; i>=0; i--)
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderType()==OP_BUY && OrderSymbol()==Symbol() && (OrderStopLoss()==0 || OrderTakeProfit()==0))
           {
            bool result1=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-stoploss*PipValue*Point,OrderOpenPrice()+takeprofit*PipValue*Point,0,Blue);
            if(result1==false)Print("Error in OrderModify BUY, setting SL and TP. Error code=",GetLastError());
           }
         //---
         if(OrderType()==OP_SELL && OrderSymbol()==Symbol() && (OrderStopLoss()==0 || OrderTakeProfit()==0))
           {
            bool result2=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+stoploss*PipValue*Point,OrderOpenPrice()-takeprofit*PipValue*Point,0,Red);
            if(result2==false)Print("Error in OrderModify SELL, setting SL and TP. Error code=",GetLastError());
           }
         //---
         if((OrderType()==OP_BUYLIMIT || OrderType()==OP_BUYSTOP) && OrderSymbol()==Symbol() && (OrderStopLoss()==0 || OrderTakeProfit()==0))
           {
            bool result3=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-stoploss*PipValue*Point,OrderOpenPrice()+takeprofit*PipValue*Point,0,Blue);
            if(result3==false)Print("Error in OrderModify BUYSTOP OR BUYLIMIT, setting SL and TP. Error code=",GetLastError());
           }
         //---
         if((OrderType()==OP_SELLLIMIT || OrderType()==OP_SELLSTOP) && OrderSymbol()==Symbol() && (OrderStopLoss()==0 || OrderTakeProfit()==0))
           {
            bool result4=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+stoploss*PipValue*Point,OrderOpenPrice()-takeprofit*PipValue*Point,0,Red);
            if(result4==false)Print("Error in OrderModify SELLSTOP OR SELLLIMIT,setting SL and TP. Error code=",GetLastError());
           }
        }
   else
     {
      Print("OrderSelect() error - ",ErrorDescription(GetLastError()));
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void Movebreakeven()
  {
   RefreshRates();
//---
   if(OrdersTotal()>0)
     {
      for(int i=OrdersTotal();i>=0;i--)
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
           {
            if(OrderType()<=OP_SELL && OrderSymbol()==Symbol())
              {
               if(OrderType()==OP_BUY)
                 {
                  if(Bid-OrderOpenPrice()>=breakevengain*PipValue*Point)
                    {
                     if((OrderStopLoss()-OrderOpenPrice()<breakeven*PipValue*Point) || OrderStopLoss()==0)
                       {
                        bool result5=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+breakeven*PipValue*Point,OrderTakeProfit(),0,Blue);
                        if(result5==false)Print("Error in OrderModify BUY ORDER BREAKEVEN. Error code=",GetLastError());
                       }
                    }
                 }
               else
                 {
                  if(OrderOpenPrice()-Ask>=breakevengain*PipValue*Point)
                    {
                     if((OrderOpenPrice()-OrderStopLoss()<breakeven*PipValue*Point) || OrderStopLoss()==0)
                       {
                        bool result6=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-breakeven*PipValue*Point,OrderTakeProfit(),0,Red);
                        if(result6==false)Print("Error in OrderModify SELL ORDER BREAKEVEN. Error code=",GetLastError());
                       }

                    }
                 }
              }
           }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void TrailingStop()
  {
//---
   RefreshRates();
//---
   if(OrdersTotal()>0)
     {
      for(int i=OrdersTotal();i>=0;i--)
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
           {
            if(OrderType()<=OP_SELL && OrderSymbol()==Symbol())
              {
               if(OrderType()==OP_BUY)
                 {
                  if(Ask>(OrderOpenPrice()+trailingstart*PipValue*Point) && OrderStopLoss()<Bid-(trailingstop+0.001)*PipValue*Point)
                    {
                     bool result7=OrderModify(OrderTicket(),OrderOpenPrice(),Bid-trailingstop*PipValue*Point,OrderTakeProfit(),0,Blue);

                     if(result7==false)Print("Error in OrderModify BUY TRAILINGSTOP. Error code=",GetLastError());
                    }
                 }
               else
                 {
                  if((Bid<OrderOpenPrice()-trailingstart*PipValue*Point && OrderStopLoss()>Ask+(trailingstop+0.001)*PipValue*Point) || (OrderStopLoss()==0))
                    {
                     bool result8=OrderModify(OrderTicket(),OrderOpenPrice(),Ask+trailingstop*PipValue*Point,OrderTakeProfit(),0,Red);
                     if(result8==false)Print("Error in OrderModify SELL TRAILINGSTOP. Error code=",GetLastError());
                    }

                 }
              }
           }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CloseOnProfit()
  {
   int orderstotal=OrdersTotal();
   int orders=0;
   int ordticket[30][2];
   for(int i=0; i<orderstotal; i++)
      //---
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if((OrderType()!=OP_SELL || OrderType()!=OP_BUY) && OrderSymbol()!=Symbol())
           {
            continue;
           }
         if(OrderProfit()>Orderprofit)
           {
            ordticket[orders][0] = OrderOpenTime();
            ordticket[orders][1] = OrderTicket();
            orders++;
           }
        }
   if(orders>1)
     {
      ArrayResize(ordticket,orders);
      ArraySort(ordticket);
     }
   for(i=0; i<orders; i++)
     {
      if(OrderSelect(ordticket[i][1],SELECT_BY_TICKET)==true)
        {
         bool ret=OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),4,Red);
         if(ret==false)
            Print("OrderClose() error - ",ErrorDescription(GetLastError()));
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int CloseAllOrders()
  {
//---
   double profit=AccountInfoDouble(ACCOUNT_PROFIT);
   double profitpercent=NormalizeDouble(Percentofbalance*(AccountBalance()/100),2);
   int ticket;
   if(profit>ProfitAmount || profit>profitpercent)
     {
      //---
      if(OrdersTotal()==0) return(0);
      for(int i=OrdersTotal()-1; i>=0; i--)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
           {
            Print("order ticket: ",OrderTicket(),"order magic: ",OrderMagicNumber());
            if(OrderType()==0)
              {
               ticket=OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_BID),3,Red);
               if(ticket==-1) Print("Error on closing BUY position.Code :",GetLastError());
               if(ticket>0) Print("Position ",OrderTicket()," closed. ");
              }
            if(OrderType()==1)
              {
               ticket=OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_ASK),3,Red);
               if(ticket==-1) Print("Error on closing SELL position.Code : ",GetLastError());
               if(ticket>0) Print("Position ",OrderTicket()," closed. ");
              }
           }
        }
     }

   return(0);
  }
//+------------------------------------------------------------------+
//| stealth mode                                                     |
//+------------------------------------------------------------------+
void SetSLTPstealth()
  {
//---
   for(int i=OrdersTotal()-1; i>=0; i--)
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderType()==OP_BUY && OrderSymbol()==Symbol() && (OrderStopLoss()==0 || OrderTakeProfit()==0))
           {
            //---
            buystoplossprice=(OrderOpenPrice()-stoploss*PipValue*Point);
            buytakeprofitprice=OrderOpenPrice()+takeprofit*PipValue*Point;
           }
         if(buystoplossprice<OrderOpenPrice() && buytakeprofitprice>OrderOpenPrice())Print("BUY,  SL:    ",buystoplossprice,"   and TP:    ",buytakeprofitprice);
         //---
         if(OrderType()==OP_SELL && OrderSymbol()==Symbol() && (OrderStopLoss()==0 || OrderTakeProfit()==0))
           {
            //---
            sellstoplossprice=OrderOpenPrice()+stoploss*PipValue*Point;
            selltakeprofitprice=OrderOpenPrice()-takeprofit*PipValue*Point;
           }
         if(sellstoplossprice>OrderOpenPrice() && selltakeprofitprice<OrderOpenPrice())Print("SELL,  SL:    ",sellstoplossprice,"   and TP:   ",selltakeprofitprice);
         //---
         if((OrderType()==OP_BUYLIMIT || OrderType()==OP_BUYSTOP) && OrderSymbol()==Symbol() && (OrderStopLoss()==0 || OrderTakeProfit()==0))
           {
            //bool result3 = OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-stoploss*PipValue*Point,OrderOpenPrice()+takeprofit*PipValue*Point,0,Blue);
            //if(result3 == false)Print("Error in OrderModify BUYSTOP OR BUYLIMIT, setting SL and TP. Error code=",GetLastError());          
           }
         //---
         if((OrderType()==OP_SELLLIMIT || OrderType()==OP_SELLSTOP) && OrderSymbol()==Symbol() && (OrderStopLoss()==0 || OrderTakeProfit()==0))
           {
            //bool result4 = OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+stoploss*PipValue*Point,OrderOpenPrice()-takeprofit*PipValue*Point,0,Red); 
            //if(result4 == false)Print("Error in OrderModify SELLSTOP OR SELLLIMIT,setting SL and TP. Error code=",GetLastError());         
           }
        }
   else
     {
      //Print("OrderSelect() error - ", ErrorDescription(GetLastError()));
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CloseonStealthSLTPbuy() //closing buy at SL or TP
  {
   int orderstotal=OrdersTotal();
   for(int i=0; i<orderstotal; i++)
      //---
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderType()==OP_BUY && OrderSymbol()==Symbol())
           {
            if((stoploss>0 && (MarketInfo(OrderSymbol(),MODE_BID)<=buystoplossprice)) || (takeprofit>0 && (MarketInfo(OrderSymbol(),MODE_ASK)>=buytakeprofitprice)))
              {
               bool ret=OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),4,Red);
               if(ret==false)Print("OrderClose() error - ",ErrorDescription(GetLastError()));
              }
           }
        }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CloseonStealthSLTPsell() // closing sell at SL or TP 
  {
   int orderstotal=OrdersTotal();
//---
   for(int i=0; i<orderstotal; i++)
      //---
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderType()==OP_SELL && OrderSymbol()==Symbol())
           {
            if((stoploss>0 && (MarketInfo(OrderSymbol(),MODE_ASK)>=sellstoplossprice)) || (takeprofit>0 && (MarketInfo(OrderSymbol(),MODE_BID)<=selltakeprofitprice)))
              {
               bool ret=OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),4,Red);
               if(ret==false)Print("OrderClose() error - ",ErrorDescription(GetLastError()));
              }
           }
        }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void Movebreakevenstealth() // moving SL to BE in stealth mode
  {
   RefreshRates();
   if(OrdersTotal()>0)
     {
      for(int i=OrdersTotal();i>=0;i--)
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
           {
            if(OrderType()<=OP_SELL && OrderSymbol()==Symbol())
              {
               if(OrderType()==OP_BUY)
                 {
                  if(Bid-OrderOpenPrice()>=breakevengain*PipValue*Point)
                    {
                     if((buystoplossprice-OrderOpenPrice()<breakeven*PipValue*Point) || buystoplossprice==0)
                       {
                        buystoplossprice=OrderOpenPrice()+breakeven*PipValue*Point;
                        if(buystoplossprice>OrderOpenPrice() && buytakeprofitprice>OrderOpenPrice())Print("BUY, setting SL to breakeven :   ",buystoplossprice);
                       }
                    }
                 }
               else
                 {
                  if(OrderOpenPrice()-Ask>=breakevengain*PipValue*Point)
                    {
                     if((OrderOpenPrice()-sellstoplossprice<breakeven*PipValue*Point) || sellstoplossprice==0)
                       {
                        sellstoplossprice=OrderOpenPrice()-breakeven*PipValue*Point;
                        if(sellstoplossprice<OrderOpenPrice() && selltakeprofitprice<OrderOpenPrice())Print("SELL, setting SL to breakeven:    ",sellstoplossprice);
                       }

                    }
                 }
              }
           }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void TrailingStopstealth() // trailing stop in stealth mode
  {
   RefreshRates();
   if(OrdersTotal()>0)
     {
      for(int i=OrdersTotal();i>=0;i--)
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
           {
            if(OrderType()<=OP_SELL && OrderSymbol()==Symbol())
              {
               if(OrderType()==OP_BUY)
                 {
                  if(Ask>(OrderOpenPrice()+trailingstart*PipValue*Point) && buystoplossprice<Bid-(trailingstop+0.001)*PipValue*Point)
                    {
                     buystoplossprice=Bid-trailingstop*PipValue*Point;
                     Print("BUY, stoploss trailed to :  ",buystoplossprice);
                    }
                 }
               else
                 {
                  if((Bid<OrderOpenPrice()-trailingstart*PipValue*Point && sellstoplossprice>Ask+(trailingstop+0.001)*PipValue*Point) || (sellstoplossprice==0))
                    {
                     sellstoplossprice=Ask+trailingstop*PipValue*Point;
                     Print("SELL, stoploss trailed to :  ",sellstoplossprice);
                    }
                 }
              }
           }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
  }
//+------------------------------------------------------------------+
