//+------------------------------------------------------------------+
//|                                   Divergence_CalculateSignal.mqh |
//|                                        Copyright 2015, Scriptong |
//|                                          http://advancetools.net |
//+------------------------------------------------------------------+
#property copyright "Scriptong"
#property link      "http://advancetools.net"

#property strict

#define ERROR_UNKNOWN_SYMBOL                       4301
#define ERROR_SYMBOL_NOT_SELECT                    4302
#define ERROR_SYMBOL_PARAMETER                     4303

#define PREFIX                                  "DIVVIEW_MS"                                       // Префикс имени графических объектов, отображаемых индикатором 

#define TITLE_CLASS_A                           "ClassA"
#define TITLE_CLASS_B                           "ClassB"
#define TITLE_CLASS_C                           "ClassC"
#define TITLE_CLASS_H                           "Hidden"
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum ENUM_INDICATOR_TYPE
  {
   RSI,                                                                                            // RSI
   MACD,                                                                                           // MACD   
   MOMENTUM,                                                                                       // Momentum
   RVI,                                                                                            // RVI
   STOCHASTIC,                                                                                     // Stochastic
   StdDev,                                                                                         // Standart deviation
   DERIVATIVE,                                                                                     // Derivative / производная
   WILLIAM_BLAU,                                                                                   // William Blau
   CUSTOM                                                                                          // Custom / Пользовательский
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum ENUM_MARKET_APPLIED_PRICE
  {
   MARKET_APPLIED_PRICE_CLOSE,                                                                     // Close/Close   
   MARKET_APPLIED_PRICE_HIGHLOW                                                                    // High/Low   
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum ENUM_CUSTOM_PARAM_CNT
  {
   PARAM_CNT_0,                                                                                    // 0   
   PARAM_CNT_1,                                                                                    // 1   
   PARAM_CNT_2,                                                                                    // 2   
   PARAM_CNT_3,                                                                                    // 3   
   PARAM_CNT_4,                                                                                    // 4   
   PARAM_CNT_5,                                                                                    // 5   
   PARAM_CNT_6,                                                                                    // 6   
   PARAM_CNT_7,                                                                                    // 7   
   PARAM_CNT_8,                                                                                    // 8   
   PARAM_CNT_9,                                                                                    // 9   
   PARAM_CNT_10,                                                                                   // 10   
   PARAM_CNT_11,                                                                                   // 11   
   PARAM_CNT_12,                                                                                   // 12   
   PARAM_CNT_13,                                                                                   // 13   
   PARAM_CNT_14,                                                                                   // 14   
   PARAM_CNT_15,                                                                                   // 15   
   PARAM_CNT_16,                                                                                   // 16   
   PARAM_CNT_17,                                                                                   // 17   
   PARAM_CNT_18,                                                                                   // 18   
   PARAM_CNT_19,                                                                                   // 19   
   PARAM_CNT_20                                                                                    // 20   
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum ENUM_MESSAGE_CODE
  {
   MESSAGE_CODE_UNKNOWN_SYMBOL,
   MESSAGE_CODE_NOT_ENOUGH_BARS,
   MESSAGE_CODE_NOT_ENOUGH_MEMORY,
   MESSAGE_CODE_POINT_ERROR
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum ENUM_EXTREMUM_TYPE
  {
   EXTREMUM_TYPE_NONE,
   EXTREMUM_TYPE_MIN,
   EXTREMUM_TYPE_MAX
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum ENUM_PRICE_TYPE
  {
   PRICE_TYPE_INDICATOR,
   PRICE_TYPE_MARKET
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum ENUM_BUFFER_MODE
  {
   BUFFER_MODE_LOW,
   BUFFER_MODE_HIGH,
   BUFFER_MODE_CLOSE,
   BUFFER_MODE_CUSTOM
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum ENUM_DIV_TYPE
  {
   DIV_TYPE_NONE,
   DIV_TYPE_BULLISH,
   DIV_TYPE_BEARISH
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
struct DivergenceData
  {
   ENUM_DIV_TYPE     type;                                                                      // Type of divergence: bearish or bullish
   color             divColor;                                                                  // Display color of divergence
   datetime          regTime;                                                                   // Registration time of divergence
   double            extremePrice;                                                              // Minimal price for bullish divergence and maximal price for bearich divergence
   string            divClass;                                                                  // Class of divergence: A, B, C or hidden

                     DivergenceData()
     {
      Init();
     }

   void Init()
     {
      type=DIV_TYPE_NONE;
      divColor=clrNONE;
      extremePrice=0.0;
      regTime=0;
      divClass="";
     }

   void operator=(const DivergenceData &divData)
     {
      type=divData.type;
      divColor= divData.divColor;
      regTime = divData.regTime;
      extremePrice=divData.extremePrice;
      divClass=divData.divClass;
     }
  };
// Finding the divergence at the one period of one symbol. It is used in Duvergence_Viewer_MultiSymbols indicator.
class CDivergence
  {
   bool              m_isActivate;
   bool              m_useCoincidenceCharts;
   bool              m_excludeOverlaps;

   string            m_symbol;
   ENUM_TIMEFRAMES   m_tf;

   ENUM_INDICATOR_TYPE m_indicatorType;
   int               m_divergenceDepth;

   int               m_barsPeriod1;
   int               m_barsPeriod2;
   int               m_barsPeriod3;
   ENUM_APPLIED_PRICE m_indAppliedPrice;
   ENUM_MA_METHOD    m_indMAMethod;

   int               m_findExtInterval;
   ENUM_MARKET_APPLIED_PRICE m_marketAppliedPrice;

   string            m_customName;
   int               m_customBuffer;
   ENUM_CUSTOM_PARAM_CNT m_customParamCnt;
   double            m_customParam1;
   double            m_customParam2;
   double            m_customParam3;
   double            m_customParam4;
   double            m_customParam5;
   double            m_customParam6;
   double            m_customParam7;
   double            m_customParam8;
   double            m_customParam9;
   double            m_customParam10;
   double            m_customParam11;
   double            m_customParam12;
   double            m_customParam13;
   double            m_customParam14;
   double            m_customParam15;
   double            m_customParam16;
   double            m_customParam17;
   double            m_customParam18;
   double            m_customParam19;
   double            m_customParam20;

   bool              m_showClassA;
   color             m_bullsDivAColor;
   color             m_bearsDivAColor;
   bool              m_showClassB;
   color             m_bullsDivBColor;
   color             m_bearsDivBColor;
   bool              m_showClassC;
   color             m_bullsDivCColor;
   color             m_bearsDivCColor;
   bool              m_showHidden;
   color             m_bullsDivHColor;
   color             m_bearsDivHColor;

   bool              m_useRussian;

   double            m_point;
   int               m_digits;
   int               m_lastBarsCnt;

   datetime          m_lastCalcTime;
   datetime          m_lastDivergenceTime;                                                            // Time of right bar with the divergence

   double            m_indBuffer[];
   double            m_addIndBuffer[];

   int               m_startBar;
   int               m_indBufferSize;
   int               m_addIndBufferSize;

   DivergenceData    m_lastDivergence;

public:
                     CDivergence(string symbol,ENUM_TIMEFRAMES tf,ENUM_INDICATOR_TYPE indicatorType,int divergenceDepth,int barsPeriod1,int barsPeriod2,int barsPeriod3,ENUM_APPLIED_PRICE indAppliedPrice,
                                                   ENUM_MA_METHOD indMAMethod,int findExtInterval,ENUM_MARKET_APPLIED_PRICE marketAppliedPrice,string customName,int customBuffer,ENUM_CUSTOM_PARAM_CNT customParamCnt,
                                                   double customParam1,double customParam2,double customParam3,double customParam4,double customParam5,double customParam6,double customParam7,double customParam8,
                                                   double customParam9,double customParam10,double customParam11,double customParam12,double customParam13,double customParam14,double customParam15,
                                                   double customParam16,double customParam17,double customParam18,double customParam19,double customParam20,bool coincidenceCharts,bool excludeOverlaps,bool showClassA,
                                                   color bullsDivAColor,color bearsDivAColor,bool showClassB,color bullsDivBColor,color bearsDivBColor,bool showClassC,color bullsDivCColor,color bearsDivCColor,
                                                   bool showHidden,color bullsDivHColor,color bearsDivHColor,int startBar);
                    ~CDivergence(void);
   bool              Init(void);
   void              ProcessTick(DivergenceData &divData);

private:
   string            GetStringByMessageCode(ENUM_MESSAGE_CODE messageCode) const;
   string            GetRussianMessage(ENUM_MESSAGE_CODE messageCode) const;
   string            GetEnglishMessage(ENUM_MESSAGE_CODE messageCode) const;

   void              RecalculateBaseIndicator(int barIndex,bool useFullCalculate);
   void              CalculateDataByWilliamBlau(int barIndex,int limit1,int limit2);
   double            GetBaseIndicatorValue(int barIndex) const;
   double            GetCustomIndicatorValue(int barIndex) const;
   double            GetPrice(int barIndex) const;

   void              ProcessBar(int barIndex,int total);
   bool              ProcessByIndicatorExtremum(int barIndex,int total);
   bool              ProcessByMarketExtremum(int barIndex,int total,ENUM_EXTREMUM_TYPE desiredExtremum);

   ENUM_EXTREMUM_TYPE GetBufferExtremum(int barIndex,int total,ENUM_BUFFER_MODE mode) const;
   int               GetMarketExtremumAtInterval(int barIndex,int total,ENUM_EXTREMUM_TYPE extType) const;
   ENUM_EXTREMUM_TYPE GetMarketExtremum(int barIndex,int total,ENUM_EXTREMUM_TYPE desiredExtremum) const;
   bool              SearchLeftReferencePoint(int barIndex,int total,int rightIndExtBarIndex,int rightPriceExtBarIndex,ENUM_EXTREMUM_TYPE divExtType);

   bool              IsPairExtremums(int barIndex,int startBarIndex,int total,ENUM_EXTREMUM_TYPE neededExtType,int &indExtBarIndex,int &priceExtBarIndex);
   int               GetIndicatorExtremumAtInterval(int barIndex,ENUM_EXTREMUM_TYPE extType) const;

   bool              DefineAndShowDivergence(int barIndex,ENUM_EXTREMUM_TYPE extremumType,int rightBarIndex,int leftBarIndex,int priceRightBarIndex,int priceLeftBarIndex);
   double            GetMarketPrice(int barIndex,ENUM_EXTREMUM_TYPE extremumType) const;
   double            GetSeriesPrice(int barIndex,ENUM_BUFFER_MODE mode) const;
   double            GetIndOrMarketPrice(ENUM_EXTREMUM_TYPE extremumType,ENUM_PRICE_TYPE priceType,int barIndex) const;
   bool              IsDivLineBreak(ENUM_EXTREMUM_TYPE extremumType,ENUM_PRICE_TYPE priceType,int leftBarIndex,int rightBarIndex,double leftBarValue,double rightBarValue) const;
   string            GetDivergenceType(double indRightValue,double indLeftValue,double priceRightValue,double priceLeftValue,ENUM_EXTREMUM_TYPE extType) const;
   bool              IsShowDivergence(int barIndex,double indRightValue,double indLeftValue,double priceRightValue,double priceLeftValue,int priceRightBarIndex,int priceLeftBarIndex,
                                      ENUM_EXTREMUM_TYPE extType);
   bool              IsChartsCoincidence(int leftBarIndex,int rightBarIndex,int curBarIndex,ENUM_EXTREMUM_TYPE extremumType);

   double            GetExtremumPriceByInterval(int priceRightBarIndex,int priceLeftBarIndex,ENUM_EXTREMUM_TYPE extType) const;
  };
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Constructor                                                                                                                                                                                       |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
CDivergence::CDivergence(string symbol,ENUM_TIMEFRAMES tf,ENUM_INDICATOR_TYPE indicatorType,int divergenceDepth,int barsPeriod1,int barsPeriod2,int barsPeriod3,ENUM_APPLIED_PRICE indAppliedPrice,
                         ENUM_MA_METHOD indMAMethod,int findExtInterval,ENUM_MARKET_APPLIED_PRICE marketAppliedPrice,string customName,int customBuffer,ENUM_CUSTOM_PARAM_CNT customParamCnt,
                         double customParam1,double customParam2,double customParam3,double customParam4,double customParam5,double customParam6,double customParam7,double customParam8,
                         double customParam9,double customParam10,double customParam11,double customParam12,double customParam13,double customParam14,double customParam15,
                         double customParam16,double customParam17,double customParam18,double customParam19,double customParam20,bool coincidenceCharts,bool excludeOverlaps,bool showClassA,
                         color bullsDivAColor,color bearsDivAColor,bool showClassB,color bullsDivBColor,color bearsDivBColor,bool showClassC,color bullsDivCColor,color bearsDivCColor,
                         bool showHidden,color bullsDivHColor,color bearsDivHColor,int startBar)
   : m_isActivate(false)
   ,m_symbol(symbol)
   ,m_tf(tf)
   ,m_indicatorType(indicatorType)
   ,m_divergenceDepth(divergenceDepth)
   ,m_barsPeriod1(barsPeriod1)
   ,m_barsPeriod2(barsPeriod2)
   ,m_barsPeriod3(barsPeriod3)
   ,m_indAppliedPrice(indAppliedPrice)
   ,m_indMAMethod(indMAMethod)
   ,m_findExtInterval(findExtInterval)
   ,m_marketAppliedPrice(marketAppliedPrice)
   ,m_customName(customName)
   ,m_customBuffer(customBuffer)
   ,m_customParamCnt(customParamCnt)
   ,m_customParam1(customParam1)
   ,m_customParam2(customParam2)
   ,m_customParam3(customParam3)
   ,m_customParam4(customParam4)
   ,m_customParam5(customParam5)
   ,m_customParam6(customParam6)
   ,m_customParam7(customParam7)
   ,m_customParam8(customParam8)
   ,m_customParam9(customParam9)
   ,m_customParam10(customParam10)
   ,m_customParam11(customParam11)
   ,m_customParam12(customParam12)
   ,m_customParam13(customParam13)
   ,m_customParam14(customParam14)
   ,m_customParam15(customParam15)
   ,m_customParam16(customParam16)
   ,m_customParam17(customParam17)
   ,m_customParam18(customParam18)
   ,m_customParam19(customParam19)
   ,m_customParam20(customParam20)
   ,m_useCoincidenceCharts(coincidenceCharts)
   ,m_excludeOverlaps(excludeOverlaps)
   ,m_showClassA(showClassA)
   ,m_bullsDivAColor(bullsDivAColor)
   ,m_bearsDivAColor(bearsDivAColor)
   ,m_showClassB(showClassB)
   ,m_bullsDivBColor(bullsDivBColor)
   ,m_bearsDivBColor(bearsDivBColor)
   ,m_showClassC(showClassC)
   ,m_bullsDivCColor(bullsDivCColor)
   ,m_bearsDivCColor(bearsDivCColor)
   ,m_showHidden(showHidden)
   ,m_bullsDivHColor(bullsDivHColor)
   ,m_bearsDivHColor(bearsDivHColor)
   ,m_lastCalcTime(0)
   ,m_lastDivergenceTime(0)
   ,m_lastBarsCnt(0)
   ,m_startBar(startBar)
   ,m_indBufferSize(divergenceDepth+barsPeriod3+findExtInterval+3)
   ,m_addIndBufferSize(divergenceDepth+barsPeriod3+findExtInterval+3)
  {
   string language=TerminalInfoString(TERMINAL_LANGUAGE);
   m_useRussian=(language=="Russian");
  }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Destructor                                                                                                                                                                                        |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
CDivergence::~CDivergence(void)
  {
  }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Initialization of the class                                                                                                                                                                       |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
bool CDivergence::Init()
  {
   SymbolInfoDouble(m_symbol,SYMBOL_BID);
   int error = GetLastError();
   if((error>= ERROR_UNKNOWN_SYMBOL && error <= ERROR_SYMBOL_PARAMETER) || error == ERR_UNKNOWN_SYMBOL)
     {
      Alert(WindowExpertName(),GetStringByMessageCode(MESSAGE_CODE_UNKNOWN_SYMBOL));
      return false;
     }

   iTime(m_symbol,m_tf,1);

   if(ArrayResize(m_indBuffer,m_indBufferSize)<0 || ArrayResize(m_addIndBuffer,m_addIndBufferSize)<0)
     {
      Alert(WindowExpertName(),GetStringByMessageCode(MESSAGE_CODE_NOT_ENOUGH_MEMORY));
      return false;
     }

   if(!ArraySetAsSeries(m_addIndBuffer,true))
     {
      Alert(WindowExpertName(),GetStringByMessageCode(MESSAGE_CODE_NOT_ENOUGH_MEMORY));
      return false;
     }

   m_point=SymbolInfoDouble(m_symbol,SYMBOL_POINT);
   if(m_point<=0.0)
     {
      Alert(WindowExpertName(),GetStringByMessageCode(MESSAGE_CODE_POINT_ERROR));
      return false;
     }

   m_digits=(int)SymbolInfoInteger(m_symbol,SYMBOL_DIGITS);
   m_isActivate=true;
   return true;
  }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Getting string by code of message and terminal language                                                                                                                                           |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
string CDivergence::GetStringByMessageCode(ENUM_MESSAGE_CODE messageCode) const
  {
   if(m_useRussian)
      return GetRussianMessage(messageCode);

   return GetEnglishMessage(messageCode);
  }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Getting string by code of message for russian language                                                                                                                                            |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
string CDivergence::GetRussianMessage(ENUM_MESSAGE_CODE messageCode) const
  {
   switch(messageCode)
     {
      case MESSAGE_CODE_UNKNOWN_SYMBOL:                     return ": в списке символов обнаружен неизвестный символ  (" + m_symbol + "). Индикатор отключен.";
      case MESSAGE_CODE_NOT_ENOUGH_MEMORY:                  return ": недостаточно памяти для размещения массивов значений базового индикатора. Индикатор отключен.";
      case MESSAGE_CODE_POINT_ERROR:                        return ": величина пункта символа " + m_symbol + " равна нулю. Индикатор отключен.";
     }

   return "";
  }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Getting string by code of message for english language                                                                                                                                            |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
string CDivergence::GetEnglishMessage(ENUM_MESSAGE_CODE messageCode) const
  {
   switch(messageCode)
     {
      case MESSAGE_CODE_UNKNOWN_SYMBOL:                     return ": unknown symbol (" + m_symbol + ") was found. The indicator is turned off.";
      case MESSAGE_CODE_NOT_ENOUGH_MEMORY:                  return ": not enough memory to allocate arrays of base indicator values. The indicator is turned off.";
      case MESSAGE_CODE_POINT_ERROR:                        return ": the point value of symbol " + m_symbol + " equal to zero. The indicator is turned off.";
     }

   return "";
  }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Process the new tick                                                                                                                                                                              |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
void CDivergence::ProcessTick(DivergenceData &divData)
  {
   if(!m_isActivate)
      return;

   divData=m_lastDivergence;

// Checking the necessity of calculate the current bar
   int newBarsCnt=iBars(m_symbol,m_tf);
   if(GetLastError()!=ERR_NO_ERROR)
      return;

   if(m_lastCalcTime==iTime(m_symbol,m_tf,0) && m_lastBarsCnt==newBarsCnt)
      return;

// One new bar or more      
   int limit=(int)MathMin(m_startBar,newBarsCnt-m_indBufferSize-2);;
   bool useFullCalculate= true;
   int lastCalculateBar = iBarShift(m_symbol,m_tf,m_lastCalcTime);
   if(lastCalculateBar==1 && m_lastBarsCnt==newBarsCnt-1) // New bar appears
     {
      limit=1;
      useFullCalculate=false;
     }

// Calculate new data
   if(m_startBar==1)
      m_lastDivergence.Init();
   for(int i=limit; i>0; i--)
     {
      RecalculateBaseIndicator(i,useFullCalculate);
      ProcessBar(i,newBarsCnt);
      useFullCalculate=false;
     }

   m_lastCalcTime= iTime(m_symbol,m_tf,0);
   m_lastBarsCnt = newBarsCnt;
   divData=m_lastDivergence;
  }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Recalculate the values of base indicator                                                                                                                                                          |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
void CDivergence::RecalculateBaseIndicator(int barIndex,bool useFullCalculate)
  {
   int limit1 = barIndex + m_indBufferSize - 1;
   int limit2 = barIndex + m_addIndBufferSize - 1;
   if(!useFullCalculate)
     {
      limit1 = barIndex;
      limit2 = barIndex;
      MoveBuffer(m_indBuffer);
      if(m_indicatorType==WILLIAM_BLAU)
         MoveBuffer(m_addIndBuffer);
     }

   if(m_indicatorType==WILLIAM_BLAU)
     {
      CalculateDataByWilliamBlau(barIndex,limit1,limit2);
      return;
     }

   for(int i=limit1-barIndex; i>=0; i--)
      m_indBuffer[i]=GetBaseIndicatorValue(i+barIndex);
  }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Recalculate the values of William Blau                                                                                                                                                            |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
void CDivergence::CalculateDataByWilliamBlau(int barIndex,int limit1,int limit2)
  {
   for(int i=limit2,k=limit2-barIndex; i>=barIndex && k>=0; i--,k--)
      m_addIndBuffer[k]=iMA(m_symbol,m_tf,m_barsPeriod2,0,MODE_EMA,m_indAppliedPrice,i)-iMA(m_symbol,m_tf,m_barsPeriod1,0,MODE_EMA,m_indAppliedPrice,i);

   for(int i=limit1-barIndex; i>=0; i--)
      m_indBuffer[i]=iMAOnArray(m_addIndBuffer,0,m_barsPeriod3,0,MODE_EMA,i);
  }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Calculate the value of base indicator at the specified bar                                                                                                                                        |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
double CDivergence::GetBaseIndicatorValue(int barIndex) const
  {
   switch(m_indicatorType)
     {
      case RSI:         return iRSI(m_symbol, m_tf, m_barsPeriod1, m_indAppliedPrice, barIndex);
      case MACD:        return iMACD(m_symbol, m_tf, m_barsPeriod1, m_barsPeriod2, 1, m_indAppliedPrice, MODE_MAIN, barIndex);
      case MOMENTUM:    return iMomentum(m_symbol, m_tf, m_barsPeriod1, m_indAppliedPrice, barIndex);
      case RVI:         return iRVI(m_symbol, m_tf, m_barsPeriod1, MODE_MAIN, barIndex);
      case STOCHASTIC:  return iStochastic(m_symbol, m_tf, m_barsPeriod1, m_barsPeriod2, m_barsPeriod3, MODE_EMA, 1, MODE_MAIN, barIndex);
      case StdDev:      return iStdDev(m_symbol, m_tf, m_barsPeriod1, 0, MODE_EMA, PRICE_CLOSE, barIndex);
      case DERIVATIVE:  return 100.0 * (GetPrice(barIndex) - GetPrice(barIndex + m_barsPeriod1)) / m_barsPeriod1;
      case CUSTOM:      return GetCustomIndicatorValue(barIndex);
     }

   return 0.0;
  }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Calculate the price value at the specified bar                                                                                                                                                    |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
double CDivergence::GetPrice(int barIndex) const
  {
   barIndex=(int) MathMin(iBars(m_symbol,m_tf)-1,barIndex);

   switch(m_indAppliedPrice)
     {
      case PRICE_CLOSE:    return(iClose(m_symbol, m_tf, barIndex));
      case PRICE_OPEN:     return(iOpen(m_symbol, m_tf, barIndex));
      case PRICE_HIGH:     return(iHigh(m_symbol, m_tf, barIndex));
      case PRICE_LOW:      return(iLow(m_symbol, m_tf, barIndex));
      case PRICE_MEDIAN:   return((iHigh(m_symbol, m_tf, barIndex) + iLow(m_symbol, m_tf, barIndex)) / 2);
      case PRICE_TYPICAL:  return((iHigh(m_symbol, m_tf, barIndex) + iLow(m_symbol, m_tf, barIndex) + iClose(m_symbol, m_tf, barIndex)) / 3);
      case PRICE_WEIGHTED: return((iHigh(m_symbol, m_tf, barIndex) + iLow(m_symbol, m_tf, barIndex) + 2 * iClose(m_symbol, m_tf, barIndex)) / 4);
     }

   return 0.0;
  }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Calculate the value of the custom indicator at the specified bar                                                                                                                                  |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
double CDivergence::GetCustomIndicatorValue(int barIndex) const
  {
   switch(m_customParamCnt)
     {
      case PARAM_CNT_0:    return iCustom(m_symbol, m_tf, m_customName, m_customBuffer, barIndex);
      case PARAM_CNT_1:    return iCustom(m_symbol, m_tf, m_customName, m_customParam1, m_customBuffer, barIndex);
      case PARAM_CNT_2:    return iCustom(m_symbol, m_tf, m_customName, m_customParam1, m_customParam2, m_customBuffer, barIndex);
      case PARAM_CNT_3:    return iCustom(m_symbol, m_tf, m_customName, m_customParam1, m_customParam2, m_customParam3, m_customBuffer, barIndex);
      case PARAM_CNT_4:    return iCustom(m_symbol, m_tf, m_customName, m_customParam1, m_customParam2, m_customParam3, m_customParam4, m_customBuffer, barIndex);
      case PARAM_CNT_5:    return iCustom(m_symbol, m_tf, m_customName, m_customParam1, m_customParam2, m_customParam3, m_customParam4, m_customParam5, m_customBuffer, barIndex);
      case PARAM_CNT_6:    return iCustom(m_symbol, m_tf, m_customName, m_customParam1, m_customParam2, m_customParam3, m_customParam4, m_customParam5, m_customParam6, m_customBuffer, barIndex);
      case PARAM_CNT_7:    return iCustom(m_symbol, m_tf, m_customName, m_customParam1, m_customParam2, m_customParam3, m_customParam4, m_customParam5, m_customParam6, m_customParam7,
                                          m_customBuffer,barIndex);
         case PARAM_CNT_8:    return iCustom(m_symbol, m_tf, m_customName, m_customParam1, m_customParam2, m_customParam3, m_customParam4, m_customParam5, m_customParam6, m_customParam7, m_customParam8,
                                             m_customBuffer,barIndex);
            case PARAM_CNT_9:    return iCustom(m_symbol, m_tf, m_customName, m_customParam1, m_customParam2, m_customParam3, m_customParam4, m_customParam5, m_customParam6, m_customParam7, m_customParam8,
                                                m_customParam9,m_customBuffer,barIndex);
               case PARAM_CNT_10:   return iCustom(m_symbol, m_tf, m_customName, m_customParam1, m_customParam2, m_customParam3, m_customParam4, m_customParam5, m_customParam6, m_customParam7, m_customParam8,
                                                   m_customParam9,m_customParam10,m_customBuffer,barIndex);
                  case PARAM_CNT_11:   return iCustom(m_symbol, m_tf, m_customName, m_customParam1, m_customParam2, m_customParam3, m_customParam4, m_customParam5, m_customParam6, m_customParam7, m_customParam8,
                                                      m_customParam9,m_customParam10,m_customParam11,m_customBuffer,barIndex);
                     case PARAM_CNT_12:   return iCustom(m_symbol, m_tf, m_customName, m_customParam1, m_customParam2, m_customParam3, m_customParam4, m_customParam5, m_customParam6, m_customParam7, m_customParam8,
                                                         m_customParam9,m_customParam10,m_customParam11,m_customParam12,m_customBuffer,barIndex);
                        case PARAM_CNT_13:   return iCustom(m_symbol, m_tf, m_customName, m_customParam1, m_customParam2, m_customParam3, m_customParam4, m_customParam5, m_customParam6, m_customParam7, m_customParam8,
                                                            m_customParam9,m_customParam10,m_customParam11,m_customParam12,m_customParam13,m_customBuffer,barIndex);
                           case PARAM_CNT_14:   return iCustom(m_symbol, m_tf, m_customName, m_customParam1, m_customParam2, m_customParam3, m_customParam4, m_customParam5, m_customParam6, m_customParam7, m_customParam8,
                                                               m_customParam9,m_customParam10,m_customParam11,m_customParam12,m_customParam13,m_customParam14,m_customBuffer,barIndex);
                              case PARAM_CNT_15:   return iCustom(m_symbol, m_tf, m_customName, m_customParam1, m_customParam2, m_customParam3, m_customParam4, m_customParam5, m_customParam6, m_customParam7, m_customParam8,
                                                                  m_customParam9,m_customParam10,m_customParam11,m_customParam12,m_customParam13,m_customParam14,m_customParam15,m_customBuffer,barIndex);
                                 case PARAM_CNT_16:   return iCustom(m_symbol, m_tf, m_customName, m_customParam1, m_customParam2, m_customParam3, m_customParam4, m_customParam5, m_customParam6, m_customParam7, m_customParam8,
                                                                     m_customParam9,m_customParam10,m_customParam11,m_customParam12,m_customParam13,m_customParam14,m_customParam15,m_customParam16,
                                                                     m_customBuffer,barIndex);
                                    case PARAM_CNT_17:   return iCustom(m_symbol, m_tf, m_customName, m_customParam1, m_customParam2, m_customParam3, m_customParam4, m_customParam5, m_customParam6, m_customParam7, m_customParam8,
                                                                        m_customParam9,m_customParam10,m_customParam11,m_customParam12,m_customParam13,m_customParam14,m_customParam15,m_customParam16,m_customParam17,
                                                                        m_customBuffer,barIndex);
                                       case PARAM_CNT_18:   return iCustom(m_symbol, m_tf, m_customName, m_customParam1, m_customParam2, m_customParam3, m_customParam4, m_customParam5, m_customParam6, m_customParam7, m_customParam8,
                                                                           m_customParam9,m_customParam10,m_customParam11,m_customParam12,m_customParam13,m_customParam14,m_customParam15,m_customParam16,m_customParam17,
                                                                           m_customParam18,m_customBuffer,barIndex);
                                          case PARAM_CNT_19:   return iCustom(m_symbol, m_tf, m_customName, m_customParam1, m_customParam2, m_customParam3, m_customParam4, m_customParam5, m_customParam6, m_customParam7, m_customParam8,
                                                                              m_customParam9,m_customParam10,m_customParam11,m_customParam12,m_customParam13,m_customParam14,m_customParam15,m_customParam16,m_customParam17,
                                                                              m_customParam18,m_customParam19,m_customBuffer,barIndex);
                                             case PARAM_CNT_20:   return iCustom(m_symbol, m_tf, m_customName, m_customParam1, m_customParam2, m_customParam3, m_customParam4, m_customParam5, m_customParam6, m_customParam7, m_customParam8,
                                                                                 m_customParam9,m_customParam10,m_customParam11,m_customParam12,m_customParam13,m_customParam14,m_customParam15,m_customParam16,m_customParam17,
                                                                                 m_customParam18,m_customParam19,m_customParam20,m_customBuffer,barIndex);
                                                }

                                                return 0.0;
     }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Process the new bar                                                                                                                                                                               |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
   void CDivergence::ProcessBar(int barIndex,int total)
     {
      bool result=false;
      if(ProcessByIndicatorExtremum(barIndex,total))
         result=true;

      if(m_marketAppliedPrice==MARKET_APPLIED_PRICE_CLOSE)
        {
         if(ProcessByMarketExtremum(barIndex,total,EXTREMUM_TYPE_NONE))
            result=true;
        }
      else
         if(ProcessByMarketExtremum(barIndex,total,EXTREMUM_TYPE_MAX) || 
            ProcessByMarketExtremum(barIndex,total,EXTREMUM_TYPE_MIN))
            result=true;

      if(result)
         m_lastDivergenceTime=iTime(m_symbol,m_tf,barIndex+1);
     }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| First searched indicator extremum, and behind it (if extremum of indicator was found) - the price extremum                                                                                        |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
   bool CDivergence::ProcessByIndicatorExtremum(int barIndex,int total)
     {
      // Is the indicator extremum exists?
      ENUM_EXTREMUM_TYPE divExtType=GetBufferExtremum(0,m_indBufferSize,BUFFER_MODE_CUSTOM);
      if(divExtType==EXTREMUM_TYPE_NONE)
         return false;

      // Indicator extremum was found. Finding the extremum of market price
      int priceExtBarIndex=GetMarketExtremumAtInterval(barIndex,total,divExtType);
      if(priceExtBarIndex<0)
         return false;

      return SearchLeftReferencePoint(barIndex, total, barIndex, priceExtBarIndex, divExtType);
     }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| First searched the price extremum, and behind it (if extremum of price was found) - the indicator extremum                                                                                        |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
   bool CDivergence::ProcessByMarketExtremum(int barIndex,int total,ENUM_EXTREMUM_TYPE desiredExtremum)
     {
      // Is the market extremum?
      ENUM_EXTREMUM_TYPE divExtType=GetMarketExtremum(barIndex,total,desiredExtremum);
      if(divExtType==EXTREMUM_TYPE_NONE)
         return false;

      // Market price extremum was found. Finding the extremum of indicator
      int indExtBarIndex=GetIndicatorExtremumAtInterval(0,divExtType);
      if(indExtBarIndex<0)
         return false;

      return SearchLeftReferencePoint(barIndex, total, indExtBarIndex + barIndex, barIndex, divExtType);
     }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Determining of the extremum type at the spesified element of speciefied array                                                                                                                     |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
   ENUM_EXTREMUM_TYPE CDivergence::GetBufferExtremum(int barIndex,int total,ENUM_BUFFER_MODE mode) const
     {
      if(barIndex+2>=total || barIndex<0)
        {
         Print(__FUNCTION__,", Possible error!!! barIndex = ",barIndex,", mode = ",EnumToString(mode),", total = ",total);
         return EXTREMUM_TYPE_NONE;
        }

      double valueRight,valueCenter,valueLeft;
      if(mode==BUFFER_MODE_CUSTOM)
        {
         valueRight=m_indBuffer[barIndex];
         valueCenter=m_indBuffer[barIndex+1];
         valueLeft=m_indBuffer[barIndex+2];
        }
      else
        {
         valueRight=GetSeriesPrice(barIndex,mode);
         valueCenter=GetSeriesPrice(barIndex+1,mode);
         valueLeft=GetSeriesPrice(barIndex+2,mode);
        }

      if(valueCenter<valueRight && valueCenter<valueLeft)
         return EXTREMUM_TYPE_MIN;

      if(valueCenter>valueRight && valueCenter>valueLeft)
         return EXTREMUM_TYPE_MAX;

      return EXTREMUM_TYPE_NONE;
     }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Finding the market extremum at the specified interval from said bar                                                                                                                               |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
   int CDivergence::GetMarketExtremumAtInterval(int barIndex,int total,ENUM_EXTREMUM_TYPE extType) const
     {
      int limit = (int)MathMin(barIndex + m_findExtInterval, total - 2);
      for(int i = barIndex; i < limit; i++)
        {
         if(m_marketAppliedPrice==MARKET_APPLIED_PRICE_HIGHLOW)
           {
            if(extType==EXTREMUM_TYPE_MAX)
               if(GetBufferExtremum(i,total,BUFFER_MODE_HIGH)==extType)
                  return i;

            if(extType==EXTREMUM_TYPE_MIN)
               if(GetBufferExtremum(i,total,BUFFER_MODE_LOW)==extType)
                  return i;
           }
         else
         if(GetBufferExtremum(i,total,BUFFER_MODE_CLOSE)==extType)
            return i;
        }

      return -1;
     }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Search the left extremums of price and indicator                                                                                                                                                  |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
   bool CDivergence::SearchLeftReferencePoint(int barIndex,int total,int rightIndExtBarIndex,int rightPriceExtBarIndex,ENUM_EXTREMUM_TYPE divExtType)
     {
      int lastBar=(int)MathMin(barIndex+1+m_divergenceDepth,total);
      int leftPriceExtBarIndex=-1,leftIndExtBarIndex=-1;

      bool result=false;
      for(int i=(int)MathMax(rightIndExtBarIndex,rightPriceExtBarIndex)+1; i<lastBar; i++)
        {

         if(!IsPairExtremums(barIndex,i,total,divExtType,leftIndExtBarIndex,leftPriceExtBarIndex))
            continue;

         if(DefineAndShowDivergence(barIndex,divExtType,rightIndExtBarIndex,leftIndExtBarIndex,rightPriceExtBarIndex,leftPriceExtBarIndex))
            result=true;
        }

      return result;
     }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Determining whether an extremum of price at a specified bar                                                                                                                                       |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
   ENUM_EXTREMUM_TYPE CDivergence::GetMarketExtremum(int barIndex,int total,ENUM_EXTREMUM_TYPE desiredExtremum) const
     {
      if(m_marketAppliedPrice==MARKET_APPLIED_PRICE_HIGHLOW)
        {
         if(desiredExtremum!=EXTREMUM_TYPE_MIN && GetBufferExtremum(barIndex,total,BUFFER_MODE_HIGH)==EXTREMUM_TYPE_MAX)
            return EXTREMUM_TYPE_MAX;

         if(desiredExtremum!=EXTREMUM_TYPE_MAX && GetBufferExtremum(barIndex,total,BUFFER_MODE_LOW)==EXTREMUM_TYPE_MIN)
            return EXTREMUM_TYPE_MIN;

         return EXTREMUM_TYPE_NONE;
        }

      return GetBufferExtremum(barIndex, total, BUFFER_MODE_CLOSE);
     }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Finding the pair of corresponding extremums of base indicator and market price                                                                                                                    |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
   bool CDivergence::IsPairExtremums(int barIndex,int startBarIndex,int total,ENUM_EXTREMUM_TYPE neededExtType,int &indExtBarIndex,int &priceExtBarIndex)
     {
      // Is the indicator extremum exists?
      indExtBarIndex=startBarIndex;
      priceExtBarIndex=startBarIndex;
      ENUM_EXTREMUM_TYPE divExtType=GetBufferExtremum(startBarIndex-barIndex,m_indBufferSize,BUFFER_MODE_CUSTOM);
      if(divExtType!=neededExtType) // neededExtType can't be equal to EXTREMUM_TYPE_NONE
        {
         // Is the market extremum?
         divExtType=GetMarketExtremum(startBarIndex,total,neededExtType);
         if(divExtType!=neededExtType) // neededExtType can't be equal to EXTREMUM_TYPE_NONE
            return false;

         // Market price extremum was found. Finding the extremum of indicator
         indExtBarIndex=GetIndicatorExtremumAtInterval(startBarIndex-barIndex,divExtType);
         if(indExtBarIndex<0)
            return false;
         indExtBarIndex+=barIndex;
        }
      // Indicator extremum was found. Finding the extremum of market price
      else
        {
         priceExtBarIndex=GetMarketExtremumAtInterval(startBarIndex,total,divExtType);
         if(priceExtBarIndex<0)
            return false;
        }

      return true;
     }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Receiving the price of specified element of specified timeseries                                                                                                                                  |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
   double CDivergence::GetSeriesPrice(int barIndex,ENUM_BUFFER_MODE mode) const
     {
      switch(mode)
        {
         case BUFFER_MODE_HIGH:            return iHigh(m_symbol, m_tf, barIndex);
         case BUFFER_MODE_CLOSE:           return iClose(m_symbol, m_tf, barIndex);
         case BUFFER_MODE_LOW:             return iLow(m_symbol, m_tf, barIndex);
        }

      return 0.0;
     }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Finding the indicator extremum at the specified interval from said bar                                                                                                                            |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
   int CDivergence::GetIndicatorExtremumAtInterval(int barIndex,ENUM_EXTREMUM_TYPE extType) const
     {
      int limit = (int)MathMin(barIndex + m_findExtInterval, m_indBufferSize - 2);
      for(int i = barIndex; i < limit; i++)
         if(GetBufferExtremum(i,m_indBufferSize,BUFFER_MODE_CUSTOM)==extType)
            return i;

      return -1;
     }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Determining the divergence existence and its showing                                                                                                                                              |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
   bool CDivergence::DefineAndShowDivergence(int barIndex,ENUM_EXTREMUM_TYPE extremumType,int rightBarIndex,int leftBarIndex,int priceRightBarIndex,int priceLeftBarIndex)
     {
      // Checking the bar index of indicator's extremums
      leftBarIndex++;
      rightBarIndex++;
      int indRightBarIndex= rightBarIndex-barIndex;
      int indLeftBarIndex = leftBarIndex-barIndex;
      if(indRightBarIndex<0 || indRightBarIndex>=m_indBufferSize || indLeftBarIndex<=indRightBarIndex || indLeftBarIndex>=m_indBufferSize)
        {
         Print(__FUNCTION__,". Possible error. Time bar = ",iTime(m_symbol,m_tf,barIndex),", indRightIndex = ",indRightBarIndex,", indLeftIndex = ",indLeftBarIndex,", max value = ",m_indBufferSize-1);
         return false;
        }
      priceRightBarIndex++;
      priceLeftBarIndex++;

      // The indicator's and price values on specified bars
      double indRightValue= m_indBuffer[indRightBarIndex];
      double indLeftValue = m_indBuffer[indLeftBarIndex];
      double priceRightValue= GetMarketPrice(priceRightBarIndex,extremumType);
      double priceLeftValue = GetMarketPrice(priceLeftBarIndex,extremumType);

      // Checking the divergence existence
      if((indRightValue>indLeftValue && priceRightValue>priceLeftValue) || 
         (indRightValue<indLeftValue && priceRightValue<priceLeftValue))
         return false;

      // Checking for exit of one of the indicator values beyond the line of divergence
      if(IsDivLineBreak(extremumType,PRICE_TYPE_INDICATOR,indLeftBarIndex,indRightBarIndex,indLeftValue,indRightValue))
         return false;

      // Checking for exit of one of the market prices beyond the line of divergence
      if(IsDivLineBreak(extremumType,PRICE_TYPE_MARKET,priceLeftBarIndex,priceRightBarIndex,priceLeftValue,priceRightValue))
         return false;

      // Checking the coincidence of charts of price and base indicator
      if(m_useCoincidenceCharts)
         if(!IsChartsCoincidence((int)MathMax(leftBarIndex,priceLeftBarIndex),(int)MathMin(rightBarIndex,priceRightBarIndex),barIndex,extremumType))
            return false;

      // Checking the overlaps of current divergence line with last divergence line
      if(m_excludeOverlaps)
         if(iTime(NULL,0,(int)MathMax(leftBarIndex,priceLeftBarIndex))<=m_lastDivergenceTime)
            return false;

      // The class definition of divergence, the need for its display and color of display
      return IsShowDivergence(barIndex, indRightValue, indLeftValue, priceRightValue, priceLeftValue, priceRightBarIndex, priceLeftBarIndex, extremumType);
     }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Checking the coincidence of charts of price and base indicator                                                                                                                                    |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
   bool CDivergence::IsChartsCoincidence(int leftBarIndex,int rightBarIndex,int curBarIndex,ENUM_EXTREMUM_TYPE extremumType)
     {
      leftBarIndex++;
      rightBarIndex--;
      if(leftBarIndex<curBarIndex || rightBarIndex<curBarIndex || leftBarIndex-curBarIndex>=m_indBufferSize)
        {
         Print(__FUNCTION__,". Possible error. Time bar = ",iTime(m_symbol,m_tf,curBarIndex),", leftIndex = ",leftBarIndex,", rightBarIndex = ",rightBarIndex,", curBarIndex = ",curBarIndex,
               ", max value = ",m_indBufferSize-1,", symbol = ",m_symbol,", tf = ",EnumToString(m_tf));
         return false;
        }

      double prevPriceValue=GetMarketPrice(leftBarIndex,extremumType);
      double prevIndValue=m_indBuffer[leftBarIndex-curBarIndex];
      for(int i=leftBarIndex-1; i>=rightBarIndex; i--)
        {
         double curPriceValue=GetMarketPrice(i,extremumType);
         double curIndValue=m_indBuffer[i-curBarIndex];
         if((curPriceValue>prevPriceValue && curIndValue<=prevIndValue) || 
            (curPriceValue<prevPriceValue && curIndValue>=prevIndValue) || 
            (curPriceValue==prevPriceValue && curIndValue!=prevIndValue))
            return false;

         prevPriceValue=curPriceValue;
         prevIndValue=curIndValue;
        }

      return true;
     }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Receive the price of specified bar index                                                                                                                                                          |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
   double CDivergence::GetMarketPrice(int barIndex,ENUM_EXTREMUM_TYPE extremumType) const
     {
      if(barIndex<0 || barIndex>=iBars(m_symbol,m_tf))
         return 0.0;

      if(m_marketAppliedPrice==MARKET_APPLIED_PRICE_CLOSE)
         return iClose(m_symbol, m_tf, barIndex);

      if(extremumType==EXTREMUM_TYPE_MAX)
         return iHigh(m_symbol, m_tf, barIndex);

      return iLow(m_symbol, m_tf, barIndex);
     }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Is break the divergence line?                                                                                                                                                                     |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
   bool CDivergence::IsDivLineBreak(ENUM_EXTREMUM_TYPE extremumType,ENUM_PRICE_TYPE priceType,int leftBarIndex,int rightBarIndex,double leftBarValue,double rightBarValue) const
     {
      double kKoef;
      double bKoef=GetBAndKKoefs(rightBarIndex,rightBarValue,leftBarIndex,leftBarValue,kKoef);
      if(bKoef==EMPTY_VALUE)
         return true;

      for(int i=leftBarIndex-1; i>rightBarIndex; i--)
        {
         double lineValue = kKoef * i + bKoef;
         double basePrice = GetIndOrMarketPrice(extremumType, priceType, i);

         if(extremumType==EXTREMUM_TYPE_MAX && lineValue<basePrice)
            return true;

         if(extremumType==EXTREMUM_TYPE_MIN && lineValue>basePrice)
            return true;
        }

      return false;
     }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Calculating the price by values of base indicator or by market price                                                                                                                              |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
   double CDivergence::GetIndOrMarketPrice(ENUM_EXTREMUM_TYPE extremumType,ENUM_PRICE_TYPE priceType,int barIndex) const
     {
      if(priceType==PRICE_TYPE_INDICATOR)
         if(barIndex<0 || barIndex>=m_indBufferSize)
           {
            Print(__FUNCTION__,". Possible error. barIndex = ",barIndex,", max value = ",m_indBufferSize-1);
            return 0.0;
           }
      else
         return m_indBuffer[barIndex];

      return GetMarketPrice(barIndex, extremumType);
     }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| The class definition of divergence, the need for its display and color of display                                                                                                                 |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
   bool CDivergence::IsShowDivergence(int barIndex,double indRightValue,double indLeftValue,double priceRightValue,double priceLeftValue,int priceRightBarIndex,int priceLeftBarIndex,
                                      ENUM_EXTREMUM_TYPE extType)
     {
      string divClass=GetDivergenceType(indRightValue,indLeftValue,priceRightValue,priceLeftValue,extType);
      if((!m_showClassA  &&  divClass==TITLE_CLASS_A) || 
         (!m_showClassB && divClass == TITLE_CLASS_B) ||
         (!m_showClassC && divClass == TITLE_CLASS_C) ||
         (!m_showHidden && divClass == TITLE_CLASS_H))
         return false;

      m_lastDivergence.divClass=divClass;
      m_lastDivergence.type=(extType==EXTREMUM_TYPE_MIN) ? DIV_TYPE_BULLISH : DIV_TYPE_BEARISH;
      m_lastDivergence.extremePrice=GetExtremumPriceByInterval(priceRightBarIndex,priceLeftBarIndex,extType);
      m_lastDivergence.regTime=iTime(m_symbol,m_tf,barIndex-1);

      if(divClass==TITLE_CLASS_A)
         m_lastDivergence.divColor=(extType==EXTREMUM_TYPE_MIN) ? m_bullsDivAColor : m_bearsDivAColor;

      if(divClass==TITLE_CLASS_B)
         m_lastDivergence.divColor=(extType==EXTREMUM_TYPE_MIN) ? m_bullsDivBColor : m_bearsDivBColor;

      if(divClass==TITLE_CLASS_C)
         m_lastDivergence.divColor=(extType==EXTREMUM_TYPE_MIN) ? m_bullsDivCColor : m_bearsDivCColor;

      if(divClass==TITLE_CLASS_H)
         m_lastDivergence.divColor=(extType==EXTREMUM_TYPE_MIN) ? m_bullsDivHColor : m_bearsDivHColor;

      return true;
     }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| The class definition of divergence                                                                                                                                                                |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
   string CDivergence::GetDivergenceType(double indRightValue,double indLeftValue,double priceRightValue,double priceLeftValue,ENUM_EXTREMUM_TYPE extType) const
     {
      if(extType==EXTREMUM_TYPE_MIN)
        {
         if(indRightValue>indLeftValue && priceRightValue<priceLeftValue)
            return TITLE_CLASS_A;
         if(indRightValue>indLeftValue && MathAbs(priceRightValue-priceLeftValue)<m_point/10)
            return TITLE_CLASS_B;
         if(MathAbs(indRightValue-indLeftValue)<m_point/100 && priceRightValue<priceLeftValue)
            return TITLE_CLASS_C;

         return TITLE_CLASS_H;
        }

      if(indRightValue<indLeftValue && priceRightValue>priceLeftValue)
         return TITLE_CLASS_A;
      if(indRightValue<indLeftValue && MathAbs(priceRightValue-priceLeftValue)<m_point/10)
         return TITLE_CLASS_B;
      if(MathAbs(indRightValue-indLeftValue)<m_point/100 && priceRightValue>priceLeftValue)
         return TITLE_CLASS_C;

      return TITLE_CLASS_H;
     }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Defining the extremum at specified interval                                                                                                                                                       |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
   double CDivergence::GetExtremumPriceByInterval(int priceRightBarIndex,int priceLeftBarIndex,ENUM_EXTREMUM_TYPE extType) const
     {
      int barsCnt=priceLeftBarIndex-priceRightBarIndex+1;

      // Bearish divergence. We can find the max local price
      if(extType==EXTREMUM_TYPE_MAX)
         return iHigh(m_symbol, m_tf, iHighest(m_symbol, m_tf, MODE_HIGH, barsCnt, priceRightBarIndex));

      // Bullish divergence. We can find the min local price
      if(extType==EXTREMUM_TYPE_MIN)
         return iLow(m_symbol, m_tf, iLowest(m_symbol, m_tf, MODE_LOW, barsCnt, priceRightBarIndex));

      return 0.0;
     }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Move the elements of specified array on one index upward                                                                                                                                          |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
   void MoveBuffer(double &buffer[])
     {
      for(int i=ArraySize(buffer)-1; i>0; i--)
         buffer[i]=buffer[i-1];
     }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Calculating the coefficients K and B of the equation straight line through specified points                                                                                                       |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
   double GetBAndKKoefs(int x1,double y1,int x2,double y2,double &kKoef)
     {
      if(x2==x1)
         return EMPTY_VALUE;

      kKoef=(y2-y1)/(x2-x1);
      return y1 - kKoef * x1;
     }
//+------------------------------------------------------------------+
