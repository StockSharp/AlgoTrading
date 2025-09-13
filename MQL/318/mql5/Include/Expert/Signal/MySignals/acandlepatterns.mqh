//+------------------------------------------------------------------+
//|                           Advanced version of CandlePatterns.mqh |
//|                      Copyright © 2011, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//|                                              Revision 2011.11.10 |
//+------------------------------------------------------------------+
#include <Expert\ExpertSignal.mqh>
//+------------------------------------------------------------------+
//| enumerators                                                      |
//+------------------------------------------------------------------+
enum ENUM_CANDLE_PATTERNS  // candlestick patterns
  {
   CANDLE_PATTERN_THREE_BLACK_CROWS     = 1,
   CANDLE_PATTERN_THREE_WHITE_SOLDIERS  = 2,
   CANDLE_PATTERN_DARK_CLOUD_COVER      = 3,
   CANDLE_PATTERN_PIERCING_LINE         = 4,
   CANDLE_PATTERN_MORNING_DOJI          = 5,
   CANDLE_PATTERN_EVENING_DOJI          = 6,
   CANDLE_PATTERN_BEARISH_ENGULFING     = 7,
   CANDLE_PATTERN_BULLISH_ENGULFING     = 8,
   CANDLE_PATTERN_EVENING_STAR          = 9,
   CANDLE_PATTERN_MORNING_STAR          = 10,
   CANDLE_PATTERN_HAMMER                = 11,
   CANDLE_PATTERN_HANGING_MAN           = 12,
   CANDLE_PATTERN_BEARISH_HARAMI        = 13,
   CANDLE_PATTERN_BULLISH_HARAMI        = 14,
   CANDLE_PATTERN_BEARISH_MEETING_LINES = 15,
   CANDLE_PATTERN_BULLISH_MEETING_LINES = 16
  };
//+------------------------------------------------------------------+
//| CCandlePattern class.                                            |
//| Derived from CExpertSignal class.                                |
//+------------------------------------------------------------------+
class CCandlePattern : public CExpertSignal
  {
protected:
   //--- indicators
   CiMA              m_MA;
   //--- input parameters
   int               m_ma_period;

public:
   //--- class constructor
                     CCandlePattern();
   //--- input parameters initialization methods
   void              MAPeriod(int period) { m_ma_period=period;                 }
   //--- initialization
   virtual bool      ValidationSettings();
   virtual bool      InitIndicators(CIndicators *indicators);
   //--- method for checking of a certiain candlestick pattern
   bool              CheckCandlestickPattern(ENUM_CANDLE_PATTERNS CandlePattern);
   //--- methods for checking of bullish/bearish candlestick pattern
   bool              CheckPatternAllBullish();
   bool              CheckPatternAllBearish();

protected:
   //--- indicators initialization methods
   bool              InitMA(CIndicators *indicators);
   //--- methods, used for check of the candlestick pattern formation
   double            AvgBody(int ind);
   double            MA(int ind)                const { return(m_MA.Main(ind));             }
   double            Open(int ind)              const { return(m_open.GetData(ind));        }
   double            High(int ind)              const { return(m_high.GetData(ind));        }
   double            Low(int ind)               const { return(m_low.GetData(ind));         }
   double            Close(int ind)             const { return(m_close.GetData(ind));       }
   double            CloseAvg(int ind)          const { return(MA(ind));                    }
   double            MidPoint(int ind)          const { return(0.5*(High(ind)+Low(ind)));   }
   double            MidOpenClose(int ind)      const { return(0.5*(Open(ind)+Close(ind))); }
   //--- methods for checking of candlestick patterns
   bool              CheckPatternThreeBlackCrows();
   bool              CheckPatternThreeWhiteSoldiers();
   bool              CheckPatternDarkCloudCover();
   bool              CheckPatternPiercingLine();
   bool              CheckPatternMorningDoji();
   bool              CheckPatternEveningDoji();
   bool              CheckPatternBearishEngulfing();
   bool              CheckPatternBullishEngulfing();
   bool              CheckPatternEveningStar();
   bool              CheckPatternMorningStar();
   bool              CheckPatternHammer();
   bool              CheckPatternHangingMan();
   bool              CheckPatternBearishHarami();
   bool              CheckPatternBullishHarami();
   bool              CheckPatternBearishMeetingLines();
   bool              CheckPatternBullishMeetingLines();
  };
//+------------------------------------------------------------------+
//| CCandlePattern class constructor.                                |
//| INPUT:  no.                                                      |
//| OUTPUT: no.                                                      |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
void CCandlePattern::CCandlePattern()
  {
//--- initialization of protected data
   m_used_series=USE_SERIES_OPEN+USE_SERIES_HIGH+USE_SERIES_LOW+USE_SERIES_CLOSE;
//--- set default inputs
   m_ma_period=12;
  }
//+------------------------------------------------------------------+
//| Validation settings.                                             |
//| INPUT:  no.                                                      |
//| OUTPUT: true-if settings are correct, false otherwise.           |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CCandlePattern::ValidationSettings()
  {
//--- validation settings of additional filters
   if(!CExpertSignal::ValidationSettings()) return(false);
//--- initial data checks
   if(m_ma_period<=0)
     {
      printf(__FUNCTION__+": period MA must be greater than 0");
      return(false);
     }
//--- ok
   return(true);
  }
//+------------------------------------------------------------------+
//| Create MA, Open, High, Low and Close time series                 |
//| INPUT:  indicators -pointer of indicator collection.             |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CCandlePattern::InitIndicators(CIndicators *indicators)
  {
//--- check collection
   if(indicators==NULL) return(false);
//--- create and initialize MA indicator
   if(!InitMA(indicators)) return(false);
//--- create and initialize Open series
   if(!InitOpen(indicators)) return(false);
//--- create and initialize High series
   if(!InitHigh(indicators)) return(false);
//--- create and initialize Low series
   if(!InitLow(indicators)) return(false);
//--- create and initialize Close series
   if(!InitClose(indicators)) return(false);
//--- ok
   return(true);
  }
//+------------------------------------------------------------------+
//| Create MA indicators.                                            |
//| INPUT:  indicators -pointer of indicator collection.             |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CCandlePattern::InitMA(CIndicators *indicators)
  {
//--- add MA indicator to collection
   if(!indicators.Add(GetPointer(m_MA)))
     {
      printf(__FUNCTION__+": error adding object");
      return(false);
     }
//--- initialize MA indicator
   if(!m_MA.Create(m_symbol.Name(),m_period,m_ma_period,0,MODE_SMA,PRICE_CLOSE))
     {
      printf(__FUNCTION__+": error initializing object");
      return(false);
     }
//--- resize MA buffer
   m_MA.BufferResize(50);
//--- ok
   return(true);
  }
//+------------------------------------------------------------------+
//| Returns the averaged value of candle body size                   |
//+------------------------------------------------------------------+
double CCandlePattern::AvgBody(int ind)
  {
   double candle_body=0;
///--- calculate the averaged size of the candle's body
   for(int i=ind; i<ind+m_ma_period; i++)
     {
      candle_body+=MathAbs(Open(i)-Close(i));
     }
   candle_body=candle_body/m_ma_period;
///--- return body size
   return(candle_body);
  }
//+------------------------------------------------------------------+
//| Checks formation of bullish patterns                             |
//+------------------------------------------------------------------+
bool CCandlePattern::CheckPatternAllBullish()
  {
   return(CheckPatternThreeWhiteSoldiers() || 
          CheckPatternPiercingLine() || 
          CheckPatternMorningDoji() || 
          CheckPatternBullishEngulfing() || 
          CheckPatternBullishHarami() || 
          CheckPatternMorningStar() || 
          CheckPatternBullishMeetingLines());
  }
//+------------------------------------------------------------------+
//| Checks formation of bearish patterns                             |
//+------------------------------------------------------------------+
bool CCandlePattern::CheckPatternAllBearish()
  {
   return(CheckPatternThreeBlackCrows() || 
          CheckPatternDarkCloudCover() || 
          CheckPatternEveningDoji() || 
          CheckPatternBearishEngulfing() || 
          CheckPatternBearishHarami() || 
          CheckPatternEveningStar() || 
          CheckPatternBearishMeetingLines());
  }
//+------------------------------------------------------------------+
//| Checks formation of Three Black Crows candlestick pattern        |
//+------------------------------------------------------------------+
bool CCandlePattern::CheckPatternThreeBlackCrows()
  {
//--- 3 Black Crows
   if((Open(3)-Close(3)>AvgBody(1)) && // long black
      (Open(2)-Close(2)>AvgBody(1)) && 
      (Open(1)-Close(1)>AvgBody(1)) && 
      (MidPoint(2)<MidPoint(3))     && // lower midpoints
      (MidPoint(1)<MidPoint(2)))
      return(true);
//---
   return(false);
  }
//+------------------------------------------------------------------+
//| Checks formation of Three White Soldiers candlestick pattern     |
//+------------------------------------------------------------------+
bool CCandlePattern::CheckPatternThreeWhiteSoldiers()
  {
//--- 3 White Soldiers
   if((Close(3)-Open(3)>AvgBody(1)) && // long white
      (Close(2)-Open(2)>AvgBody(1)) && 
      (Close(1)-Open(1)>AvgBody(1)) && 
      (MidPoint(2)>MidPoint(3))     && // higher midpoints
      (MidPoint(1)>MidPoint(2)))
      return(true);
//---
   return(false);
  }
//+------------------------------------------------------------------+
//| Checks formation of Dark Cloud Cover candlestick pattern         |
//+------------------------------------------------------------------+
bool CCandlePattern::CheckPatternDarkCloudCover()
  {
//--- Dark cloud cover
   if((Close(2)-Open(2)>AvgBody(1)) && // long white
      (Close(1)<Close(2))           && // close within previous body
      (Close(1)>Open(2))            && 
      (MidOpenClose(2)>CloseAvg(1)) && // uptrend
      (Open(1)>High(2)))               // open at new high  
      return(true);
//---
   return(false);
  }
//+------------------------------------------------------------------+
//| Checks formation of Piercing Line candlestick pattern            |
//+------------------------------------------------------------------+
bool CCandlePattern::CheckPatternPiercingLine()
  {
//--- Piercing Line
   if((Close(1)-Open(1)>AvgBody(1)) && // long white
      (Open(2)-Close(2)>AvgBody(1)) && // long black
      (Close(2)>Close(1))           && // close inside previous body
      (Close(1)<Open(2))            && 
      (MidOpenClose(2)<CloseAvg(2)) && // downtrend
      (Open(1)<Low(2)))                // close inside previous body
      return(true);
//---
   return(false);
  }
//+------------------------------------------------------------------+
//| Checks formation of Morning Doji candlestick pattern             |
//+------------------------------------------------------------------+
bool CCandlePattern::CheckPatternMorningDoji()
  {
//--- Morning Doji
   if((Open(3)-Close(3)>AvgBody(1)) &&
      (AvgBody(2)<AvgBody(1)*0.1)   &&
      (Close(2)<Close(3))           &&
      (Open(2)<Open(3))             &&
      (Open(1)>Close(2))            &&
      (Close(1)>Close(2)))
      return(true);
//---
   return(false);
  }
//+------------------------------------------------------------------+
//| Checks formation of Evening Doji candlestick pattern             |
//+------------------------------------------------------------------+
bool CCandlePattern::CheckPatternEveningDoji()
  {
//--- Evening Doji
   if((Close(3)-Open(3)>AvgBody(1)) &&
      (AvgBody(2)<AvgBody(1)*0.1)   &&
      (Close(2)>Close(3))           &&
      (Open(2)>Open(3))             &&
      (Open(1)<Close(2))            &&
      (Close(1)<Close(2)))
      return(true);
//---
   return(false);
  }
//+------------------------------------------------------------------+
//| Checks formation of Bearish Engulfing candlestick pattern        |
//+------------------------------------------------------------------+
bool CCandlePattern::CheckPatternBearishEngulfing()
  {
//--- Bearish Engulfing
   if((Open(2)<Close(2))            &&
      (Open(1)-Close(1)>AvgBody(1)) &&
      (Close(1)<Open(2))            &&
      (MidOpenClose(2)>CloseAvg(2)) &&
      (Open(1)>Close(2)))
      return(true);
//---
   return(false);
  }
//+------------------------------------------------------------------+
//| Checks formation of Bullish Engulfing candlestick pattern        |
//+------------------------------------------------------------------+
bool CCandlePattern::CheckPatternBullishEngulfing()
  {
//--- Bullish Engulfing
   if((Open(2)>Close(2))            &&
      (Close(1)-Open(1)>AvgBody(1)) && 
      (Close(1)>Open(2))            &&
      (MidOpenClose(2)<CloseAvg(2)) && 
      (Open(1)<Close(2)))
      return(true);
//---
   return(false);
  }
//+------------------------------------------------------------------+
//| Checks formation of Evening Star candlestick pattern             |
//+------------------------------------------------------------------+
bool CCandlePattern::CheckPatternEveningStar()
  {
//--- Evening Star
   if((Close(3)-Open(3)>AvgBody(1))              && 
      (MathAbs(Close(2)-Open(2))<AvgBody(1)*0.5) && 
      (Close(2)>Close(3))                        &&
      (Open(2)>Open(3))                          &&
      (Close(1)<MidOpenClose(3)))
      return(true);
//---
   return(false);
  }
//+------------------------------------------------------------------+
//| Checks formation of Morning Star candlestick pattern             |
//+------------------------------------------------------------------+
bool CCandlePattern::CheckPatternMorningStar()
  {
//--- Morning Star
   if((Open(3)-Close(3)>AvgBody(1))              &&
      (MathAbs(Close(2)-Open(2))<AvgBody(1)*0.5) &&
      (Close(2)<Close(3))                        &&
      (Open(2)<Open(3))                          &&
      (Close(1)>MidOpenClose(3)))
      return(true);
//---
   return(false);
  }
//+------------------------------------------------------------------+
//| Checks formation of Hammer candlestick pattern                   |
//+------------------------------------------------------------------+
bool CCandlePattern::CheckPatternHammer()
  {
//--- Hammer
   if((MidPoint(1)<CloseAvg(2))                                  && // down trend
      (MathMin(Open(1),Close(1))>(High(1)-(High(1)-Low(1))/3.0)) && // body in upper 1/3
      (Close(1)<Close(2)) && (Open(1)<Open(2)))                     // body gap
      return(true);
//---
   return(false);
  }
//+------------------------------------------------------------------+
//| Checks formation of Hanging Man candlestick pattern              |
//+------------------------------------------------------------------+
bool CCandlePattern::CheckPatternHangingMan()
  {
//--- Hanging man
   if((MidPoint(1)>CloseAvg(2))                                 && // up trend
      (MathMin(Open(1),Close(1)>(High(1)-(High(1)-Low(1))/3.0)) && // body in upper 1/3
      (Close(1)>Close(2)) && (Open(1)>Open(2))))                   // body gap
      return(true);
//---
   return(false);
  }
//+------------------------------------------------------------------+
//| Checks formation of Bearish Harami candlestick pattern           |
//+------------------------------------------------------------------+
bool CCandlePattern::CheckPatternBearishHarami()
  {
//--- Bearish Harami
   if((Close(1)<Open(1))              && // black day
      ((Close(2)-Open(2))>AvgBody(1)) && // long white
      ((Close(1)>Open(2))             && 
      (Open(1)<Close(2)))             && // engulfment
      (MidPoint(2)>CloseAvg(2)))         // up trend
      return(true);
//---
   return(false);
  }
//+------------------------------------------------------------------+
//| Checks formation of Bullish Harami candlestick pattern           |
//+------------------------------------------------------------------+
bool CCandlePattern::CheckPatternBullishHarami()
  {
//--- Bullish Harami
   if((Close(1)>Open(1))              && // white day
      ((Open(2)-Close(2))>AvgBody(1)) && // long black
      ((Close(1)<Open(2))             && 
      (Open(1)>Close(2)))             && // engulfment
      (MidPoint(2)<CloseAvg(2)))         // down trend
      return(true);
//---
   return(false);
  }
//+------------------------------------------------------------------+
//| Checks formation of Bearish Meeting Lines candlestick pattern    |
//+------------------------------------------------------------------+
bool CCandlePattern::CheckPatternBearishMeetingLines()
  {
//--- Bearish MeetingLines
   if((Close(2)-Open(2)>AvgBody(1))                && // long white
      ((Open(1)-Close(1))>AvgBody(1))              && // long black
      (MathAbs(Close(1)-Close(2))<0.1*AvgBody(1)))    // doji close
      return(true);
//---
   return(false);
  }
//+------------------------------------------------------------------+
//| Checks formation of Bullish Meeting Lines candlestick pattern    |
//+------------------------------------------------------------------+
bool CCandlePattern::CheckPatternBullishMeetingLines()
  {
//--- Bullish MeetingLines
   if((Open(2)-Close(2)>AvgBody(1))             && // long black
      ((Close(1)-Open(1))>AvgBody(1))           && // long white
      (MathAbs(Close(1)-Close(2))<0.1*AvgBody(1))) // doji close
      return(true);
//---
   return(false);
  }
//-------------------------------------------------------------------+
//| Checks formation of a certain candlestick pattern                |
//+------------------------------------------------------------------+
bool CCandlePattern::CheckCandlestickPattern(ENUM_CANDLE_PATTERNS CandlePattern)
  {
   switch(CandlePattern)
     {
      case CANDLE_PATTERN_THREE_BLACK_CROWS:      return(CheckPatternThreeBlackCrows());
      case CANDLE_PATTERN_THREE_WHITE_SOLDIERS:   return(CheckPatternThreeWhiteSoldiers());
      case CANDLE_PATTERN_DARK_CLOUD_COVER:       return(CheckPatternDarkCloudCover());
      case CANDLE_PATTERN_PIERCING_LINE:          return(CheckPatternPiercingLine());
      case CANDLE_PATTERN_MORNING_DOJI:           return(CheckPatternMorningDoji());
      case CANDLE_PATTERN_EVENING_DOJI:           return(CheckPatternEveningDoji());
      case CANDLE_PATTERN_BEARISH_ENGULFING:      return(CheckPatternBearishEngulfing());
      case CANDLE_PATTERN_BULLISH_ENGULFING:      return(CheckPatternBullishEngulfing());
      case CANDLE_PATTERN_EVENING_STAR:           return(CheckPatternEveningStar());
      case CANDLE_PATTERN_MORNING_STAR:           return(CheckPatternMorningStar());
      case CANDLE_PATTERN_HAMMER:                 return(CheckPatternHammer());
      case CANDLE_PATTERN_HANGING_MAN:            return(CheckPatternHangingMan());
      case CANDLE_PATTERN_BEARISH_HARAMI:         return(CheckPatternBearishHarami());
      case CANDLE_PATTERN_BULLISH_HARAMI:         return(CheckPatternBullishHarami());
      case CANDLE_PATTERN_BEARISH_MEETING_LINES:  return(CheckPatternBearishMeetingLines());
      case CANDLE_PATTERN_BULLISH_MEETING_LINES:  return(CheckPatternBullishMeetingLines());
     }
//---
   return(false);
  }
//+------------------------------------------------------------------+
