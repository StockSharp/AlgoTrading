//+------------------------------------------------------------------+
//|                                               HeikenAshiIdea.mq4 |
//|                                  Copyright 2016-2017, soubra2003 |
//|                         https://www.mql5.com/en/users/soubra2003 |
//+------------------------------------------------------------------+


#property copyright "Copyright 2016-2017, Soubra2003"
#property link      "https://www.mql5.com/en/users/soubra2003/seller"
#property version   "1.00"
#property strict


//---
#define DASHES       ".........................................................."
#define ExpiredErr   "Heiken Ashi Idea: Please contact the seller, this version has expired."
#define AccErr       "Heiken Ashi Idea: Please contact the seller, this version has limitation."
#define SymbolErr    "Heiken Ashi Idea: Currently, This EA not developed for trading metals."
#define BalanceErr   "Heiken Ashi Idea: NOT ENOUGH BALANCE! Check your trading account balance and/or the lot size."
#define ErrMagic     "Heiken Ashi Idea: Please write a valid Magic Number."
#define ErrSL        "Heiken Ashi Idea: Please write a valid STOP LOSS."
#define ErrTP        "Heiken Ashi Idea: Please write a valid TAKE PROFIT."
#define ErrDist      "Heiken Ashi Idea: Please write a valid Distance value for the pending orders."


enum ENUM_LOT_TYPE { Auto,Fixed };
//---

input int      MagicNumberBuy       = 123;         //Buy Magic Number
input int      MagicNumberSell      = 456;         //Sell Magic Number
input ENUM_LOT_TYPE  LotType        = Auto;        //Lot Type
input double   calculated_amount    = 1000;        //Amount For AUTO Lot
input double   calculated_lot       = 0.01;        //Auto Lot Size Each Amount
input double   FixedLot             = 0.01;        //Fixed Lot Size
input double   StopLoss             = 0;           //Stop Loss (0 = Disable)
input double   TakeProfit           = 20;          //Take Profit (0 = Disable)
input int      Slippage             = 10;          //Max. Slippage
input double   Distance             = 8;           //Pending Order Distance
input bool     UseCloseAll          = true;        //Close All on New Bar
input ENUM_TIMEFRAMES ClsTF         = PERIOD_W1;   //Close All Time Frame

//---
double   MyPoint        = Point;
string   TradesComment  = "By HeikenAshiIdea";
double   Lots;
double   MinLot;
double   MaxLot;
//+------------------------------------------------------------------+
//| Expert initialization function
//+------------------------------------------------------------------+
int OnInit()
  {
   Comment(" ");

   if(Symbol()=="Gold" || Symbol()=="GOLD" || Symbol()=="gold" || Symbol()=="XAUUSD" || Symbol()=="AUCMDUSD"
      || Symbol() == "Silver" || Symbol() == "SILVER" || Symbol() == "silver" || Symbol() == "XAGUSD" || Symbol() == "E_SI"
      || Symbol() == "Copper" || Symbol() == "COPPER" || Symbol() == "copper" || Symbol() == "CUCMDUSD"
      || Symbol() == "XAUEUR" || Symbol() == "Gold.Euro"    || Symbol() == "Gold.Eur"
      || Symbol() == "XAGEUR" || Symbol() == "Silver.Euro"  || Symbol() == "Silver.Eur"
      || Symbol() == "USOil"  || Symbol() == "USOIL"  || Symbol() == "UKOil"  || Symbol() == "UKOIL"
      || Symbol() == "NGAS"   || Symbol() == "NGas"   || Symbol() == "Bund"   || Symbol() == "BUND"   || Symbol() == "bund"
      || Symbol() == "Oil" || Symbol() == "Brent" || Symbol() == "BRENT" || Symbol() == "brent"       || Symbol() == "Crude"  || Symbol() == "COPPER" || Symbol() == "BRENTCMDUSD"
      || Symbol() == "WTI" || Symbol() == "Light" || Symbol() == "LIGHT" || Symbol() == "LIGHTCMDUSD" || Symbol() == "COPPER"
      || Symbol() == "Palladium" || Symbol() == "PALLADIUM" || Symbol() == "palladium" || Symbol() == "PDCMDUSD"
      || Symbol() == "Platinum"  || Symbol() == "PLATINUM"  || Symbol() == "platinum"  || Symbol() == "PTCMDUSD" )
     {
      Comment(SymbolErr);
      Alert(SymbolErr);
      return(INIT_FAILED);
     }

   if(Digits==3 || Digits==5) MyPoint=Point*10;

   MinLot = MarketInfo( NULL,MODE_MINLOT );
   MaxLot = MarketInfo( NULL,MODE_MAXLOT );


//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   Print("Bye. Skype: onesoubra");
  }
//+------------------------------------------------------------------+
//| Expert tick function
//+------------------------------------------------------------------+
void OnTick()
  {
   if(UseCloseAll)
      if(iVolume(NULL,ClsTF,0)<=2)
         CloseAll();

   if( Hour() < 9 )  return;
   if( Hour() > 19 ) return;
   if( !ActiveMarket(1) ) return;

//--- Detect Parameter Errors
//if(MagicNumber <= 0) {Comment(ErrMagic);  Print(ErrMagic);  return;}
   if(Distance <= 5)    {Comment(ErrDist);   Print(ErrDist);   return;}
   if(StopLoss < 0)     {Comment(ErrSL);     Print(ErrSL);     return;}
   if(TakeProfit < 0)   {Comment(ErrTP);     Print(ErrTP);     return;}

//---
   if(LotType==Auto)
     {
      Lots=AccountBalance()/calculated_amount*calculated_lot;
      if(Lots < MinLot) Lots = MinLot;
      if(Lots > MaxLot) Lots = MaxLot;
     }
   else
      Lots=FixedLot;

//--- Trading
   if(TotalOrdersCount(MagicNumberBuy)<1)
      if(AshiUp(1440)==1 && AshiUp()==1)
         BuyExecute();

   if(TotalOrdersCount(MagicNumberSell)<1)
      if(AshiDown(1440)==1 && AshiDown()==1)
         SellExecute();
  }
//+------------------------------------------------------------------+
//| Expert TotalOrdersCount function
//+------------------------------------------------------------------+
int TotalOrdersCount(int Magic)
  {
   int result=0;
   for(int i=0; i<OrdersTotal(); i++)
     {
      int ordselect_=OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==Magic)
         result++;
     }
   return(result);
  }
//+------------------------------------------------------------------+
//| Expert BuyExecute function
//+------------------------------------------------------------------+
void BuyExecute()
  {
   int      tkt_buy=0;
   double   OrderPrice=0;

   OrderPrice=Ask;
   tkt_buy=OrderSend(Symbol(),//Pair
                     OP_BUYLIMIT,                  //Command Type
                     NormalizeDouble(Lots,Digits), //Lot Size
                     OrderPrice-Distance*MyPoint,  //Market Price
                     Slippage,                     //Max. Slippage
                     NULL,                         //Stop Loss
                     NULL,                         //Take Profit
                     TradesComment,                //Comment
                     MagicNumberBuy,               //Magic No.
                     0,                            //Expiration (Only Pending Orders)
                     clrNONE);                     //Arrow Color

   if(tkt_buy>0)
     {
      Print("Buy order placed successfully");

      //---
      double TheStopLoss=0;
      double TheTakeProfit=0;

      bool MyOrderSelect=OrderSelect(tkt_buy,SELECT_BY_TICKET);
      if(StopLoss > 0)   TheStopLoss   = OrderOpenPrice()-StopLoss*MyPoint;
      if(TakeProfit > 0) TheTakeProfit = OrderOpenPrice()+TakeProfit*MyPoint;

      bool MyOrderModify = OrderModify(OrderTicket(),                         //Selected Ticket No.
                                       OrderOpenPrice(),                      //Selected Order Open Price
                                       /*Modify*/  NormalizeDouble(TheStopLoss,Digits),   //Selected Order Stop Loss
                                       /*Modify*/  NormalizeDouble(TheTakeProfit,Digits), //Selected Order Take Profit
                                       0,                                     //Selected Order Expiration (Only Pending Orders)
                                       Green);                                //Selected Order Arrow Color

      if(!MyOrderModify) Print("Unable to place SL/TP for the Buy order: ",ErrorDescription(GetLastError()));
     }
   else Print("Buy order failed with error: ",ErrorDescription(GetLastError()));
  }
//+------------------------------------------------------------------+
//| Expert SellExecute function
//+------------------------------------------------------------------+
void SellExecute()
  {
   int      tkt_sell=0;
   double   OrderPrice=0;

   OrderPrice=Bid;
   tkt_sell = OrderSend(Symbol(),                     //Pair
                        OP_SELLLIMIT,                 //Command Type
                        NormalizeDouble(Lots,Digits), //Lot Size
                        OrderPrice+Distance*MyPoint,  //Market Price
                        Slippage,                     //Max. Slippage
                        NULL,                         //Stop Loss
                        NULL,                         //Take Profit
                        TradesComment,                //Comment
                        MagicNumberSell,              //Magic No.
                        0,                            //Expiration (Only Pending Orders)
                        clrNONE);                     //Arrow Color

   if(tkt_sell>0)
     {
      Print("Sell order placed successfully");

      //---
      double TheStopLoss=0;
      double TheTakeProfit=0;

      bool MyOrderSelect=OrderSelect(tkt_sell,SELECT_BY_TICKET);
      if(StopLoss > 0)   TheStopLoss   = OrderOpenPrice()+StopLoss*MyPoint;
      if(TakeProfit > 0) TheTakeProfit = OrderOpenPrice()-TakeProfit*MyPoint;

      bool MyOrderModify = OrderModify(OrderTicket(),                         //Selected Ticket No.
                                       OrderOpenPrice(),                      //Selected Order Open Price
                                       /*Modify*/  NormalizeDouble(TheStopLoss,Digits),   //Selected Order Stop Loss
                                       /*Modify*/  NormalizeDouble(TheTakeProfit,Digits), //Selected Order Stop Loss
                                       0,                                     //Selected Order Expiration (Only Pending Orders)
                                       Green);                                //Selected Order Arrow Color

      if(!MyOrderModify) Print("Unable to place SL/TP for the Sell order: ",ErrorDescription(GetLastError()));
     }
   else Print("Sell order failed with error: ",ErrorDescription(GetLastError()));
  }
//+------------------------------------------------------------------+
//| Expert SellExecute function
//+------------------------------------------------------------------+
void CloseAll()
  {
   int total=OrdersTotal();

   for(int i=total-1; i>=0; i--)
     {
      int  ticket = OrderSelect(i,SELECT_BY_POS);
      int  type   = OrderType();
      bool result = false;

      if(OrderSymbol()==Symbol() && (OrderMagicNumber()==MagicNumberBuy || OrderMagicNumber()==MagicNumberSell))
         switch(type)
           {
            //Close opened long positions
            case OP_BUY  : result=OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_BID),Slippage,clrNONE);
            break;

            //Delete opened buystop orders
            case OP_BUYSTOP  : result=OrderDelete(OrderTicket(),clrNONE);
            break;

            //Delete opened buylimit orders
            case OP_BUYLIMIT  : result=OrderDelete(OrderTicket(),clrNONE);
            break;

            //Close opened short positions
            case OP_SELL : result=OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_ASK),Slippage,clrNONE);
            break;

            //Delete opened sellstop orders
            case OP_SELLSTOP : result=OrderDelete(OrderTicket(),clrNONE);
            break;

            //Delete opened selllimit orders
            case OP_SELLLIMIT : result=OrderDelete(OrderTicket(),clrNONE);
           }

      if(result==false)
        {
         Comment("Order ",OrderTicket()," failed to close. Error: ",ErrorDescription(GetLastError()));
         Sleep(750);
        }
     }
  }
//+------------------------------------------------------------------+
//| return error description
//+------------------------------------------------------------------+
string ErrorDescription(int error_code)
  {
   string error_string;

//---
   switch(error_code)
     {
      //--- codes returned from trade server
      case 0:   error_string="no error";                                                   break;
      case 1:   error_string="no error, trade conditions not changed";                     break;
      case 2:   error_string="common error";                                               break;
      case 3:   error_string="invalid trade parameters";                                   break;
      case 4:   error_string="trade server is busy";                                       break;
      case 5:   error_string="old version of the client terminal";                         break;
      case 6:   error_string="no connection with trade server";                            break;
      case 7:   error_string="not enough rights";                                          break;
      case 8:   error_string="too frequent requests";                                      break;
      case 9:   error_string="malfunctional trade operation (never returned error)";       break;
      case 64:  error_string="account disabled";                                           break;
      case 65:  error_string="invalid account";                                            break;
      case 128: error_string="trade timeout";                                              break;
      case 129: error_string="invalid price";                                              break;
      case 130: error_string="invalid stops";                                              break;
      case 131: error_string="invalid trade volume";                                       break;
      case 132: error_string="market is closed";                                           break;
      case 133: error_string="trade is disabled";                                          break;
      case 134: error_string="not enough money";                                           break;
      case 135: error_string="price changed";                                              break;
      case 136: error_string="off quotes";                                                 break;
      case 137: error_string="broker is busy (never returned error)";                      break;
      case 138: error_string="requote";                                                    break;
      case 139: error_string="order is locked";                                            break;
      case 140: error_string="long positions only allowed";                                break;
      case 141: error_string="too many requests";                                          break;
      case 145: error_string="modification denied because order is too close to market";   break;
      case 146: error_string="trade context is busy";                                      break;
      case 147: error_string="expirations are denied by broker";                           break;
      case 148: error_string="amount of open and pending orders has reached the limit";    break;
      case 149: error_string="hedging is prohibited";                                      break;
      case 150: error_string="prohibited by FIFO rules";                                   break;

      //--- mql4 errors
      case 4000: error_string="no error (never generated code)";                           break;
      case 4001: error_string="wrong function pointer";                                    break;
      case 4002: error_string="array index is out of range";                               break;
      case 4003: error_string="no memory for function call stack";                         break;
      case 4004: error_string="recursive stack overflow";                                  break;
      case 4005: error_string="not enough stack for parameter";                            break;
      case 4006: error_string="no memory for parameter string";                            break;
      case 4007: error_string="no memory for temp string";                                 break;
      case 4008: error_string="non-initialized string";                                    break;
      case 4009: error_string="non-initialized string in array";                           break;
      case 4010: error_string="no memory for array\' string";                              break;
      case 4011: error_string="too long string";                                           break;
      case 4012: error_string="remainder from zero divide";                                break;
      case 4013: error_string="zero divide";                                               break;
      case 4014: error_string="unknown command";                                           break;
      case 4015: error_string="wrong jump (never generated error)";                        break;
      case 4016: error_string="non-initialized array";                                     break;
      case 4017: error_string="dll calls are not allowed";                                 break;
      case 4018: error_string="cannot load library";                                       break;
      case 4019: error_string="cannot call function";                                      break;
      case 4020: error_string="expert function calls are not allowed";                     break;
      case 4021: error_string="not enough memory for temp string returned from function";  break;
      case 4022: error_string="system is busy (never generated error)";                    break;
      case 4023: error_string="dll-function call critical error";                          break;
      case 4024: error_string="internal error";                                            break;
      case 4025: error_string="out of memory";                                             break;
      case 4026: error_string="invalid pointer";                                           break;
      case 4027: error_string="too many formatters in the format function";                break;
      case 4028: error_string="parameters count is more than formatters count";            break;
      case 4029: error_string="invalid array";                                             break;
      case 4030: error_string="no reply from chart";                                       break;
      case 4050: error_string="invalid function parameters count";                         break;
      case 4051: error_string="invalid function parameter value";                          break;
      case 4052: error_string="string function internal error";                            break;
      case 4053: error_string="some array error";                                          break;
      case 4054: error_string="incorrect series array usage";                              break;
      case 4055: error_string="custom indicator error";                                    break;
      case 4056: error_string="arrays are incompatible";                                   break;
      case 4057: error_string="global variables processing error";                         break;
      case 4058: error_string="global variable not found";                                 break;
      case 4059: error_string="function is not allowed in testing mode";                   break;
      case 4060: error_string="function is not confirmed";                                 break;
      case 4061: error_string="send mail error";                                           break;
      case 4062: error_string="string parameter expected";                                 break;
      case 4063: error_string="integer parameter expected";                                break;
      case 4064: error_string="double parameter expected";                                 break;
      case 4065: error_string="array as parameter expected";                               break;
      case 4066: error_string="requested history data is in update state";                 break;
      case 4067: error_string="internal trade error";                                      break;
      case 4068: error_string="resource not found";                                        break;
      case 4069: error_string="resource not supported";                                    break;
      case 4070: error_string="duplicate resource";                                        break;
      case 4071: error_string="cannot initialize custom indicator";                        break;
      case 4072: error_string="cannot load custom indicator";                              break;
      case 4073: error_string="no history data";                                           break;
      case 4074: error_string="not enough memory for history data";                        break;
      case 4075: error_string="not enough memory for indicator";                           break;
      case 4099: error_string="end of file";                                               break;
      case 4100: error_string="some file error";                                           break;
      case 4101: error_string="wrong file name";                                           break;
      case 4102: error_string="too many opened files";                                     break;
      case 4103: error_string="cannot open file";                                          break;
      case 4104: error_string="incompatible access to a file";                             break;
      case 4105: error_string="no order selected";                                         break;
      case 4106: error_string="unknown symbol";                                            break;
      case 4107: error_string="invalid price parameter for trade function";                break;
      case 4108: error_string="invalid ticket";                                            break;
      case 4109: error_string="trade is not allowed in the expert properties";             break;
      case 4110: error_string="longs are not allowed in the expert properties";            break;
      case 4111: error_string="shorts are not allowed in the expert properties";           break;
      case 4200: error_string="object already exists";                                     break;
      case 4201: error_string="unknown object property";                                   break;
      case 4202: error_string="object does not exist";                                     break;
      case 4203: error_string="unknown object type";                                       break;
      case 4204: error_string="no object name";                                            break;
      case 4205: error_string="object coordinates error";                                  break;
      case 4206: error_string="no specified subwindow";                                    break;
      case 4207: error_string="graphical object error";                                    break;
      case 4210: error_string="unknown chart property";                                    break;
      case 4211: error_string="chart not found";                                           break;
      case 4212: error_string="chart subwindow not found";                                 break;
      case 4213: error_string="chart indicator not found";                                 break;
      case 4220: error_string="symbol select error";                                       break;
      case 4250: error_string="notification error";                                        break;
      case 4251: error_string="notification parameter error";                              break;
      case 4252: error_string="notifications disabled";                                    break;
      case 4253: error_string="notification send too frequent";                            break;
      case 4260: error_string="ftp server is not specified";                               break;
      case 4261: error_string="ftp login is not specified";                                break;
      case 4262: error_string="ftp connect failed";                                        break;
      case 4263: error_string="ftp connect closed";                                        break;
      case 4264: error_string="ftp change path error";                                     break;
      case 4265: error_string="ftp file error";                                            break;
      case 4266: error_string="ftp error";                                                 break;
      case 5001: error_string="too many opened files";                                     break;
      case 5002: error_string="wrong file name";                                           break;
      case 5003: error_string="too long file name";                                        break;
      case 5004: error_string="cannot open file";                                          break;
      case 5005: error_string="text file buffer allocation error";                         break;
      case 5006: error_string="cannot delete file";                                        break;
      case 5007: error_string="invalid file handle (file closed or was not opened)";       break;
      case 5008: error_string="wrong file handle (handle index is out of handle table)";   break;
      case 5009: error_string="file must be opened with FILE_WRITE flag";                  break;
      case 5010: error_string="file must be opened with FILE_READ flag";                   break;
      case 5011: error_string="file must be opened with FILE_BIN flag";                    break;
      case 5012: error_string="file must be opened with FILE_TXT flag";                    break;
      case 5013: error_string="file must be opened with FILE_TXT or FILE_CSV flag";        break;
      case 5014: error_string="file must be opened with FILE_CSV flag";                    break;
      case 5015: error_string="file read error";                                           break;
      case 5016: error_string="file write error";                                          break;
      case 5017: error_string="string size must be specified for binary file";             break;
      case 5018: error_string="incompatible file (for string arrays-TXT, for others-BIN)"; break;
      case 5019: error_string="file is directory, not file";                               break;
      case 5020: error_string="file does not exist";                                       break;
      case 5021: error_string="file cannot be rewritten";                                  break;
      case 5022: error_string="wrong directory name";                                      break;
      case 5023: error_string="directory does not exist";                                  break;
      case 5024: error_string="specified file is not directory";                           break;
      case 5025: error_string="cannot delete directory";                                   break;
      case 5026: error_string="cannot clean directory";                                    break;
      case 5027: error_string="array resize error";                                        break;
      case 5028: error_string="string resize error";                                       break;
      case 5029: error_string="structure contains strings or dynamic arrays";              break;
      default:   error_string="unknown error";
     }

//---
   return(error_string);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int AshiUp(int TF=PERIOD_CURRENT)
  {
   double haLowHigh_1 = iCustom(Symbol(),TF,"Heiken Ashi",Red,White,Red,White,0,1);
   double haOpen_1    = iCustom(Symbol(),TF,"Heiken Ashi",Red,White,Red,White,2,1);
   double haClose_1   = iCustom(Symbol(),TF,"Heiken Ashi",Red,White,Red,White,3,1);

   double haLowHigh_0 = iCustom(Symbol(),TF,"Heiken Ashi",Red,White,Red,White,0,0);
   double haOpen_0    = iCustom(Symbol(),TF,"Heiken Ashi",Red,White,Red,White,2,0);
   double haClose_0   = iCustom(Symbol(),TF,"Heiken Ashi",Red,White,Red,White,3,0);

   if((haClose_0>haOpen_0 && haOpen_0==haLowHigh_0) && 
      (haClose_1>haOpen_1 && haOpen_1!=haLowHigh_1))
      return(1);

   else  return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int AshiDown(int TF=PERIOD_CURRENT)
  {
   double haLowHigh_1 = iCustom(Symbol(),TF,"Heiken Ashi",Red,White,Red,White,0,1);
   double haOpen_1    = iCustom(Symbol(),TF,"Heiken Ashi",Red,White,Red,White,2,1);
   double haClose_1   = iCustom(Symbol(),TF,"Heiken Ashi",Red,White,Red,White,3,1);

   double haLowHigh_0 = iCustom(Symbol(),TF,"Heiken Ashi",Red,White,Red,White,0,0);
   double haOpen_0    = iCustom(Symbol(),TF,"Heiken Ashi",Red,White,Red,White,2,0);
   double haClose_0   = iCustom(Symbol(),TF,"Heiken Ashi",Red,White,Red,White,3,0);

   if((haClose_0<haOpen_0 && haOpen_0==haLowHigh_0) && 
      (haClose_1<haOpen_1 && haOpen_1!=haLowHigh_1))
      return(1);

   else  return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool ActiveMarket(int TF=PERIOD_CURRENT)
  {
   double ATRCurrent = iATR(NULL,TF,14,0);
   double ATRPrev    = iATR(NULL,TF,14,1);

   if(ATRCurrent>ATRPrev)
      return(true);

//---
   return(false);
  }

//+------------------------------------------------------------------+
//Bye
