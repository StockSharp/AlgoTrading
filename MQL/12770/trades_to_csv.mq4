//+------------------------------------------------------------------+
//|                                                Trades_to_CSV.mq4 |
//|                                                                  |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "Inovance Inc."
#property link      "https://www.inovancetech.com"
#property description "Export Trades to CSV"
#property version   "1.00"
#property strict
//---
input double   TakeProfit=50.0;
input double   StopLoss=50.0;
input double   Lots=0.1;
input int      CCIPeriod=14;
input int      MACDFastPeriod = 12;
input int      MACDSlowPeriod = 26;
input int      MACDSignalPeriod=9;
input string   FileName="myfilename.csv";
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   int handle=FileOpen(FileName,FILE_CSV|FILE_READ|FILE_WRITE,",");
//Create output file
   FileWrite(handle,"Order","Profit/Loss","Ticket Number","Open Price","Close Price","Open Time","Close Time","Symbol","Lots");
//Create headers
   FileClose(handle);
//Close file
   Print("File Header Written Correctly");
//Print success message
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
  }
//+------------------------------------------------------------------+
//| Example strategy logic                                            |
//+------------------------------------------------------------------+
int Signal()
  {
   int mode;
//Example Indicators
   double CCI=iCCI(NULL,0,CCIPeriod,PRICE_OPEN,0);
   double MACDhist=iOsMA(NULL,0,MACDFastPeriod,MACDSlowPeriod,MACDSignalPeriod,PRICE_OPEN,0);
//Example Entry Conditions       
   if(CCI>-125 && CCI<-42 && MACDhist>-0.00114 && MACDhist<0.00038)
      //Exampl Long Rule
     {
      mode=1;
      //Long opportunity
     }
   else if(CCI>125 && CCI<208 && MACDhist>-0.00038 && MACDhist<0.00190)
//Short Rule
     {
      mode=2;
      //Short opportunity
     }
   else
     {
      mode=0;
      //Nothing
     }
   return(mode);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   int cnt,ticket,slippage,total;
//Check to make sure there enough bars in chart to calculate indicators        
   if(Bars<300)
     {
      Print("Not enough bars in chart");
      return(0);
     }
//Account for 4 and 5 digit brokers  
   if(Digits==5 || Digits==3)
     {
      slippage=5*10;
     }
   else
     {
      slippage=5;
     }
   total=OrdersTotal();
//Make sure no existing orders open
   if(total<1)
     {
      if(Signal()==1)
         //Long Opportunity
        {
         ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,slippage,0,0,"My EA",12345,0,Green);
         //Buy order sent
         if(ticket>0)
           {
            Print("Buy:",OrderOpenPrice());
           }
         return(0);
        }
      if(Signal()==2)
         //Short Opportunity
        {
         ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,slippage,0,0,"My EA",12345,0,Red);
         //Sell order sent
         if(ticket>0)
           {
            Print("Sell:",OrderOpenPrice());
           }

         return(0);
        }
      return(0);
     }
   for(cnt=0;cnt<total;cnt++)
     {
      if(OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES)==TRUE)
         //Check for open orders
         if(OrderType()==OP_BUY)
            //If Buy Order open
           {
            if(Signal()==2 || (OrderProfit()>TakeProfit) || (OrderProfit()<-StopLoss))
               //And Short Criteria Met, take profit hit, or stop loss hit
              {
               if(OrderClose(OrderTicket(),OrderLots(),Bid,slippage,Violet)>0)
                  //Close Buy Order
                 {
                  if(OrderSelect(OrderTicket(),SELECT_BY_TICKET,MODE_HISTORY)==TRUE)
                     //Select closed order
                    {
                     int handle=FileOpen(FileName,FILE_CSV|FILE_READ|FILE_WRITE,",");
                     //Open file  
                     FileSeek(handle,0,SEEK_END);
                     //Go to last line                       
                     FileWrite(handle,"Buy Order Closed",OrderProfit(),OrderTicket(),OrderOpenPrice(),OrderClosePrice(),OrderOpenTime(),OrderCloseTime(),OrderSymbol(),OrderLots());
                     //Write Buy Order close info
                     Print("Buy order Closed, Data Written : ",OrderProfit());
                     FileClose(handle);
                     //Close file
                     Print("Buy Close FileClosed");
                    }
                 }
               return(0);
              }
           }
      else
      //If Sell Order open
        {
         if(Signal()==1 || (OrderProfit()>TakeProfit) || (OrderProfit()<-StopLoss))
            //And Long Criteria Met, take profit hit, or stop loss hit
           {
            if(OrderClose(OrderTicket(),OrderLots(),Ask,slippage,Violet)>0)
               //Close Sell Order
              {
               if(OrderSelect(OrderTicket(),SELECT_BY_TICKET,MODE_HISTORY)==TRUE)
                  //Select closed order
                 {
                  int handle=FileOpen(FileName,FILE_CSV|FILE_READ|FILE_WRITE,",");
                  //Open file  
                  FileSeek(handle,0,SEEK_END);
                  //Go to last line                       
                  FileWrite(handle,"Sell Order Closed",OrderProfit(),OrderTicket(),OrderOpenPrice(),OrderClosePrice(),OrderOpenTime(),OrderCloseTime(),OrderSymbol(),OrderLots());
                  //Write Buy Order close info
                  Print("Sell order Closed, Data Written : ",OrderProfit());
                  FileClose(handle);
                  //Close file
                  Print("Sell Close FileClosed");
                 }
              }
            return(0);
           }
        }
     }
   return(0);
  }
//+------------------------------------------------------------------+
