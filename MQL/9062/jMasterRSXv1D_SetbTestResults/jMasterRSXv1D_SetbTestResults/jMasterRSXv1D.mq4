//+------------------------------------------------------------------------------------------------------+
//|                                                                                     jMasterRSXv1.mq4 |
//|                                                                                   Jiri Balcar © 2009 |
//|                                                                                    jirimac@yahoo.com |
//|                                                             modifications © 2009 by Richard Ingraham |
//|                                                                              ringraham44@charter.net |
//|                                                                                        GBPUSD/EURUSD |
//| ***NOTE: While these modifications were designed using GBPUSD                                        |
//|          the original trading logic was used on EURUSD with                                          | 
//|          success. The modifications use almost the same trading logic (very slight mods)             | 
//|          with enhanced functionalty for various brokers It will                                      | 
//|          now work with 4 or 5 digit (fractional pip broker). The modifications                       | 
//|          also now have enhanced trade size and money mangement                                       | 
//|          features and trailing/stoploss/+takeproit options.                                          | 
//|                                                                                                      | 
//|          While the simplicity of the original coding was not able                                    | 
//|          to be maintained with the added features, I believe you                                     | 
//|          will see why they were needed.                                                              | 
//|                                                                                                      | 
//|          I recommend and have included the ability to turn off all of the added options during       | 
//|          optimization. Just optimize the time period parameters and indicator parameters using       | 
//|          the money management feature set real low (1 or 2%). Then try using the features to refine  | 
//|          your results further. This EA is a real BARNSTORMER for profits if you have patience.       | 
//|          In testing many EAs, I've noticed that the market has had a very fundamental change around  | 
//|          Jan/Mar of 09. This EA functions real well with the 'new' market conditions, although I've  | 
//|          tested it on prior years and got it working well then, too. I've not yet been able to get it| 
//|          to cross over that time period and maintain good functionality. You can achieve good results| 
//|          using this EA with settings that generate lots of trades, however the drawdown is quite high| 
//|          and it seems to work best with fewer trades (8-15 per month) on GBPUSD, but is very accurate|
//|          at that rate. I use it on 1H GBPUSD chart.                                                  | 
//|          My backtesting is complete and forward testing is commencing but will (obviously) take time | 
//|          to generate meaningful results. Please post yours as well.                                  | 
//|                                                                                                      | 
//|          You may use the Open Price Only feature in the tester as it only uses the open prices of    | 
//|          each bar, which really helps the optimization time. You can easily exceed a profit factor   | 
//|          of 3 with this EA with less than 40% drawdown as long as your willing to accept few trades. | 
//|                                                                                                      | 
//|  IMPORTANT NOTE!!!                                                                                   | 
//|          In Optimizing this EA it is VERY important to turn off the Trail/Takeprofit features and    | 
//|          ONLY use a Pip Stoploss setting if desired to suit your comfort level. Otherwise the test   | 
//|          results will reflect the natural support and resistance levels in the PAST market and not   | 
//|          give a true reading of the indicators effectiveness for FUTURE results in the long term.    | 
//|                                                                                                      | 
//|          Also please note the if you use any of the BAR features for Stop/TakeProfit/Trail the EA    | 
//|          defaults to the PIP setting if the BAR selected is too close to the price being used for the| 
//|          feature (I used a 10 pip buffer). So when using these features set the Pip levels to 0 if   | 
//|          you don't want that to effect the bar price selection.  I use them in combo together AFTER  | 
//|          getting the best indicator settings.                                                        | 
//|                                                                                                      | 
//|          Best Regards and wishes for success!!                   Richard Ingraham                    | 
//+------------------------------------------------------------------------------------------------------+
extern string TRADE_COMM         = "RSXv1_D";
extern int    MAGICMA            = 072609;// Multiple EAs can run on different charts simultaneously by changing this
                                          //   for each one
extern double Lots               = 0.1;   // Lots traded if MM is not enabled
extern double DecreaseFactor     = 0;     // will decrease trades in proportion to the success rate of the EA
extern int    DF_Engage          = 1;     // Number of consecutive losses before Decrease Factor is engaged
extern bool   UseMoneyManagement = true;  //
extern double MaximumRisk        = 0.01;  // Will trade multiple times if the maximum lot size is exceeded by the 
extern double Max_Lot_Size       = 50;    //   % calculations up to the max_num_lots amount. This parameter is broker
extern double Max_Num_Lots       = 500;   //   specific and must adhere to their policies. if unsure of maximums, ask
                                          //   broker, some restrict the maximum number of lots traded per instrument.
                                          //   The tester does NOT adhere to those policies and will differ from live
                                          //   trading results if these parameters are incorrect!!                           
                                          //   If unsure of Maximum Lot Size leave this parameter @ 0.                      
extern int LongPer = 15;                  // Utilizes RSX indicator data from these TWO timeframes 
extern int ShortPer = 5;                  //
extern int LongRSX = 10;                  // Indicator timeframe parameters for the above time periods
extern int ShortRSX = 4;                  //

extern int LongParBuy = 25;               // Trading logic: Will Buy if RSX (Long Time Period) is above the LongParBuy parameter and                  
extern int ShortParBuy = 25;              //                if the RSX (Short Time Period) is below the ShortParBuy parameter

extern int LongParSell = 75;              //                Will Sell if RSX (Long Time Period) is bleow the LongParSell parameter and                  
extern int ShortParSell = 75;             //                if the RSX (Short Time Period) is above the ShortParSell parameter

extern int LongTrendSpread = 4;           // How many bars of the LongPer Trend before trading allowed MIN 2

                                          // Linear Regression Channel is calculated on 4HR time period, then if the slope is greater than
                                          // the LinRegTrade parameter trading will be allowed. Open trades will still be
                                          // exited at the specified indicator regardless of this setting. This ONLY controls
                                          // the opening of new orders.                                                
extern int LinRegLen = 5;                 // Length of Linear Regression Channel for Trade Control
extern double LinRegTrade = 40;           // Slope of Linear Regression Channel to allow trading (in pips)
extern bool TradeTrend = true;           // Trading if Linear Regression Slope GREATER than LinRegTrade parameter ALWAYS SET AS TRUE
//extern bool TradeRange = false;          // Trading if Linear Regression Slope LESS than LinRegTrade parameter (NOT USED)

extern string s2 = "__________________Take Profit Parameters";
extern string s2a = "..................Mode 0 = Profit by Pips Mode";
extern string s2b = "..................Mode 1 = Profit by Highest/Lowest of Range of Bars";
extern string s2c = "..................Mode 2 = Profit by High/Low of Bar x";

extern bool UseTakeProfit = false;
extern int TakeProfitMode = 0;

extern string s3 = "__________________Stop Loss Parameters";
extern string s31 = "..................Mode 0 = Stop by Pips Mode";
extern string s32 = "..................Mode 1 = Stop by Highest/Lowest of Range of Bars";
extern string s33 = "..................Mode 2 = Stop by High/Low of Bar x";

extern bool UseStopLoss = false;
extern int StopMode = 1;

extern string s4 = "__________________Trailing Stop Parameters";
extern string s41 = "..................Mode 0 = Trail by Pips Mode";
extern string s42 = "..................Mode 1 = Trail by Highest/Lowest of Range of Bars";
extern string s43 = "..................Mode 2 = Trail by High/Low of Bar x";

extern bool UseTrail = false;
extern int TrailMode = 1;

extern string s5 = "__________________Buy Order Parameters";
extern int TakeProfitPip_Buy = 150;
extern int TakeProfitBar_Buy = 0;

extern int StopPip_Buy = 0;
extern int StopBar_Buy = 7;

extern int TrailPip_Buy = 0;
extern int TrailBar_Buy = 7;
extern int TrailMin_Buy = 0;

extern string s6 = "__________________Sell Order Parameters";
extern int TakeProfitPip_Sell = 150;
extern int TakeProfitBar_Sell = 0;

extern int StopPip_Sell = 0;
extern int StopBar_Sell = 7;

extern int TrailPip_Sell = 0;
extern int TrailBar_Sell = 7;
extern int TrailMin_Sell = 0;

bool cBuy, cSell, cExitBuy, cExitSell, Buy, Sell;
int lastgotime, gotime,digits;
double point;

double LR_line[100],Sup_line[100], Res_line[100];

//+------------------------------------------------------------------+
//| Calculate open orders                                            |
//+------------------------------------------------------------------+
int OpenOrders(string symbol)
{
  int co=0;
  int total  = OrdersTotal();

  for (int i=total-1; i >=0; i--)
  {
    OrderSelect(i,SELECT_BY_POS,MODE_TRADES);     
    if (OrderMagicNumber()==MAGICMA && OrderSymbol()==Symbol() && (OrderType()==OP_BUY || OrderType()==OP_SELL))
    {
      co++;
    }
  }
  return(co);
} 
//+------------------------------------------------------------------+
//| Calculate optimal lot size                                       |
//+------------------------------------------------------------------+
double LotsOptimized()
  {
   double lot=Lots, MarginReq, LotVel, MINLOT, MAXLOT;
   int    orders=HistoryTotal();     // history orders total
   int    losses=0;                  // number of losses orders without a break
//---- select lot size
   if(UseMoneyManagement)
      {
         MarginReq = MarketInfo(Symbol(), MODE_MARGINREQUIRED);
         if (MarginReq == 0.0)
                       return(0);
              
         LotVel = GetFreeMargin()
                  * MaximumRisk / MarginReq;  
         lot=NormalizeDouble(LotVel,1);
      }
      
   //---- calcuulate number of losses orders without a break
   if(DecreaseFactor>0)
     {
      for(int i=orders-1;i>=0;i--)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==false) { Print("Error in history!"); break; }
         if(OrderSymbol()!=Symbol() || OrderType()>OP_SELL) continue;
         //----
         if(OrderProfit()>0) break;
         if(OrderProfit()<0) losses++;
        }
      if(losses>DF_Engage) lot=NormalizeDouble(lot-(lot*losses/DecreaseFactor),1);
     }
   MINLOT = MarketInfo(Symbol(), MODE_MINLOT);
   if (MINLOT < 0) return(0);
   if (lot < MINLOT) lot = MINLOT;
          
   if (lot > Max_Num_Lots) lot = Max_Num_Lots;

   //---- return lot size
   return(lot);
  }
//+==================================================================+
//| GetFreeMargin() function                                         |
//+==================================================================+
double GetFreeMargin()
  {
//----+
   switch(AccountFreeMarginMode())
       {
        
        case 0: return(AccountFreeMargin() + AccountProfit());
        case 1: return(AccountFreeMargin());
        case 2: if (AccountProfit() > 0) 
                  return(AccountFreeMargin());
                else
                  return(AccountFreeMargin() - AccountProfit());
        case 3: if (AccountProfit() < 0) 
                  return(AccountFreeMargin());
                else
                  return(AccountFreeMargin() - AccountProfit());
       }
//----+
  } 
//+------------------------------------------------------------------+
//| Check for open order conditions                                  |
//+------------------------------------------------------------------+
void CheckForOpen(bool Buying, bool Selling)
   {
   
      int hstTotal=HistoryTotal(),BarNum,SLPips;
      double CurBid,CurAsk, StopLevel, TakeProfitLevel,SLevel,TPLevel,TradeLots;
      Buy=false;
      Sell=false;
            
      if(Max_Lot_Size<=0) Max_Lot_Size = MarketInfo(Symbol(),MODE_MAXLOT);
      StopLevel = 0;
      TakeProfitLevel = 0;
      if(UseStopLoss)
         {
            if(StopMode == 0)
               {
                  if(Buying) StopLevel = NormalizeDouble(Ask-(StopPip_Buy*point), digits);
                  if(Selling) StopLevel = NormalizeDouble(Bid+(StopPip_Sell*point), digits);
               }
            if(StopMode == 1)
               {
                  if(Buying)
                     {
                        SLevel = iLowest(Symbol(),LongPer,MODE_LOW,StopBar_Buy,0);
                        StopLevel = iLow(Symbol(),LongPer,SLevel);
                        if(StopLevel>Ask - (10*point) && StopPip_Buy > 0) StopLevel = NormalizeDouble(Ask-(StopPip_Buy*point), digits);
                        else StopLevel = 0;
                     }
                  if(Selling)
                     {
                        SLevel = iHighest(Symbol(),LongPer,MODE_HIGH,StopBar_Sell,0);
                        StopLevel = iHigh(Symbol(),LongPer,SLevel);
                        if(StopLevel<Bid + (10*point) && StopPip_Sell > 0) StopLevel = NormalizeDouble(Bid +(StopPip_Sell*point), digits);
                        else StopLevel = 0;
                     }
               }
            if(StopMode == 2)
               {
                  if(Buying)
                     {
                        StopLevel = iLow(Symbol(),LongPer,StopBar_Buy);
                        if(StopLevel>Ask - (10*point) && StopPip_Buy > 0) StopLevel = NormalizeDouble(Ask-(StopPip_Buy*point), digits);
                        else StopLevel = 0;
                     }
                  if(Selling)
                     {
                        StopLevel = iHigh(Symbol(),LongPer,StopBar_Sell);
                        if(StopLevel<Bid + (10*point) && StopPip_Sell > 0) StopLevel = NormalizeDouble(Bid +(StopPip_Sell*point), digits);
                        else StopLevel = 0;
                     }
               }
         }
      if(UseTakeProfit)
         {
            if(TakeProfitMode == 0)
               {
                  if(Buying) TakeProfitLevel = NormalizeDouble(Ask+(TakeProfitPip_Buy*point), digits);
                  if(Selling) TakeProfitLevel = NormalizeDouble(Bid-(TakeProfitPip_Sell*point), digits);
               }
            if(TakeProfitMode == 1)
               {
                  if(Buying)
                     {
                        SLevel = iLowest(Symbol(),LongPer,MODE_LOW,TakeProfitBar_Buy,0);
                        TakeProfitLevel = iLow(Symbol(),LongPer,SLevel);
                        if(TakeProfitLevel<Ask + (10*point) && TakeProfitPip_Buy > 0) TakeProfitLevel = NormalizeDouble(Ask+(TakeProfitPip_Buy*point), digits);
                        else TakeProfitLevel = 0;
                     }
                  if(Selling)
                     {
                        SLevel = iHighest(Symbol(),LongPer,MODE_HIGH,TakeProfitBar_Sell,0);
                        TakeProfitLevel = iHigh(Symbol(),LongPer,SLevel);
                        if(TakeProfitLevel>Bid - (10*point) && TakeProfitPip_Sell > 0) TakeProfitLevel = NormalizeDouble(Bid -(TakeProfitPip_Sell*point), digits);
                        else TakeProfitLevel = 0;
                     }
               }
            if(TakeProfitMode == 2)
               {
                  if(Buying)
                     {
                        TakeProfitLevel = iLow(Symbol(),LongPer,TakeProfitBar_Buy);
                        if(TakeProfitLevel<Ask + (10*point) && TakeProfitPip_Buy > 0) TakeProfitLevel = NormalizeDouble(Ask+(TakeProfitPip_Buy*point), digits);
                        else TakeProfitLevel = 0;
                     }
                  if(Selling)
                     {
                        TakeProfitLevel = iHigh(Symbol(),LongPer,TakeProfitBar_Sell);
                        if(TakeProfitLevel>Bid - (10*point) && TakeProfitPip_Sell > 0) TakeProfitLevel = NormalizeDouble(Bid -(TakeProfitPip_Sell*point), digits);
                        else TakeProfitLevel = 0;
                     }
               }
         }
         
      double    res=0;
      TradeLots = LotsOptimized();
      while(TradeLots>Max_Lot_Size)
         {
             if(!Buying && !Selling) break;
          //---- buy conditions
             if(Buying)
               {
                  res=OrderSend(Symbol(),OP_BUY,Max_Lot_Size,Ask,(5*point),StopLevel,TakeProfitLevel,TRADE_COMM,MAGICMA,0,Blue);
                  if(res < 0) {break;}
                  TradeLots = TradeLots - Max_Lot_Size;
               }
          //---- sell conditions
             else if(Selling)  
               {
                  res=OrderSend(Symbol(),OP_SELL,Max_Lot_Size,Bid,(5*point),StopLevel,TakeProfitLevel,TRADE_COMM,MAGICMA,0,Red);
                  if(res < 0) {break;}
                  TradeLots = TradeLots - Max_Lot_Size;
               }
         }
      if(TradeLots>Max_Lot_Size)
         {
            if(Buying)
               {
                  Print("Error Opening Buy Orders....ceasing. Error #",GetLastError(),"  Lots left untraded = ",TradeLots);
                  return;
               }
            if(Selling)
               {
                  Print("Error Opening Sell Orders....ceasing. Error #",GetLastError(),"  Lots left untraded = ",TradeLots);
                  return;
               }
         }
      if(Buying)
         {
            res=OrderSend(Symbol(),OP_BUY,TradeLots,Ask,(8*point),StopLevel,TakeProfitLevel,TRADE_COMM,MAGICMA,0,Blue);
            if(res > 0) return; else Print("Error Opening Buy Order...ceasing");
            return;
         }
      else if(Selling)  
         {
            res=OrderSend(Symbol(),OP_SELL,TradeLots,Bid,(8*point),StopLevel,TakeProfitLevel,TRADE_COMM,MAGICMA,0,Red);
            if(res > 0) return; else Print("Error Opening Sell Order...ceasing");
            return;
         }
   //----
      return;
   }
  
//+------------------------------------------------------------------+
//| Check for close order conditions                                 |
//+------------------------------------------------------------------+

void CheckForClose()
  {
//----
   for(int i=OrdersTotal()-1;i>=0;i--)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false)        break;
      if(OrderMagicNumber()!=MAGICMA || OrderSymbol()!=Symbol()) continue;
      //---- check order type 
      if(OrderType()==OP_BUY)
        {
         if(cExitBuy==true) OrderClose(OrderTicket(),OrderLots(),Bid,50,White);
        }
      if(OrderType()==OP_SELL)
        {
         if(cExitSell==true) OrderClose(OrderTicket(),OrderLots(),Ask,50,White);
        }
     }
}

//+------------------------------------------------------------------+
//| MoveTrailingStop() Function                                      |
//+------------------------------------------------------------------+
void MoveTrailingStop()
{
   int cnt;
   bool moved = false;
   double Trailer_buy, Trailer_sell, TrailBuffer=10*point, TrailMinB, TrailMinS;
   int TBar_Buy, TBar_Sell, total;

   if(TrailMode==0)
      {
         if(TrailPip_Buy>0) { Trailer_buy = NormalizeDouble(Ask-(TrailPip_Buy * point),digits); } else Trailer_buy=0;

         if(TrailPip_Sell>0){ Trailer_sell = NormalizeDouble(Bid + (TrailPip_Sell * point),digits); } else Trailer_sell=0;

         total=OrdersTotal();
         for(cnt=0;cnt<total;cnt++)
         {
            OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
            if(OrderType() <= OP_SELL  &&  OrderSymbol()==Symbol() )
            {
               if( OrderMagicNumber() == MAGICMA)
                  {
                     if( OrderType()==OP_BUY  &&  NormalizeDouble(OrderStopLoss(),digits) != Trailer_buy && Trailer_buy > 0)
                        {
                           if(Trailer_buy<Ask-TrailBuffer && Ask>OrderOpenPrice() && Trailer_buy > OrderStopLoss())
                              {
                                 moved = OrderModify(OrderTicket(),OrderOpenPrice(),Trailer_buy,OrderTakeProfit(),0,Blue);
                                 if(!moved) Print("Error on Modify - Buy");
                              }
                        }
                     if(OrderType()==OP_SELL  &&  NormalizeDouble(OrderStopLoss(),digits) != Trailer_sell && Trailer_sell > 0)
                        {
                           if(Trailer_sell>Bid+TrailBuffer && Bid<OrderOpenPrice() && (Trailer_sell < OrderStopLoss() || OrderStopLoss()==0))
                              {
                                 moved = OrderModify(OrderTicket(),OrderOpenPrice(),Trailer_sell,OrderTakeProfit(),0,Red);
                                 if(!moved) Print("Error on Modify - Sell");
                              }
                        }
                  }
            }// Ordertype() buy or sell and Same Symbol()
         } // for Orders =
         
      }
   if(TrailMode==1)
      {
         if(TrailBar_Buy>0) { TBar_Buy=iLowest(Symbol(),0,MODE_LOW,TrailBar_Buy,0); Trailer_buy = NormalizeDouble(Low[TBar_Buy],digits); } else Trailer_buy=0;

         if(TrailBar_Sell>0){ TBar_Sell=iHighest(Symbol(),0,MODE_HIGH,TrailBar_Sell,0); Trailer_sell = NormalizeDouble(High[TBar_Sell],digits); } else Trailer_sell=0;


         if(TrailMin_Buy>0)TrailMinB = NormalizeDouble(TrailMin_Buy*point,digits); else TrailMinB=0;
         if(TrailMin_Sell>0)TrailMinS = NormalizeDouble(TrailMin_Sell*point,digits); else TrailMinS=0;

         total=OrdersTotal();
         for(cnt=0;cnt<total;cnt++)
         {
            OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
            if(OrderType() <= OP_SELL  &&  OrderSymbol()==Symbol() )
            {
               if( OrderMagicNumber() == MAGICMA)
                  {
                     if( OrderType()==OP_BUY  &&  NormalizeDouble(OrderStopLoss(),digits) != Trailer_buy && Trailer_buy > 0)
                        {
                           if(Trailer_buy<Ask-TrailMinB-TrailBuffer && Ask>OrderOpenPrice() && Trailer_buy > OrderStopLoss())
                              {
                                 moved = OrderModify(OrderTicket(),OrderOpenPrice(),Trailer_buy,OrderTakeProfit(),0,Blue);
                                 if(!moved) Print("Error on Modify - Buy");
                              }
                        }
                     if(OrderType()==OP_SELL  &&  NormalizeDouble(OrderStopLoss(),digits) != Trailer_sell && Trailer_sell > 0)
                        {
                           if(Trailer_sell>Bid+TrailMinS+TrailBuffer && Bid<OrderOpenPrice() && (Trailer_sell < OrderStopLoss() || OrderStopLoss()==0))
                              {
                                 moved = OrderModify(OrderTicket(),OrderOpenPrice(),Trailer_sell,OrderTakeProfit(),0,Red);
                                 if(!moved) Print("Error on Modify - Sell");
                              }
                        }
                  }
            }// Ordertype() buy or sell and Same Symbol()
         } // for Orders =
      } //Trail Mode == 1
   if(TrailMode==2)
      {
         if(TrailBar_Buy>0) { Trailer_buy = NormalizeDouble(Low[TrailBar_Buy],digits); } else Trailer_buy=0;

         if(TrailBar_Sell>0){ Trailer_sell = NormalizeDouble(High[TrailBar_Sell],digits); } else Trailer_sell=0;


         if(TrailMin_Buy>0)TrailMinB = NormalizeDouble(TrailMin_Buy*point,digits); else TrailMinB=0;
         if(TrailMin_Sell>0)TrailMinS = NormalizeDouble(TrailMin_Sell*point,digits); else TrailMinS=0;

         total=OrdersTotal();
         for(cnt=0;cnt<total;cnt++)
         {
            OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
            if(OrderType() <= OP_SELL  &&  OrderSymbol()==Symbol() )
            {
               if( OrderMagicNumber() == MAGICMA)
                  {
                     if( OrderType()==OP_BUY  &&  NormalizeDouble(OrderStopLoss(),digits) != Trailer_buy && Trailer_buy > 0)
                        {
                           if(Trailer_buy<Ask-TrailMinB-TrailBuffer && Ask>OrderOpenPrice() && Trailer_buy > OrderStopLoss())
                              {
                                 moved = OrderModify(OrderTicket(),OrderOpenPrice(),Trailer_buy,OrderTakeProfit(),0,Blue);
                                 if(!moved) Print("Error on Modify - Buy");
                              }
                        }
                     if(OrderType()==OP_SELL  &&  NormalizeDouble(OrderStopLoss(),digits) != Trailer_sell && Trailer_sell > 0)
                        {
                           if(Trailer_sell>Bid+TrailMinS+TrailBuffer && Bid<OrderOpenPrice() && (Trailer_sell < OrderStopLoss() || OrderStopLoss()==0))
                              {
                                 moved = OrderModify(OrderTicket(),OrderOpenPrice(),Trailer_sell,OrderTakeProfit(),0,Red);
                                 if(!moved) Print("Error on Modify - Sell");
                              }
                        }
                  }
            }// Ordertype() buy or sell and Same Symbol()
         } // for Orders =
      } //Trail Mode == 2
      return(0);
}



//+------------------------------------------------------------------+
//| Start function                                                   |
//+------------------------------------------------------------------+
void start()
  {
   if(Digits<=4)
   {
      point=0.01;
      digits=2;
   }
   else
   {
      point=0.0001;
      digits=4;
   }  
//___________________________________ Testing Control only
   if(LongPer!=5 && LongPer!=15 && LongPer!=30 && LongPer!=60 && LongPer!=240 && LongPer!=1440) return;
   if(ShortPer!=5 && ShortPer!=15 && ShortPer!=30 && ShortPer!=60 && ShortPer!=240 && ShortPer!=1440) return;
//___________________________________ Trade only @ bar open
      gotime = Time[0];  
      if(gotime == lastgotime) return;
      lastgotime = gotime;
      
//___________________________________ Check & Move Trailing Stops
   if(UseTrail) MoveTrailingStop();
   
//___________________________________ Develope Linear Regression Channel
   double a,b,c,
       sumy=0.0,
       sumx=0.0,
       sumxy=0.0,
       sumx2=0.0,
       h=0.0,l=0.0,
       LR_Slope,
       LR_Width;   
   int x;
   ArrayInitialize(LR_line,0.0000);
   
   for(int i=0; i<LinRegLen; i++)
      {
         sumy+=iClose(Symbol(),PERIOD_H4,i);
         sumxy+=iClose(Symbol(),PERIOD_H4,i)*i;
         sumx+=i;
         sumx2+=i*i;
      }
   
   c=sumx2*LinRegLen-sumx*sumx;
   
   if(c==0.0)
      {
         Alert("Error in linear regression!");
         return;
      }
   
   
   // Line equation    
   b=(sumxy*LinRegLen-sumx*sumy)/c;
   a=(sumy-sumx*b)/LinRegLen;
   // Linear regression line in buffer
   for(x=0;x<LinRegLen;x++)
      {
         LR_line[x]=a+b*x;
      }
   for(x=0;x<LinRegLen;x++)
      {
        if(iHigh(Symbol(),PERIOD_H4,x)-LR_line[x] > h) h = iHigh(Symbol(),PERIOD_H4,x)-LR_line[x];
        if(LR_line[x] - iLow(Symbol(),PERIOD_H4,x)> l) l = LR_line[x] - iLow(Symbol(),PERIOD_H4,x);
      }

   // Drawing support - resistance lines   
   if (h>l)
      {
        for(x=0;x<LinRegLen;x++)
         {
           Sup_line[x]=a-h+b*x;
           Res_line[x]=a+h+b*x;
         } 
      }
   else
      {
        for(x=0;x<LinRegLen;x++)
         {
           Sup_line[x]=a-l+b*x;
           Res_line[x]=a+l+b*x;
         }
      }
   LR_Slope = NormalizeDouble(MathAbs(LR_line[0] - LR_line[LinRegLen-1]), digits);
   LR_Width = NormalizeDouble(Res_line[0] - Sup_line[0], digits);
   Print(" LR Slope = ",LR_Slope,"  LR Channel Width = ",LR_Width);

//+------------------------------------------------------------------+
//| Trading conditions                                               |
//+------------------------------------------------------------------+

   double cRSX_0 = iCustom(NULL, ShortPer, "rsx",ShortRSX, 0, 1);
   double cRSX_0P = iCustom(NULL, ShortPer, "rsx",ShortRSX, 0, 2);
   double cRSX_1 = iCustom(NULL, LongPer, "rsx",LongRSX, 0, 1);
   double cRSX_1P = iCustom(NULL, LongPer, "rsx",LongRSX, 0, LongTrendSpread);

   
   if(TradeTrend)
      {
         cSell  = cRSX_1 < LongParSell && cRSX_0 > ShortParSell && cRSX_1 < cRSX_1P && cRSX_0 < cRSX_0P;
         cBuy   = cRSX_1 > LongParBuy && cRSX_0 < ShortParBuy && cRSX_1 > cRSX_1P && cRSX_0 > cRSX_0P;

         cExitBuy  = cSell;
         cExitSell = cBuy ;
      }

//---- calculate open orders by current symbol
   if(OpenOrders(Symbol())==0 && LR_Slope > LinRegTrade*point) CheckForOpen(cBuy, cSell);
   if(OpenOrders(Symbol())!=0) CheckForClose();
//----
  }
//+------------------------------------------------------------------+






