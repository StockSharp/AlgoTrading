//+------------------------------------------------------------------+
//|                                              SignalUniversal.mqh |
//|                                    Copyright (c) 2020, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//|                               https://www.mql5.com/en/code/32107 |
//+------------------------------------------------------------------+
#include <Expert/ExpertSignal.mqh>

#include <ExpresSParserS/v1.2/Functors/Series.mqh>
#include <ExpresSParserS/v1.2/Functors/SymbolProps.mqh>

#include <IndStats/IndicatR.mqh>

sinput string _E_N_D___O_F___U_N_I_V_E_R_S_A_L___S_I_G_N_A_L_S___M_O_D_U_L_E_ = "";

// wizard description start
//+------------------------------------------------------------------+
//| Description of the class                                         |
//| Title=Universal signals from arbitrary indicators                |
//| Type=SignalAdvanced                                              |
//| Name=Universal Signal                                            |
//| ShortName=Universal                                              |
//| Class=CSignalUniversal                                           |
//| Page=signal_universal                                            |
//| Parameter=Pattern_0,int,100,Signal A (1-st model) weight         |
//| Parameter=Pattern_1,int,100,Signal B (2-nd model) weight         |
//| Parameter=Pattern_2,int,100,Signal C (3-rd model) weight         |
//| Parameter=Pattern_3,int,100,Signal D (4-th model) weight         |
//| Parameter=Pattern_4,int,100,Signal E (5-tt model) weight         |
//| Parameter=Pattern_5,int,100,Signal F (6-th model) weight         |
//| Parameter=Pattern_6,int,100,Signal G (7-th model) weight         |
//| Parameter=Pattern_7,int,100,Signal H (8-th model) weight         |
//+------------------------------------------------------------------+
// wizard description end
//+------------------------------------------------------------------+
class CSignalUniversal: public CExpertSignal
{
  protected:
   // "weights" of market models (0-100)
   int               m_patterns[8];
   const RubbArray<IndicatR::TradeSignals> *ts; // pointer to current signals (if any), static internally
   
  public:
                     CSignalUniversal(void);
                    ~CSignalUniversal(void);
   // methods of setting adjustable parameters
   // methods of adjusting "weights" of market models
   void              Pattern_0(int value)        { m_patterns[0] = value; }
   void              Pattern_1(int value)        { m_patterns[1] = value; }
   void              Pattern_2(int value)        { m_patterns[2] = value; }
   void              Pattern_3(int value)        { m_patterns[3] = value; }
   void              Pattern_4(int value)        { m_patterns[4] = value; }
   void              Pattern_5(int value)        { m_patterns[5] = value; }
   void              Pattern_6(int value)        { m_patterns[6] = value; }
   void              Pattern_7(int value)        { m_patterns[7] = value; }
   // method of creating the indicator and timeseries
   virtual bool      InitIndicators(CIndicators *indicators);
   // methods of checking if the market models are formed
   virtual int       LongCondition(void) override;
   virtual int       ShortCondition(void) override;
   virtual bool      CheckCloseLong(double &price) override;
   virtual bool      CheckCloseShort(double &price) override;

   virtual double    Direction(void) override;
};

//+------------------------------------------------------------------+
//| Constructor                                                      |
//+------------------------------------------------------------------+
CSignalUniversal::CSignalUniversal(void)
{
  ArrayInitialize(m_patterns, 100);
}
//+------------------------------------------------------------------+
//| Destructor                                                       |
//+------------------------------------------------------------------+
CSignalUniversal::~CSignalUniversal(void)
{
}
//+------------------------------------------------------------------+
//| Create indicators.                                               |
//+------------------------------------------------------------------+
bool CSignalUniversal::InitIndicators(CIndicators *indicators)
{
  if(IndicatR::handleInit(m_symbol != NULL ? m_symbol.Name() : NULL, m_period) != INIT_SUCCEEDED) return false;

  if(indicators != NULL)
  {
    // initialization of indicators and timeseries of additional filters
    return CExpertSignal::InitIndicators(indicators);
  }
  
  return true;
}

double CSignalUniversal::Direction(void) override
{
  ts = IndicatR::handleStart(m_symbol != NULL ? m_symbol.Name() : NULL, m_period); // recalculate signals
  #ifdef UNIVERSAL_SIGNAL_DEBUG_LOG
  for(int i = 0; i < ts.size(); i++)
  {
    const string id = IndicatR::GetSignal(ts[i].index);
    Print("* ", id, " e:", IS_PATTERN_USAGE(ts[i].index), " a:", ts[i].alert, " b:", ts[i].buy, " s:", ts[i].sell, " x:", ts[i].buyExit, " y:", ts[i].sellExit);
  }
  #endif
  return CExpertSignal::Direction();
}

//+------------------------------------------------------------------+
//| "Voting" that price will grow.                                   |
//+------------------------------------------------------------------+
int CSignalUniversal::LongCondition(void) override
{
  for(int i = 0; i < ts.size(); i++)
  {
    if(IS_PATTERN_USAGE(ts[i].index) && ts[i].buy)
    {
      return m_patterns[ts[i].index];
    }
  }

  return 0;
}

//+------------------------------------------------------------------+
//| "Voting" that price will fall.                                   |
//+------------------------------------------------------------------+
int CSignalUniversal::ShortCondition(void) override
{
  for(int i = 0; i < ts.size(); i++)
  {
    if(IS_PATTERN_USAGE(ts[i].index) && ts[i].sell)
    {
      return m_patterns[ts[i].index];
    }
  }

  return 0;
}
//+------------------------------------------------------------------+
bool CSignalUniversal::CheckCloseLong(double &price) override
{
  for(int i = 0; i < ts.size(); i++)
  {
    if(IS_PATTERN_USAGE(ts[i].index) && ts[i].buyExit)
    {
      return true;
    }
  }
  return CExpertSignal::CheckCloseLong(price);
}

bool CSignalUniversal::CheckCloseShort(double &price) override
{
  for(int i = 0; i < ts.size(); i++)
  {
    if(IS_PATTERN_USAGE(ts[i].index) && ts[i].sellExit)
    {
      return true;
    }
  }
  return CExpertSignal::CheckCloseShort(price);
}

