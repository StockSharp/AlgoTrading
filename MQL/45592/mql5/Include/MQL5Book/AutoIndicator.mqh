//+------------------------------------------------------------------+
//|                                                AutoIndicator.mqh |
//|                                 Copyright © 2017-2020, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//+------------------------------------------------------------------+
#include <MQL5Book/RTTI.mqh>
#include <MQL5Book/MqlParamBuilder.mqh>

//+------------------------------------------------------------------+
//| Defines for making indicators' IDs with metadata                 |
//+------------------------------------------------------------------+
#define MAKE_IND(P,B,W,ID) (int)((W << 24) | ((B & 0xFF) << 16) | ((P & 0xFF) << 8) | (ID & 0xFF))
#define IND_PARAMS(X)   ((X >> 8) & 0xFF)
#define IND_BUFFERS(X)  ((X >> 16) & 0xFF)
#define IND_WINDOW(X)   ((uchar)(X >> 24))
#define IND_ID(X)       ((ENUM_INDICATOR)(X & 0xFF))

//+------------------------------------------------------------------+
//| All indicators (types and metadata)                              |
//+------------------------------------------------------------------+
enum IndicatorType
{
   iCustom_ = MAKE_IND(0, 0, 0, IND_CUSTOM), // {iCustom}(...)[?]

   iAC_ = MAKE_IND(0, 1, 1, IND_AC), // iAC( )[1]*
   iAD_volume = MAKE_IND(1, 1, 1, IND_AD), // iAD(volume)[1]*
   iADX_period = MAKE_IND(1, 3, 1, IND_ADX), // iADX(period)[3]*
   iADXWilder_period = MAKE_IND(1, 3, 1, IND_ADXW), // iADXWilder(period)[3]*
   iAlligator_jawP_jawS_teethP_teethS_lipsP_lipsS_method_price = MAKE_IND(8, 3, 0, IND_ALLIGATOR), // iAlligator(jawP,jawS,teethP,teethS,lipsP,lipsS,method,price)[3]
   iAMA_period_fast_slow_shift_price = MAKE_IND(5, 1, 0, IND_AMA), // iAMA(period,fast,slow,shift,price)[1]
   iAO_ = MAKE_IND(0, 1, 1, IND_AO), // iAO( )[1]*
   iATR_period = MAKE_IND(1, 1, 1, IND_ATR), // iATR(period)[1]*
   iBands_period_shift_deviation_price = MAKE_IND(4, 3, 0, IND_BANDS), // iBands(period,shift,deviation,price)[3]
   iBearsPower_period = MAKE_IND(1, 1, 1, IND_BEARS), // iBearsPower(period)[1]*
   iBullsPower_period = MAKE_IND(1, 1, 1, IND_BULLS), // iBullsPower(period)[1]*
   iBWMFI_volume = MAKE_IND(1, 1, 1, IND_BWMFI), // iBWMFI(volume)[1]*
   iCCI_period_price = MAKE_IND(2, 1, 1, IND_CCI), // iCCI(period,price)[1]*
   iChaikin_fast_slow_method_volume = MAKE_IND(4, 1, 1, IND_CHAIKIN), // iChaikin(fast,slow,method,volume)[1]*
   iDEMA_period_shift_price = MAKE_IND(3, 1, 0, IND_DEMA), // iDEMA(period,shift,price)[1]
   iDeMarker_period = MAKE_IND(1, 1, 1, IND_DEMARKER), // iDeMarker(period)[1]*
   iEnvelopes_period_shift_method_price_deviation = MAKE_IND(5, 2, 0, IND_ENVELOPES), // iEnvelopes(period,shift,method,price,deviation)[2]
   iForce_period_method_volume = MAKE_IND(3, 1, 1, IND_FORCE), // iForce(period,method,volume)[1]*
   iFractals_ = MAKE_IND(0, 2, 0, IND_FRACTALS), // iFractals( )[2]
   iFrAMA_period_shift_price = MAKE_IND(3, 1, 0, IND_FRAMA), // iFrAMA(period,shift,price)[1]
   iGator_jawP_jawS_teethP_teethS_lipsP_lipsS_method_price = MAKE_IND(8, 4, 1, IND_GATOR), // iGator(jawP,jawS,teethP,teethS,lipsP,lipsS,method,price)[4]*
   iIchimoku_tenkan_kijun_senkou = MAKE_IND(3, 5, 0, IND_ICHIMOKU), // iIchimoku(tenkan,kijun,senkou)[5]
   iMomentum_period_price = MAKE_IND(2, 1, 1, IND_MOMENTUM), // iMomentum(period,price)[1]*
   iMFI_period_volume = MAKE_IND(2, 1, 1, IND_MFI), // iMFI(period,volume)[1]*
   iMA_period_shift_method_price = MAKE_IND(4, 1, 0, IND_MA), // iMA(period,shift,method,price)[1]
   iMACD_fast_slow_signal_price = MAKE_IND(4, 2, 1, IND_MACD), // iMACD(fast,slow,signal,price)[2]*
   iOBV_volume = MAKE_IND(1, 1, 1, IND_OBV), // iOBV(volume)[1]*
   iOsMA_fast_slow_signal_price = MAKE_IND(4, 1, 1, IND_OSMA), // iOsMA(fast,slow,signal,price)[1]*
   iRSI_period_price = MAKE_IND(2, 1, 1, IND_RSI), // iRSI(period,price)[1]*
   iRVI_period = MAKE_IND(1, 2, 1, IND_RVI), // iRVI(period)[2]*
   iSAR_step_maximum = MAKE_IND(2, 1, 0, IND_SAR), // iSAR(step,maximum)[1]
   iStdDev_period_shift_method_price = MAKE_IND(4, 1, 1, IND_STDDEV), // iStdDev(period,shift,method,price)[1]*
   iStochastic_K_D_slowing_method_price = MAKE_IND(5, 2, 1, IND_STOCHASTIC), // iStochastic(K,D,slowing,method,price)[2]*
   iTEMA_period_shift_price = MAKE_IND(3, 1, 0, IND_TEMA), // iTEMA(period,shift,price)[1]
   iTriX_period_price = MAKE_IND(2, 1, 1, IND_TRIX), // iTriX(period,price)[1]*
   iVIDyA_momentum_smooth_shift_price = MAKE_IND(4, 1, 0, IND_VIDYA), // iVIDyA(momentum,smooth,shift,price)[1]
   iVolumes_volume = MAKE_IND(1, 1, 1, IND_VOLUMES), // iVolumes(volume)[1]*
   iWPR_period = MAKE_IND(1, 1, 1, IND_WPR), // iWPR(period)[1]*
};

//+------------------------------------------------------------------+
//| Indicator builder                                                |
//+------------------------------------------------------------------+
class AutoIndicator
{
protected:
   IndicatorType type;      // selected indicator type
   string symbol;           // optional work symbol
   ENUM_TIMEFRAMES tf;      // optional work timeframe
   MqlParamBuilder builder; // helper object to build array of parameters
   int handle;              // indicator handle
   string name;             // custom indicator name
   
   // helper to check parameters of built-in indicators
   bool isNumerical(const int i)
   {
      if(builder.typeOf(i) == TYPE_STRING || builder.typeOf(i) == TYPE_BOOL)
      {
         return false;
      }
      return true;
   }

   // helper to check parameters of built-in indicators
   bool checkForNumericals()
   {
      const int pnum = builder.size(); // ArraySize(params);
      for(int i = 0; i < pnum; i++)
      {
         if(!isNumerical(i))
         {
            Print(EnumToString(type) + " parameter ", (i + 1), " must be a number, ",
               EnumToString(builder.typeOf(i)), " given");
            return false;
         }
      }
      return true;
   }
    
   // check number and types of parameters of built-in indicators
   bool checkStandard()
   {
      const int pnum = builder.size(); // ArraySize(params);
      if(IND_PARAMS(type) != pnum)
      {
         Print(EnumToString(type) + " requires " + (string)IND_PARAMS(type) + " parameters, " + (string)pnum + " given");
         return false;
      }
      
      // built-in indicators have numeric parameters only
      return checkForNumericals();
   }

   // return integer constant for specific textual descrpition
   int lookUpLiterals(const string &s)
   {
      if(s == "sma") return MODE_SMA;
      else if(s == "ema") return MODE_EMA;
      else if(s == "smma") return MODE_SMMA;
      else if(s == "lwma") return MODE_LWMA;
      
      else if(s == "close") return PRICE_CLOSE;
      else if(s == "open") return PRICE_OPEN;
      else if(s == "high") return PRICE_HIGH;
      else if(s == "low") return PRICE_LOW;
      else if(s == "median") return PRICE_MEDIAN;
      else if(s == "typical") return PRICE_TYPICAL;
      else if(s == "weighted") return PRICE_WEIGHTED;

      else if(s == "lowhigh") return STO_LOWHIGH; // 0
      else if(s == "closeclose") return STO_CLOSECLOSE; // 1

      else if(s == "tick") return VOLUME_TICK; // 0
      else if(s == "real") return VOLUME_REAL; // 1
      
      return -1;
   }

   // parse comma separated list of parameters 'p1,p2,p3,...'
   // into array of elements of appropriate types,
   // for example, '1.0' goes to double, 123 makes int, "text" means string,
   // true/false are booleans, "2021.01.01" is a datetime
   int parseParameters(const string &list)
   {
      string sparams[];
      const int n = StringSplit(list, ',', sparams);
      
      for(int i = 0; i < n; i++)
      {
         // normalize the string (trim it and change to lower case)
         StringTrimLeft(sparams[i]);
         StringTrimRight(sparams[i]);
         StringToLower(sparams[i]);
         
         if(StringGetCharacter(sparams[i], 0) == '"'
         && StringGetCharacter(sparams[i], StringLen(sparams[i]) - 1) == '"')
         {
            // anything inside quotes is a string
            builder << StringSubstr(sparams[i], 1, StringLen(sparams[i]) - 2);
         }
         else
         {
            string part[];
            int p = StringSplit(sparams[i], '.', part);
            if(p == 2) // double/float
            {
               builder << StringToDouble(sparams[i]);
            }
            else if(p == 3) // datetime
            {
               builder << StringToTime(sparams[i]);
            }
            else if(sparams[i] == "true")
            {
               builder << true;
            }
            else if(sparams[i] == "false")
            {
               builder << false;
            }
            else // int
            {
               int x = lookUpLiterals(sparams[i]);
               if(x == -1)
               {
                  x = (int)StringToInteger(sparams[i]);
               }
               builder << x;
            }
         }
      }
      
      if(type != iCustom_)
      {
         checkStandard(); // show warning for wrong types or number of parameters
      }
      return n;
   }

   // calls IndicatorCreate for prepared list of parameters
   int create()
   {
      MqlParam p[];
      // fill array 'p' with parameters acquired by builder
      builder >> p;
      
      if(type == iCustom_)
      {
         // insert name of custom indicator at the beginning
         ArraySetAsSeries(p, true);
         const int n = ArraySize(p);
         ArrayResize(p, n + 1);
         p[n].type = TYPE_STRING;
         p[n].string_value = name;
         ArraySetAsSeries(p, false);
      }
      
      return IndicatorCreate(symbol, tf, IND_ID(type), ArraySize(p), p);
   }

public:
   AutoIndicator(const IndicatorType t, const string custom, const string parameters,
      const string s = NULL, const ENUM_TIMEFRAMES p = 0):
      type(t), name(custom), symbol(s), tf(p), handle(INVALID_HANDLE)
   {
      if(type != iCustom_ && StringLen(name) > 0)
      {
         PrintFormat("Indicator name '%s' will be discarded, because of built-in type selection", name);
         name = NULL;
      }
      
      if(type == iCustom_ && StringLen(name) == 0)
      {
         Print("Custom indicator name is missing");
      }
      else
      {
         PrintFormat("Initializing %s(%s) %s, %s",
            (type == iCustom_ ? name : EnumToString(type)), parameters,
            (symbol == NULL ? _Symbol : symbol), EnumToString(tf == 0 ? _Period : tf));
         // parse string with indicator parameters
         parseParameters(parameters);
         // then create and store its handle
         handle = create();
      }
   }
   
   int getHandle() const
   {
      return handle;
   }
   
   string getName() const
   {
      if(type != iCustom_)
      {
         const string s = EnumToString(type);
         const int p = StringFind(s, "_");
         if(p > 0) return StringSubstr(s, 0, p);
         return s;
      }
      return name;
   }
};
//+------------------------------------------------------------------+
