//+------------------------------------------------------------------+
//|                                                      AK47_A1.mq4 |
//|                               Copyright © 2007, RedPlumSoft, Inc |
//|                                       http://www.redplumsoft.com |
//+------------------------------------------------------------------+

#property copyright "Copyright © 2007, RedPlumSoft, Inc"
#property link      "http://www.redplumsoft.com"

extern double Lots               = 0;        // 0 - auto 
extern double LotsRiskReductor   = 1;        // 1 to *10
extern int    MaxOrders          = 1;        // 1 to *20
extern int    MaxLots            = 100;

extern double TakeProfit         = 100;      // 10 to *100
extern double StopLoss           = 0;        // 0 - none
extern double TrailingStop       = 50;       // 0 - none

extern double SpanGator          = 0.5;      // 0 to *5
extern int    SlipPage           = 3;

//extern double TrendNonFlat       = 10;     // trend points to recognize non-flat
//extern int    TrendMaxPeriods    = 3;      // maximum trend age to enter in   
//extern double TrailingTake       = 0;      // 0 - none
//extern double TTLDays            = 0;      // 0 - no limit
//extern double SafetyGapDemarker  = 0.2;    // 0 to 0.2
//extern double SafetyGapWPR       = 0;      // 0 to 0.3

#define DIRECTION_NONE 0
#define DIRECTION_BUY 1
#define DIRECTION_SELL -1

/* -------------------------------------------------
ranges of -WPR 
   < 20 - OverBuy
   > 80 - OverSell

strategy of WPR usage:  
   I.  Do not Buy if OverSell, do not Sell if OverBuy
   II. Buy or Sell if WPR in middle

SafetyGapWPR used to avoid reach dangerous limits
------------------------------------------------- */

// -------------------------------------------------
// expert initialization function                                   |
// -------------------------------------------------
int init()
  {
   return(0);
  }
  
// -------------------------------------------------
// expert deinitialization function                                 
// -------------------------------------------------
int deinit()
  {
   return(0);
  }
  
// -------------------------------------------------
// expert global variables
// -------------------------------------------------
  
double gatorv_jaw, gatorv_teeth, gatorv_lips; 
double fractalv_lower, fractalv_upper;

double demarkerv[], wprv[];
static int OrderMagic = 131313; 
   
// =================================================
// start the expert
// =================================================

int start()
  {
   //-------------------------------------
   // verify state
   //-------------------------------------
   if(!IsTradeAllowed()) return(0);
   if(!IsConnected()) return(0);
   if(IsStopped()) return(0);
     
   if(IsTradeContextBusy()) 
     {
      Print("Trade context is busy!");
      return(0);
     } 

   //-------------------------------------
   // initial operations
   //-------------------------------------

   // check datetime, params and prepare indicators
   if(!IsDateTimeEnabled(TimeCurrent())) return(0);
   if(!CheckParams()) return(0);  
   if(!PrepareIndicators()) return(0);

   //PrintDebugInfo()

   //-------------------------------------
   // analize current situation
   //-------------------------------------

   // how many orders already open?
   int curOrder, numOrders = OrdersTotal();
   
   //-------------------------------------
   // check open orders status 
   //-------------------------------------
   
   if(numOrders > 0) 
     { 
      for(curOrder = 0; curOrder < numOrders; curOrder++) 
        {
         if(GetOrderByPos(curOrder))
           {
            if(TradeSignalCloseOrder()) 
              CloseOrder();
            else
              {
               if(TrailingStop > 0) TrailOrderStop();
              }
           }
        }
      
      //if(numOrders >= MaxOrders) 
      return(0);
     } 

   //-------------------------------------
   // try to open new orders if possible
   //-------------------------------------

   //is there any trade signal ??
   int tradesignal = TradeSignalOpenOrder();
   if(tradesignal == DIRECTION_NONE) return(0);
   
   double lots_vol = CalcLotsVolume();
   // check there is enough money   
   if(lots_vol == 0 || !CheckAccount(lots_vol)) return(0);   
   
   int ticket = OpenOrder(tradesignal, lots_vol);
   return(0);
  }
     
// =================================================
// initializing and checking functions
// =================================================

// -------------------------------------------------
// bool CheckParams()
// -------------------------------------------------
bool CheckParams()
  {
   if(Bars < 100)
     {
      Print("Bars less than 100");
      return(false);  
     }
   
   if(TakeProfit< 10)
     {
      Print("TakeProfit is less than 10");
      return(false);  
     }  
   
   if(Lots == 0 && LotsRiskReductor < 1)
     {
      Print("LotsRiskReductor is less than 1");
      return(false);  
     }  

   return(true);  
  }

// -------------------------------------------------
// bool CheckAccount()
// -------------------------------------------------
bool CheckAccount(double LotsVolume)
  {
   bool result = AccountFreeMargin() > (1000 * LotsVolume);
   
   if(!result) 
     Print("No money to open more orders.", 
       " Free Margin = ", AccountFreeMargin(),
       " Balance = ", AccountBalance());
   return(result);  
  }

// -------------------------------------------------
// CalcLotsVolume()
// -------------------------------------------------
double CalcLotsVolume()
  {
   double lv;
   
   if(Lots > 0) 
      lv = Lots;
   else    
      lv = 0.1 * MathFloor(AccountEquity() / MaxOrders / (LotsRiskReductor * AccountLeverage()));

   if(lv < 0.1) 
      lv = 0.1; 
   else if(lv > MaxLots) 
      lv = MaxLots; 
   
   return(lv);
  } 

// -------------------------------------------------
// bool PrepareIndicators()
// -------------------------------------------------
bool PrepareIndicators()
  {
   int ip, ib, lookback; 
   double dm, fr;

   // Alligator   

   gatorv_jaw = iAlligator(NULL, 0, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_MEDIAN, MODE_GATORJAW, 0);
   if(IsError("Alligator Jaw")) return(false);

   gatorv_teeth = iAlligator(NULL, 0, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_MEDIAN, MODE_GATORTEETH, 0);
   if(IsError("Alligator Teeth")) return(false);

   gatorv_lips = iAlligator(NULL, 0, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_MEDIAN, MODE_GATORLIPS, 0);
   if(IsError("Alligator Lips")) return(false);

   // Fractals

   lookback = 3; // look for fresh markers only
   
   fractalv_lower = 0;
   fractalv_upper = 0;
      
   for(ib=0; ib<=lookback; ib++)
     {
      // Lower fractals 
      fr = iFractals(NULL, 0, MODE_LOWER, ib);
      if(IsError("Lower Fractals")) return(false);
      if(fr != 0) fractalv_lower = fr;

      // Upper fractals 
      fr = iFractals(NULL, 0, MODE_UPPER, ib);
      if(IsError("Upper Fractals")) return(false);
      if(fr != 0) fractalv_upper = fr;
     }

   // DeMArker and WPR

   lookback = 0;

   ArrayResize(demarkerv, lookback+1);
   ArrayResize(wprv, lookback+1);

   for(ib=0; ib<=lookback; ib++)
     {
      // DeMarker 
      demarkerv[ib] = iDeMarker(NULL, 0, 13, ib);
      if(IsError("DeMarker")) return(false);

      // William's Percent Rate * -1
      wprv[ib] = -iWPR(NULL, 0, 14, ib+1) / 100; 
      if(IsError("WPR")) return(false);
     }
   
   return(true);
  }

// =================================================
// trade signalling functions
// =================================================

// -------------------------------------------------
// TradeSignalOpenOrder()
// -------------------------------------------------
int TradeSignalOpenOrder()
  {
   if(!IsGatorActive()) return(DIRECTION_NONE);

   if(WasWPROverBuy() || WasWPROverSell()) return(DIRECTION_NONE);

   if(IsFractalLower() && WasDemarkerHigh() //&& IsDerivationPlus()
      ) return(DIRECTION_BUY);

   if(IsFractalUpper() && WasDemarkerLow() //&& IsDerivationMinus()
      ) return(DIRECTION_SELL);  
   
   return(DIRECTION_NONE);  
  }

// -------------------------------------------------
// TradeSignalCloseOrder
// -------------------------------------------------
bool TradeSignalCloseOrder()
  {
   return(!IsOrderProfitable()); //|| IsOrderExpired() 
  } 

// -------------------------------------------------
// IsDateTimeEnabled
// -------------------------------------------------
bool IsDateTimeEnabled(datetime t)
  {
   int day_y=TimeDayOfYear(t);
   int day_w=TimeDayOfWeek(t);

   return(day_w > 0 && day_w < 6 && day_y > 7 && day_y < 360);
  }
    
// -------------------------------------------------
// IsGatorActive()
// -------------------------------------------------
bool IsGatorActive()
  {
   return(gatorv_lips-gatorv_teeth >= SpanGator * Point
       && gatorv_teeth-gatorv_jaw >= SpanGator * Point 
       && gatorv_lips-gatorv_jaw >= SpanGator * Point);
  }        

// -------------------------------------------------
// IsFractalLower()
// -------------------------------------------------
bool IsFractalLower()
  {
   return(fractalv_lower != 0); 
   //&&(fractalv_lower < gatorv_jaw || fractalv_lower > gatorv_lips));
      //fractalv_lower > gatorv_teeth));  /*** NEW ***/
     // && fractalv_upper == 0);
  } 
  
// -------------------------------------------------
// IsFractalUpper()
// -------------------------------------------------
bool IsFractalUpper()
  {
   return(fractalv_upper != 0);
   // && (fractalv_upper > gatorv_lips || fractalv_upper < gatorv_jaw));
         //fractalv_upper < gatorv_teeth));  /*** NEW ***/ 
   
   // && fractalv_lower == 0); 
  }      
     
// -------------------------------------------------
//  IsOrderProfitable 
// -------------------------------------------------
bool IsOrderProfitable() 
  {
   return(true); // TODO : analyze profitability
  }  
    
// -------------------------------------------------
// WasDemarkerLow()
// -------------------------------------------------
bool WasDemarkerLow()
  {
   return(ArrayMinValue(demarkerv) < 0.5); 
  } 

// -------------------------------------------------
// WasDemarkerHigh()
// -------------------------------------------------
bool WasDemarkerHigh()
  {
   return(ArrayMaxValue(demarkerv) > 0.5); 
  } 
  
// -------------------------------------------------
// WasWPROverBuy()
// -------------------------------------------------
bool WasWPROverBuy()
  {
   return(ArrayMinValue(wprv) <= 0.25); 
  }      
      
// -------------------------------------------------
// WasWPROverSell()
// -------------------------------------------------
bool WasWPROverSell()
  {
   return(ArrayMaxValue(wprv) >= 0.75); 
  }   

// =================================================
// order functions
// =================================================

// -------------------------------------------------
// OpenOrder
// -------------------------------------------------
int OpenOrder(int direction, double numlots, string comment = "") 
  { 
   double price, stop_loss=0, take_profit=0;
   
   //calc order bounds 
   price = PriceOpen(direction);
   take_profit = price + TakeProfit * Point * direction; 
   
   if(StopLoss > 0)
     {
      stop_loss = PriceClose(direction) - StopLoss * Point * direction; 
      //stop_loss = stop_loss * (1/(numlots/0.1)); // convert numloss points to amounts
     }      

   return(OrderSend(Symbol(), DirectionOrderType(direction), 
     numlots, price, SlipPage, stop_loss, take_profit,  
     comment, OrderMagic, 0, ColorOpen(direction)));
  }

// -------------------------------------------------
//  CloseOrder()
// -------------------------------------------------
void CloseOrder()
  {
   int direction = OrderTypeDirection();
   if(direction == 0) return;

   OrderClose(OrderTicket(), OrderLots(), PriceClose(direction), SlipPage, ColorClose(direction));
  }
  
// -------------------------------------------------
// GetOrderByPos
// -------------------------------------------------
bool GetOrderByPos(int pos)
  {
   return(OrderSelect(pos, SELECT_BY_POS, MODE_TRADES) 
      && (OrderType() <= OP_SELL) 
      && OrderSymbol() == Symbol());
  }
        
// -------------------------------------------------
// TrailOrderStop()
// -------------------------------------------------
void TrailOrderStop()
  {   
   int direction = OrderTypeDirection();
   
   double trail_val = NormalizeDouble(TrailingStop * Point * direction, Digits);
   double old_stopv = NormalizeDouble(iif(direction>0 || OrderStopLoss()!=0, OrderStopLoss(), 999999), Digits);
   double new_stopv = NormalizeDouble(PriceClose(direction) - trail_val, Digits);
   double gap_price = NormalizeDouble(new_stopv - OrderOpenPrice(), Digits);
   double gap_stops = NormalizeDouble(new_stopv - old_stopv, Digits);
   
   //double new_takep = NormalizeDouble(OrderTakeProfit(), Digits);
   //double gap_tp_sl = NormalizeDouble(new_takep - new_stopv, Digits);

   if(gap_price * direction > 0 && gap_stops * direction >= Point)
     { 
      //if(MathAbs(gap_tp_sl) < TrailingStop * Point) new_takep = NormalizeDouble(new_takep + trail_val, Digits);
      //Print("Modifying order# ", OrderTicket(), 
      //  "; Price = ", OrderOpenPrice(), 
      //  "; S/L = ", OrderStopLoss(), 
      //  "; New S/L = ", new_stopv, 
      //  "; T/P = ", OrderTakeProfit());
        
      OrderModify(OrderTicket(), OrderOpenPrice(), new_stopv, OrderTakeProfit(), 0, ColorOpen(direction));

      if(GetLastError() != 0)
        {
        }
     }
  }

// -------------------------------------------------
// PrintDebugInfo
// -------------------------------------------------
void PrintDebugInfo()
  {
   Print(
      /*  
      "; Jaw = ", gatorv_jaw, 
      "; Teeth = ", gatorv_teeth, 
      "; Lips = ", gatorv_lips, 
      */
      
      //"; Point: ", Point,
      "; WPR: ", ArrayMinValue(wprv), " - ", ArrayMaxValue(wprv),
      "; Demarker: ", ArrayMinValue(demarkerv), " - ", ArrayMaxValue(demarkerv)

      //"; Der D1 = ", tderv_d1, 
      //"; Der W1 = ", trend_ders[per_w1],
      //"; Der H1 = ", trend_ders[per_h1],

      //"; Age D1 = ", tagev_d1 
      //"; Age W1 = ", trend_ages[per_w1],
      //"; Age H1 = ", trend_ages[per_h1]
      );
   return(0); 
  }

// =================================================
// SUPPORT
// =================================================

// =================================================
// trading direction symmetry
// =================================================
 
// -------------------------------------------------
// OrderType <--> Direction
// -------------------------------------------------
int OrderTypeDirection()
  {
   return(1 - 2 * (OrderType() % 2));
  }

int DirectionOrderType(int direction)
  {
   return(iif(direction > 0, OP_BUY, OP_SELL));
  }

bool IsOrderDirection(int direction)
  {
   return(direction == 0 || direction == OrderTypeDirection());
  }
  
// -------------------------------------------------
// Color Open / Close
// -------------------------------------------------
color ColorOpen(int direction)
  {
   return(iif(direction > 0, Green, Red));
  }

color ColorClose(int direction)
  {
   return(Violet);
  }
  
// -------------------------------------------------
// PriceOpen / Close
// -------------------------------------------------

double PriceOpen(int direction)
  {
   return(iif(direction > 0, Bid, Ask));
  }  

double PriceClose(int direction)
  {
   return(iif(direction > 0, Ask, Bid));
  }  

// =================================================
// logical
// =================================================

double iif(bool condition, double ifTrue, double ifFalse)
  {
   if(condition) return(ifTrue);
   return(ifFalse);
  }
 
string iifStr(bool condition, string ifTrue, string ifFalse)
  {
   if(condition) return(ifTrue);
   return(ifFalse);
  }

// =================================================
// math
// =================================================

int Sign(double x)
  {
   if(x > 0) return(1);
   if(x < 0) return(-1);
   return(0);
  }

// =================================================
// date / time
// =================================================
      
// -------------------------------------------------
// Order Ages 
// -------------------------------------------------

double OrderAgePeriods()
  {
   return(OrderAgeSeconds() / PeriodSeconds());
  }

double OrderAgeDays()
  {
   return(OrderAgeHours() / 24);
  }
         
double OrderAgeHours()
  {
   return(OrderAgeMinutes() / 60);
  }
            
double OrderAgeMinutes()
  {
   return(OrderAgeSeconds() / 60);
  }
         
double OrderAgeSeconds()
  {
   return(TimeCurrent() - OrderOpenTime());
  }              

int PeriodSeconds()
  {
   return(Period() * 60);
  }
  
// -------------------------------------------------
// Names of objects
// -------------------------------------------------
string OrderTypeName(int OrdType)
  {
   switch(OrdType)
     {
      case OP_BUY:         return("BUY");
      case OP_SELL:        return("SELL");
      case OP_BUYLIMIT:    return("BUYLIMIT");
      case OP_SELLLIMIT:   return("SELLLIMIT");
      case OP_BUYSTOP:     return("BUYSTOP");
      case OP_SELLSTOP:    return("SELLSTOP");
		default:		         return("UnknownOrder");
     }
  }

string PeriodName(int PerNum)
  {
	switch(PerNum)
     {
		case PERIOD_M1:    return("M1");
		case PERIOD_M5:    return("M5");
		case PERIOD_M15:   return("M15");
		case PERIOD_M30:   return("M30");
		case PERIOD_H1:    return("H1");
		case PERIOD_H4:    return("H4");
		case PERIOD_D1:    return("D1");
		case PERIOD_W1:    return("W1");
		case PERIOD_MN1:   return("M1");
		default:		       return("UnknownPeriod");
	  }
  }
int PeriodIndex(int PerNum)
  {
	switch(PerNum)
     {
		case PERIOD_M1:    return(0);
		case PERIOD_M5:    return(1);
		case PERIOD_M15:   return(2);
		case PERIOD_M30:   return(3);
		case PERIOD_H1:    return(4);
		case PERIOD_H4:    return(5);
		case PERIOD_D1:    return(6);
		case PERIOD_W1:    return(7);
		case PERIOD_MN1:   return(8);
		default:		       return(-1);
	  }
  }

// =================================================
// error handling
// =================================================

// -------------------------------------------------
// IsError()
// -------------------------------------------------
bool IsError(string Whose="Raptor V1")  
  {
   int ierr = GetLastError(); 
 //bool result = (ierr!= 0);
   bool result = (ierr > 1);
   if(result) Print(Whose, " error = ", ierr, "; desc = ", ErrorDescription(ierr));
   return(result);
  }
  
//+------------------------------------------------------------------+
//| return error description                                         |
//+------------------------------------------------------------------+
string ErrorDescription(int error_code)
  {
   string error_string;
//----
   switch(error_code)
     {
      //---- codes returned from trade server
      case 0:
      case 1:   error_string="no error";                                                  break;
      case 2:   error_string="common error";                                              break;
      case 3:   error_string="invalid trade parameters";                                  break;
      case 4:   error_string="trade server is busy";                                      break;
      case 5:   error_string="old version of the client terminal";                        break;
      case 6:   error_string="no connection with trade server";                           break;
      case 7:   error_string="not enough rights";                                         break;
      case 8:   error_string="too frequent requests";                                     break;
      case 9:   error_string="malfunctional trade operation (never returned error)";      break;
      case 64:  error_string="account disabled";                                          break;
      case 65:  error_string="invalid account";                                           break;
      case 128: error_string="trade timeout";                                             break;
      case 129: error_string="invalid price";                                             break;
      case 130: error_string="invalid stops";                                             break;
      case 131: error_string="invalid trade volume";                                      break;
      case 132: error_string="market is closed";                                          break;
      case 133: error_string="trade is disabled";                                         break;
      case 134: error_string="not enough money";                                          break;
      case 135: error_string="price changed";                                             break;
      case 136: error_string="off quotes";                                                break;
      case 137: error_string="broker is busy (never returned error)";                     break;
      case 138: error_string="requote";                                                   break;
      case 139: error_string="order is locked";                                           break;
      case 140: error_string="long positions only allowed";                               break;
      case 141: error_string="too many requests";                                         break;
      case 145: error_string="modification denied because order too close to market";     break;
      case 146: error_string="trade context is busy";                                     break;
      case 147: error_string="expirations are denied by broker";                          break;
      case 148: error_string="amount of open and pending orders has reached the limit";   break;
      //---- mql4 errors
      case 4000: error_string="no error (never generated code)";                          break;
      case 4001: error_string="wrong function pointer";                                   break;
      case 4002: error_string="array index is out of range";                              break;
      case 4003: error_string="no memory for function call stack";                        break;
      case 4004: error_string="recursive stack overflow";                                 break;
      case 4005: error_string="not enough stack for parameter";                           break;
      case 4006: error_string="no memory for parameter string";                           break;
      case 4007: error_string="no memory for temp string";                                break;
      case 4008: error_string="not initialized string";                                   break;
      case 4009: error_string="not initialized string in array";                          break;
      case 4010: error_string="no memory for array\' string";                             break;
      case 4011: error_string="too long string";                                          break;
      case 4012: error_string="remainder from zero divide";                               break;
      case 4013: error_string="zero divide";                                              break;
      case 4014: error_string="unknown command";                                          break;
      case 4015: error_string="wrong jump (never generated error)";                       break;
      case 4016: error_string="not initialized array";                                    break;
      case 4017: error_string="dll calls are not allowed";                                break;
      case 4018: error_string="cannot load library";                                      break;
      case 4019: error_string="cannot call function";                                     break;
      case 4020: error_string="expert function calls are not allowed";                    break;
      case 4021: error_string="not enough memory for temp string returned from function"; break;
      case 4022: error_string="system is busy (never generated error)";                   break;
      case 4050: error_string="invalid function parameters count";                        break;
      case 4051: error_string="invalid function parameter value";                         break;
      case 4052: error_string="string function internal error";                           break;
      case 4053: error_string="some array error";                                         break;
      case 4054: error_string="incorrect series array using";                             break;
      case 4055: error_string="custom indicator error";                                   break;
      case 4056: error_string="arrays are incompatible";                                  break;
      case 4057: error_string="global variables processing error";                        break;
      case 4058: error_string="global variable not found";                                break;
      case 4059: error_string="function is not allowed in testing mode";                  break;
      case 4060: error_string="function is not confirmed";                                break;
      case 4061: error_string="send mail error";                                          break;
      case 4062: error_string="string parameter expected";                                break;
      case 4063: error_string="integer parameter expected";                               break;
      case 4064: error_string="double parameter expected";                                break;
      case 4065: error_string="array as parameter expected";                              break;
      case 4066: error_string="requested history data in update state";                   break;
      case 4099: error_string="end of file";                                              break;
      case 4100: error_string="some file error";                                          break;
      case 4101: error_string="wrong file name";                                          break;
      case 4102: error_string="too many opened files";                                    break;
      case 4103: error_string="cannot open file";                                         break;
      case 4104: error_string="incompatible access to a file";                            break;
      case 4105: error_string="no order selected";                                        break;
      case 4106: error_string="unknown symbol";                                           break;
      case 4107: error_string="invalid price parameter for trade function";               break;
      case 4108: error_string="invalid ticket";                                           break;
      case 4109: error_string="trade is not allowed in the expert properties";            break;
      case 4110: error_string="longs are not allowed in the expert properties";           break;
      case 4111: error_string="shorts are not allowed in the expert properties";          break;
      case 4200: error_string="object is already exist";                                  break;
      case 4201: error_string="unknown object property";                                  break;
      case 4202: error_string="object is not exist";                                      break;
      case 4203: error_string="unknown object type";                                      break;
      case 4204: error_string="no object name";                                           break;
      case 4205: error_string="object coordinates error";                                 break;
      case 4206: error_string="no specified subwindow";                                   break;
      default:   error_string="unknown error";
     }
//----
   return(error_string);
  }

// =================================================
// misc
// =================================================

// -------------------------------------------------
// Array Min/Max Value
// -------------------------------------------------
double ArrayMinValue(double a[])
  {
   return(a[ArrayMinimum(a)]);
  }

double ArrayMaxValue(double a[])
  {
   return(a[ArrayMaximum(a)]);
  }  

