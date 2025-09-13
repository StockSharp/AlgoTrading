//+------------------------------------------------------------------+
//|                                               Simulator_Test.mq4 |
//|                                         Developed by Coders Guru |
//|                                           Copyrighted for xpworx |
//|                                            http://www.xpworx.com |
//+------------------------------------------------------------------+
#property copyright "xpworx"
#property link      "http://www.xpworx.com"
string   ver  = "Last Modified: 2014.02.16";
//+------------------------------------------------------------------+
extern 	int 			TakeProfit 				= 50;
extern 	int 			StopLoss 				= 50;
extern 	double 		Lots 						= 0.1;
extern 	int      	MagicNumber          = 12345;
//+------------------------------------------------------------------+
double   mPoint               = 0.0001;
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
// Simulator   v2.0     2014-02-16 
//+------------------------------------------------------------------+
#define  sTicket           0
#define  sSymbol           1
#define  sType             2
#define  sLots             3
#define  sOpenPrice        4
#define  sStopLoss         5
#define  sTakeProfit       6
#define  sMagicNumber      7
#define  sState            8
//++-----------------------------++
#define  State_Empty       1
#define  State_Instant     2
#define  State_Closed      3
#define  State_Deleted     4
#define  State_TakeProfit  5
#define  State_StopLoss    6
#define  State_Modified    7
//++-----------------------------++
#define  ROWS         100
#define  COLS         9
string   Trades[COLS,ROWS];
//++-----------------------------++
void Add(string iTicket, string iSymbol, string iType, string iLots,
string iOpenPrice, string iStopLoss, string iTakeProfit, string iMagicNumber, string iState)
{
   //FIFO Shift
   for (int cnt = ROWS-1 ; cnt >0  ; cnt--)
   {
      for(int c = 0 ; c < COLS ; c++) Trades[c][cnt] = Trades[c][cnt-1];
   }
   
   if(StrToDouble(iTakeProfit)>0)
   {
      if(StrToDouble(iTakeProfit)-MathFloor(StrToDouble(iTakeProfit))>0)  //Value or Price??
      {
         Trades[sTakeProfit][0] = iTakeProfit;
      }
      else 
      {
         if(StrToInteger(iType)==OP_BUY)
         Trades[sTakeProfit][0] = StrToDouble(iOpenPrice) + StrToDouble(iTakeProfit)*mPoint; 
         if(StrToInteger(iType)==OP_SELL)
         Trades[sTakeProfit][0] = StrToDouble(iOpenPrice) - StrToDouble(iTakeProfit)*mPoint; 
      }
   }
   else
   {
      Trades[sTakeProfit][0] = 0;
   }
   
   
   if(StrToDouble(iStopLoss)>0)
   {
      if(StrToDouble(iStopLoss)-MathFloor(StrToDouble(iStopLoss))>0)  //Value or Price??
      {
         Trades[sStopLoss][0] = iStopLoss;
      }
      else 
      {
         if(StrToInteger(iType)==OP_BUY)
         Trades[sStopLoss][0] = StrToDouble(iOpenPrice) - StrToDouble(iStopLoss)*mPoint; 
         if(StrToInteger(iType)==OP_SELL)
         Trades[sStopLoss][0] = StrToDouble(iOpenPrice) + StrToDouble(iStopLoss)*mPoint; 
      }
   }
   else
   {
      Trades[sStopLoss][0] = 0;
   }
   
   Trades[sTicket][0]=iTicket; 
   Trades[sSymbol][0]=iSymbol; 
   Trades[sType][0]=iType;
   Trades[sLots][0]=iLots; 
   Trades[sOpenPrice][0]=iOpenPrice; 
   Trades[sMagicNumber][0]=iMagicNumber; 
   Trades[sState][0]=iState; 
}
//++-----------------------------++
void Modify(string iTicket, string iSymbol, string iType, string iLots,
string iOpenPrice, string iStopLoss, string iTakeProfit, string iMagicNumber, string iState)
{
   for (int cnt = 0 ; cnt < ROWS ; cnt++)
   {
      if(Trades[sTicket][cnt]==iTicket)
      {
         if(iSymbol!="")Trades[sSymbol][cnt]=iSymbol; 
         if(iType!="")Trades[sType][cnt]=iType; 
         if(iLots!="")Trades[sLots][cnt]=iLots; 
         if(iOpenPrice!="")Trades[sOpenPrice][cnt]=iOpenPrice; 
         if(iStopLoss!="")Trades[sStopLoss][cnt]=iStopLoss; 
         if(iTakeProfit!="")Trades[sTakeProfit][cnt]=iTakeProfit; 
         if(iMagicNumber!="")Trades[sMagicNumber][cnt]=iMagicNumber; 
         if(iState!="")Trades[sState][cnt]=iState; 
      }
   }
}
//++-----------------------------++
void Load(int magic = 0)
{
   for (int a=0; a<COLS; a++)
   {
      for(int b=0; b<ROWS; b++) Trades[a,b] = EMPTY_VALUE;  
   }
   
   if(magic>0)
   {
      for(int cnt=0; cnt< OrdersTotal(); cnt++)
      {
         OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
         if(OrderMagicNumber()==magic)
         {
            Add(OrderTicket(),Symbol(),OrderType(),OrderLots(),OrderOpenPrice(),OrderStopLoss(),OrderTakeProfit(),OrderMagicNumber(),State_Instant);
         }
      }
   }          
}
//++-----------------------------++
void PrintTrades(int option=0)
{
   string com = "";
   string type = "";
   for(int cnt = 0 ; cnt < ROWS; cnt++)
   {
      if(Trades[sTicket,cnt]=="2147483647") break;
      
      if (StrToInteger(Trades[sType,cnt])==OP_BUY) type = "Buy";
      if (StrToInteger(Trades[sType,cnt])==OP_SELL) type = "Sell";
      
      com = com + "Index = " + cnt + "     Ticket = " + Trades[sTicket,cnt] +  " Type = " + type 
      + "  OpenPrice = " + Trades[sOpenPrice,cnt]+ "  StopLoss = " + Trades[sStopLoss,cnt] + "  TakeProfit = " + Trades[sTakeProfit,cnt] + "\n";
      
   }
   
   if(option==0) Print(com);
   if(option==1) Comment(com);
}
//++-----------------------------++
bool OrderFind(string ticket)
{
   for(int cnt=0; cnt<ROWS; cnt++) 
   {
   	if(Trades[sTicket,cnt] == ticket)
   	{
   		return(true);
   		break;
   	}
   } 
   return(false);
}
//++-----------------------------++
string OrderGet(string iTicket,int index)
{   
   for(int cnt=0 ; cnt<ROWS; cnt++)
   {
      if(Trades[sTicket][cnt]==iTicket)
      {
         return(Trades[index][cnt]); 
      }
   }
   return("-1");
}
//++-----------------------------++
bool Filter(int tick)
{
	if(OrderSelect(tick,SELECT_BY_TICKET,MODE_TRADES))
	{
		//Write your filters here
		if(OrderMagicNumber()==MagicNumber)
		return(true); 
	}
	return(false);	
}
//++-----------------------------++
string AutoAdd()
{
	for(int cnt=0; cnt<OrdersTotal(); cnt++)
	{
		if(OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES))
		{
			if(!Filter(OrderTicket())) continue;
			if(!OrderFind(OrderTicket())) 
			{
				Add(OrderTicket(),OrderSymbol(),OrderType(),OrderLots(),OrderOpenPrice(),
				OrderStopLoss(),OrderTakeProfit(),OrderMagicNumber(),State_Instant);
				return(OrderTicket());
			}
		}
	}
	return("-1");
}
//++-----------------------------++
int Simulate()
{
   //Add automatically
   int NewTick = AutoAdd();
   if(NewTick!="-1") 
   {
   	//Raise open event
   	OnOpen(Trades[sTicket,0]); 
   	return(0);
   }
   
   //Check for changes
   for(int cnt = 0; cnt < ROWS; cnt++)
   {
      if(Trades[sTicket,cnt]=="2147483647") continue;
      
      if(StrToInteger(Trades[sType,cnt])==OP_BUY && StrToInteger(Trades[sState,cnt])==State_Instant)
      {
         if(Bid>=StrToDouble(Trades[sTakeProfit,cnt]) && StrToDouble(Trades[sTakeProfit,cnt])>0)
         {
            Modify(Trades[sTicket,cnt],"","","","","","","",State_TakeProfit);
            OnTakeProfit(Trades[sTicket,cnt]); 
            return(0); break;
         }
         if(Bid<=StrToDouble(Trades[sStopLoss,cnt]) && StrToDouble(Trades[sStopLoss,cnt])>0)
         {
            Modify(Trades[sTicket,cnt],"","","","","","","",State_StopLoss);
            OnStopLoss(Trades[sTicket,cnt]);
            return(0); break;
         }
      }
      
      if(StrToInteger(Trades[sType,cnt])==OP_SELL && StrToInteger(Trades[sState,cnt])==State_Instant)
      {
         if(Ask<=StrToDouble(Trades[sTakeProfit,cnt]) && StrToDouble(Trades[sTakeProfit,cnt])>0)
         {
            Modify(Trades[sTicket,cnt],"","","","","","","",State_TakeProfit);
            OnTakeProfit(Trades[sTicket,cnt]); 
            return(0); break;
         }
         if(Ask>=StrToDouble(Trades[sStopLoss,cnt]) && StrToDouble(Trades[sStopLoss,cnt])>0)
         {
            Modify(Trades[sTicket,cnt],"","","","","","","",State_StopLoss);
            OnStopLoss(Trades[sTicket,cnt]);
            return(0); break;
         }
      }
      
      //Modified
      if(OrderExist(StrToInteger(Trades[sTicket,cnt])))
      {
      	if(OrderSelect(StrToInteger(Trades[sTicket,cnt]),SELECT_BY_TICKET,MODE_TRADES))
      	{
      		if(OrderStopLoss()!=StrToDouble(Trades[sStopLoss,cnt]))
      		{
      			Modify(Trades[sTicket,cnt],"","","",OrderStopLoss(),"","","",State_Modified);
            	OnModify(Trades[sTicket,cnt]);
      		}
      		if(OrderTakeProfit()!=StrToDouble(Trades[sTakeProfit,cnt]))
      		{
      			Modify(Trades[sTicket,cnt],"","","","",OrderTakeProfit(),"","",State_Modified);
            	OnModify(Trades[sTicket,cnt]);
      		}
      	}
      }
      
      if(OrderExist(StrToInteger(Trades[sTicket,cnt]))==false && StrToInteger(Trades[sState,cnt])==State_Instant)
      {
         Modify(Trades[sTicket,cnt],"","","","","","","",State_Closed);
         OnClose(Trades[sTicket,cnt]);
         return(0); break;
      }
   }
   return(0);
}
//++-----------------------------++
bool OrderExist(int tick)
{
   for(int cnt=0; cnt<OrdersTotal();cnt++) 
   {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(tick==OrderTicket() && tick>0)
      { 
         return(true);
         break;
      }
   } 
   return(false);
}
//++-----------------------------++
void OnOpen(string tick)
{
   //place your OnOpen code here
   Alert("Order #",tick, " OPENED");
}
//++-----------------------------++
void OnStopLoss(string tick)
{
   //place your OnStopLoss code here
   Alert("Order #",tick, " closed by StopLoss");
}
//+-----------------------------
void OnTakeProfit(string tick)
{
   //place your OnTakeProfit code here
   Alert("Order #",tick, " closed by TakeProfit");
}
//++-----------------------------++
void OnClose(string tick)
{
   //place your OnTakeProfit code here
   Alert("Order #",tick, " CLOSED");
}
//++-----------------------------++
void OnModify(string tick)
{
   //place your OnModify code here
   Alert("Order #",tick, " MODIFIED");
}
//++-----------------------------++

//+------------------------------------------------------------------+
int init()
{
   mPoint = GetPoint(); 
   Load();
   return(0);
}
//+------------------------------------------------------------------+
int deinit() 
{
   return(0);
}
//+------------------------------------------------------------------+


//+------------------------------------------------------------------+
int start()
{
   
   Simulate();
	//+------
	
   int Signal = CheckSignal();
   //+------
   
   for(int cnt=0;cnt<OrdersTotal();cnt++) 
   {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderMagicNumber()!=MagicNumber) continue;
      if(OrderType()==OP_BUY && Signal==OP_SELL) CloseOrder(OrderTicket(),OrderLots());
      if(OrderType()==OP_SELL && Signal==OP_BUY) CloseOrder(OrderTicket(),OrderLots());
   }
   //+------
   
   int tick;
   if(!TradeExist(MagicNumber)) 
   {
      if(Signal==OP_BUY)
      {
         tick = OpenOrder(true, Symbol(), OP_BUY, Lots, 5, StopLoss, TakeProfit, MagicNumber, WindowExpertName());
         if(tick>-1)
         {
            //Here you can add manually
            //OrderSelect(tick,SELECT_BY_TICKET,MODE_TRADES);
            //Add(tick,Symbol(),OrderType(),OrderLots(),OrderOpenPrice(),OrderStopLoss(),OrderTakeProfit(),MagicNumber,State_Instant);
         }
      }
      if(Signal==OP_SELL) 
      {
         tick = OpenOrder(true, Symbol(), OP_SELL, Lots, 5, StopLoss, TakeProfit, MagicNumber, WindowExpertName());
         if(tick>-1)
         {
            //Here you can add manually
            //OrderSelect(tick,SELECT_BY_TICKET,MODE_TRADES);
            //Add(tick,Symbol(),OrderType(),OrderLots(),OrderOpenPrice(),OrderStopLoss(),OrderTakeProfit(),MagicNumber,State_Instant);
         }
      }
   } 
     
   //+------
   Simulate();
   PrintTrades(1);
   
   return(0);
}
//+------------------------------------------------------------------+
int CheckSignal()
{
   double FMAC = iMA(NULL,0,13,0,MODE_EMA,PRICE_CLOSE,0);
   double FMAP = iMA(NULL,0,13,0,MODE_EMA,PRICE_CLOSE,1);
   double SMAC = iMA(NULL,0,50,0,MODE_EMA,PRICE_CLOSE,0);
   double SMAP = iMA(NULL,0,50,0,MODE_EMA,PRICE_CLOSE,1);
   
   if(FMAC>SMAC && FMAP<SMAP) return(OP_BUY);
   if(FMAC<SMAC && FMAP>SMAP) return(OP_SELL);
   return(-1);
}
//+------------------------------------------------------------------+
bool TradeExist(int magic)
{
   int total  = OrdersTotal();
   for(int cnt = 0 ; cnt < total ; cnt++)
   {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if (OrderMagicNumber()== magic)
      return (true);
    }
    return (false);
}
//+------------------------------------------------------------------+
double GetPoint(string symbol = "")
{
   if(symbol=="") symbol=Symbol();
   if(StringFind(symbol,"XAU")>-1 || StringFind(symbol,"xau")>-1) return(0.1);  //Gold
   if(MarketInfo(symbol,MODE_DIGITS)==2 || MarketInfo(symbol,MODE_DIGITS)==3) return(0.01);
   if(MarketInfo(symbol,MODE_DIGITS)==4 || MarketInfo(symbol,MODE_DIGITS)==5) return(0.0001);
   if(MarketInfo(symbol,MODE_DIGITS)==0) return(1); //Indexes
   return(0.0001);
}
//+------------------------------------------------------------------+
bool CloseOrder(int ticket, double lot = 0)
{
   bool result;
   int tries = 5;
   int pause = 500;
   double ask = NormalizeDouble(Ask,Digits);
   double bid = NormalizeDouble(Bid,Digits);
   if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
   {
      if(OrderType()==OP_BUY)
      {
         for(int c = 0 ; c <= 5 ; c++)
         {
            if(lot==0) result = OrderClose(OrderTicket(),OrderLots(),bid,5,Violet);
            else result = OrderClose(OrderTicket(),lot,bid,5,Violet);
            if(result==true) break; 
            else
            {
               Sleep(pause);
               continue;
            }
         }
      }
      if(OrderType()==OP_SELL)
      {
         for(c = 0 ; c <= 5 ; c++)
         {
            if(lot==0) result = OrderClose(OrderTicket(),OrderLots(),ask,5,Violet);
            else result = OrderClose(OrderTicket(),lot,ask,5,Violet);
            if(result==true) break; 
            else
            {
               Sleep(pause);
               continue;
            }
         }
      }
      if(OrderType()>OP_SELL)
      {
         for(c = 0 ; c <= 5 ; c++)
         {
            result = OrderDelete(OrderTicket(),Violet);
            if(result==true) break; 
            else
            {
               Sleep(pause);
               continue;
            }
         }
      }
   }
   return(result);
}
//+------------------------------------------------------------------+
int OpenOrder(bool STP, string TradeSymbol, int TradeType, double TradeLot, int TradeSlippage, double TradeStopLoss, 
double TradeTakeProfit, int TradeMagicNumber, string TradeComment = "", int TriesCount = 5, int Pause = 500)
{
   int ticket=0, cnt=0;

   bool DobuleLimits;
   string limit = "";
   if(TradeStopLoss!=0) limit = DoubleToStr(TradeStopLoss,Digits);
   else limit = DoubleToStr(TradeTakeProfit,Digits);
   int point_pos = StringFind(limit,".",0);
   string num_after = StringSubstr(limit,point_pos+1,StringLen(limit)-point_pos);
   double fraction = StrToDouble(num_after);
   if(fraction>0) DobuleLimits=true;
   else DobuleLimits=false;
   
   //Print(DobuleLimits,":",after,":",d);
   
   if(STP)
   {
      if(TradeType == OP_BUY)
      {
         for(cnt = 0 ; cnt < TriesCount ; cnt++)
         {
            if(TradeStopLoss==0 && TradeTakeProfit==0)
            {
               ticket=OrderSend(TradeSymbol,OP_BUY,TradeLot,Ask,TradeSlippage,0,0,TradeComment,TradeMagicNumber,0,Green);
            }
            else
            {
               if(DobuleLimits)
               {
                  if(TradeStopLoss>0 && TradeTakeProfit>0)
                  {
                     ticket=OrderSend(TradeSymbol,OP_BUY,TradeLot,Ask,TradeSlippage,0,0,TradeComment,TradeMagicNumber,0,Green);
                     if(ticket>-1) 
                     {
                        OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES);
                        OrderModify(ticket,OrderOpenPrice(),TradeStopLoss,TradeTakeProfit,0,Green);
                     }
                  }
                  if(TradeStopLoss==0 && TradeTakeProfit>0)
                  {
                     ticket=OrderSend(TradeSymbol,OP_BUY,TradeLot,Ask,TradeSlippage,0,0,TradeComment,TradeMagicNumber,0,Green);
                     if(ticket>-1) 
                     {
                        OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES);
                        OrderModify(ticket,OrderOpenPrice(),0,TradeTakeProfit,0,Green);
                     }
                  }
                  if(TradeStopLoss>0 && TradeTakeProfit==0)
                  {
                     ticket=OrderSend(TradeSymbol,OP_BUY,TradeLot,Ask,TradeSlippage,0,0,TradeComment,TradeMagicNumber,0,Green);
                     if(ticket>-1) 
                     {
                        OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES);
                        OrderModify(ticket,OrderOpenPrice(),TradeStopLoss,0,0,Green);
                     }
                  }
               }
               else
               {
                  if(TradeStopLoss>0 && TradeTakeProfit>0)
                  {
                     ticket=OrderSend(TradeSymbol,OP_BUY,TradeLot,Ask,TradeSlippage,0,0,TradeComment,TradeMagicNumber,0,Green);
                     if(ticket>-1) 
                     {
                        OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES);
                        OrderModify(ticket,OrderOpenPrice(),Ask-TradeStopLoss*mPoint,Ask+TradeTakeProfit*mPoint,0,Green);
                     }
                  }
                  if(TradeStopLoss==0 && TradeTakeProfit>0)
                  {
                     ticket=OrderSend(TradeSymbol,OP_BUY,TradeLot,Ask,TradeSlippage,0,0,TradeComment,TradeMagicNumber,0,Green);
                     if(ticket>-1) 
                     {
                        OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES);
                        OrderModify(ticket,OrderOpenPrice(),0,Ask+TradeTakeProfit*mPoint,0,Green);
                     }
                  }
                  if(TradeStopLoss>0 && TradeTakeProfit==0)
                  {
                     ticket=OrderSend(TradeSymbol,OP_BUY,TradeLot,Ask,TradeSlippage,0,0,TradeComment,TradeMagicNumber,0,Green);
                     if(ticket>-1) 
                     {
                        OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES);
                        OrderModify(ticket,OrderOpenPrice(),Ask-TradeStopLoss*mPoint,0,0,Green);
                     }
                  }
               }
               if(ticket==-1)
               { 
                  Sleep(Pause);
                  continue;
               }
               else
               {
                  break;
               }
            }
         }
      }
   
      if(TradeType == OP_SELL)
      {
         for(cnt = 0 ; cnt < TriesCount ; cnt++)
         {
            if(TradeStopLoss==0 && TradeTakeProfit==0)
            {
               ticket=OrderSend(TradeSymbol,OP_SELL,TradeLot,Bid,TradeSlippage,0,0,TradeComment,TradeMagicNumber,0,Red);
            }
            else
            {
               if(DobuleLimits)
               {
                  if(TradeStopLoss>0 && TradeTakeProfit>0)
                  {
                     ticket=OrderSend(TradeSymbol,OP_SELL,TradeLot,Bid,TradeSlippage,0,0,TradeComment,TradeMagicNumber,0,Red);
                     if(ticket>-1) 
                     {
                        OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES);
                        OrderModify(ticket,OrderOpenPrice(),TradeStopLoss,TradeTakeProfit,0,Red);
                     }
                  }
                  if(TradeStopLoss==0 && TradeTakeProfit>0)
                  {
                     ticket=OrderSend(TradeSymbol,OP_SELL,TradeLot,Bid,TradeSlippage,0,0,TradeComment,TradeMagicNumber,0,Red);
                     if(ticket>-1) 
                     {
                        OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES);
                        OrderModify(ticket,OrderOpenPrice(),0,TradeTakeProfit,0,Red);
                     }
                  }
                  if(TradeStopLoss>0 && TradeTakeProfit==0)
                  {
                     ticket=OrderSend(TradeSymbol,OP_SELL,TradeLot,Bid,TradeSlippage,0,0,TradeComment,TradeMagicNumber,0,Red);
                     if(ticket>-1) 
                     {
                        OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES);
                        OrderModify(ticket,OrderOpenPrice(),TradeStopLoss,0,0,Red);
                     }
                  }
               }
               else
               {
                  if(TradeStopLoss>0 && TradeTakeProfit>0)
                  {
                     ticket=OrderSend(TradeSymbol,OP_SELL,TradeLot,Bid,TradeSlippage,0,0,TradeComment,TradeMagicNumber,0,Red);
                     if(ticket>-1) 
                     {
                        OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES);
                        OrderModify(ticket,OrderOpenPrice(),Bid+TradeStopLoss*mPoint,Bid-TradeTakeProfit*mPoint,0,Red);
                     }
                  }
                  if(TradeStopLoss==0 && TradeTakeProfit>0)
                  {
                     ticket=OrderSend(TradeSymbol,OP_SELL,TradeLot,Bid,TradeSlippage,0,0,TradeComment,TradeMagicNumber,0,Red);
                     if(ticket>-1) 
                     {
                        OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES);
                        OrderModify(ticket,OrderOpenPrice(),0,Bid-TradeTakeProfit*mPoint,0,Red);
                     }
                  }
                  if(TradeStopLoss>0 && TradeTakeProfit==0)
                  {
                     ticket=OrderSend(TradeSymbol,OP_BUY,TradeLot,Bid,TradeSlippage,0,0,TradeComment,TradeMagicNumber,0,Red);
                     if(ticket>-1) 
                     {
                        OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES);
                        OrderModify(ticket,OrderOpenPrice(),Bid+TradeStopLoss*mPoint,0,0,Red);
                     }
                  }
               }
               if(ticket==-1)
               { 
                  Sleep(Pause);
                  continue;
               }
               else
               {
                  break;
               }
            }
         }
      }
   }
   else
   {
      if(TradeType == OP_BUY)
      {
         for(cnt = 0 ; cnt < TriesCount ; cnt++)
         {
            if(TradeStopLoss==0 && TradeTakeProfit==0)
            {
               ticket=OrderSend(TradeSymbol,OP_BUY,TradeLot,Ask,TradeSlippage,0,0,TradeComment,TradeMagicNumber,0,Green);
            }
            else
            {
               if(DobuleLimits)
               {
                  if(TradeStopLoss>0 && TradeTakeProfit>0)
                  {
                     ticket=OrderSend(TradeSymbol,OP_BUY,TradeLot,Ask,TradeSlippage,TradeStopLoss,TradeTakeProfit,TradeComment,TradeMagicNumber,0,Green);
                  }
                  if(TradeStopLoss==0 && TradeTakeProfit>0)
                  {
                     ticket=OrderSend(TradeSymbol,OP_BUY,TradeLot,Ask,TradeSlippage,0,TradeTakeProfit,TradeComment,TradeMagicNumber,0,Green);
                  }
                  if(TradeStopLoss>0 && TradeTakeProfit==0)
                  {
                     ticket=OrderSend(TradeSymbol,OP_BUY,TradeLot,Ask,TradeSlippage,TradeStopLoss,0,TradeComment,TradeMagicNumber,0,Green);
                  }
               }
               else
               {
                  if(TradeStopLoss>0 && TradeTakeProfit>0)
                  {
                     ticket=OrderSend(TradeSymbol,OP_BUY,TradeLot,Ask,TradeSlippage,Ask-TradeStopLoss*mPoint,Ask+TradeTakeProfit*mPoint,TradeComment,TradeMagicNumber,0,Green);
                  }
                  if(TradeStopLoss==0 && TradeTakeProfit>0)
                  {
                     ticket=OrderSend(TradeSymbol,OP_BUY,TradeLot,Ask,TradeSlippage,0,Ask+TradeTakeProfit*mPoint,TradeComment,TradeMagicNumber,0,Green);
                  }
                  if(TradeStopLoss>0 && TradeTakeProfit==0)
                  {
                     ticket=OrderSend(TradeSymbol,OP_BUY,TradeLot,Ask,TradeSlippage,Ask-TradeStopLoss*mPoint,0,TradeComment,TradeMagicNumber,0,Green);
                  }
               }
               if(ticket==-1)
               { 
                  Sleep(Pause);
                  continue;
               }
               else
               {
                  break;
               }
            }
         }
      }
      
      if(TradeType == OP_SELL)
      {
         for(cnt = 0 ; cnt < TriesCount ; cnt++)
         {
            if(TradeStopLoss==0 && TradeTakeProfit==0)
            {
               ticket=OrderSend(TradeSymbol,OP_SELL,TradeLot,Bid,TradeSlippage,0,0,TradeComment,TradeMagicNumber,0,Red);
            }
            else
            {
               if(DobuleLimits)
               {
                  if(TradeStopLoss>0 && TradeTakeProfit>0)
                  {
                     ticket=OrderSend(TradeSymbol,OP_SELL,TradeLot,Bid,TradeSlippage,TradeStopLoss,TradeTakeProfit,TradeComment,TradeMagicNumber,0,Red);
                  }
                  if(TradeStopLoss==0 && TradeTakeProfit>0)
                  {
                     ticket=OrderSend(TradeSymbol,OP_SELL,TradeLot,Bid,TradeSlippage,0,TradeTakeProfit,TradeComment,TradeMagicNumber,0,Red);
                  }
                  if(TradeStopLoss>0 && TradeTakeProfit==0)
                  {
                     ticket=OrderSend(TradeSymbol,OP_SELL,TradeLot,Bid,TradeSlippage,TradeStopLoss,0,TradeComment,TradeMagicNumber,0,Red);
                  }
               }
               else
               {
                  if(TradeStopLoss>0 && TradeTakeProfit>0)
                  {
                     ticket=OrderSend(TradeSymbol,OP_SELL,TradeLot,Bid,TradeSlippage,Bid+TradeStopLoss*mPoint,Bid-TradeTakeProfit*mPoint,TradeComment,TradeMagicNumber,0,Red);
                  }
                  if(TradeStopLoss==0 && TradeTakeProfit>0)
                  {
                     ticket=OrderSend(TradeSymbol,OP_SELL,TradeLot,Bid,TradeSlippage,0,Bid-TradeTakeProfit*mPoint,TradeComment,TradeMagicNumber,0,Red);
                  }
                  if(TradeStopLoss>0 && TradeTakeProfit==0)
                  {
                     ticket=OrderSend(TradeSymbol,OP_SELL,TradeLot,Bid,TradeSlippage,Bid+TradeStopLoss*mPoint,0,TradeComment,TradeMagicNumber,0,Red);
                  }
               }
               if(ticket==-1)
               { 
                  Sleep(Pause);
                  continue;
               }
               else
               {
                  break;
               }
            }
         }
      }
   }  
   return(ticket);
}
//+------------------------------------------------------------------+
