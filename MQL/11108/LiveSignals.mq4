/**
  Variables
**/  

double glbOrderProfit;
double glbOrderOpen;
double glbOrderStop;
double glbOrderType;
double glbOrderTicket;
double mOpen, mClose, mStopLoss, mTakeProfit; 
double ask, bid, point;

int mNumber, spread, file;
string mSymbol, mType, opendate, closedate;

datetime date1, date2;  

double Lots = 5;

int init()
{
return(0);
}

int deinit()
{
return(0);
}

int start()
{

   file = FileOpen("signals.csv",FILE_READ|FILE_CSV,',');
   while (!FileIsEnding(file))
     {
  
       mNumber = StrToInteger(FileReadString(file));
       opendate = FileReadString(file);       
       closedate = FileReadString(file);
       mOpen = StrToDouble(FileReadString(file));    
       mClose = StrToDouble(FileReadString(file));       
       mTakeProfit = StrToDouble(FileReadString(file));
       mStopLoss = StrToDouble(FileReadString(file));
       mType = FileReadString(file);   
       mSymbol = FileReadString(file);   

       date1=StrToTime(opendate);
       date2=StrToTime(closedate);

       ask     = MarketInfo(mSymbol,MODE_ASK);
       bid     = MarketInfo(mSymbol,MODE_BID);   
       spread  = MarketInfo(mSymbol,MODE_SPREAD);   
       point   = MarketInfo(mSymbol,MODE_POINT);

       if (TimeCurrent() == date1) 
       {
         if(mType == "Sell" && OrderFind(mNumber, mSymbol) == false)
         {   
           OrderSend(mSymbol, OP_SELL, Lots, bid, 3, mStopLoss, mTakeProfit, "Sell Order", mNumber, 0, Blue);
         }
         if(mType == "Buy" && OrderFind(mNumber, mSymbol) == false)
         {
           OrderSend(mSymbol, OP_BUY, Lots, ask, 3, mStopLoss, mTakeProfit, "Buy Order", mNumber, 0, Blue);
         }
       }
       
       else if (TimeCurrent() >= date2 && OrderFind(mNumber, mSymbol) == true) 
       {
           CloseOrder(glbOrderTicket);
       }
       
       else 
       {
         continue;
       }  
                
       Print("Magic=", mNumber, ",Symbol=", mSymbol, ",Type=", mType, ",Open=", mOpen, ",Close=", mClose, ",SL=", mStopLoss, ",TakeProfit=", mTakeProfit);   
         
       
      }
   
   FileClose(file);  
   return(0);
}
 

/**
  Close Order Function
**/  

void CloseOrder( int Ticket )
{

  OrderSelect(Ticket, SELECT_BY_TICKET);

  if (OrderType() == OP_BUY) {
    OrderClose(Ticket, OrderLots(), Bid, 0);     
  }  

  else {
    OrderClose(Ticket, OrderLots(), Ask, 0);      
  }  

 return;  

}   
 
/**
  Order Find Function
**/  

bool OrderFind(int Magic, string symbol) 
{

   glbOrderType = -1;
   glbOrderTicket = -1;
   glbOrderProfit = 0;
   glbOrderOpen = -1;
   glbOrderStop = -1;

   int total = OrdersTotal();

   bool res = false;

   for(int cnt = 0 ; cnt < total ; cnt++)

     {

       OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);

       if(OrderMagicNumber() == Magic && OrderSymbol() == symbol)

         {

           glbOrderType = OrderType();
           glbOrderTicket = OrderTicket();
           glbOrderProfit = OrderProfit();
           glbOrderOpen = OrderOpenPrice();
           glbOrderStop = OrderStopLoss();

           res = true;

         }

     }

 return(res);

}
 

/**
  Scan History
**/  

bool InHistory(int Magic) 
{
  int i,hstTotal=OrdersHistoryTotal();
  for(i=0;i<hstTotal;i++)
    {
     
     if(OrderMagicNumber() == Magic) {
        return(TRUE);
        break;
     }
     
    }
}
 