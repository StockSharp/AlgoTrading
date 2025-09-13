//+------------------------------------------------------------------+
//|                                                     IsNewBar.mqh |
//|                               Copyright © 2011, Nikolay Kositsin |
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+ 
#property copyright "2011,   Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+------------------------------------------------------------------+
//|  јлгоритм определени€ момента по€влени€ нового бара              |
//+------------------------------------------------------------------+  
class CIsNewBar
  {
   //----
public:
   //---- функци€ определени€ момента по€влени€ нового бара
   bool IsNewBar(string symbol,ENUM_TIMEFRAMES timeframe)
     {
      //---- получим врем€ по€влени€ текущего бара
      datetime TNew=datetime(SeriesInfoInteger(symbol,timeframe,SERIES_LASTBAR_DATE));

      if(TNew!=m_TOld && TNew) // проверка на по€вление нового бара
        {
         m_TOld=TNew;
         return(true); // по€вилс€ новый бар!
        }
      //----
      return(false); // новых баров пока нет!
     };

   //---- конструктор класса    
                     CIsNewBar(){m_TOld=-1;};

protected: datetime m_TOld;
   //---- 
  };
//+------------------------------------------------------------------+
