// Универсальный класс для получения текущей рыночной информации
#property copyright "Scriptong"
#property link      "http://scriptong.myqip.ru/"
#property version "1.00"
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
struct SymbolInfo
  {
   int               digits;

   double            ask;
   double            bid;
   double            freezeLevel;
   double            point;
   double            tickSize;
   double            tickValue;
   double            spread;
   double            stopLevel;
   double            volumeMax;
   double            volumeMin;
   double            volumeStep;
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class GetSymbolInfo
  {
   bool              m_isECN;
   string            m_symbol;
   int               m_slippage;

   SymbolInfo        m_symbolInfo;

public:
   void              GetSymbolInfo(string symbol,int slippage,bool isECN);

   SymbolInfo        GetAllSymbolInfo(void) const;
   void              RefreshInfo();

   int               GetDigits() const;
   double            GetAsk() const;
   double            GetBid() const;
   double            GetFreezeLevel() const;
   double            GetPoint() const;
   double            GetTickSize() const;
   double            GetTickValue() const;
   double            GetSpread() const;
   double            GetStopLevel() const;
   double            GetVolumeMax() const;
   double            GetVolumeMin() const;
   double            GetVolumeStep() const;

   string            GetSymbol() const;
   int               GetSlippage() const;
  };
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Конструктор класса                                                                                                                                                                       |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
void GetSymbolInfo::GetSymbolInfo(string symbol,int slippage,bool isECN) : m_symbol(symbol)
   ,m_slippage(slippage)
   ,m_isECN(isECN)

  {
   RefreshInfo();
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Непосредственное получение рыночной информации                                                                                                                                           |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
void GetSymbolInfo::RefreshInfo()
  {
   m_symbolInfo.digits          = (int) SymbolInfoInteger(m_symbol, SYMBOL_DIGITS);
   m_symbolInfo.point           = SymbolInfoDouble(m_symbol, SYMBOL_POINT);

   m_symbolInfo.tickSize        = SymbolInfoDouble(m_symbol, SYMBOL_TRADE_TICK_SIZE);
   m_symbolInfo.tickValue       = SymbolInfoDouble(m_symbol, SYMBOL_TRADE_TICK_VALUE);

   m_symbolInfo.ask             = SymbolInfoDouble(m_symbol, SYMBOL_ASK);
   m_symbolInfo.bid             = SymbolInfoDouble(m_symbol, SYMBOL_BID);
   m_symbolInfo.spread          = m_symbolInfo.ask - m_symbolInfo.bid;

   m_symbolInfo.volumeMin       = SymbolInfoDouble(m_symbol, SYMBOL_VOLUME_MIN);
   m_symbolInfo.volumeMax       = SymbolInfoDouble(m_symbol, SYMBOL_VOLUME_MAX);
   m_symbolInfo.volumeStep      = SymbolInfoDouble(m_symbol, SYMBOL_VOLUME_STEP);

   m_symbolInfo.freezeLevel     = NormalizeDouble(MarketInfo(m_symbol, MODE_FREEZELEVEL) * m_symbolInfo.point, m_symbolInfo.digits);
   m_symbolInfo.stopLevel       = MarketInfo(m_symbol, MODE_STOPLEVEL) * m_symbolInfo.point;

// Коррекция Stop Level ля тех ДЦ, в которых его вроде бы нет (но на самом деле есть - скрытый) и для тех ДЦ, у которых есть. Для последних увеличивается на тик для повышения надежности
   if(m_symbolInfo.stopLevel==0)
     {
      if(!m_isECN)
         m_symbolInfo.stopLevel=NormalizeDouble(2*m_symbolInfo.spread,m_symbolInfo.digits);
     }
   else
      m_symbolInfo.stopLevel=NormalizeDouble(m_symbolInfo.stopLevel+m_symbolInfo.tickSize,m_symbolInfo.digits);
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Получение данных по символу в виде структуры                                                                                                                                             |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
SymbolInfo GetSymbolInfo::GetAllSymbolInfo(void) const
  {
   return m_symbolInfo;
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Получение внешними классами информации, собранной в классе GetSymbolInfo                                                                                                                 |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
int GetSymbolInfo::GetDigits(void) const
  {
   return (m_symbolInfo.digits);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetSymbolInfo::GetAsk(void) const
  {
   return (m_symbolInfo.ask);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetSymbolInfo::GetBid(void) const
  {
   return (m_symbolInfo.bid);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetSymbolInfo::GetFreezeLevel(void) const
  {
   return (m_symbolInfo.freezeLevel);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetSymbolInfo::GetPoint(void) const
  {
   return (m_symbolInfo.point);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetSymbolInfo::GetTickSize(void) const
  {
   return (m_symbolInfo.tickSize);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetSymbolInfo::GetTickValue(void) const
  {
   return (m_symbolInfo.tickValue);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetSymbolInfo::GetSpread(void) const
  {
   return (m_symbolInfo.spread);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetSymbolInfo::GetStopLevel(void) const
  {
   return (m_symbolInfo.stopLevel);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetSymbolInfo::GetVolumeMax(void) const
  {
   return (m_symbolInfo.volumeMax);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetSymbolInfo::GetVolumeMin(void) const
  {
   return (m_symbolInfo.volumeMin);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetSymbolInfo::GetVolumeStep(void) const
  {
   return (m_symbolInfo.volumeStep);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string GetSymbolInfo::GetSymbol(void) const
  {
   return (m_symbol);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int GetSymbolInfo::GetSlippage(void) const
  {
   return (m_slippage);
  }
//+------------------------------------------------------------------+
