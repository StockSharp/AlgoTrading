//=====================================================================================================================//
//  email: nikolaospantzos@gmail.com                                                                     CloseDeleteEA //
//=====================================================================================================================//
#property copyright   "Copyright 2014-2017, Nikolaos Pantzos"
#property link        "https://www.mql5.com/en/users/pannik"
#property version     "1.1"
#property description "This Expert Advisor is a tool to use for close/delete open positions/orders."
//#property icon        "\\Images\\CloseDeleteEA_Logo.ico";
#property strict
//=====================================================================================================================//
extern bool   CloseAllBuy         = true;
extern bool   CloseAllSell        = true;
extern bool   CloseMarketOrders   = true;
extern bool   DeletePendingOrders = true;
extern bool   CloseOnlyProfit     = false;
extern bool   CloseOnlyLoss       = false;
extern bool   ClearChart          = true;
extern string MagicNumberInfo     = "0:modify all orders, >0:modify identifier orders";
extern int    MagicNumber         = 0;
extern int    SizeBackGround      = 100;
//=====================================================================================================================//
bool MarketClosedCom;
bool CallMain;
int CountOpenOrders=0;
int CurrentOpenOrders=0;
string BackgroundName;
//=====================================================================================================================//
int OnInit()
  {
//----------------------------------
   EventSetMillisecondTimer(10);
//----------------------------------
   CurrentOpenOrders=0;
//----------------------------------
   BackgroundName="Background-"+WindowExpertName();
   DisplayImage(BackgroundName,"g",SizeBackGround,"Webdings",DarkSlateGray,0,14);
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
      ObjectDelete(BackgroundName);
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
   if(CallMain==true) MainFunction();
//----------------------------------
  }
//=====================================================================================================================//
void MainFunction()
  {
//----------------------------------
   MarketClosedCom=false;
   int MarketOrders=0;
   int PendingOrders=0;
//----------------------------------
   for(int cnt=0; cnt<OrdersTotal(); cnt++)
     {
      if(OrderSelect(cnt,SELECT_BY_POS))
        {
         if((OrderMagicNumber()!=MagicNumber) && (MagicNumber!=0)) continue;
         if((OrderProfit()<0) && (CloseOnlyProfit==true)) continue;
         if((OrderProfit()>0) && (CloseOnlyLoss==true)) continue;
         if((OrderType()==OP_BUY)&&(CloseAllBuy==false)) continue;
         if((OrderType()==OP_SELL)&&(CloseAllSell==false)) continue;
         if((OrderMagicNumber()==MagicNumber) || (MagicNumber==0)) CurrentOpenOrders++;
        }
     }
   CountOpenOrders=MathMax(CountOpenOrders,CurrentOpenOrders);
//----------------------------------
   for(int i=OrdersTotal()-1; i>=0; i--)
     {
      if(OrderSelect(i,SELECT_BY_POS))
        {
         if((OrderMagicNumber()!=MagicNumber) && (MagicNumber!=0)) continue;
         if((OrderProfit()<0) && (CloseOnlyProfit==true)) continue;
         if((OrderProfit()>0) && (CloseOnlyLoss==true)) continue;
         if((OrderType()==OP_BUY)&&(CloseAllBuy==false)) continue;
         if((OrderType()==OP_SELL)&&(CloseAllSell==false)) continue;
         //---
         if((OrderMagicNumber()==MagicNumber) || (MagicNumber==0))
           {
            bool result=false;
            //---
            if(CloseMarketOrders==true)
              {
               if((OrderType()==OP_BUY)&&(CloseAllBuy==true)) MarketOrders++;
               if((OrderType()==OP_SELL)&&(CloseAllSell==true)) MarketOrders++;
              }
            //---
            if(DeletePendingOrders==true)
              {
               if((OrderType()==OP_BUYLIMIT) || (OrderType()==OP_SELLLIMIT) || (OrderType()==OP_BUYSTOP) || (OrderType()==OP_SELLSTOP)) PendingOrders++;
              }
            //----------------------------------
            if(CloseMarketOrders==true)
              {
               switch(OrderType())
                 {
                  //Close opened long positions
                  case OP_BUY: result=OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_BID),5,0); break;
                  //Close opened short positions
                  case OP_SELL: result=OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_ASK),5,0); break;
                 }
              }
            //----------------------------------
            if(DeletePendingOrders==true)
              {
               switch(OrderType())
                 {
                  //Delete limit buy positions
                  case OP_BUYLIMIT: result=OrderDelete(OrderTicket()); break;
                  //Delete limit sell positions
                  case OP_SELLLIMIT: result=OrderDelete(OrderTicket()); break;
                  //Delete stop buy positions
                  case OP_BUYSTOP: result=OrderDelete(OrderTicket()); break;
                  //Delete stop sell positions
                  case OP_SELLSTOP: result=OrderDelete(OrderTicket()); break;
                 }
              }
            //----------------------------------
            //Not close order
            if(result==false)
              {
               RefreshRates();
               Sleep(1000);
              }
            //----------------------------------
            //Closed Market
            if(GetLastError()==132)
              {
               MarketClosedCom=true;
               Print(WindowExpertName()+": Could not run, market is closed!!!");
               break;
              }
            //----------------------------------
            if(GetLastError()!=132)
              {
               Comment("==================",
                       "\n\n     ",WindowExpertName(),
                       "\n\n==================",
                       "\n  Start Close/Delete Orders ",
                       "\n\n         Please wait!!! ",
                       "\n\n       Orders: ",MarketOrders,"/",CountOpenOrders,
                       "\n==================");
              }
            //----------------------------------
           }
        }
     }
//----------------------------------
//Finish close
   if((MarketOrders==0) && (CloseMarketOrders==true) && (DeletePendingOrders==false) && (MarketClosedCom==false))
     {
      Comment("==================",
              "\n\n     ",WindowExpertName(),
              "\n\n==================",
              "\n\n    Have closed all orders ",
              "\n\n    you can unload expert. ",
              "\n\n==================");
     }
//----------------------------------
//Finish delete
   if((PendingOrders==0) && (DeletePendingOrders==true) && (CloseMarketOrders==false) && (MarketClosedCom==false))
     {
      Comment("==================",
              "\n\n     ",WindowExpertName(),
              "\n\n==================",
              "\n\n    Have deleted all orders ",
              "\n\n    you can unload expert. ",
              "\n\n==================");
     }
//----------------------------------
//Finish close and delete
   if(((PendingOrders==0) && (DeletePendingOrders==true)) && ((MarketOrders==0) && (CloseMarketOrders==true)))
     {
      Comment("==================",
              "\n\n     ",WindowExpertName(),
              "\n\n==================",
              "\n\n Have Close/Delete all orders ",
              "\n\n    you can unload expert. ",
              "\n\n==================");
     }
//----------------------------------
//Close market
   if(MarketClosedCom==true)
     {
      MarketClosedCom=true;
      Print(WindowExpertName()+": Could not run, market is closed!!!");
      Comment("==================",
              "\n\n     ",WindowExpertName(),
              "\n\n==================",
              "\n\n        Market is closed!!! ",
              "\n\n  Not closed/deleted orders. ",
              "\n\n==================");
     }
//----------------------------------
  }
//=====================================================================================================================//
void DisplayImage(string StringName,string Image,int FontSize,string TypeImage,color FontColor,int Xposition,int Yposition)
  {
//----------------------------------
   ObjectCreate(StringName,OBJ_LABEL,0,0,0);
   ObjectSet(StringName,OBJPROP_CORNER,0);
   ObjectSet(StringName,OBJPROP_BACK,FALSE);
   ObjectSet(StringName,OBJPROP_XDISTANCE,Xposition);
   ObjectSet(StringName,OBJPROP_YDISTANCE,Yposition);
   ObjectSetText(StringName,Image,FontSize,TypeImage,FontColor);
//----------------------------------
  }
//=====================================================================================================================//
