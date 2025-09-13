//+------------------------------------------------------------------+
//|                                                     IndicatR.mqh |
//|                                 Copyright © 2017-2020, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//|           Universal framework for trading on indicators' signals |
//|                               https://www.mql5.com/en/code/32107 |
//| Based on IndicatN.mqh from https://www.mql5.com/en/articles/3264 |
//+------------------------------------------------------------------+

#property strict

#define MAX_PARAM_NUM 20
#define MAX_SIGNAL_NUM 8
#define MAX_INDICATOR_NUM 6

enum UpZeroDown
{
  EqualOrNone,
  UpSideOrAboveOrPositve,
  DownSideOrBelowOrNegative,
  NotEqual
};

enum SignalCondition
{
  Disabled,
  NotEmptyIndicatorX,
  SignOfValueIndicatorX,
  IndicatorXcrossesIndicatorY,
  IndicatorXcrossesLevelX,
  IndicatorXrelatesToIndicatorY,
  IndicatorXrelatesToLevelX,
  ExpressionByIndicatorX,
};

enum SignalType
{
  Alert_, // Alert
  Buy,
  Sell,
  CloseBuy,
  CloseSell,
  CloseAll,
  BuyAndCloseSell,
  SellAndCloseBuy,
  ModifyStopLoss,
  ModifyTakeProfit,
  //PlaceBuyLimit,
  //PlaceSellLimit,
  //PlaceBuyStop,
  //PlaceSellStop,
  ProceedToNextCondition
};

enum IndicatorType
{
  iCustom_, // iCustom

  iAC_,
  iAD_,
  tADX_period_price,
  // xADXWilder_period,
  tAlligator_jawP_jawS_teethP_teethS_lipsP_lipsS_method_price,
  // xAMA_period_fast_slow_shift_price
  iAO_,
  iATR_period,
  tBands_period_deviation_shift_price,
  iBearsPower_period_price,
  iBullsPower_period_price,
  iBWMFI_,
  iCCI_period_price,
  // xChaikin_fast_slow_method_volume
  // xDEMA__period_shift_price
  iDeMarker_period,
  tEnvelopes_period_method_shift_price_deviation,
  iForce_period_method_price,
  dFractals,
  // xFrAMA_period_shift_price
  dGator_jawP_jawS_teethP_teethS_lipsP_lipsS_method_price,
  fIchimoku_tenkan_kijun_senkou,
  iMomentum_period_price,
  iMFI_period,
  iMA_period_shift_method_price,
  dMACD_fast_slow_signal_price,
  iOBV_price,
  iOsMA_fast_slow_signal_price,
  iRSI_period_price,
  dRVI_period,
  iSAR_step_maximum,
  iStdDev_period_shift_method_price,
  dStochastic_K_D_slowing_method_price,
  // xTEMA_period_shift_price
  // xTriX_period_price
  // xVIDyA_chande_smooth_shift_price
  // xVolumes_volume
  iWPR_period

};



// ***   I N P U T S
sinput string __I_N_D_I_C_A_T_O_R_S__ = "__I_N_D_I_C_A_T_O_R_S__";

input string __INDICATOR_1 = "";
input IndicatorType Indicator1Selector = iCustom_; // ·     Selector
input string Indicator1Name = ""; // ·     Name
input string Parameter1List = "" /*1.0,value:t,value:t*/; // ·     Parameters
input string Indicator1Buffer = ""; // ·     Buffer
input int Indicator1Bar = 1; // ·     Bar

input string __INDICATOR_2 = "";
input IndicatorType Indicator2Selector = iCustom_; // ·     Selector
input string Indicator2Name = ""; // ·     Name
input string Parameter2List = "" /*1.0,value:t,value:t*/; // ·     Parameters
input string Indicator2Buffer = ""; // ·     Buffer
input int Indicator2Bar = 1; // ·     Bar

input string __INDICATOR_3 = "";
input IndicatorType Indicator3Selector = iCustom_; // ·     Selector
input string Indicator3Name = ""; // ·     Name
input string Parameter3List = "" /*1.0,value:t,value:t*/; // ·     Parameters
input string Indicator3Buffer = ""; // ·     Buffer
input int Indicator3Bar = 1; // ·     Bar

input string __INDICATOR_4 = "";
input IndicatorType Indicator4Selector = iCustom_; // ·     Selector
input string Indicator4Name = ""; // ·     Name
input string Parameter4List = "" /*1.0,value:t,value:t*/; // ·     Parameters
input string Indicator4Buffer = ""; // ·     Buffer
input int Indicator4Bar = 1; // ·     Bar

input string __INDICATOR_5 = "";
input IndicatorType Indicator5Selector = iCustom_; // ·     Selector
input string Indicator5Name = ""; // ·     Name
input string Parameter5List = "" /*1.0,value:t,value:t*/; // ·     Parameters
input string Indicator5Buffer = ""; // ·     Buffer
input int Indicator5Bar = 1; // ·     Bar

input string __INDICATOR_6 = "";
input IndicatorType Indicator6Selector = iCustom_; // ·     Selector
input string Indicator6Name = ""; // ·     Name
input string Parameter6List = "" /*1.0,value:t,value:t*/; // ·     Parameters
input string Indicator6Buffer = ""; // ·     Buffer
input int Indicator6Bar = 1; // ·     Bar


sinput string __S_I_G_N_A_L_S__ = "__S_I_G_N_A_L_S__";

input string __SIGNAL_A = "";
input SignalCondition ConditionA = Disabled; // ·     Condition A
input string IndicatorA1 = ""; // ·     Indicator X for signal A
input string IndicatorA2 = ""; // ·     Indicator Y for signal A
input double LevelA1 = 0; // ·     Level X for signal A
input double LevelA2 = 0; // ·     Level Y for signal A
input UpZeroDown DirectionA = EqualOrNone; // ·     Direction or sign A
input SignalType ExecutionA = Alert_; // ·     Action A

input string __SIGNAL_B = "";
input SignalCondition ConditionB = Disabled; // ·     Condition B
input string IndicatorB1 = ""; // ·     Indicator X for signal B
input string IndicatorB2 = ""; // ·     Indicator Y for signal B
input double LevelB1 = 0; // ·     Level X for signal B
input double LevelB2 = 0; // ·     Level Y for signal B
input UpZeroDown DirectionB = EqualOrNone; // ·     Direction or sign B
input SignalType ExecutionB = Alert_; // ·     Action B

input string __SIGNAL_C = "";
input SignalCondition ConditionC = Disabled; // ·     Condition C
input string IndicatorC1 = ""; // ·     Indicator X for signal C
input string IndicatorC2 = ""; // ·     Indicator Y for signal C
input double LevelC1 = 0; // ·     Level X for signal C
input double LevelC2 = 0; // ·     Level Y for signal C
input UpZeroDown DirectionC = EqualOrNone; // ·     Direction or sign C
input SignalType ExecutionC = Alert_; // ·     Action C

input string __SIGNAL_D = "";
input SignalCondition ConditionD = Disabled; // ·     Condition D
input string IndicatorD1 = ""; // ·     Indicator X for signal D
input string IndicatorD2 = ""; // ·     Indicator Y for signal D
input double LevelD1 = 0; // ·     Level X for signal D
input double LevelD2 = 0; // ·     Level Y for signal D
input UpZeroDown DirectionD = EqualOrNone; // ·     Direction or sign D
input SignalType ExecutionD = Alert_; // ·     Action D

input string __SIGNAL_E = "";
input SignalCondition ConditionE = Disabled; // ·     Condition E
input string IndicatorE1 = ""; // ·     Indicator X for signal E
input string IndicatorE2 = ""; // ·     Indicator Y for signal E
input double LevelE1 = 0; // ·     Level X for signal E
input double LevelE2 = 0; // ·     Level Y for signal E
input UpZeroDown DirectionE = EqualOrNone; // ·     Direction or sign E
input SignalType ExecutionE = Alert_; // ·     Action E

input string __SIGNAL_F = "";
input SignalCondition ConditionF = Disabled; // ·     Condition F
input string IndicatorF1 = ""; // ·     Indicator X for signal F
input string IndicatorF2 = ""; // ·     Indicator Y for signal F
input double LevelF1 = 0; // ·     Level X for signal F
input double LevelF2 = 0; // ·     Level Y for signal F
input UpZeroDown DirectionF = EqualOrNone; // ·     Direction or sign F
input SignalType ExecutionF = Alert_; // ·     Action F

input string __SIGNAL_G = "";
input SignalCondition ConditionG = Disabled; // ·     Condition G
input string IndicatorG1 = ""; // ·     Indicator X for signal G
input string IndicatorG2 = ""; // ·     Indicator Y for signal G
input double LevelG1 = 0; // ·     Level X for signal G
input double LevelG2 = 0; // ·     Level Y for signal G
input UpZeroDown DirectionG = EqualOrNone; // ·     Direction or sign G
input SignalType ExecutionG = Alert_; // ·     Action G

input string __SIGNAL_H = "";
input SignalCondition ConditionH = Disabled; // ·     Condition H
input string IndicatorH1 = ""; // ·     Indicator X for signal H
input string IndicatorH2 = ""; // ·     Indicator Y for signal H
input double LevelH1 = 0; // ·     Level X for signal H
input double LevelH2 = 0; // ·     Level Y for signal H
input UpZeroDown DirectionH = EqualOrNone; // ·     Direction or sign H
input SignalType ExecutionH = Alert_; // ·     Action H

// we can not use defines to generate inputs, because comments are skipped,
// but comments are used to specify input names
// this will not work as expected:
//   #define SIGNAL_DEF(X) input string __SIGNAL_##X = ""; \
//   input SignalCondition Condition##X = Disabled; // ·     Condition X
//   SIGNAL_DEF("Y")

input string __O_P_T_I_M_I_Z_A_T_I_O_N = "";
input double var1 = EMPTY_VALUE;
input double var2 = EMPTY_VALUE;
input double var3 = EMPTY_VALUE;
input double var4 = EMPTY_VALUE;
input double var5 = EMPTY_VALUE;
input double var6 = EMPTY_VALUE;
input double var7 = EMPTY_VALUE;
input double var8 = EMPTY_VALUE;
input double var9 = EMPTY_VALUE;


#include "fmtprnt3.mqh"
#include "RubbArray.mqh"
#include <ExpresSParserS/v1.2/ExpressionCompiler.mqh>


// context wrapper, namespace

class IndicatR
{

public:

static IndicatorType lookUpStandardIndicator(const string &name)
{
  for(int i = iAC_; i <= iWPR_period; i++)
  {
    string type = EnumToString(IndicatorType(i));
    int p = StringFind(type, "_");
    if(p > 0) type = StringSubstr(type, 0, p);
    StringSetCharacter(type, 0, 'i');
    if(StringCompare(name, type) == 0)
    {
      return (IndicatorType)i;
    }
  }
  return iCustom_;
}

// ***   G E T T E R S

static double readOptVar(int i)
{
  switch(i)
  {
    case 1:
      return var1;
    case 2:
      return var2;
    case 3:
      return var3;
    case 4:
      return var4;
    case 5:
      return var5;
    case 6:
      return var6;
    case 7:
      return var7;
    case 8:
      return var8;
    case 9:
      return var9;
  }
  return EMPTY_VALUE;
}

static IndicatorType GetIndicatorSelector(int i)
{
  switch(i)
  {
    case 0:
      return Indicator1Selector;
    case 1:
      return Indicator2Selector;
    case 2:
      return Indicator3Selector;
    case 3:
      return Indicator4Selector;
    case 4:
      return Indicator5Selector;
    case 5:
      return Indicator6Selector;
  }
  return iCustom_;
}

static string GetIndicatorName(int i)
{
  switch(i)
  {
    case 0:
      return Indicator1Name;
    case 1:
      return Indicator2Name;
    case 2:
      return Indicator3Name;
    case 3:
      return Indicator4Name;
    case 4:
      return Indicator5Name;
    case 5:
      return Indicator6Name;
  }
  return "???" + (string)i;
}

static string GetIndicatorFullName(int i)
{
  switch(i)
  {
    case 0:
      return EnumToString(Indicator1Selector) + " " + Indicator1Name + "@" + Indicator1Buffer + "(" + Parameter1List + ")[" + (string)Indicator1Bar + "]";
    case 1:
      return EnumToString(Indicator2Selector) + " " + Indicator2Name + "@" + Indicator2Buffer + "(" + Parameter2List + ")[" + (string)Indicator2Bar + "]";
    case 2:
      return EnumToString(Indicator3Selector) + " " + Indicator3Name + "@" + Indicator3Buffer + "(" + Parameter3List + ")[" + (string)Indicator3Bar + "]";
    case 3:
      return EnumToString(Indicator4Selector) + " " + Indicator4Name + "@" + Indicator4Buffer + "(" + Parameter4List + ")[" + (string)Indicator4Bar + "]";
    case 4:
      return EnumToString(Indicator5Selector) + " " + Indicator5Name + "@" + Indicator5Buffer + "(" + Parameter5List + ")[" + (string)Indicator5Bar + "]";
    case 5:
      return EnumToString(Indicator6Selector) + " " + Indicator6Name + "@" + Indicator6Buffer + "(" + Parameter6List + ")[" + (string)Indicator6Bar + "]";
  }
  return "???" + (string)i;
}

static string GetIndicatorShortName(int i)
{
  switch(i)
  {
    case 0:
      return EnumToString(Indicator1Selector) + " " + Indicator1Name;
    case 1:
      return EnumToString(Indicator2Selector) + " " + Indicator2Name;
    case 2:
      return EnumToString(Indicator3Selector) + " " + Indicator3Name;
    case 3:
      return EnumToString(Indicator4Selector) + " " + Indicator4Name;
    case 4:
      return EnumToString(Indicator5Selector) + " " + Indicator5Name;
    case 5:
      return EnumToString(Indicator6Selector) + " " + Indicator6Name;
  }
  return "???" + (string)i;
}

static string GetIndicatorList(int i)
{
  switch(i)
  {
    case 0:
      return Parameter1List;
    case 1:
      return Parameter2List;
    case 2:
      return Parameter3List;
    case 3:
      return Parameter4List;
    case 4:
      return Parameter5List;
    case 5:
      return Parameter6List;
  }
  return "???" + (string)i;
}

static string GetIndicatorBuffer(int i)
{
  switch(i)
  {
    case 0:
      return Indicator1Buffer;
    case 1:
      return Indicator2Buffer;
    case 2:
      return Indicator3Buffer;
    case 3:
      return Indicator4Buffer;
    case 4:
      return Indicator5Buffer;
    case 5:
      return Indicator6Buffer;
  }
  return "???" + (string)i;
}

static int GetIndicatorBar(int i)
{
  switch(i)
  {
    case 0:
      return Indicator1Bar;
    case 1:
      return Indicator2Bar;
    case 2:
      return Indicator3Bar;
    case 3:
      return Indicator4Bar;
    case 4:
      return Indicator5Bar;
    case 5:
      return Indicator6Bar;
  }
  return 0;
}


/////////

static string GetSignal(int c)
{
  switch(c)
  {
    case 0:
      return __SIGNAL_A;
    case 1:
      return __SIGNAL_B;
    case 2:
      return __SIGNAL_C;
    case 3:
      return __SIGNAL_D;
    case 4:
      return __SIGNAL_E;
    case 5:
      return __SIGNAL_F;
    case 6:
      return __SIGNAL_G;
    case 7:
      return __SIGNAL_H;
  }
  return "[BAD SIGNAL INDEX: " + (string)c + "]";
}

static SignalCondition GetCondition(int c)
{
  switch(c)
  {
    case 0:
      return ConditionA;
    case 1:
      return ConditionB;
    case 2:
      return ConditionC;
    case 3:
      return ConditionD;
    case 4:
      return ConditionE;
    case 5:
      return ConditionF;
    case 6:
      return ConditionG;
    case 7:
      return ConditionH;
  }
  return Disabled;
}

static UpZeroDown GetDirection(int c)
{
  switch(c)
  {
    case 0:
      return DirectionA;
    case 1:
      return DirectionB;
    case 2:
      return DirectionC;
    case 3:
      return DirectionD;
    case 4:
      return DirectionE;
    case 5:
      return DirectionF;
    case 6:
      return DirectionG;
    case 7:
      return DirectionH;
  }
  return EqualOrNone;
}

static SignalType GetExecution(int c)
{
  switch(c)
  {
    case 0:
      return ExecutionA;
    case 1:
      return ExecutionB;
    case 2:
      return ExecutionC;
    case 3:
      return ExecutionD;
    case 4:
      return ExecutionE;
    case 5:
      return ExecutionF;
    case 6:
      return ExecutionG;
    case 7:
      return ExecutionH;
  }
  return Alert_;
}

/*

Can be a string: customName@buffer(param1,param2)[bar]

*/
static string GetIndicator(int c, int i)
{
  switch(c)
  {
    case 0:
      return i == 0? IndicatorA1 : IndicatorA2;
    case 1:
      return i == 0? IndicatorB1 : IndicatorB2;
    case 2:
      return i == 0? IndicatorC1 : IndicatorC2;
    case 3:
      return i == 0? IndicatorD1 : IndicatorD2;
    case 4:
      return i == 0? IndicatorE1 : IndicatorE2;
    case 5:
      return i == 0? IndicatorF1 : IndicatorF2;
    case 6:
      return i == 0? IndicatorG1 : IndicatorG2;
    case 7:
      return i == 0? IndicatorH1 : IndicatorH2;
  }
  return "???" + (string)c;
}

static double GetLevel(int c, int i)
{
  switch(c)
  {
    case 0:
      return i == 0? LevelA1 : LevelA2;
    case 1:
      return i == 0? LevelB1 : LevelB2;
    case 2:
      return i == 0? LevelC1 : LevelC2;
    case 3:
      return i == 0? LevelD1 : LevelD2;
    case 4:
      return i == 0? LevelE1 : LevelE2;
    case 5:
      return i == 0? LevelF1 : LevelF2;
    case 6:
      return i == 0? LevelG1 : LevelG2;
    case 7:
      return i == 0? LevelH1 : LevelH2;
  }
  return 0;
}


// ***   M  A  I  N

class ParameterBase
{
  protected:
    int i;
    double d;
    datetime t;
    string s;
    bool b;
  
    string type;
    
    #ifdef __MQL5__
    ENUM_DATATYPE edt;
    #endif

  public:
    virtual string getType() const
    {
      return type;
    }
    
    virtual bool isNumerical() const
    {
      return (type == "double") || (type == "int");
    }

    virtual bool isString() const
    {
      return (type == "string");
    }
    
    virtual double getValue(void) const = 0;

    virtual string getString(void) const
    {
      if(isString())
      {
        return s;
      }
      return NULL;
    }
    
    #ifdef __MQL5__
    ENUM_DATATYPE getDataType() const
    {
      return edt;
    }
    #endif
    
};

class ParameterString: public ParameterBase
{
  public:
    ParameterString(string x)
    {
      s = x;
      type = "string";
      #ifdef __MQL5__
      edt = TYPE_STRING;
      #endif
    }
    
    virtual double getValue(void) const
    {
      return 0;
    }
};

template<typename T>
class Parameter : public ParameterBase
{
  public:
    
    Parameter(T v)
    {
      type = typename(v);
      if(type == "int")
      {
        i = (int)v;
        #ifdef __MQL5__
        edt = TYPE_INT;
        #endif
      }
      else if(type == "double")
      {
        d = (double)v;
        #ifdef __MQL5__
        edt = TYPE_DOUBLE;
        #endif
      }
      else if(type == "datetime")
      {
        t = (datetime)v;
        #ifdef __MQL5__
        edt = TYPE_DOUBLE;
        #endif
      }
      else if(type == "bool")
      {
        b = (bool)v;
        #ifdef __MQL5__
        edt = TYPE_INT;
        #endif
      }
    }
    
    virtual double getValue(void) const
    {
      if(type == "int")
      {
        return i;
      }
      else if(type == "double")
      {
        return d;
      }
      else if(type == "datetime")
      {
        return (double)t;
      }
      else if(type == "bool")
      {
        return b;
      }
      return NULL;
    }
};

class Custom
{
  private:
    int index;
    string symbol;
    #ifdef __MQL5__    
    ENUM_TIMEFRAMES tf;
    #else
    int tf;
    #endif
    IndicatorType type;
    string indicator;
    string paramstr; // as pre-parsed string
    int _buffer;
    int _bar;
    int pnum;
    ParameterBase *params[];
    bool initOk;
    #ifdef __MQL5__    
    int handle;
    #endif
    
    double CheckForOpt(string &text)
    {
      double result = EMPTY_VALUE;
      int opt = StringFind(text, "=var");
      if(opt > -1)
      {
        string so = StringSubstr(text, opt + 4);
        if(StringLen(so) == 1)
        {
          int ch = StringGetCharacter(so, 0);
          if(ch >= '1' && ch <= '9')
          {
            int optN = (int)StringToInteger(so);
            result = IndicatR::readOptVar(optN);
            if(result == EMPTY_VALUE)
            {
              Print("Optimization parameter is empty:", text);
            }
          }
        }
        text = StringSubstr(text, 0, opt);
      }
      return result;
    }

    int lookUpBufferLiterals(const string &s)
    {
      if(s == "main") return 0;
      else if(s == "signal") return 1;
      else if(s == "upper") return 1;
      else if(s == "lower") return 2;
      
      else if(s == "jaw") return 1;
      else if(s == "teeth") return 2;
      else if(s == "lips") return 3;

      else if(s == "tenkan") return 1;
      else if(s == "kijun") return 2;
      else if(s == "senkouA") return 3;
      else if(s == "senkouB") return 4;
      else if(s == "chikou") return 3;

      else if(s == "+di") return 1;
      else if(s == "-di") return 2;
      
      return -1;
    }
    
    int lookUpLiterals(const string &s)
    {
      if(s == "sma") return 0;
      else if(s == "ema") return 1;
      else if(s == "smma") return 2;
      else if(s == "lwma") return 3;
      
      else if(s == "close") return 0;
      else if(s == "open") return 1;
      else if(s == "high") return 2;
      else if(s == "low") return 3;
      else if(s == "median") return 4;
      else if(s == "typical") return 5;
      else if(s == "weighted") return 6;

      else if(s == "lowhigh") return STO_LOWHIGH; // 0
      else if(s == "closeclose") return STO_CLOSECLOSE; // 1
      
      return -1;
    }

    int ParseParameters(const string &list)
    {
      string sparams[];
      int n = StringSplit(list, ',', sparams);
      int k = 0;
      
      for(int i = 0; i < n; i++)
      {
        string pair[];
        int m = StringSplit(sparams[i], ':', pair);
        ArrayResize(params, ArraySize(params) + 1);
        if(m == 2)
        {
          double ropt = CheckForOpt(pair[0]);
          
          if(pair[1] == "i")
          {
            if(ropt == EMPTY_VALUE)
            {
              int x = (int)StringToInteger(pair[0]);
              params[k++] = new Parameter<int>(x);
            }
            else
            {
              params[k++] = new Parameter<int>((int)ropt);
            }
          }
          else if(pair[1] == "d")
          {
            if(ropt == EMPTY_VALUE)
            {
              double y = StringToDouble(pair[0]);
              params[k++] = new Parameter<double>(y);
            }
            else
            {
              params[k++] = new Parameter<double>(ropt);
            }
          }
          else if(pair[1] == "t")
          {
            datetime z = StringToTime(pair[0]);
            params[k++] = new Parameter<datetime>(z);
          }
          else if(pair[1] == "b")
          {
            bool b = StringToInteger(pair[0]);
            params[k++] = new Parameter<bool>(b);
          }
          else if(pair[1] == "s")
          {
            // can't mix Parameter<string> with other Parameter<xyz> in MQL!
            params[k++] = new ParameterString(pair[0]);
          }
        }
        else // deduce type from content: 11.0 - double, 11 - int, 2015.01.01 20:00 - date/time, true/false - bool, "text" - string
        {
          if(StringGetCharacter(sparams[i], 0) == '"' && StringGetCharacter(sparams[i], StringLen(sparams[i]) - 1) == '"')
          {
            string s = StringSubstr(sparams[i], 1, StringLen(sparams[i]) - 2);
            params[k++] = new ParameterString(s);
          }
          else
          {
            double ropt = CheckForOpt(sparams[i]);

            string part[];
            int p = StringSplit(sparams[i], '.', part);
            if(p == 2) // double
            {
              if(ropt == EMPTY_VALUE)
              {
                double y = StringToDouble(sparams[i]);
                params[k++] = new Parameter<double>(y);
              }
              else
              {
                params[k++] = new Parameter<double>(ropt);
              }
            }
            else if(p == 3) // datetime
            {
              datetime z = StringToTime(sparams[i]);
              params[k++] = new Parameter<datetime>(z);
            }
            else if(sparams[i] == "true")
            {
              params[k++] = new Parameter<bool>(true);
            }
            else if(sparams[i] == "false")
            {
              params[k++] = new Parameter<bool>(false);
            }
            else
            {
              if(ropt == EMPTY_VALUE)
              {
                int x = lookUpLiterals(sparams[i]);
                if(x == -1)
                {
                  x = (int)StringToInteger(sparams[i]);
                }
                params[k++] = new Parameter<int>(x);
              }
              else
              {
                params[k++] = new Parameter<double>(ropt);
              }
            }
          }
        }
      }
      return n;
    }

    #ifdef __MQL5__

    double executeStandard(const int buffer, const int bar)
    {
      double result[1];
      int n = CopyBuffer(handle, buffer, bar, 1, result);
      if(n == 1) return result[0];
      
      Print("CopyBuffer error:", GetLastError());
      
      return EMPTY_VALUE;
    }
    
    int create()
    {
      switch(type)
      {
        case iAC_:
          return iAC(symbol, tf);
        case iAD_:
          return iAD(symbol, tf, VOLUME_TICK);
        case tADX_period_price:
          return iADX(symbol, tf, (int)params[0].getValue()); // , params[1].getValue() - applied price is not used in MT5!
        case tAlligator_jawP_jawS_teethP_teethS_lipsP_lipsS_method_price:
          return iAlligator(symbol, tf, (int)params[0].getValue(), (int)params[1].getValue(), (int)params[2].getValue(), (int)params[3].getValue(), (int)params[4].getValue(), (int)params[5].getValue(), (ENUM_MA_METHOD)(params[6].getValue()), (ENUM_APPLIED_PRICE)(params[7].getValue()));
        case iAO_:
          return iAO(symbol, tf);
        case iATR_period:
          return iATR(symbol, tf, (int)params[0].getValue());
        case tBands_period_deviation_shift_price:
          return iBands(symbol, tf, (int)params[0].getValue(), (int)params[1].getValue(), params[2].getValue(), (ENUM_APPLIED_PRICE)params[3].getValue());
        case iBearsPower_period_price:
          return iBearsPower(symbol, tf, (int)params[0].getValue()); // , params[1].getValue() - applied price is omited in MT5!
        case iBullsPower_period_price:
          return iBullsPower(symbol, tf, (int)params[0].getValue()); // , params[1].getValue()
        case iBWMFI_:
          return iBWMFI(symbol, tf, VOLUME_TICK);
        case iCCI_period_price:
          return iCCI(symbol, tf, (int)params[0].getValue(), (ENUM_APPLIED_PRICE)params[1].getValue());
        case iDeMarker_period:
          return iDeMarker(symbol, tf, (int)params[0].getValue());
        case tEnvelopes_period_method_shift_price_deviation:
          return iEnvelopes(symbol, tf, (int)params[0].getValue(), (int)params[1].getValue(), (ENUM_MA_METHOD)params[2].getValue(), (ENUM_APPLIED_PRICE)params[3].getValue(), params[4].getValue());
        case iForce_period_method_price:
          return iForce(symbol, tf, (int)params[0].getValue(), (ENUM_MA_METHOD)params[1].getValue(), VOLUME_TICK); // params[2].getValue() - applied price is omitted
        case dFractals:
          return iFractals(symbol, tf);
        case dGator_jawP_jawS_teethP_teethS_lipsP_lipsS_method_price:
          return iGator(symbol, tf, (int)params[0].getValue(), (int)params[1].getValue(), (int)params[2].getValue(), (int)params[3].getValue(), (int)params[4].getValue(), (int)params[5].getValue(), (ENUM_MA_METHOD)params[6].getValue(), (ENUM_APPLIED_PRICE)params[7].getValue());
        case fIchimoku_tenkan_kijun_senkou:
          return iIchimoku(symbol, tf, (int)params[0].getValue(), (int)params[1].getValue(), (int)params[2].getValue());
        case iMomentum_period_price:
          return iMomentum(symbol, tf, (int)params[0].getValue(), (ENUM_APPLIED_PRICE)params[1].getValue());
        case iMFI_period:
          return iMFI(symbol, tf, (int)params[0].getValue(), VOLUME_TICK);
        case iMA_period_shift_method_price:
          return iMA(symbol, tf, (int)params[0].getValue(), (int)params[1].getValue(), (ENUM_MA_METHOD)params[2].getValue(), (ENUM_APPLIED_PRICE)params[3].getValue());
        case dMACD_fast_slow_signal_price:
          return iMACD(symbol, tf, (int)params[0].getValue(), (int)params[1].getValue(), (int)params[2].getValue(), (ENUM_APPLIED_PRICE)params[3].getValue());
        case iOBV_price:
          return iOBV(symbol, tf, VOLUME_TICK); // params[0].getValue() - applied price is omitted in MT5
        case iOsMA_fast_slow_signal_price:
          return iOsMA(symbol, tf, (int)params[0].getValue(), (int)params[1].getValue(), (int)params[2].getValue(), (ENUM_APPLIED_PRICE)params[3].getValue());
        case iRSI_period_price:
          return iRSI(symbol, tf, (int)params[0].getValue(), (ENUM_APPLIED_PRICE)params[1].getValue());
        case dRVI_period:
          return iRVI(symbol, tf, (int)params[0].getValue());
        case iSAR_step_maximum:
          Print("SAR ", params[0].getType(), (double)params[0].getValue(), (double)params[1].getValue(), iSAR(symbol, tf, params[0].getValue(), params[1].getValue()));
          return iSAR(symbol, tf, params[0].getValue(), params[1].getValue());
        case iStdDev_period_shift_method_price:
          return iStdDev(symbol, tf, (int)params[0].getValue(), (int)params[1].getValue(), (ENUM_MA_METHOD)params[2].getValue(), (ENUM_APPLIED_PRICE)params[3].getValue());
        case dStochastic_K_D_slowing_method_price:
          return iStochastic(symbol, tf, (int)params[0].getValue(), (int)params[1].getValue(), (int)params[2].getValue(), (ENUM_MA_METHOD)params[3].getValue(), (ENUM_STO_PRICE)params[4].getValue());
        case iWPR_period:
          return iWPR(symbol, tf, (int)params[0].getValue());
        case iCustom_:
          {
            MqlParam p[];
            ArrayResize(p, ArraySize(params) + 1);
            p[0].type = TYPE_STRING;
            p[0].string_value = indicator;
            for(int i = 0; i < ArraySize(params); i++)
            {
              p[i + 1].type = params[i].getDataType();
              switch(p[i + 1].type)
              {
                case TYPE_INT:
                  p[i + 1].integer_value = (int)params[i].getValue();
                  break;
                case TYPE_DOUBLE:
                  p[i + 1].double_value = params[i].getValue();
                  break;
                case TYPE_STRING:
                  p[i + 1].string_value= params[i].getString();
                  break;
              }
            }
            return IndicatorCreate(symbol, tf, IND_CUSTOM, ArraySize(p), p);
          }
      }
      return INVALID_HANDLE;
    }
    
    #else // __MQL4__

    double executeStandard(const int buffer, const int bar)
    {
      switch(type)
      {
        case iAC_:
          return iAC(symbol, tf, bar);
        case iAD_:
          return iAD(symbol, tf, bar);
        case tADX_period_price:
          return iADX(symbol, tf, (int)params[0].getValue(), (int)params[1].getValue(), buffer, bar);
        case tAlligator_jawP_jawS_teethP_teethS_lipsP_lipsS_method_price:
          return iAlligator(symbol, tf, (int)params[0].getValue(), (int)params[1].getValue(), (int)params[2].getValue(), (int)params[3].getValue(), (int)params[4].getValue(), (int)params[5].getValue(), (int)params[6].getValue(), (int)params[7].getValue(), buffer, bar);
        case iAO_:
          return iAO(symbol, tf, bar);
        case iATR_period:
          return iATR(symbol, tf, (int)params[0].getValue(), bar);
        case tBands_period_deviation_shift_price:
          return iBands(symbol, tf, (int)params[0].getValue(), params[1].getValue(), (int)params[2].getValue(), (int)params[3].getValue(), buffer, bar);
        case iBearsPower_period_price:
          return iBearsPower(symbol, tf, (int)params[0].getValue(), (int)params[1].getValue(), bar);
        case iBullsPower_period_price:
          return iBullsPower(symbol, tf, (int)params[0].getValue(), (int)params[1].getValue(), bar);
        case iBWMFI_:
          return iBWMFI(symbol, tf, bar);
        case iCCI_period_price:
          return iCCI(symbol, tf, (int)params[0].getValue(), (int)params[1].getValue(), bar);
        case iDeMarker_period:
          return iDeMarker(symbol, tf, (int)params[0].getValue(), bar);
        case tEnvelopes_period_method_shift_price_deviation:
          return iEnvelopes(symbol, tf, (int)params[0].getValue(), (int)params[1].getValue(), (int)params[2].getValue(), (int)params[3].getValue(), params[4].getValue(), buffer, bar);
        case iForce_period_method_price:
          return iForce(symbol, tf, (int)params[0].getValue(), (int)params[1].getValue(), (int)params[2].getValue(), bar);
        case dFractals:
          return iFractals(symbol, tf, buffer, bar);
        case dGator_jawP_jawS_teethP_teethS_lipsP_lipsS_method_price:
          return iGator(symbol, tf, (int)params[0].getValue(), (int)params[1].getValue(), (int)params[2].getValue(), (int)params[3].getValue(), (int)params[4].getValue(), (int)params[5].getValue(), (int)params[6].getValue(), (int)params[7].getValue(), buffer, bar);
        case fIchimoku_tenkan_kijun_senkou:
          return iIchimoku(symbol, tf, (int)params[0].getValue(), (int)params[1].getValue(), (int)params[2].getValue(), buffer, bar);
        case iMomentum_period_price:
          return iMomentum(symbol, tf, (int)params[0].getValue(), (int)params[1].getValue(), bar);
        case iMFI_period:
          return iMFI(symbol, tf, (int)params[0].getValue(), bar);
        case iMA_period_shift_method_price:
          return iMA(symbol, tf, (int)params[0].getValue(), (int)params[1].getValue(), (int)params[2].getValue(), (int)params[3].getValue(), bar);
        case dMACD_fast_slow_signal_price:
          return iMACD(symbol, tf, (int)params[0].getValue(), (int)params[1].getValue(), (int)params[2].getValue(), (int)params[3].getValue(), buffer, bar);
        case iOBV_price:
          return iOBV(symbol, tf, (int)params[0].getValue(), bar);
        case iOsMA_fast_slow_signal_price:
          return iOsMA(symbol, tf, (int)params[0].getValue(), (int)params[1].getValue(), (int)params[2].getValue(), (int)params[3].getValue(), bar);
        case iRSI_period_price:
          return iRSI(symbol, tf, (int)params[0].getValue(), (int)params[1].getValue(), bar);
        case dRVI_period:
          return iRVI(symbol, tf, (int)params[0].getValue(), buffer, bar);
        case iSAR_step_maximum:
          Print("SAR ", params[0].getType(), (double)params[0].getValue(), (double)params[1].getValue(), iSAR(symbol, tf, params[0].getValue(), params[1].getValue(), bar));
          return iSAR(symbol, tf, params[0].getValue(), params[1].getValue(), bar);
        case iStdDev_period_shift_method_price:
          return iStdDev(symbol, tf, (int)params[0].getValue(), (int)params[1].getValue(), (int)params[2].getValue(), (int)params[3].getValue(), bar);
        case dStochastic_K_D_slowing_method_price:
          return iStochastic(symbol, tf, (int)params[0].getValue(), (int)params[1].getValue(), (int)params[2].getValue(), (int)params[3].getValue(), (int)params[4].getValue(), buffer, bar);
        case iWPR_period:
          return iWPR(symbol, tf, (int)params[0].getValue(), bar);
      }
      return EMPTY_VALUE;
    }
    #endif
    
    bool checkForNumericals()
    {
      for(int i = 0; i < pnum; i++)
      {
        if(!params[i].isNumerical())
        {
          Print(EnumToString(type) + " parameter ", (i + 1), " must be a number, ", params[i].getType(), " given");
          return false;
        }
      }
      return true;
    }
    
    
    bool checkStandard()
    {
      if(IndicatR::paramCountsByType[type] != pnum)
      {
        Print(EnumToString(type) + " requires " + (string)IndicatR::paramCountsByType[type] + " parameters, " + (string)pnum + " given");
        return false;
      }
      
      return checkForNumericals();
    }
    

  public:
    Custom(const int _index, const string name, const string data, const string bus, const int bar, const IndicatorType t = iCustom_, const string _symbol = NULL, const ENUM_TIMEFRAMES _tf = 0): index(_index), indicator(name), _bar(bar), type(t)
    {
      _buffer = lookUpBufferLiterals(bus);
      if(_buffer == -1)
      {
        _buffer = (int)StringToInteger(bus);
      }
      
      symbol = _symbol == NULL ? _Symbol : _symbol;
      tf = _tf;
      paramstr = data;
      pnum = ParseParameters(data);
      if(pnum > MAX_PARAM_NUM)
      {
        Print("Too many parameters, maximum:", MAX_PARAM_NUM);
        initOk = false;
      }
      else
      {
        #ifdef __MQL5__
        handle = create();
        #endif
        
        if(type != iCustom_)
        {
          initOk = checkStandard();
        }
        else
        {
          initOk = true;
        }
        
        #ifdef __MQL5__
        initOk = initOk && (handle != INVALID_HANDLE);
        if(initOk)
        {
          if(index != -1)
          {
            // _commonFunctionTable
            IndicatR::ec.functionTable().add(new Func_CopyBuffer("IND" + (string)(index + 1), handle));
          }
          else
          if(StringLen(name) > 0)
          {
            // _commonFunctionTable
            IndicatR::ec.functionTable().add(new Func_CopyBuffer(name, handle));
          }
          else
          {
            PrintFormat("Incompatible indicator setup: %d %s %s", index, name, data);
          }
        }
        #endif
      }
    }
    
    virtual ~Custom()
    {
      for(int i = 0; i < pnum; i++)
      {
        delete params[i];
      }
    }

    #ifdef __MQL4__

    double CallCustom1(int buffer, int bar)
    {
      return iCustom(symbol, tf, indicator, params[0].getValue(), buffer, bar);
    }
    
    double CallCustom2(int buffer, int bar)
    {
      return iCustom(symbol, tf, indicator, params[0].getValue(), params[1].getValue(), buffer, bar);
    }

    double CallCustom3(int buffer, int bar)
    {
      return iCustom(symbol, tf, indicator, params[0].getValue(), params[1].getValue(), params[2].getValue(), buffer, bar);
    }
    
    double CallCustom4(int buffer, int bar)
    {
      return iCustom(symbol, tf, indicator, params[0].getValue(), params[1].getValue(), params[2].getValue(), params[3].getValue(), buffer, bar);
    }

    double CallCustom5(int buffer, int bar)
    {
      return iCustom(symbol, tf, indicator, params[0].getValue(), params[1].getValue(), params[2].getValue(), params[3].getValue(), params[4].getValue(), buffer, bar);
    }

    double CallCustom6(int buffer, int bar)
    {
      return iCustom(symbol, tf, indicator, params[0].getValue(), params[1].getValue(), params[2].getValue(), params[3].getValue(), params[4].getValue(), params[5].getValue(), buffer, bar);
    }

    double CallCustom7(int buffer, int bar)
    {
      return iCustom(symbol, tf, indicator, params[0].getValue(), params[1].getValue(), params[2].getValue(), params[3].getValue(), params[4].getValue(), params[5].getValue(), params[6].getValue(), buffer, bar);
    }

    double CallCustom8(int buffer, int bar)
    {
      return iCustom(symbol, tf, indicator, params[0].getValue(), params[1].getValue(), params[2].getValue(), params[3].getValue(), params[4].getValue(), params[5].getValue(), params[6].getValue(), params[7].getValue(), buffer, bar);
    }
    
    double CallCustom9(int buffer, int bar)
    {
      return iCustom(symbol, tf, indicator, params[0].getValue(), params[1].getValue(), params[2].getValue(), params[3].getValue(), params[4].getValue(), params[5].getValue(), params[6].getValue(), params[7].getValue(), params[8].getValue(), buffer, bar);
    }

    double CallCustom10(int buffer, int bar)
    {
      return iCustom(symbol, tf, indicator, params[0].getValue(), params[1].getValue(), params[2].getValue(), params[3].getValue(), params[4].getValue(), params[5].getValue(), params[6].getValue(), params[7].getValue(), params[8].getValue(), params[9].getValue(), buffer, bar);
    }
    
    double CallCustom11(int buffer, int bar)
    {
      return
      iCustom(symbol, tf, indicator,
      params[0].getValue(), params[1].getValue(), params[2].getValue(), params[3].getValue(), params[4].getValue(), params[5].getValue(), params[6].getValue(), params[7].getValue(), params[8].getValue(), params[9].getValue(),
      params[10].getValue(),
      buffer, bar);
    }

    double CallCustom12(int buffer, int bar)
    {
      return
      iCustom(symbol, tf, indicator,
      params[0].getValue(), params[1].getValue(), params[2].getValue(), params[3].getValue(), params[4].getValue(), params[5].getValue(), params[6].getValue(), params[7].getValue(), params[8].getValue(), params[9].getValue(),
      params[10].getValue(), params[11].getValue(),
      buffer, bar);
    }

    double CallCustom13(int buffer, int bar)
    {
      return
      iCustom(symbol, tf, indicator,
      params[0].getValue(), params[1].getValue(), params[2].getValue(), params[3].getValue(), params[4].getValue(), params[5].getValue(), params[6].getValue(), params[7].getValue(), params[8].getValue(), params[9].getValue(),
      params[10].getValue(), params[11].getValue(), params[12].getValue(),
      buffer, bar);
    }

    double CallCustom14(int buffer, int bar)
    {
      return
      iCustom(symbol, tf, indicator,
      params[0].getValue(), params[1].getValue(), params[2].getValue(), params[3].getValue(), params[4].getValue(), params[5].getValue(), params[6].getValue(), params[7].getValue(), params[8].getValue(), params[9].getValue(),
      params[10].getValue(), params[11].getValue(), params[12].getValue(), params[13].getValue(),
      buffer, bar);
    }

    double CallCustom15(int buffer, int bar)
    {
      return
      iCustom(symbol, tf, indicator,
      params[0].getValue(), params[1].getValue(), params[2].getValue(), params[3].getValue(), params[4].getValue(), params[5].getValue(), params[6].getValue(), params[7].getValue(), params[8].getValue(), params[9].getValue(),
      params[10].getValue(), params[11].getValue(), params[12].getValue(), params[13].getValue(), params[14].getValue(),
      buffer, bar);
    }

    double CallCustom16(int buffer, int bar)
    {
      return
      iCustom(symbol, tf, indicator,
      params[0].getValue(), params[1].getValue(), params[2].getValue(), params[3].getValue(), params[4].getValue(), params[5].getValue(), params[6].getValue(), params[7].getValue(), params[8].getValue(), params[9].getValue(),
      params[10].getValue(), params[11].getValue(), params[12].getValue(), params[13].getValue(), params[14].getValue(), params[15].getValue(),
      buffer, bar);
    }

    double CallCustom17(int buffer, int bar)
    {
      return
      iCustom(symbol, tf, indicator,
      params[0].getValue(), params[1].getValue(), params[2].getValue(), params[3].getValue(), params[4].getValue(), params[5].getValue(), params[6].getValue(), params[7].getValue(), params[8].getValue(), params[9].getValue(),
      params[10].getValue(), params[11].getValue(), params[12].getValue(), params[13].getValue(), params[14].getValue(), params[15].getValue(), params[16].getValue(),
      buffer, bar);
    }

    double CallCustom18(int buffer, int bar)
    {
      return
      iCustom(symbol, tf, indicator,
      params[0].getValue(), params[1].getValue(), params[2].getValue(), params[3].getValue(), params[4].getValue(), params[5].getValue(), params[6].getValue(), params[7].getValue(), params[8].getValue(), params[9].getValue(),
      params[10].getValue(), params[11].getValue(), params[12].getValue(), params[13].getValue(), params[14].getValue(), params[15].getValue(), params[16].getValue(), params[17].getValue(),
      buffer, bar);
    }

    double CallCustom19(int buffer, int bar)
    {
      return
      iCustom(symbol, tf, indicator,
      params[0].getValue(), params[1].getValue(), params[2].getValue(), params[3].getValue(), params[4].getValue(), params[5].getValue(), params[6].getValue(), params[7].getValue(), params[8].getValue(), params[9].getValue(),
      params[10].getValue(), params[11].getValue(), params[12].getValue(), params[13].getValue(), params[14].getValue(), params[15].getValue(), params[16].getValue(), params[17].getValue(), params[18].getValue(),
      buffer, bar);
    }

    double CallCustom20(int buffer, int bar)
    {
      return
      iCustom(symbol, tf, indicator,
      params[0].getValue(), params[1].getValue(), params[2].getValue(), params[3].getValue(), params[4].getValue(), params[5].getValue(), params[6].getValue(), params[7].getValue(), params[8].getValue(), params[9].getValue(),
      params[10].getValue(), params[11].getValue(), params[12].getValue(), params[13].getValue(), params[14].getValue(), params[15].getValue(), params[16].getValue(), params[17].getValue(), params[18].getValue(), params[19].getValue(),
      buffer, bar);
    }
    
    double execute(const int buffer, const int bar)
    {
      if(!initOk) return EMPTY_VALUE;
      
      if(type != iCustom_) return executeStandard(buffer, bar);
      
      switch(pnum)
      {
        case 0:
          return iCustom(symbol, tf, indicator, buffer, bar);
        case 1:
          return CallCustom1(buffer, bar);
        case 2:
          return CallCustom2(buffer, bar);
        case 3:
          return CallCustom3(buffer, bar);
        case 4:
          return CallCustom4(buffer, bar);
        case 5:
          return CallCustom5(buffer, bar);
        case 6:
          return CallCustom6(buffer, bar);
        case 7:
          return CallCustom7(buffer, bar);
        case 8:
          return CallCustom8(buffer, bar);
        case 9:
          return CallCustom9(buffer, bar);
        case 10:
          return CallCustom10(buffer, bar);
        case 11:
          return CallCustom11(buffer, bar);
        case 12:
          return CallCustom12(buffer, bar);
        case 13:
          return CallCustom13(buffer, bar);
        case 14:
          return CallCustom14(buffer, bar);
        case 15:
          return CallCustom15(buffer, bar);
        case 16:
          return CallCustom16(buffer, bar);
        case 17:
          return CallCustom17(buffer, bar);
        case 18:
          return CallCustom18(buffer, bar);
        case 19:
          return CallCustom19(buffer, bar);
        case 20:
          return CallCustom20(buffer, bar);
      }
      return EMPTY_VALUE;
    }

    #else // __MQL5__

    double execute(const int buffer, const int bar)
    {
      if(!initOk) return EMPTY_VALUE;
      
      return executeStandard(buffer, bar);
    }

    #endif

    double execute()
    {
      if(!initOk) return EMPTY_VALUE;
      
      return execute(_buffer, _bar);
    }
    
    bool isOk()
    {
      return initOk;
    }

    string getFullName() const
    {
      CFormatOut out(_Digits, 0);
      return out << EN(type) << " " << indicator << "@" << _buffer << "(" << paramstr << ")[" << _bar << "]" >> true;
    }
    
};


//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
static int handleInit(const string _symbol = NULL, const ENUM_TIMEFRAMES _tf = 0)
{
  for(int i = 0; i < MAX_INDICATOR_NUM; i++)
  {
    if(GetIndicatorName(i) != "" || GetIndicatorSelector(i) != iCustom_)
    {
      Print("Initializing ", GetIndicatorFullName(i));
      ci << new Custom(i, GetIndicatorName(i), GetIndicatorList(i), GetIndicatorBuffer(i), GetIndicatorBar(i), GetIndicatorSelector(i), _symbol, _tf);
    }
  }
  
  for(int i = 0; i < MAX_SIGNAL_NUM; i++)
  {
    if(GetCondition(i) != Disabled)
    {
      Print("Analyzing condition ", (CharToString((uchar)('A' + i))), " ", GetSignal(i), " ", EnumToString(GetCondition(i)));
      IndicatR::co << new Condition(i, GetCondition(i), GetIndicator(i, 0), GetIndicator(i, 1), GetLevel(i, 0), GetLevel(i, 1), GetDirection(i), GetExecution(i));
    }
  }

  int n = ci.size();
  ArrayResize(V, n);
  ArrayInitialize(V, EMPTY_VALUE);
  ArrayResize(W, n);
  ArrayInitialize(W, EMPTY_VALUE);
  
  Print("Indicators specified: ", n);
  
  int good = 0;
  for(int i = 0; i < n; i++)
  {
    #ifdef __MQL4__
    GetLastError();
    V[i] = ci[i].execute();
    int ec = GetLastError();
    if(ec != 0)
    {
      Print("Indicator ", (i + 1), " failed:", ec);
    }
    else
    {
      good += ci[i].isOk();
    }
    #else
    good += ci[i].isOk();
    #endif
  }

  Print("Indicators successfully initialized: ", good);

  if(good < n) return INIT_FAILED;
  
  n = co.size();
  good = 0;
  for(int i = 0; i < n; i++)
  {
    good += co[i].isOk();
  }
  
  Print("Conditions successfully initialized: ", good);

  if(good < n) return INIT_FAILED;
  
  return INIT_SUCCEEDED;
}


class TradeSignals
{
  public:
    bool alert;
    bool buy;
    bool sell;
    bool buyExit;
    bool sellExit;
    bool ModifySL;
    bool ModifyTP;
    
    int index;
    double value;
    
    string message;
  
    TradeSignals(): alert(false), buy(false), sell(false), buyExit(false), sellExit(false), ModifySL(false), ModifyTP(false), value(EMPTY_VALUE), message(""){}
};


class Condition
{
  private:
    int index;
    SignalCondition cond;
    int ind1;
    int ind2;
    double lvl1;
    double lvl2;
    UpZeroDown dir;
    SignalType exec;
    
    bool initOk;
    
    int parseIndicator(const string &data)
    {
      string name;
      string params;
      int bar = 0;
      
      int p = StringFind(data, "(");
      if(p > -1)
      {
        int pp = StringFind(data, ")", p);
        if(pp < p)
        {
          Print("Indicator signature is incorrect: missing ')' - " + data);
          return 0;
        }
        if(pp - p - 1 > 0)
        {
          params = StringSubstr(data, p + 1, pp - p - 1);
        }
      }
      
      int ba = StringFind(data, "[");
      if(ba > -1)
      {
        bar = StringFind(data, "]", ba);
        if(bar < ba)
        {
          Print("Indicator signature is incorrect: missing ']' - " + data);
          return 0;
        }
        if(bar - ba - 1 > 0)
        {
          string sb = StringSubstr(data, ba + 1, bar - ba - 1);
          bar = (int)StringToInteger(sb);
        }
        else
        {
          bar = 0;
        }
      }
      
      int x = MathMin(p, ba);
      if(x == -1)
      {
        x = MathMax(p, ba);
      }
      
      if(x > 0)
      {
        name = StringSubstr(data, 0, x);
      }
      else
      {
        name = data;
      }

      string bus;
      int buf = StringFind(name, "@");
      if(buf > -1)
      {
        bus = StringSubstr(name, buf + 1);
        name = StringSubstr(name, 0, buf);
      }
      
      IndicatorType t = IndicatR::lookUpStandardIndicator(name);
      
      Print("Initializing adhoc ", name, "@", bus, "(", params, ")[", bar, "], type=", EnumToString(t));
      
      if(t != iCustom_) name = "";
      
      IndicatR::ci << new Custom(-1, name, params, bus, bar, t);
      
      return IndicatR::ci.size();
      
    }
    
  public:
    Condition(const int i, const SignalCondition c, const string si1, const string si2, const double l1, const double l2, const UpZeroDown d, const SignalType st):
      index(i), cond(c), lvl1(l1), lvl2(l2), dir(d), exec(st), ind1(0), ind2(0)
    {
      initOk = false;
      int lead = -1;
      
      if(cond != ExpressionByIndicatorX && StringLen(si1) > 0)
      {
        lead = (int)StringToInteger(si1); // 0 means not a number, assume indicator id
        if(lead == 0)
        {
          ind1 = parseIndicator(si1);
        }
        else
        {
          ind1 = lead;
        }
      }
      
      if(StringLen(si2) > 0)
      {
        lead = (int)StringToInteger(si2);
        if(lead == 0)
        {
          ind2 = parseIndicator(si2);
        }
        else
        {
          ind2 = lead;
        }
      }
      
      
      int n = IndicatR::ci.size();
      string bad;
      
      CFormatOut out(_Digits, 0);
      
      if(cond == IndicatorXcrossesIndicatorY || cond == IndicatorXrelatesToIndicatorY)
      {
        initOk = ind1 > 0 && ind1 < n + 1 && ind2 > 0 && ind2 < n + 1;
        if(!initOk)
        {
          bad = out << ": IndicatorX=" << ind1 << ", IndicatorY=" << ind2 << ", can be " << (n > 0 ? (n == 1? "1" : "1.." + (string)n) : "1 after adding an indicator (currently 0)") >> true;
        }
      }
      else if(cond == NotEmptyIndicatorX || cond == SignOfValueIndicatorX || cond == IndicatorXcrossesLevelX || cond == IndicatorXrelatesToLevelX)
      {
        initOk = ind1 > 0 && ind1 < n + 1;
        if(!initOk)
        {
          bad = out << ": IndicatorX=" << ind1 << ", can be " << (n > 0 ? (n == 1? "1" : "1.." + (string)n) : "1 after adding an indicator (currently 0)") >> true;
        }
      }
      else if(cond == ExpressionByIndicatorX)
      {
        IndicatR::vti.adhocAllocation(true);
        IndicatR::pExpr[index] = IndicatR::ec.evaluate(IndicatR::GetIndicator(index, 0));
        IndicatR::pExpr[index].print();
        IndicatR::ec.detachResult();
        if(!IndicatR::ec.success())
        {
          Print("Syntax error in the signal:", IndicatR::GetIndicator(index, 0));
          IndicatR::pExpr[index].print();
        }
        else
        {
          initOk = true;
        }
      }
      else
      {
        initOk = true;
      }
      if(!initOk)
      {
        Print("Wrong indicator reference in Condition " + CharToString((uchar)('A' + i)) + " " + getDescription() + " " + bad);
      }
    }
    
    bool isOk()
    {
      return initOk;
    }
    
    SignalType getExecution() const
    {
      return exec;
    }
      
    bool check(const string _symbol = NULL, const ENUM_TIMEFRAMES _tf = 0)
    {
      if(!initOk) return false;
    
      int n = IndicatR::ci.size();
      
      if(cond == IndicatorXcrossesIndicatorY)
      {
        if(IndicatR::W[ind1 - 1] != EMPTY_VALUE && IndicatR::W[ind2 - 1] != EMPTY_VALUE
        && IndicatR::V[ind1 - 1] != EMPTY_VALUE && IndicatR::V[ind2 - 1] != EMPTY_VALUE)
        {
          if(dir == UpSideOrAboveOrPositve)
          {
            if(IndicatR::V[ind1 - 1] > IndicatR::V[ind2 - 1] && IndicatR::W[ind1 - 1] <= IndicatR::W[ind2 - 1])
            {
              return true;
            }
          }
          else if(dir == DownSideOrBelowOrNegative)
          {
            if(IndicatR::V[ind1 - 1] < IndicatR::V[ind2 - 1] && IndicatR::W[ind1 - 1] >= IndicatR::W[ind2 - 1])
            {
              return true;
            }
          }
        }
      }
      else if(cond == NotEmptyIndicatorX)
      {
        if(IndicatR::V[ind1 - 1] != EMPTY_VALUE)
        {
          return true;
        }
      }
      else if(cond == SignOfValueIndicatorX)
      {
        if(IndicatR::V[ind1 - 1] != EMPTY_VALUE)
        {
          if(dir == UpSideOrAboveOrPositve && IndicatR::V[ind1 - 1] > 0/* && W[ind1 - 1] <= 0*/)
          {
            return true;
          }
          if(dir == DownSideOrBelowOrNegative && IndicatR::V[ind1 - 1] < 0/* && V[ind1 - 1] >= 0*/)
          {
            return true;
          }
        }
        else
        {
          if(dir == EqualOrNone) // none means empty
          {
            return true;
          }
        }
      }
      else if(cond == IndicatorXcrossesLevelX)
      {
        if(IndicatR::V[ind1 - 1] != EMPTY_VALUE && IndicatR::W[ind1 - 1] != EMPTY_VALUE)
        {
          if(dir == UpSideOrAboveOrPositve)
          {
            if(IndicatR::V[ind1 - 1] > lvl1 && IndicatR::W[ind1 - 1] <= lvl1)
            {
              return true;
            }
          }
          else if(dir == DownSideOrBelowOrNegative)
          {
            if(IndicatR::V[ind1 - 1] < lvl1 && IndicatR::W[ind1 - 1] >= lvl1)
            {
              return true;
            }
          }
        }
      }
      else if(cond == IndicatorXrelatesToIndicatorY)
      {
        if(IndicatR::V[ind1 - 1] != EMPTY_VALUE && IndicatR::V[ind2 - 1] != EMPTY_VALUE)
        {
          if(ind1 == ind2)
          {
            if(dir == UpSideOrAboveOrPositve)
            {
              if(IndicatR::V[ind1 - 1] > IndicatR::W[ind1 - 1])
              {
                return true;
              }
            }
            else if(dir == DownSideOrBelowOrNegative)
            {
              if(IndicatR::V[ind1 - 1] < IndicatR::W[ind1 - 1])
              {
                return true;
              }
            }
            else if(dir == EqualOrNone)
            {
              if(IndicatR::V[ind1 - 1] == IndicatR::W[ind1 - 1])
              {
                return true;
              }
            }
          }
          else
          {
            if(dir == UpSideOrAboveOrPositve)
            {
              if(IndicatR::V[ind1 - 1] > IndicatR::V[ind2 - 1])
              {
                return true;
              }
            }
            else if(dir == DownSideOrBelowOrNegative)
            {
              if(IndicatR::V[ind1 - 1] < IndicatR::V[ind2 - 1])
              {
                return true;
              }
            }
            else if(dir == EqualOrNone)
            {
              if(IndicatR::V[ind1 - 1] == IndicatR::V[ind2 - 1])
              {
                return true;
              }
            }
          }
        }
      }
      else if(cond == IndicatorXrelatesToLevelX)
      {
        if(IndicatR::V[ind1 - 1] != EMPTY_VALUE)
        {
          if(dir == UpSideOrAboveOrPositve)
          {
            if(IndicatR::V[ind1 - 1] > lvl1)
            {
              return true;
            }
          }
          else if(dir == DownSideOrBelowOrNegative)
          {
            if(IndicatR::V[ind1 - 1] < lvl1)
            {
              return true;
            }
          }
          else if(dir == EqualOrNone)
          {
            if(IndicatR::V[ind1 - 1] == lvl1)
            {
              return true;
            }
          }
          else if(dir == NotEqual)
          {
            if(IndicatR::V[ind1 - 1] != lvl1)
            {
              return true;
            }
          }
        }
      }
      else if(cond == ExpressionByIndicatorX)
      {
        if(CheckPointer(IndicatR::pExpr[index]) != POINTER_INVALID)
        {
          IndicatR::ec.functionTable().setSymbol(_symbol);
          IndicatR::ec.functionTable().setTimeframe(_tf);
          const double result = IndicatR::pExpr[index].resolve();
          // IndicatR::pExpr[index].print();
          // Print("result=", result);
          if(dir == UpSideOrAboveOrPositve)
          {
            return result > 0;
          }
          else if(dir == DownSideOrBelowOrNegative)
          {
            return result < 0;
          }
          else if(dir == EqualOrNone)
          {
            return (bool)result;
          }
        }
      }
      
      return false;
    }
    
    string getDescription() const
    {
      string r = EnumToString(cond);
      string d = EnumToString(dir);
      
      if(!initOk) return r + " " + d;
    
      CFormatOut out(_Digits, ' ');
      
      int n = IndicatR::ci.size();
      Custom *c1 = ind1 > 0 && ind1 < n + 1 ? IndicatR::ci[ind1 - 1] : NULL;
      Custom *c2 = ind2 > 0 && ind2 < n + 1 ? IndicatR::ci[ind2 - 1] : NULL;
      
      if(cond == IndicatorXcrossesIndicatorY)
      {
        return out << r << d << c1.getFullName() << "=" << IndicatR::V[ind1 - 1] << "/" << c2.getFullName() << "=" << IndicatR::V[ind2 - 1] >> false;
      }
      else if(cond == NotEmptyIndicatorX)
      {
        return out << r << c1.getFullName() << "=" << IndicatR::V[ind1 - 1] >> false;
      }
      else if(cond == SignOfValueIndicatorX)
      {
        return out << r << d << c1.getFullName() << "=" << IndicatR::V[ind1 - 1] >> false;
      }
      else if(cond == IndicatorXcrossesLevelX)
      {
        return out << r << d << c1.getFullName() << "=" << IndicatR::V[ind1 - 1] << "vs" << lvl1 >> false;
      }
      else if(cond == IndicatorXrelatesToIndicatorY)
      {
        return out << r << d << c1.getFullName() << "=" << IndicatR::V[ind1 - 1] << "/" << c2.getFullName() << "=" << IndicatR::V[ind2 - 1] >> false;
      }
      else if(cond == IndicatorXrelatesToLevelX)
      {
        return out << r << d << c1.getFullName() << "=" << IndicatR::V[ind1 - 1] << "vs" << lvl1 >> false;
      }
      
      return r + " " + d;
    }
    
    void execute(TradeSignals &result)
    {
      if(!initOk) return;

      string s = "Condition " + CharToString((uchar)('A' + index)) + ": " + getDescription() + " ";
      result.value = EMPTY_VALUE;
      result.index = index;
      
      if(exec == ProceedToNextCondition)
      {
        Print(s + "proceed");
        // continue
      }
      else if(exec == Buy)
      {
        result.message = s + "buy";
        result.buy = true;
        // 0 means buy by current price (market),
        // value higher than price stop buy order,
        // value lower than price limit buy order
        if(ind1 > 0) result.value = IndicatR::V[ind1 - 1];
      }
      else if(exec == Sell)
      {
        result.message = s + "sell";
        result.sell = true;
        if(ind1 > 0) result.value = IndicatR::V[ind1 - 1];
      }
      else if(exec == CloseBuy)
      {
        result.message = s + "buy close";
        result.buyExit = true;
      }
      else if(exec == CloseSell)
      {
        result.message = s + "sell close";
        result.sellExit = true;
      }
      else if(exec == CloseAll)
      {
        result.message = s + "all close";
        result.buyExit = true;
        result.sellExit = true;
      }
      else if(exec == BuyAndCloseSell)
      {
        result.message = s + "buy/exit sell";
        result.buy = true;
        result.sellExit = true;
      }
      else if(exec == SellAndCloseBuy)
      {
        result.message = s + "sell/exit buy";
        result.sell = true;
        result.buyExit = true;
      }
      else if(exec == ModifyStopLoss)
      {
        CFormatOut out(_Digits, ' ');
        result.ModifySL = true;
        if(ind1 > 0)
        {
          result.message = (out << s << "modify SL:" << IndicatR::V[ind1 - 1] >> false);
          result.value = IndicatR::V[ind1 - 1];
        }
      }
      else if(exec == ModifyTakeProfit)
      {
        CFormatOut out(_Digits, ' ');
        result.ModifyTP = true;
        if(ind1 > 0)
        {
          result.message = (out << s << "modify TP:" << IndicatR::V[ind1 - 1] >> false);
          result.value = IndicatR::V[ind1 - 1];
        }
      }
      else if(exec == Alert_)
      {
        result.alert = true;
        result.message = s;
      }
      else
      {
        Print(s);
      }
    }
};

static const RubbArray<TradeSignals> *getSignals()
{
  return &ts;
}

//+------------------------------------------------------------------+
//| expert start/tick function                                       |
//+------------------------------------------------------------------+
static const RubbArray<TradeSignals> *handleStart(const string _symbol = NULL, const ENUM_TIMEFRAMES _tf = 0)
{
  static datetime lastBar = 0;
  ts.clear();
  
  if(lastBar != iTime(_symbol, _tf, 0))
  {
    ArrayCopy(W, V);
    lastBar = iTime(_symbol, _tf, 0);
  }
  
  for(int i = 0; i < ArraySize(V); i++)
  {
    V[i] = ci[i].execute();
    vti.set("C" + (string)(i + 1), V[i]);
    vti.set("P" + (string)(i + 1), W[i]);
  }
  
  for(int i = 0; i < co.size(); i++)
  {
    if(co[i].check(_symbol, _tf))
    {
      TradeSignals *result = new TradeSignals();
      co[i].execute(result);
      ts << result;
    }
    else
    {
      while(co[i].getExecution() == ProceedToNextCondition && i < MAX_SIGNAL_NUM)
      {
        i++;
      }
    }
  }
  
  return &ts;
}


private:

static int paramCountsByType[];
static VariableTable vti; // indicators injected into expressions (common table)
static ExpressionCompiler ec;
static Promise *pExpr[MAX_SIGNAL_NUM];

static RubbArray<Custom> ci;
static double V[], W[];

static RubbArray<Condition> co;
static RubbArray<TradeSignals> ts;

// end of the context
//+------------------------------------------------------------------+
};

static RubbArray<#ifdef __MQL5__ IndicatR:: #endif Custom> IndicatR::ci;
static RubbArray<#ifdef __MQL5__ IndicatR:: #endif Condition> IndicatR::co;
static RubbArray<#ifdef __MQL5__ IndicatR:: #endif TradeSignals> IndicatR::ts;
static double IndicatR::V[], IndicatR::W[];
static int IndicatR::paramCountsByType[] =
{
  0, // iCustom
  
  0, // iAC,
  0, // iAD,
  2, // iADX,
  8, // iAlligator,
  0, // iAO,
  1, // iATR,
  4, // iBands,
  2, // iBearsPower,
  2, // iBullsPower,
  0, // iBWMFI,
  2, // iCCI,
  1, // iDeMarker,
  5, // iEnvelopes,
  3, // iForce,
  0, // iFractals,
  8, // iGator,
  3, // iIchimoku,
  2, // iMomentum,
  1, // iMFI,
  4, // iMA,
  4, // iMACD,
  1, // iOBV,
  4, // iOsMA,
  2, // iRSI,
  1, // iRVI,
  2, // iSAR,
  4, // iStdDev,
  5, // iStochastic,
  1, // iWPR
  0
};

static VariableTable IndicatR::vti; // indicators (common table)
static ExpressionCompiler IndicatR::ec(IndicatR::vti);
static Promise *IndicatR::pExpr[MAX_SIGNAL_NUM] = {NULL};
