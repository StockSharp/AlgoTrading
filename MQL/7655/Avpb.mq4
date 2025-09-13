//+------------------------------------------------------------------+
//|                                                  ASCV            |
//|                                      Copyright © 2005, IDS Corp. |
//|                                                                  |
//+------------------------------------------------------------------+
extern double Lots=1;
//----
double Profit1,Profit2;
//----
int cnt, ticket, total;
int OpenBuy, OpenSell;
int OpenBuy1, OpenSell1;
int FlagBalance;
int pause=20000;
//----
double TakeProfit;
//double Lots = 1;
double TrailingStop;
double signals,signalb,Value1;
double signalscl,signalbcl;
double Closebuy,Closesell;
double slb,sls;
double slb1,sls1;
double slb10,sls10;
double ttp=15;
double dstos,dstob;
double stdd;
double PPL,P1L,P2L,P4L,P5L;
double btb,bts;
//+------------------------------------------------------------------+
//|V=30 Fr-yes 1stModify-no ModifyTP-no iStdDev                      |
//+------------------------------------------------------------------+
int start()
  {
   //if (DayOfWeek()==5){ return(0);}
   if (Hour()<2||Hour()>20) return(0);
   if (Minute()==0)
     {
      OpenSell1=0;
      OpenBuy1=0;
      OpenSell=0;
      OpenBuy=0;
      signalb=0;
     }
   if (FlagBalance==0)
     {
      Profit1=AccountBalance();
      FlagBalance=1;
     }
   // Getting signals-------------------------------
   signalb=iCustom(NULL, 0, "ASCTrend1sig",1,0);
   signals=iCustom(NULL, 0, "ASCTrend1sig",0,0);
//----
   stdd=iStdDev(NULL,0,10,MODE_SMA,0,PRICE_MEDIAN,0);
//----
   btb=iCustom(NULL, 0, "BrainTrend1Sig",1,0);
   bts=iCustom(NULL, 0, "BrainTrend1Sig",0,0);
   PPL=iCustom(NULL, 0, "Pivot_AllLevels",2,0);
   P1L=iCustom(NULL, 0, "Pivot_AllLevels",0,0);
   P2L=iCustom(NULL, 0, "Pivot_AllLevels",1,0);
   P4L=iCustom(NULL, 0, "Pivot_AllLevels",3,0);
   P5L=iCustom(NULL, 0, "Pivot_AllLevels",4,0);
//----1 
   if (stdd>0.001)
     {
      if (signalb>0)
        {
         if (signalb<Low[0] && Ask>PPL && Volume[0]-Volume[1]>=30)
           {
            OpenBuy=1;
           }
        }
      if (signals>0)
        {
         if (signals>High[0]&&  Bid<PPL && Volume[0]-Volume[1]>=30)
           {
            OpenSell=1;
           }
        }
     }
//----2
   if (signalb>0 && btb>0)
     {
      if (signalb<Low[0] && btb<Low[0] && Volume[0]-Volume[1]>30)
        {
         OpenBuy=1;
        }
     }
   if (signals>0 && bts>0)
     {
      if (signals>High[0]&&  bts>High[0] && Volume[0]-Volume[1]>30)
        {
         OpenSell=1;
        }
     }
//Trading----------------------------------
   total=OrdersTotal();
   if(total<1)
     {
      Closesell=0;
      Closebuy=0;
      if (Profit1 !=AccountBalance())
        {
         Profit2=(AccountBalance()-Profit1);
         SendMail(" ","Last order closed. Profit="+DoubleToStr(Profit2,2)+". Balance="+DoubleToStr(AccountBalance(),2));
         Profit1=AccountBalance();
        }
// check for BUY-----------------------
      if(OpenBuy==1 && signalb!=0)
        {
         slb1=signalb;
/*         +((Close[0]-signalb)/2);
         if ((Ask-slb1)>=33*Point){slb=slb1;}
         else {slb=Ask-33*Point;}
*/
         if (P2L-Ask>=50*Point) TakeProfit=P2L;
         else
           {
            if(P1L-Ask>=50*Point) TakeProfit=P1L;
            else TakeProfit=Ask+50*Point;
           }
         ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,3,slb1,TakeProfit," ",16384,0,Green);
         if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
              {
               Print("BUY order opened : ",OrderOpenPrice());
               SendMail(" ","BUY order opened : "+OrderOpenPrice());
               OpenBuy=0;
               signalb=0;
              }
           }
         else
           {
            Print("Error opening BUY order : ",GetLastError());
            return(0);
           }
        }
// check for SELL-----------------------------
      if(OpenSell==1 && signals!=0)
        {
         sls1=signals;
       /* -((signals-Close[0])/2);
         if ((sls1-Bid)>=33*Point){sls=sls1;}
         else {sls=Bid+33*Point;}
       */
         if (Bid-P4L>=50*Point) TakeProfit=P4L;
         else
           {
            if (Bid-P5L>=50*Point) TakeProfit=P5L;
            else TakeProfit=Bid-50*Point;
           }
         ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,3,sls1,TakeProfit," ",16384,0,Red);
         if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
              {
               Print("SELL order opened : ",OrderOpenPrice());
               SendMail(" ","SELL order opened : "+OrderOpenPrice());
               OpenSell=0;
               signalb=0;
              }
           }
         else
           {
            Print("Error opening SELL order : ",GetLastError());
            //           SendMail(" ","Error opening SELL order : "+GetLastError());
            return(0);
           }
        }
      return(0);
     }
//Close&Modify------------------------------------
   for(cnt=0;cnt<total;cnt++)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderType()<=OP_SELL &&   // check for opened position 
         OrderSymbol()==Symbol())  // check for symbol
        {
//Getting CloseSignals------------------------------------
         //ASC
         signalbcl=iCustom(NULL, 0, "ASCTrend1sig",1,1);
         if (Minute()==0&&signalbcl==0&&signalb==0&&(CurTime()-OrderOpenTime())<=3600)
         {Closebuy=1;}
//----
         signalscl=iCustom(NULL, 0, "ASCTrend1sig",0,1);
         if (Minute()==0&&signalscl==0&&signals==0&&(CurTime()-OrderOpenTime())<=3600)
         {Closesell=1;}
         //STO  
         dstob=iStochastic(NULL,0,5,3,3,MODE_SMA,0,MODE_MAIN,0)-iStochastic(NULL,0,5,3,3,MODE_SMA,0,MODE_SIGNAL,0);
         dstos=iStochastic(NULL,0,5,3,3,MODE_SMA,0,MODE_SIGNAL,0)-iStochastic(NULL,0,5,3,3,MODE_SMA,0,MODE_MAIN,0);
/*  if(dstos>=2)
  {
  Closebuy=1;
  } 
 if(dstob>=2)
  {
  Closesell=1;
  }
*/
//Close BUY-----------------------------------
         if(OrderType()==OP_BUY)
           {
            if(OpenSell==1||Closebuy==1)
              {
               OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet); // close position
               Sleep(pause);
               return(0); // exit
              }
            // Modify BUY ---------------------------------
            if(OrderStopLoss()<OrderOpenPrice())
            {TrailingStop=25;}
            else {TrailingStop=30;}
            //1st Modify-----------------------------------            
/*    if (OrderStopLoss()<OrderOpenPrice())
     {
    slb10=(OrderStopLoss()+(Bid-OrderOpenPrice()));
    if ((Bid-OrderOpenPrice())<(Point*TrailingStop)&& slb10>OrderStopLoss())
       {
       OrderModify(OrderTicket(),OrderOpenPrice(),slb10,OrderTakeProfit(),0,Green);
       slb10=0;
       }
     }
*/
//2nd Modify-----------------------------------
            if(Bid-OrderOpenPrice()>=Point*TrailingStop)
              {
               if(OrderStopLoss()<Bid-Point*TrailingStop)
                 {
                  OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop,OrderTakeProfit(),0,Green);
                  if (OrderStopLoss()==OrderOpenPrice())
                  {SendMail(" ","Last order SL=OP");}
                  return(0);
                 }
              }
//Modify TakeProfit BUY---------------------
            if ((OrderTakeProfit()-Bid)<(ttp*Point)&&dstob>=5)
              {
               if (OrderTakeProfit()!=0)
                 {
                  OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),OrderTakeProfit()+15*Point,0,Red);
                 }
              }

           }
//Close SELL------------------------------
         if(OrderType()==OP_SELL)
           {
            if(OpenBuy==1||Closesell==1)
              {
               OrderClose(OrderTicket(),OrderLots(),Ask,3,Violet); // close position
               Sleep(pause);
               return(0); // exit
              }
//Modify SELL ----------------------------
            if(OrderStopLoss()>OrderOpenPrice())
            {TrailingStop=25;}
            else {TrailingStop=30;}
            //1st Modify -----------------------------
/*   if (OrderStopLoss()>OrderOpenPrice())
      {
    sls10=(OrderStopLoss()-(OrderOpenPrice()-Ask));
      if ((OrderOpenPrice()-Ask)<(Point*TrailingStop)&& sls10<OrderStopLoss())
         {
         OrderModify(OrderTicket(),OrderOpenPrice(),sls10,OrderTakeProfit(),0,Red);
         sls10=0;
         }
    }
 */
//2nd Modify -----------------------------            
            if((OrderOpenPrice()-Ask)>=(Point*TrailingStop))
              {
               if((OrderStopLoss()>(Ask+Point*TrailingStop)) || (OrderStopLoss()==0))
                 {
                  OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStop,OrderTakeProfit(),0,Red);
                  if (OrderStopLoss()==OrderOpenPrice())
                  {SendMail(" ","Last order SL=OP");}
                  return(0);
                 }
              }
//Modify TakeProfit SELL------------------                 
            if ((Ask-OrderTakeProfit())<(ttp*Point)&&dstos>=5)
              {
               if (OrderTakeProfit()!=0)
                 {
                  OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),OrderTakeProfit()-15*Point,0,Red);
                 }
              }
           }
        }
     }
   return(0);
  }
//+------------------------------------------------------------------+