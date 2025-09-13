//+------------------------------------------------------------------+
//|                                                           TM.mqh |
//|                                       Copyright 2010, KTS Group. |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "Copyright 2010, KTS Group."

#include <Trade\PositionInfo.mqh>
#include <Trade\SymbolInfo.mqh>

#define  UNKNOW    (0)

//---Execution mode consts
#define  REQUEST   (1)
#define  INSTANT   (2)
#define  MARKET    (3)

//---Trade mode consts
#define  TRADE_DISABLED  (1)
#define  TRADE_FULL      (2)
#define  TRADE_LONGONLY  (3)
#define  TRADE_SHORTONLY (4)
#define  TRADE_CLOSEONLY (5)
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
struct PositionInfo
  {
   ENUM_POSITION_TYPE Type;
   ulong             MagicNumber;
   long              UID;
   datetime          TimeOpen;
   double            OpenPrice;
   double            StopLossPrice;
   double            TakeProfitPrice;
   double            Volume;
   double            Profit;

  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CTradingManager
  {
protected:
   CPositionInfo    *PosInfo;
   CSymbolInfo      *SymbInfo;
   string            Name;
   int               digits;
   int               Spread;
   double            point;
   int               StopLevel;
   int               FreezeLevel;
   double            Bid;
   double            Ask;
   double            LotVolume;
   double            MaxLot;
   double            MinLot;
   double            LotStep;
   double            LotsLimit;
   bool              Positions;
protected:
   ushort            CorrectStops(ushort Step);
   uchar             Execution();
   uchar             TradeMode();
   bool              UpdateSymbolInfo();
   double            GetIndicatorValue(int handle);
   double            CorrectLot(double CurrentLot,uchar VolumePercent,uint &IterationCount,double &rests);
   double            CorrectLot(double CurrentLot,uint &IterationCount,double &rests);
   bool              PositionIsFrozen(ENUM_POSITION_TYPE OPEN_POSITION,double SL,double TP);
   bool              StopsIsInvalid(ENUM_POSITION_TYPE OPEN_POSITION,double SL=0,double TP=0);
   bool              StopsIsInvalid(ENUM_ORDER_TYPE OPEN_ORDER,double OpenPrice=0,double SL=0,double TP=0,double StopPrice=0);
   bool              SendRequest(bool &ContinueRequest);
   bool              CheckCoincidence(ENUM_ORDER_TYPE OrderType,ENUM_POSITION_TYPE PositionType);

protected:
   MqlTradeRequest   Trade_request;
   ulong             m_deviation;
   ulong             m_magic;
   string            m_Comm;
private:
   void              UpdatePositionInfo();

public:
                     CTradingManager(void);
                    ~CTradingManager(void);
   MqlTradeResult    Trade_result;
   MqlTradeCheckResult Check_result;
   char             Initilize(CSymbolInfo  *ItemSymbInfo);
   void              SimpleTrailing(int BuyStep,int SellStep,int Deviation=0,ulong Magic=0);
   bool              SimpleTrailing(double new_sl,ulong Magic=0);
   void              TATrailing(int handle,ENUM_TIMEFRAMES SFrame=PERIOD_CURRENT,ulong Magic=0);
   bool              MoveInWithoutLoss(uint PointsLevel,uint Limit);
   bool              TakeTrailing(double new_take);
   bool              ModifyPosition(double SL=0,double TP=0,ulong Magic=0);
   bool              OpenPosition(ENUM_ORDER_TYPE Type,double Lot,double SL=0,double TP=0);
   void              SetOptions(ulong f_mn,ushort f_deviation=1,string f_Comm=" ");
   bool              CheckOpenPositions(ulong Magic=0);
   bool              ClosePosition(uchar VolumePercent,ulong deviation=ULONG_MAX);
   PositionInfo      CurrentPosition;
   bool              OpenOrder(string Symb,ENUM_ORDER_TYPE Type,ENUM_ORDER_TYPE_TIME TTime,datetime Expiration,
                               double Lot,double OpenPrice,double SL=0,double TP=0,double StopPrice=0,string Comm="");
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CTradingManager::CTradingManager(void)
  {
   SymbInfo=NULL;
   PosInfo =NULL;
   Name=NULL;
   digits=0;
   Spread=0;
   point=0;
   StopLevel=0;
   FreezeLevel=0;
   Bid=0;
   Ask=0;
   LotVolume=0;
   MaxLot=0;
   MinLot=0;
   LotStep=0;
   LotsLimit=0;
   Positions=false;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CTradingManager::~CTradingManager(void)
  {
   if(PosInfo!=NULL) delete PosInfo;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
char CTradingManager::Initilize(CSymbolInfo  *ItemSymbInfo)
  {
   SymbInfo =ItemSymbInfo;
   if((PosInfo  =new CPositionInfo)==NULL) return(-1);
   if(SymbInfo!=NULL && SymbInfo.Select()) 
    {
     Name        =SymbInfo.Name();
     digits      =SymbInfo.Digits();
     StopLevel   =SymbInfo.StopsLevel();
     FreezeLevel =SymbInfo.FreezeLevel();
     SymbInfo.InfoDouble(SYMBOL_POINT,point);
     SymbInfo.InfoDouble(SYMBOL_VOLUME_MAX,MaxLot);
     SymbInfo.InfoDouble(SYMBOL_VOLUME_MIN,MinLot);
     SymbInfo.InfoDouble(SYMBOL_VOLUME_STEP,LotStep);
     UpdateSymbolInfo();
     }
    else return(-2);
   return(0);
  }
//+------------------------------------------------------------------+
//|Getting timely information on a trade instrument          |
//+------------------------------------------------------------------+

bool CTradingManager::UpdateSymbolInfo()
  {
   if(SymbInfo.RefreshRates())
     {
      Bid        =SymbInfo.Bid();
      Ask        =SymbInfo.Ask();
      Spread     =SymbInfo.Spread();
      return(true);
     }
   return(NULL);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CTradingManager::UpdatePositionInfo()
  {
   if(PosInfo.Select(Name))
     {
      CurrentPosition.Type            =PosInfo.PositionType();
      CurrentPosition.OpenPrice       =PosInfo.PriceOpen();
      CurrentPosition.StopLossPrice   =PosInfo.StopLoss();
      CurrentPosition.TakeProfitPrice =PosInfo.TakeProfit();
      CurrentPosition.Volume          =PosInfo.Volume();
      CurrentPosition.Profit          =PosInfo.Profit();
      CurrentPosition.MagicNumber     =PosInfo.Magic();
      CurrentPosition.UID             =PosInfo.Identifier();
      CurrentPosition.TimeOpen        =PosInfo.Time();
     }
  }
//+------------------------------------------------------------------+
//|Trade execution mode                                           |
//+------------------------------------------------------------------+

uchar CTradingManager::Execution()
  {
   switch(SymbInfo.TradeExecution())
     {
      case SYMBOL_TRADE_EXECUTION_REQUEST: return(REQUEST);
      case SYMBOL_TRADE_EXECUTION_INSTANT: return(INSTANT);
      case SYMBOL_TRADE_EXECUTION_MARKET:  return(MARKET);
      default: return(UNKNOW);
     }
  }
//+------------------------------------------------------------------+
//|Trade mode                                                    |
//+------------------------------------------------------------------+

uchar CTradingManager::TradeMode()
  {
   switch(SymbInfo.TradeMode())
     {
      case SYMBOL_TRADE_MODE_DISABLED:     return(TRADE_DISABLED);
      case SYMBOL_TRADE_MODE_LONGONLY:     return(TRADE_LONGONLY);
      case SYMBOL_TRADE_MODE_SHORTONLY:    return(TRADE_SHORTONLY);
      case SYMBOL_TRADE_MODE_CLOSEONLY:    return(TRADE_CLOSEONLY);
      case SYMBOL_TRADE_MODE_FULL:         return(TRADE_FULL);
      default:                             return(UNKNOW);
     }
  }
//+------------------------------------------------------------------+
//|Checking and correcting the distance                                |
//+------------------------------------------------------------------+
ushort CTradingManager::CorrectStops(ushort Step)
  {
   ushort new_step=ushort(StopLevel);
   ushort res=(Step<new_step)?new_step:Step;
   return(res);
  }
//+------------------------------------------------------------------+
//|Checking the minimum distance of the StopsLevel when modifying the StopLoss|
//|and/or TakeProfit orders for open positions                       |
//| Parameters:                                                       |
//|  -position type                                                    |
//|  -new price of the StopLoss order                                     |
//|  -new price of the TakeProfit order                                   |
//+------------------------------------------------------------------+  

bool  CTradingManager::StopsIsInvalid(ENUM_POSITION_TYPE OPEN_POSITION,double SL=0,double TP=0)
  {
   bool IsInvalid=NULL;
   switch(OPEN_POSITION)
     {
      case POSITION_TYPE_BUY:
         IsInvalid=((SL!=0 && NormalizeDouble(Bid-SL,digits)/point<StopLevel) || (TP!=0 && NormalizeDouble(TP-Bid,digits)/point<StopLevel));
         break;

      case POSITION_TYPE_SELL:
         IsInvalid=((SL!=0 && NormalizeDouble(SL-Ask,digits)/point<StopLevel) || (TP!=0 && NormalizeDouble(Ask-TP,digits)/point<StopLevel));
         break;
     }
   return(IsInvalid);
  }
//+------------------------------------------------------------------+
//|Checking the minimum distance of the StopsLevel when opening a Sell/Buy   |
//|position and setting different pending orders                           |
//|Parameters:                                                        |
//|  - order type                                                    |
//|  - opening price                                                 |
//|  - StopLoss order price                                          |
//|  - TakeProfit order price                                        |
//|  - price at which pending orders |
//|    StopLimit or BuyLimit will be placed at the given OpenPrice(StopLimit) price |
//+------------------------------------------------------------------+  

bool  CTradingManager::StopsIsInvalid(ENUM_ORDER_TYPE OPEN_ORDER,double OpenPrice=0,double SL=0,double TP=0,double StopPrice=0)
  {
   bool IsInvalid=NULL;
   switch(OPEN_ORDER)
     {
      case ORDER_TYPE_BUY:
        {
         IsInvalid=((SL!=0 && NormalizeDouble(Bid-SL,digits)/point<StopLevel) || 
                    (TP!=0 && NormalizeDouble(TP-Ask,digits)/point<StopLevel));
        }
      break;

      case ORDER_TYPE_BUY_LIMIT:
        {
         IsInvalid=(NormalizeDouble(Ask-OpenPrice,digits)/point<StopLevel || 
                    (SL!=0 && NormalizeDouble(OpenPrice-SL,digits)/point<StopLevel) ||
                    (TP!=0 && NormalizeDouble(TP-OpenPrice,digits)/point<StopLevel));
        }
      break;

      case ORDER_TYPE_BUY_STOP:
        {
         IsInvalid=(NormalizeDouble(OpenPrice-Ask,digits)/point<StopLevel || 
                    (SL!=0 && NormalizeDouble(OpenPrice-SL,digits)/point<StopLevel) ||
                    (TP!=0 && NormalizeDouble(TP-OpenPrice,digits)/point<StopLevel));
        }
      break;

      case ORDER_TYPE_BUY_STOP_LIMIT:
        {
         IsInvalid=(NormalizeDouble(StopPrice-Ask,digits)/point<StopLevel || 
                    NormalizeDouble(StopPrice-OpenPrice,digits)/point<StopLevel || 
                    (SL!=0 && NormalizeDouble(OpenPrice-SL,digits)/point<StopLevel) ||
                    (TP!=0 && NormalizeDouble(TP-OpenPrice,digits)/point<StopLevel));
        }
      break;

      case ORDER_TYPE_SELL:
        {
         IsInvalid=((SL!=0 && NormalizeDouble(SL-Ask,digits)/point<StopLevel) || 
                    (TP!=0 && NormalizeDouble(Bid-TP,digits)/point<StopLevel));
        }
      break;

      case ORDER_TYPE_SELL_LIMIT:
        {
         IsInvalid=(NormalizeDouble(OpenPrice-Bid,digits)/point<StopLevel || 
                    (SL!=0 && NormalizeDouble(SL-OpenPrice,digits)/point<StopLevel) ||
                    (TP!=0 && NormalizeDouble(OpenPrice-TP,digits)/point<StopLevel));
        }
      break;

      case ORDER_TYPE_SELL_STOP:
        {
         IsInvalid=(NormalizeDouble(Bid-OpenPrice,digits)/point<StopLevel || 
                    (SL!=0 && NormalizeDouble(SL-OpenPrice,digits)/point<StopLevel) ||
                    (TP!=0 && NormalizeDouble(OpenPrice-TP,digits)/point<StopLevel));
        }
      break;

      case ORDER_TYPE_SELL_STOP_LIMIT:
        {
         IsInvalid=(NormalizeDouble(Bid-StopPrice,digits)/point<StopLevel || 
                    NormalizeDouble(OpenPrice-StopPrice,digits)/point<StopLevel || 
                    (SL!=0 && NormalizeDouble(SL-OpenPrice,digits)/point<StopLevel) ||
                    (TP!=0 && NormalizeDouble(OpenPrice-TP,digits)/point<StopLevel));
        }
      break;
     }
   return(IsInvalid);
  }
//+------------------------------------------------------------------+
//|Checking the open position freezing distance                     |
//+------------------------------------------------------------------+

bool CTradingManager::PositionIsFrozen(ENUM_POSITION_TYPE OPEN_POSITION,double SL,double TP)
  {
   bool Frozen=NULL;
   switch(OPEN_POSITION)
     {
      case POSITION_TYPE_BUY:
         Frozen=((TP!=0 && NormalizeDouble(TP-Bid,digits)/point<FreezeLevel) || 
                 (SL!=0 && NormalizeDouble(Bid-SL,digits)/point<FreezeLevel));
      break;

      case POSITION_TYPE_SELL:
         Frozen=((TP!=0 && NormalizeDouble(Ask-TP,digits)/point<FreezeLevel) || 
                 (SL!=0 && NormalizeDouble(SL-Ask,digits)/point<FreezeLevel));
      break;
     }

   return(Frozen);
  }
//+------------------------------------------------------------------+
//|Simple trailing stop                                        |
//+------------------------------------------------------------------+

void CTradingManager::SimpleTrailing(int BuyStep,int SellStep,int deviation=0,ulong Magic=0)
  {

   bool   Modified=NULL;
   double sl=CurrentPosition.StopLossPrice;
   double tp=CurrentPosition.TakeProfitPrice;
   double op=CurrentPosition.OpenPrice;
   double new_sl=NULL;

   switch(CurrentPosition.Type)
     {
      case POSITION_TYPE_BUY:
        {
         if(BuyStep!=0)
           {
            if(Bid-op>BuyStep*point)
              {
               if(sl<Bid-(BuyStep+deviation)*point)
                 {
                  new_sl=NormalizeDouble(Bid-BuyStep*point,digits);
                  Modified=(!PositionIsFrozen(POSITION_TYPE_BUY,sl,tp) && !StopsIsInvalid(POSITION_TYPE_BUY,new_sl));
                 }
              }
           }
        }
      break;

      case POSITION_TYPE_SELL:
        {
         if(SellStep!=0)
           {
            if(op-Ask>SellStep*point)
              {
               if(sl>Ask+(SellStep+deviation)*point)
                 {
                  new_sl=NormalizeDouble(Ask+SellStep*point,digits);
                  Modified=(!PositionIsFrozen(POSITION_TYPE_SELL,sl,tp) && !StopsIsInvalid(POSITION_TYPE_SELL,new_sl));
                 }
              }
           }
        }
      break;
     }

   if(Modified && ModifyPosition(new_sl,tp,Magic))
     {
      // Print("StopLoss has been modified-",DoubleToString(new_sl,digits));
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CTradingManager::SimpleTrailing(double new_sl,ulong Magic=0)
  {
   bool   Modified=NULL;
   double sl=CurrentPosition.StopLossPrice;
   double tp=CurrentPosition.TakeProfitPrice;
   double op=CurrentPosition.OpenPrice;

   switch(CurrentPosition.Type)
     {
      case POSITION_TYPE_BUY:
        {
         if(sl!=0 && sl<new_sl)
           {
            if(!(Modified=(!PositionIsFrozen(POSITION_TYPE_BUY,sl,tp) && !StopsIsInvalid(POSITION_TYPE_BUY,new_sl)))) return(false);
           }
         else return(false);
        }
      break;

      case POSITION_TYPE_SELL:
        {

         if(sl!=0 && sl>new_sl)
           {
            if(!(Modified=(!PositionIsFrozen(POSITION_TYPE_SELL,sl,tp) && !StopsIsInvalid(POSITION_TYPE_SELL,new_sl)))) return(false);
           }
         else return(false);
        }
      break;
     }

   if(Modified && ModifyPosition(new_sl,tp,Magic))
     {
      //Print("StopLoss has been modified-",DoubleToString(new_sl,digits));
      return(true);
     }
   return(false);
  }
//+------------------------------------------------------------------+
//|Getting the technical indicator value                        |
//+------------------------------------------------------------------+

double CTradingManager::GetIndicatorValue(int handle)
  {
   double buffer[];
   if(handle!=INVALID_HANDLE)
     {
      if(CopyBuffer(handle,0,1,1,buffer)<0) return(0);
      else return(buffer[0]);
     }
   return(0);
  }
//+------------------------------------------------------------------+
//|Indicator trailing stop (SAR,MA....)                           |
//| is advisable to be used when a new bar appears                |
//| Parameters:                                                       |
//|  handle - indicator handle                                       |
//|  Symb   - instrument                                             |
//|  SFrame - period(current period by default)                           | 
//|  Magic  - magic number(if applicable)                    |
//|* before the call, you should call CheckOpenPositions(Symb,Magic) |
//+------------------------------------------------------------------+

void CTradingManager::TATrailing(int handle,ENUM_TIMEFRAMES SFrame=PERIOD_CURRENT,ulong Magic=0)
  {
   double new_sl=NormalizeDouble(GetIndicatorValue(handle),digits);
   if(new_sl!=0)
     {
      double High[];
      double Low[];
      double tp=CurrentPosition.TakeProfitPrice;
      double sl=CurrentPosition.StopLossPrice;
      bool   Modified=NULL;
      switch(CurrentPosition.Type)
        {
         case POSITION_TYPE_BUY:
            if(CopyLow(Name,SFrame,1,1,Low)>0)
              {
               if(new_sl<Low[0] && sl>0 && new_sl>sl)
                 {
                  Modified=(!StopsIsInvalid(POSITION_TYPE_BUY,new_sl) && !PositionIsFrozen(POSITION_TYPE_BUY,sl,tp));
                 }
              }
            break;

         case POSITION_TYPE_SELL:
            if(CopyHigh(Name,SFrame,1,1,High)>0)
              {
               if(new_sl>High[0] && sl>0 && new_sl<sl)
                 {
                  Modified=(!StopsIsInvalid(POSITION_TYPE_SELL,new_sl) && !PositionIsFrozen(POSITION_TYPE_SELL,sl,tp));
                 }
              }
            break;
        }
      if(Modified && ModifyPosition(new_sl,tp,Magic))
        {
         // Print("StopLoss has been modified-",DoubleToString(new_sl,digits));
        }
     }

  }
//+------------------------------------------------------------------+
//|Moving the StopLoss order to the break-even level             |
//| PointsLevel-the limit at which the SL will be moved by  |
//| the Limit distance                                                 |
//|* before the call, you should call CheckOpenPositions(Symb,Magic) |
//+------------------------------------------------------------------+

bool CTradingManager::MoveInWithoutLoss(uint PointsLevel,uint Limit)
  {
   bool Modified=false;
   double tp=CurrentPosition.TakeProfitPrice;
   double sl=CurrentPosition.StopLossPrice;
   double op=CurrentPosition.OpenPrice;
   double new_sl=0.0;
   double Length=0.0;

   switch(CurrentPosition.Type)
     {
      case POSITION_TYPE_BUY:
        {
         Length=(sl>op)?NormalizeDouble(sl-op,digits):-NormalizeDouble(op-sl,digits);
         if(sl==0 || Length<Limit*point)
           {
            if(Bid>op && NormalizeDouble(Bid-op,digits)>=PointsLevel*point)
              {
               new_sl=op+Limit*point;
               if(new_sl!=sl)
                 {
                  if(!(Modified=(!StopsIsInvalid(POSITION_TYPE_BUY,new_sl) && !PositionIsFrozen(POSITION_TYPE_BUY,sl,tp)))) return(false);
                 }
               else return(false);
              }
            else return(false);
           }
        }
      break;

      case POSITION_TYPE_SELL:
        {
         Length=(sl>op)?-NormalizeDouble(sl-op,digits):NormalizeDouble(op-sl,digits);
         if(sl==0 || Length<Limit*point)
           {

            if(Ask<op && NormalizeDouble(op-Ask,digits)>PointsLevel*point)
              {
               new_sl=op-Limit*point;
               if(new_sl!=sl)
                 {
                  if(!(Modified=(!StopsIsInvalid(POSITION_TYPE_SELL,new_sl) && !PositionIsFrozen(POSITION_TYPE_SELL,sl,tp)))) return(false);
                 }
               else return(false);
              }
            else return(false);
           }
        }
      break;
     }
   if(Modified && ModifyPosition(new_sl,tp))
     {
      // Print("StopLoss has been moved in without a loss-",DoubleToString(new_sl,digits));
      return(true);
     }
   return(false);
  }
//+------------------------------------------------------------------+
//|Setting the magic number, slippage and comment            |
//+------------------------------------------------------------------+

void  CTradingManager::SetOptions(ulong f_mn=0,ushort f_deviation=1,string  f_Comm=" ")
  {
   UpdateSymbolInfo();
   m_magic=f_mn;
   m_deviation=(ulong)f_deviation*Spread;
   m_Comm=f_Comm;
  }
//+------------------------------------------------------------------+
//|Checking for open positions                                 |
//+------------------------------------------------------------------+
bool CTradingManager::CheckOpenPositions(ulong Magic=0)
  {
   if(PosInfo.Select(Name))
     {
      Positions=true;
      if(PosInfo.Magic()==Magic || Magic==0)
        {
         UpdatePositionInfo();
         return(true);
        }
     }
   Positions=false;
   return(false);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+

bool CTradingManager::TakeTrailing(double new_take)
  {
   bool Modified=false;
   double tp=CurrentPosition.TakeProfitPrice;
   double sl=CurrentPosition.StopLossPrice;
   double op=CurrentPosition.OpenPrice;

   switch(CurrentPosition.Type)
     {
      case POSITION_TYPE_BUY:
         if(new_take>op && new_take!=tp)
           {
            if(!(Modified=(!StopsIsInvalid(POSITION_TYPE_BUY,0,new_take) && !PositionIsFrozen(POSITION_TYPE_BUY,sl,tp)))) return(false);
           }
         else return(false);
         break;

      case POSITION_TYPE_SELL:

         if(new_take<op && new_take!=tp)
           {
            if(!(Modified=(!StopsIsInvalid(POSITION_TYPE_SELL,0,new_take) && !PositionIsFrozen(POSITION_TYPE_SELL,sl,tp)))) return(false);
           }
         else return(false);
         break;
     }
   if(Modified && ModifyPosition(sl,new_take))
     {
      return(true);
     }
   return(false);
  }
//+------------------------------------------------------------------+
//|Modifying market positions                                      |
//|* before the call, you should call CheckOpenPositions(Magic)      |
//+------------------------------------------------------------------+

bool CTradingManager::ModifyPosition(double SL=0,double TP=0,ulong Magic=0)
  {
   bool Done=NULL;
   if(CurrentPosition.MagicNumber==Magic || Magic==0)
     {
      do
        {
         bool Continue=NULL;

         Trade_request.action    =TRADE_ACTION_SLTP;
         Trade_request.symbol    =Name;
         Trade_request.sl        =SL;
         Trade_request.tp        =TP;
         Trade_request.deviation =m_deviation;

         if((Done=SendRequest(Continue))) UpdatePositionInfo();
         else
           {
            if(Continue) Done=false;
            else return(false);
           }
        }
      while(!Done);
     }
   return(Done);
  }
//+------------------------------------------------------------------+
//|Checking for the matching direction                                |
//+------------------------------------------------------------------+

bool CTradingManager::CheckCoincidence(ENUM_ORDER_TYPE OrderType,ENUM_POSITION_TYPE PositionType)
  {
   bool coincide=NULL;
   coincide=((PositionType==POSITION_TYPE_SELL && (OrderType==ORDER_TYPE_SELL || OrderType==ORDER_TYPE_SELL_LIMIT || OrderType==ORDER_TYPE_SELL_STOP || OrderType==ORDER_TYPE_SELL_STOP_LIMIT)) || 
             (PositionType==POSITION_TYPE_BUY && (OrderType==ORDER_TYPE_BUY || OrderType==ORDER_TYPE_BUY_LIMIT || OrderType==ORDER_TYPE_BUY_STOP || OrderType==ORDER_TYPE_BUY_STOP_LIMIT)));
   return(coincide);
  }
//+------------------------------------------------------------------+
//|Adjusting the lot volume required for the opening                   |
//+------------------------------------------------------------------+

double  CTradingManager::CorrectLot(double CurrentLot,uint &IterationCount,double &rests)
  {
   if(CurrentLot<=MaxLot) return(CurrentLot);
   rests=CurrentLot-MaxLot;
   IterationCount=(int)MathCeil(CurrentLot/MaxLot);
   Print("Position on "+Name+" will be opened by "+IntegerToString(IterationCount)+" parts.");
   return(MaxLot);
  }
//+------------------------------------------------------------------+
//|Opening a market position                                         |
//+------------------------------------------------------------------+

bool CTradingManager::OpenPosition(ENUM_ORDER_TYPE Type,double Lot,double SL=0,double TP=0)
  {
   bool   Done=NULL;
   double Volume=Lot;

   if(Type!=ORDER_TYPE_BUY && Type!=ORDER_TYPE_SELL) return(Done);
   switch(TradeMode()) //-- trade mode
     {
      case TRADE_DISABLED:
         Print(Name+": Trading symbol on the disabled!");
         return(Done);

      case TRADE_CLOSEONLY:
        {
         Print(Name+": Allowed only close positions!");
         if(Positions) // if there is an open position
           {
            if(CheckCoincidence(Type,CurrentPosition.Type))  return(Done);               // if the position to be opened coincides with the existing position, do not open anything
            else                                                                         // otherwise compare their volumes
              {
               Volume=(CurrentPosition.Volume<Volume) ? CurrentPosition.Volume : Volume;  // if the current position volume is less, open a position with the same volume
              }
           }
         else return(Done);
        }
      break;

      case TRADE_LONGONLY:
        {
         if(Type!=ORDER_TYPE_BUY)
           {
            Print(Name+": Allowed only long positions!");
            return(Done);
           }
        }
      break;

      case TRADE_SHORTONLY:
        {
         if(Type!=ORDER_TYPE_SELL)
           {
            Print(Name+": Allowed only short positions!");
            return(Done);
           }
        }
      break;

      default: break;
     }

   bool   NeedModify=NULL;
//--- checking stopslevel
   if(!StopsIsInvalid(Type,0,SL,TP))
     {
      double CurrentLot=Volume;
      double rests=0.0;
      double VolumeForOpen=0.0;
      int    IterationCount=0;

      uchar  EM=Execution();  //--execution type
      do
        {
         double OpenPrice=(Type==ORDER_TYPE_BUY) ? Ask : Bid;
         switch(EM)
           {
            case MARKET:
              {
               NeedModify=(SL!=0 || TP!=0);
              }
            break;

            case REQUEST:
            case INSTANT:
              {
               Trade_request.price       =OpenPrice;
               Trade_request.sl          =SL;
               Trade_request.tp          =TP;
              }
            break;

            case UNKNOW:  return(Done);
           }

         bool Continue=NULL;
         //--- setting request
         Trade_request.action       =TRADE_ACTION_DEAL;
         Trade_request.type         =Type;
         Trade_request.symbol       =Name;
         Trade_request.volume       =VolumeForOpen=(rests==0)?CorrectLot(CurrentLot,IterationCount,rests):CorrectLot(rests,IterationCount,rests);
         Trade_request.type_filling =ORDER_FILLING_FOK;
         Trade_request.deviation    =m_deviation;
         Trade_request.magic        =m_magic;
         Trade_request.comment      =m_Comm;
         //---

         if((Done=SendRequest(Continue)))
           {
            if(IterationCount>0) IterationCount--;
            if(!(Done=(IterationCount==0))) Sleep(500);
           }
         else
           {
            if(Continue) rests+=VolumeForOpen;
            else return(false);
           }
        }
      while(!Done);
     }
   else Print("Invalid stops!");

   if(Done && NeedModify)
     {
      // it may need Sleep(10000) here
      if(PosInfo.Select(Name)) ModifyPosition(SL,TP,m_magic);
     }
   return(Done);
  }
//+------------------------------------------------------------------+
//|Adjusting the lot volume required for the closing                   |
//+------------------------------------------------------------------+

double  CTradingManager::CorrectLot(double CurrentLot,uchar VolumePercent,uint &IterationCount,double &rests)
  {
   if(CurrentLot==MinLot) return(CurrentLot);

   double k=NULL;
   double NeedVolume=NULL;
   double part=NULL;
   uchar  Percent=VolumePercent;

   if(Percent==0 || Percent>100) Percent=100;      // close completely

   if(Percent!=100)
     {
      if(MinLot==0) MinLot=0.1;
      if(MaxLot==0) MaxLot=100;
      if(LotStep>0) k=1/LotStep; else k=1/MinLot;
      part=(CurrentLot*Percent)/100;
      NeedVolume=MathFloor(part*k)/k;
      if(NeedVolume<=MinLot || CurrentLot-NeedVolume<=MinLot) return(CurrentLot);
     }
   else NeedVolume=CurrentLot;

   if(NeedVolume>MaxLot)
     {
      rests=NeedVolume-MaxLot;
      IterationCount=(int)MathCeil(NeedVolume/MaxLot);
      Print("Position on "+Name+" will be closed by "+IntegerToString(IterationCount)+" parts.");
      return(MaxLot);
     }
   return(NeedVolume);
  }
//+------------------------------------------------------------------+
//|Closing a position                                                  |
//|* before the call, you should call CheckOpenPositions(Magic)      |
//+------------------------------------------------------------------+

bool CTradingManager::ClosePosition(uchar VolumePercent,ulong deviation=ULONG_MAX)
  {

   bool   Closed=false;
   bool   Done  =false;
   uint   IterationCount=0;
   if(!PositionIsFrozen(PosInfo.PositionType(),PosInfo.StopLoss(),PosInfo.TakeProfit()))
     {
      double VolumeForClose=0.0;
      double CurrentVolume=CurrentPosition.Volume;
      double rests=0.0;                                                    // the volume excess, if the volume at the closing exceeds SYMBOL_VOLUME_MAX
      bool   Continue=false;
      do
        {
         VolumeForClose=(rests==0) ? CorrectLot(CurrentVolume,VolumePercent,IterationCount,rests) : CorrectLot(rests,100,IterationCount,rests);
         if(PosInfo.Type()==POSITION_TYPE_BUY)
           {
            Done=OpenPosition(ORDER_TYPE_SELL,VolumeForClose);
           }
         else
           {
            Done=OpenPosition(ORDER_TYPE_BUY,VolumeForClose);
           }

         if(Done)
           {
            if(IterationCount>0)
              {
               IterationCount--;
               if(!(Done=(IterationCount==0))) Sleep(500);
              }
            else return(true);
           }
         else
           {
            return(false);
           }
        }
      while(!Done);
     }
   return(Closed);
  }
//+------------------------------------------------------------------+
//|Setting pending orders                                      |
//+------------------------------------------------------------------+

bool CTradingManager::OpenOrder(string Symb,
                                ENUM_ORDER_TYPE Type,
                                ENUM_ORDER_TYPE_TIME TTime,
                                datetime Expiration,
                                double Lot,
                                double OpenPrice,//  order setting price: for orders of the ...STOP_LIMIT type, the setting price is equal to the price of Limit orders,
                                double SL=0,
                                double TP=0,
                                double StopPrice=0,//  price at which ...Limit orders are set
                                string Comm="")
  {

   bool Done=NULL;

   if(Type==ORDER_TYPE_BUY && Type==ORDER_TYPE_SELL) return(Done);

   double Volume=Lot;
   switch(TradeMode()) //-- trade mode
     {
      case TRADE_DISABLED:
         Print(Name+": Trading symbol on the disabled!");
         return(Done);

      case TRADE_CLOSEONLY:
        {
         Print(Name+": Allowed only close positions!");
         if(Positions) // if there is an open position
           {
            if(CheckCoincidence(Type,CurrentPosition.Type))  return(Done);               // if the type of the order to be opened coincides with the existing position, do not set anything
            else                                                                         // otherwise compare their volumes
              {
               Volume=(CurrentPosition.Volume<Volume) ? CurrentPosition.Volume : Volume;  // if the current position volume is less, set the order with the same volume
              }
           }
         else return(Done);
        }
      break;

      case TRADE_LONGONLY:
        {
         if(Type!=ORDER_TYPE_BUY)
           {
            Print(Name+": Allowed only long positions!");
            return(Done);
           }
        }
      break;

      case TRADE_SHORTONLY:
        {
         if(Type!=ORDER_TYPE_SELL)
           {
            Print(Name+": Allowed only short positions!");
            return(Done);
           }
        }
      break;

      default: break;
     }
//--- checking stopslevel
   if(!StopsIsInvalid(Type,OpenPrice,SL,TP,StopPrice))
     {
      do
        {
         bool Continue=NULL;
         //--- setting request
         Trade_request.action       =TRADE_ACTION_PENDING;
         Trade_request.type         =Type;
         Trade_request.symbol       =Name;
         Trade_request.volume       =Volume;
         Trade_request.price        =OpenPrice;
         Trade_request.stoplimit    =StopPrice;
         Trade_request.sl           =SL;
         Trade_request.tp           =TP;
         Trade_request.type_filling =ORDER_FILLING_FOK;
         Trade_request.magic        =m_magic;
         Trade_request.type_time    =TTime;
         Trade_request.expiration   =Expiration;
         Trade_request.comment      =Comm;
         //---
         if(!(Done=SendRequest(Continue)))
           {
            if(!Continue) return(false);
           }
        }
      while(!Done);
     }
   else Print("Invalid stops!");
   return(Done);
  }
//+------------------------------------------------------------------+
//|Sending request to the trade server                               |
//+------------------------------------------------------------------+

bool CTradingManager::SendRequest(bool &ContinueRequest)
  {
   uint   CheckRetCode=NULL;
   bool   Done        =NULL;

   if(OrderCheck(Trade_request,Check_result)) // checking the accuracy of filling the trade request form
     {
      OrderSend(Trade_request,Trade_result);
      switch(Trade_result.retcode)
        {
         //---success
         case TRADE_RETCODE_PLACED:
         case TRADE_RETCODE_DONE:
         case TRADE_RETCODE_DONE_PARTIAL:
           {
            Done=true;
           }
         break;
         //---requote
         case TRADE_RETCODE_REQUOTE:
         //---prices changed
         case TRADE_RETCODE_PRICE_CHANGED:
           {
            Bid=Trade_result.bid;
            Ask=Trade_result.ask;
            ContinueRequest=true;            // repeat request
           }
         break;
         //---no price quotes
         case TRADE_RETCODE_PRICE_OFF:
           {
            Print("No quotes for query processing!");
           }
         break;

         case TRADE_RETCODE_TOO_MANY_REQUESTS:
           {
            Print("Many requests!");
            ContinueRequest=true;            // repeat request
           }
         break;

         //--Market closed
         case TRADE_RETCODE_MARKET_CLOSED:
           {
            Print("Market closed!");
           }
         break;

         default: Print("Retcode",Check_result.retcode); break;
        }
     }
   else
     {
      switch(Check_result.retcode)
        {
         case TRADE_RETCODE_INVALID_VOLUME:
           {
            Print("Invalid volume!");
           }
         break;

         case TRADE_RETCODE_INVALID_PRICE:
           {
            Print("Invalid price!");
           }
         break;

         case TRADE_RETCODE_NO_MONEY: break;

         default:/* Print("Check retcode: ",Check_result.retcode);*/ break;
        }
     }
   return(Done);
  }
//+------------------------------------------------------------------+
