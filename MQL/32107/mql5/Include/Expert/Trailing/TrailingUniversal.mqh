//+------------------------------------------------------------------+
//|                                            TrailingUniversal.mqh |
//|                                    Copyright (c) 2020, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//|                               https://www.mql5.com/en/code/32107 |
//+------------------------------------------------------------------+
#include <Expert/ExpertTrailing.mqh>

#include <ExpresSParserS/v1.2/Functors/Series.mqh>
#include <ExpresSParserS/v1.2/Functors/SymbolProps.mqh>
#include <ExpresSParserS/v1.2/Functors/GlobalVars.mqh>

#include <IndStats/IndicatR.mqh>

// wizard description start
//+------------------------------------------------------------------+
//| Description of the class                                         |
//| Title=Trailing Stop based on Universal signals                   |
//| Type=Trailing                                                    |
//| Name=Universal Trailing                                          |
//| Class=TrailingUniversal                                          |
//| Page=                                                            |
//+------------------------------------------------------------------+
// wizard description end

//+------------------------------------------------------------------+
class TrailingUniversal : public CExpertTrailing
{
  protected:

  public:
                  TrailingUniversal(void) {}
                 ~TrailingUniversal(void) {}
    virtual bool  CheckTrailingStopLong(CPositionInfo *position, double &sl, double &tp) override;
    virtual bool  CheckTrailingStopShort(CPositionInfo *position, double &sl, double &tp) override;
};


//+------------------------------------------------------------------+
//| Checking trailing stop and/or profit for long position.          |
//+------------------------------------------------------------------+
bool TrailingUniversal::CheckTrailingStopLong(CPositionInfo *position, double &sl, double &tp) override
{
  const RubbArray<IndicatR::TradeSignals> *ts = IndicatR::getSignals();
  bool change = false;
  
  const double price = m_symbol.Bid();
  const double level = m_symbol.StopsLevel() * m_symbol.Point();
  const double pos_sl = position.StopLoss();
  const double pos_tp = position.TakeProfit();

  for(int i = 0; i < ts.size(); i++)
  {
    if(ts[i].ModifySL && ts[i].value < price - level && ts[i].value > pos_sl)
    {
      sl = ts[i].value;
      change = true;
    }
    else
    if(ts[i].ModifyTP && ts[i].value > price + level)
    {
      tp = ts[i].value;
      change = true;
    }
  }
  
  return change;
}

//+------------------------------------------------------------------+
//| Checking trailing stop and/or profit for short position.         |
//+------------------------------------------------------------------+
bool TrailingUniversal::CheckTrailingStopShort(CPositionInfo *position, double &sl, double &tp) override
{
  const RubbArray<IndicatR::TradeSignals> *ts = IndicatR::getSignals();
  bool change = false;
  
  const double price = m_symbol.Ask();
  const double level = m_symbol.StopsLevel() * m_symbol.Point();
  const double pos_sl = position.StopLoss();
  const double pos_tp = position.TakeProfit();

  for(int i = 0; i < ts.size(); i++)
  {
    if(ts[i].ModifySL && ts[i].value > price + level && ts[i].value < (pos_sl > 0 ? pos_sl : DBL_MAX))
    {
      sl = ts[i].value;
      change = true;
    }
    else
    if(ts[i].ModifyTP && ts[i].value < price - level)
    {
      tp = ts[i].value;
      change = true;
    }
  }
  
  return change;
}
//+------------------------------------------------------------------+
