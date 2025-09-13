//====================================================================================================================================================//
#property copyright   "Copyright 2014-2017, Nikolaos Pantzos"
#property link        "https://www.mql5.com/en/users/pannik"
#property version     "1.5"
#property description "This Expert Advisor is a tool to close open orders and delete position in target profit."
#property description "\nExpert manage only orders' symbole is same with chart symbol. You can to use expert in multiple charts."
#property description "\nSet 'ManageBuySellOrders' = 'Seperate_Buy_From_Sell_Basket' to manage orders buy as one basket, and sell as other basket."
#property description "\nSet 'ManageBuySellOrders' = 'Same_Buy_And_Sell_Basket' to manage orders buy and sell as one basket."
//#property icon        "\\Images\\TargetImage.ico";
#property strict
//====================================================================================================================================================//
enum Ordr {Seperate_Buy_From_Sell_Basket, Same_Buy_And_Sell_Basket};
enum Targ {Target_In_Total_Pips, Target_In_Currency_Per_Lot, Target_In_Percentage_Balance};
//====================================================================================================================================================//
extern string CloseDeletePositions       = "||========== Close and Delete Positions Sets ========||";
extern Ordr   ManageBuySellOrders        = Seperate_Buy_From_Sell_Basket;
extern bool   CloseBuyOrders             = true;
extern bool   CloseSellOrders            = true;
extern bool   DeleteBuyPendingPositions  = true;
extern bool   DeleteSellPendingPositions = true;
extern string TargetTypeAndLevel         = "||========== Target Type And Level Sets ========||";
extern Targ   TypeTargetUse              = Target_In_Percentage_Balance;
extern bool   CloseInProfit              = true;
extern double TargetProfitInPips         = 5.0;
extern double TargetProfitInCurrency     = 50.0;
extern double TargetProfitInPercentage   = 10.0;
extern bool   CloseInLoss                = true;
extern double TargetLossInPips           = -10.0;
extern double TargetLossInCurrency       = -100.0;
extern double TargetLossInPercentage     = -10.0;
extern string ManagePosition             = "||========== Manage Position Sets ========||";
extern string MagicNumberInfo            = ">0:Close identifier orders, 0:Close all orders, -1:Close only manual orders";
extern int    MagicNumber                = 0;
extern string GeneralSettings            = "||========== General Sets ========||";
extern bool   ClearChart                 = false;
extern int    Slippage                   = 3;
extern int    SizeBackGround             = 100;
//====================================================================================================================================================//
bool MarketClosedCom;
bool CallMain;
int MultiplierPoint;
double DigitPoint;
string ExpertName;
//====================================================================================================================================================//
int OnInit()
  {
//----------------------------------
   EventSetMillisecondTimer(10);
//----------------------------------
   DisplayImage("Background1"+WindowExpertName(),"g",SizeBackGround,"Webdings",clrSteelBlue,0,14);
   DisplayImage("Background2"+WindowExpertName(),"g",SizeBackGround,"Webdings",clrSteelBlue,0,122);
//----------------------------------
   ExpertName=WindowExpertName();
//----------------------------------
   DigitPoint=MarketInfo(Symbol(),MODE_POINT);
   MultiplierPoint=1;
   if(MarketInfo(Symbol(),MODE_DIGITS)==3 || MarketInfo(Symbol(),MODE_DIGITS)==5)
     {
      MultiplierPoint=10;
      DigitPoint*=MultiplierPoint;
     }
//---
   Slippage*=MultiplierPoint;//confirm slipage
//----------------------------------
//confirm range
   if(MagicNumber<-1)
      MagicNumber=-1;
//----------------------------------
   return(INIT_SUCCEEDED);
//----------------------------------
  }
//=====================================================================================================================//
void OnDeinit(const int reason)
  {
//----------------------------------
   if(ClearChart==true)
     {
      ObjectsDeleteAll();
      Comment("");
     }
   else
     {
      ObjectDelete("Background1"+WindowExpertName());
      ObjectDelete("Background2"+WindowExpertName());
      Comment("");
     }
//----------------------------------
   EventKillTimer();
//----------------------------------
  }
//=====================================================================================================================//
void OnTick()
  {
//----------------------------------
   CallMain=false;
//----------------------------------
//expert not enabled
   if((!IsExpertEnabled()) && (!IsTesting()))
     {
      Comment("==================",
              "\n\n         ",WindowExpertName(),
              "\n\n==================",
              "\n\n     Expert Not Enabled ! ! !",
              "\n\n    Please Turn On Expert",
              "\n\n==================");
      return;
     }
   else
     {
      CallMain=true;
     }
//----------------------------------
  }
//=====================================================================================================================//
void OnTimer()
  {
//----------------------------------
   if(CallMain==true)
      MainFunction();
//----------------------------------
  }
//=====================================================================================================================//
void MainFunction()
  {
//----------------------------------
   int MarketOrders=0;
   int OrdersBuy=0;
   int OrdersSell=0;
   int BuyPendingPositions=0;
   int SellPendingPositions=0;
   double ProfitBuy=0;
   double ProfitSell=0;
   double PipsBuy=0;
   double PipsSell=0;
   double LotsBuy=0;
   double LotsSell=0;
   MarketClosedCom=false;
//----------------------------------
//----------------------------------
   for(int i=OrdersTotal()-1; i>=0; i--)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(((OrderMagicNumber()==MagicNumber) || (MagicNumber==0)) || ((OrderMagicNumber()==0) && (MagicNumber==-1)))
           {
            if(CloseBuyOrders==true)
              {
               if(OrderType()==OP_BUY)
                 {
                  MarketOrders++;
                  OrdersBuy++;
                  LotsBuy+=OrderLots();
                  ProfitBuy+=OrderProfit()+OrderCommission()+OrderSwap();
                  PipsBuy+=(MarketInfo(Symbol(),MODE_BID)-OrderOpenPrice())/DigitPoint;
                 }
              }
            //---
            if(CloseSellOrders==true)
              {
               if(OrderType()==OP_SELL)
                 {
                  MarketOrders++;
                  OrdersSell++;
                  LotsSell+=OrderLots();
                  ProfitSell+=OrderProfit()+OrderCommission()+OrderSwap();
                  PipsSell+=(OrderOpenPrice()-MarketInfo(Symbol(),MODE_ASK))/DigitPoint;
                 }
              }
            //---
            if(DeleteBuyPendingPositions==true)
              {
               if((OrderType()==OP_BUYSTOP) || (OrderType()==OP_BUYLIMIT))
                  BuyPendingPositions++;
              }
            //---
            if(DeleteSellPendingPositions==true)
              {
               if((OrderType()==OP_SELLSTOP) || (OrderType()==OP_SELLLIMIT))
                  SellPendingPositions++;
              }
            //----------------------------------
            //Closed Market
            if(GetLastError()==132)
              {
               MarketClosedCom=true;
               break;
              }
            //----------------------------------
           }
        }
     }
//----------------------------------
//Close orders
   if(TypeTargetUse==0)//close in pips
     {
      //----------------------------------
      if(CloseInProfit==true)
        {
         //---Close buy
         if((CloseBuyOrders==true) && (PipsBuy>=TargetProfitInPips) && (ManageBuySellOrders==0))
           {
            if((DeleteBuyPendingPositions==true) && (BuyPendingPositions>0))
               DeletePositions(1);
            if(OrdersBuy>0)
               CloseOrders(1);
           }
         //---Close sell
         if((CloseSellOrders==true) && (PipsSell>=TargetProfitInPips) && (ManageBuySellOrders==0))
           {
            if((DeleteSellPendingPositions==true) && (SellPendingPositions>0))
               DeletePositions(-1);
            if(OrdersSell>0)
               CloseOrders(-1);
           }
         //---Close all
         if((PipsBuy+PipsSell>=TargetProfitInPips) && (ManageBuySellOrders==1))
           {
            //---Close buy
            if(CloseBuyOrders==true)
              {
               if((DeleteBuyPendingPositions==true) && (BuyPendingPositions>0))
                  DeletePositions(1);
               if(OrdersBuy>0)
                  CloseOrders(1);
              }
            //---Close sell
            if(CloseSellOrders==true)
              {
               if((DeleteSellPendingPositions==true) && (SellPendingPositions>0))
                  DeletePositions(-1);
               if(OrdersSell>0)
                  CloseOrders(-1);
              }
           }
        }
      //----------------------------------
      if(CloseInLoss==true)
        {
         //---Close buy
         if((CloseBuyOrders==true) && (PipsBuy<=TargetLossInPips) && (ManageBuySellOrders==0))
           {
            if((DeleteBuyPendingPositions==true) && (BuyPendingPositions>0))
               DeletePositions(1);
            if(OrdersBuy>0)
               CloseOrders(1);
           }
         //---Close sell
         if((CloseSellOrders==true) && (PipsSell<=TargetLossInPips) && (ManageBuySellOrders==0))
           {
            if((DeleteSellPendingPositions==true) && (SellPendingPositions>0))
               DeletePositions(-1);
            if(OrdersSell>0)
               CloseOrders(-1);
           }
         //---Close all
         if((PipsBuy+PipsSell<=TargetLossInPips) && (ManageBuySellOrders==1))
           {
            //---Close buy
            if(CloseBuyOrders==true)
              {
               if((DeleteBuyPendingPositions==true) && (BuyPendingPositions>0))
                  DeletePositions(1);
               if(OrdersBuy>0)
                  CloseOrders(1);
              }
            //---Close sell
            if(CloseSellOrders==true)
              {
               if((DeleteSellPendingPositions==true) && (SellPendingPositions>0))
                  DeletePositions(-1);
               if(OrdersSell>0)
                  CloseOrders(-1);
              }
           }
        }
      //----------------------------------
     }
//----------------------------------
   if(TypeTargetUse==1)//close in currency
     {
      //----------------------------------
      if(CloseInProfit==true)
        {
         //---Close buy
         if((CloseBuyOrders==true) && (ProfitBuy>=TargetProfitInCurrency*LotsBuy) && (ManageBuySellOrders==0))
           {
            if((DeleteBuyPendingPositions==true) && (BuyPendingPositions>0))
               DeletePositions(1);
            if(OrdersBuy>0)
               CloseOrders(1);
           }
         //---Close sell
         if((CloseSellOrders==true) && (ProfitSell>=TargetProfitInCurrency*LotsSell) && (ManageBuySellOrders==0))
           {
            if((DeleteSellPendingPositions==true) && (SellPendingPositions>0))
               DeletePositions(-1);
            if(OrdersSell>0)
               CloseOrders(-1);
           }
         //---Close all
         if((ProfitBuy+ProfitSell>=TargetProfitInCurrency*(LotsBuy+LotsSell)) && (ManageBuySellOrders==1))
           {
            //---Close buy
            if(CloseBuyOrders==true)
              {
               if((DeleteBuyPendingPositions==true) && (BuyPendingPositions>0))
                  DeletePositions(1);
               if(OrdersBuy>0)
                  CloseOrders(1);
              }
            //---Close sell
            if(CloseSellOrders==true)
              {
               if((DeleteSellPendingPositions==true) && (SellPendingPositions>0))
                  DeletePositions(-1);
               if(OrdersSell>0)
                  CloseOrders(-1);
              }
           }
        }
      //----------------------------------
      if(CloseInLoss==true)
        {
         //---Close buy
         if((CloseBuyOrders==true) && (ProfitBuy<=TargetLossInCurrency*LotsBuy) && (ManageBuySellOrders==0))
           {
            if((DeleteBuyPendingPositions==true) && (BuyPendingPositions>0))
               DeletePositions(1);
            if(OrdersBuy>0)
               CloseOrders(1);
           }
         //---Close sell
         if((CloseSellOrders==true) && (ProfitSell<=TargetLossInCurrency*LotsSell) && (ManageBuySellOrders==0))
           {
            if((DeleteSellPendingPositions==true) && (SellPendingPositions>0))
               DeletePositions(-1);
            if(OrdersSell>0)
               CloseOrders(-1);
           }
         //---Close all
         if((ProfitBuy+ProfitSell<=TargetLossInCurrency*(LotsBuy+LotsSell)) && (ManageBuySellOrders==1))
           {
            //---Close buy
            if(CloseBuyOrders==true)
              {
               if((DeleteBuyPendingPositions==true) && (BuyPendingPositions>0))
                  DeletePositions(1);
               if(OrdersBuy>0)
                  CloseOrders(1);
              }
            //---Close sell
            if(CloseSellOrders==true)
              {
               if((DeleteSellPendingPositions==true) && (SellPendingPositions>0))
                  DeletePositions(-1);
               if(OrdersSell>0)
                  CloseOrders(-1);
              }
           }
        }
      //----------------------------------
     }
//----------------------------------
   if(TypeTargetUse==2)//close in percentage
     {
      //----------------------------------
      if(CloseInProfit==true)
        {
         //---Close buy
         if((CloseBuyOrders==true) && (ProfitBuy>=AccountBalance()+((AccountBalance()*TargetProfitInPercentage)/100.0)) && (ManageBuySellOrders==0))
           {
            if((DeleteBuyPendingPositions==true) && (BuyPendingPositions>0))
               DeletePositions(1);
            if(OrdersBuy>0)
               CloseOrders(1);
           }
         //---Close sell
         if((CloseSellOrders==true) && (ProfitSell>=AccountBalance()+((AccountBalance()*TargetProfitInPercentage)/100.0)) && (ManageBuySellOrders==0))
           {
            if((DeleteSellPendingPositions==true) && (SellPendingPositions>0))
               DeletePositions(-1);
            if(OrdersSell>0)
               CloseOrders(-1);
           }
         //---Close all
         if((ProfitBuy+ProfitSell>=AccountBalance()+((AccountBalance()*TargetProfitInPercentage)/100.0)) && (ManageBuySellOrders==1))
           {
            //---Close buy
            if(CloseBuyOrders==true)
              {
               if((DeleteBuyPendingPositions==true) && (BuyPendingPositions>0))
                  DeletePositions(1);
               if(OrdersBuy>0)
                  CloseOrders(1);
              }
            //---Close sell
            if(CloseSellOrders==true)
              {
               if((DeleteSellPendingPositions==true) && (SellPendingPositions>0))
                  DeletePositions(-1);
               if(OrdersSell>0)
                  CloseOrders(-1);
              }
           }
        }
      //----------------------------------
      if(CloseInLoss==true)
        {
         //---Close buy
         if((CloseBuyOrders==true) && (ProfitBuy<=AccountBalance()-((AccountBalance()*MathAbs(TargetLossInPercentage))/100.0)) && (ManageBuySellOrders==0))
           {
            if((DeleteBuyPendingPositions==true) && (BuyPendingPositions>0))
               DeletePositions(1);
            if(OrdersBuy>0)
               CloseOrders(1);
           }
         //---Close sell
         if((CloseSellOrders==true) && (ProfitSell<=AccountBalance()-((AccountBalance()*MathAbs(TargetLossInPercentage))/100.0)) && (ManageBuySellOrders==0))
           {
            if((DeleteSellPendingPositions==true) && (SellPendingPositions>0))
               DeletePositions(-1);
            if(OrdersSell>0)
               CloseOrders(-1);
           }
         //---Close all
         if((ProfitBuy+ProfitSell<=AccountBalance()-((AccountBalance()*MathAbs(TargetLossInPercentage))/100.0)) && (ManageBuySellOrders==1))
           {
            //---Close buy
            if(CloseBuyOrders==true)
              {
               if((DeleteBuyPendingPositions==true) && (BuyPendingPositions>0))
                  DeletePositions(1);
               if(OrdersBuy>0)
                  CloseOrders(1);
              }
            //---Close sell
            if(CloseSellOrders==true)
              {
               if((DeleteSellPendingPositions==true) && (SellPendingPositions>0))
                  DeletePositions(-1);
               if(OrdersSell>0)
                  CloseOrders(-1);
              }
           }
        }
      //----------------------------------
     }
//----------------------------------
//Screen text
   string TypeTarget="";
   string AcceptedProfitTarget="Not Use";
   string AcceptedLossTarget="Not Use";
   string LastLine="";
   string CloseBuyStr="FALSE";
   string CloseSellStr="FALSE";
   string DeleteBuyStr="FALSE";
   string DeleteSellStr="FALSE";
//----------------------------------
   if(TypeTargetUse==0)
     {
      TypeTarget="Pips";
      if(CloseInProfit==true)
         AcceptedProfitTarget=DoubleToStr(TargetProfitInPips,2);
      if(CloseInLoss==true)
         AcceptedLossTarget=DoubleToStr(TargetLossInPips,2);
     }
//---
   if(TypeTargetUse==1)
     {
      TypeTarget="Currency";
      if(CloseInProfit==true)
         AcceptedProfitTarget=DoubleToStr(TargetProfitInCurrency,2);
      if(CloseInLoss==true)
         AcceptedLossTarget=DoubleToStr(TargetLossInCurrency,2);
     }
//---
   if(TypeTargetUse==2)
     {
      TypeTarget="Percentage";
      if(CloseInProfit==true)
         AcceptedProfitTarget=DoubleToStr(TargetProfitInPercentage,2);
      if(CloseInLoss==true)
         AcceptedLossTarget=DoubleToStr(TargetLossInPercentage,2);
     }
//----------------------------------
   if(MarketOrders>0)
      LastLine="  Find For Target Orders \n  Wait to close order(s)";
   if(MarketClosedCom==true)
      LastLine="  Market is closed!!! \n  Can't close order(s)";
   if((MarketOrders==0) && (MarketClosedCom==false))
      LastLine="  Have closed all orders \n  Waiting for new order(s)";
   if(CloseBuyOrders==true)
      CloseBuyStr="TRUE";
   if(CloseSellOrders==true)
      CloseSellStr="TRUE";
   if(DeleteBuyPendingPositions==true)
      DeleteBuyStr="TRUE";
   if(DeleteSellPendingPositions==true)
      DeleteSellStr="TRUE";
//----------------------------------
   Comment("=================="+
           "\n         ",WindowExpertName(),
           "\n=================="+
           "\n    ~~~ SETTINGS ~~~"+
           "\n  Close Buy Orders:"+CloseBuyStr+
           "\n  Close Sell Orders:"+CloseSellStr+
           "\n  Delete Buy Posit :"+DeleteBuyStr+
           "\n  Delete Sell Posit :"+DeleteSellStr+
           "\n=================="+
           "\n  Target In "+TypeTarget+
           "\n  Profit Target : "+AcceptedProfitTarget+
           "\n  Loss Target  : "+AcceptedLossTarget+
           "\n=================="+
           "\n  Floating Buy: "+DoubleToStr(ProfitBuy,2)+
           "\n  Floating Sell: "+DoubleToStr(ProfitSell,2)+
           "\n=================="+
           "\n   ~~~ OPERATION ~~~"+
           "\n"+LastLine+
           "\n==================");
//----------------------------------
  }
//====================================================================================================================================================//
//Close orders function
void CloseOrders(int PositionType)
  {
//----------------------------------
   int TryCnt=0;
   bool WasOrderClosed;
//------------------------------------------------------
   for(int i=OrdersTotal()-1; i>=0; i--)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false)
         continue;
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
        {
         if(((OrderMagicNumber()==MagicNumber) || (MagicNumber==0)) || ((OrderMagicNumber()==0) && (MagicNumber==-1)))
           {
            //------------------------------------------------------
            //Close buy
            if((OrderType()==OP_BUY) && (PositionType==1))
              {
               TryCnt=0;
               WasOrderClosed=false;
               while(true)
                 {
                  //---close order
                  WasOrderClosed=OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Bid,Digits),Slippage,clrMediumAquamarine);
                  TryCnt++;
                  //---
                  if(WasOrderClosed>0)
                     break;
                  //---Unknown order
                  if(GetLastError()==1)
                     break;
                  //---Close market
                  if((GetLastError()==132) || (GetLastError()==133))
                    {
                     Print(StringConcatenate(ExpertName,": Could not close ticket: ",OrderTicket(),", market is closed or trade is disabled"));
                     break;
                    }
                  //---try 3 times to close
                  if(TryCnt>=3)
                    {
                     Print(StringConcatenate("Error: ",GetLastError()," || ",ExpertName,": Could not close ticket: ",OrderTicket()));
                     break;
                    }
                  //---
                  else
                    {
                     Print(StringConcatenate("Error: ",GetLastError()," || ",ExpertName,": receives new data and try again close ticket: ",OrderTicket()));
                     RefreshRates();
                    }
                 }//End while(...
              }//End if(OrderType()==OP_BUY)
            //------------------------------------------------------
            //Close sell
            if((OrderType()==OP_SELL) && (PositionType==-1))
              {
               TryCnt=0;
               WasOrderClosed=false;
               while(true)
                 {
                  //---close order
                  WasOrderClosed=OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Ask,Digits),Slippage,clrDarkSalmon);
                  TryCnt++;
                  //---
                  if(WasOrderClosed>0)
                     break;
                  //---Unknown order
                  if(GetLastError()==1)
                     break;
                  //---Close market
                  if((GetLastError()==132) || (GetLastError()==133))
                    {
                     Print(StringConcatenate(ExpertName,": Could not close ticket: ",OrderTicket(),", market is closed or trade is disabled"));
                     break;
                    }
                  //---try 3 times to close
                  if(TryCnt>=3)
                    {
                     Print(StringConcatenate("Error: ",GetLastError()," || ",ExpertName,": Could not close ticket: ",OrderTicket()));
                     break;
                    }
                  //---
                  else
                    {
                     Print(StringConcatenate("Error: ",GetLastError()," || ",ExpertName,": receives new data and try again close ticket: ",OrderTicket()));
                     RefreshRates();
                    }
                 }//End while(...
              }//End if(OrderType()==OP_SELL)
            //------------------------------------------------------
           }//End if(((OrderMagicNumber()...
        }//End OrderSelect(...
     }//End for(...
//------------------------------------------------------
  }
//====================================================================================================================================================//
//Delete positions function
void DeletePositions(int PositionType)
  {
//------------------------------------------------------
   int TryCnt=0;
   bool WasPositionDelete;
//------------------------------------------------------
   for(int i=OrdersTotal()-1; i>=0; i--)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false)
         continue;
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
        {
         if(((OrderMagicNumber()==MagicNumber) || (MagicNumber==0)) || ((OrderMagicNumber()==0) && (MagicNumber==-1)))
           {
            //------------------------------------------------------
            //Delete buy
            if(((OrderType()==OP_BUYSTOP) || (OrderType()==OP_BUYLIMIT)) && (PositionType==1))
              {
               TryCnt=0;
               WasPositionDelete=false;
               while(true)
                 {
                  //---delete order
                  WasPositionDelete=OrderDelete(OrderTicket(),clrMediumAquamarine);
                  TryCnt++;
                  //---
                  if(WasPositionDelete>0)
                     break;
                  //---Unknown order
                  if(GetLastError()==1)
                     break;
                  //---Close market
                  if((GetLastError()==132) || (GetLastError()==133))
                    {
                     Print(StringConcatenate(ExpertName,": Could not delete ticket: ",OrderTicket(),", market is closed or trade is disabled"));
                     break;
                    }
                  //---try 3 times to close
                  if(TryCnt>=3)
                    {
                     Print(StringConcatenate("Error: ",GetLastError()," || ",ExpertName,": Could not delete ticket: ",OrderTicket()));
                     break;
                    }
                  //---
                  else
                    {
                     Print(StringConcatenate("Error: ",GetLastError()," || ",ExpertName,": receives new data and try again delete ticket: ",OrderTicket()));
                     RefreshRates();
                    }
                 }//End while(...
              }//End if(OrderType()==OP_BUY)
            //------------------------------------------------------
            //Delete sell
            if(((OrderType()==OP_SELLSTOP) || (OrderType()==OP_SELLLIMIT)) && (PositionType==-1))
              {
               TryCnt=0;
               WasPositionDelete=false;
               while(true)
                 {
                  //---close order
                  WasPositionDelete=OrderDelete(OrderTicket(),clrDarkSalmon);
                  TryCnt++;
                  //---
                  if(WasPositionDelete>0)
                     break;
                  //---Unknown order
                  if(GetLastError()==1)
                     break;
                  //---Close market
                  if((GetLastError()==132) || (GetLastError()==133))
                    {
                     Print(StringConcatenate(ExpertName,": Could not delete ticket: ",OrderTicket(),", market is closed or trade is disabled"));
                     break;
                    }
                  //---try 3 times to close
                  if(TryCnt>=3)
                    {
                     Print(StringConcatenate("Error: ",GetLastError()," || ",ExpertName,": Could not delete ticket: ",OrderTicket()));
                     break;
                    }
                  //---
                  else
                    {
                     Print(StringConcatenate("Error: ",GetLastError()," || ",ExpertName,": receives new data and try again delete ticket: ",OrderTicket()));
                     RefreshRates();
                    }
                 }//End while(...
              }//End if(OrderType()==OP_SELL)
            //------------------------------------------------------
           }//End if(((OrderMagicNumber()...
        }//End OrderSelect(...
     }//End for(...
//------------------------------------------------------
  }
//====================================================================================================================================================//
void DisplayImage(string StringName,string Image,int FontSize,string TypeImage,color FontColor,int Xposition,int Yposition)
  {
//------------------------------------------------------
   ObjectCreate(StringName,OBJ_LABEL,0,0,0);
   ObjectSet(StringName,OBJPROP_CORNER,0);
   ObjectSet(StringName,OBJPROP_BACK,FALSE);
   ObjectSet(StringName,OBJPROP_XDISTANCE,Xposition);
   ObjectSet(StringName,OBJPROP_YDISTANCE,Yposition);
   ObjectSetText(StringName,Image,FontSize,TypeImage,FontColor);
//------------------------------------------------------
  }
//====================================================================================================================================================//
