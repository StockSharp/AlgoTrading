//+------------------------------------------------------------------+
//|                                                     IsNewBar.mqh |
//|                               Copyright � 2011, Nikolay Kositsin |
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+ 
#property copyright "2011,   Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+------------------------------------------------------------------+
//|  �������� ����������� ������� ��������� ������ ����              |
//+------------------------------------------------------------------+  
class CIsNewBar
  {
   //----
public:
   //---- ������� ����������� ������� ��������� ������ ����
   bool IsNewBar(string symbol,ENUM_TIMEFRAMES timeframe)
     {
      //---- ������� ����� ��������� �������� ����
      datetime TNew=datetime(SeriesInfoInteger(symbol,timeframe,SERIES_LASTBAR_DATE));

      if(TNew!=m_TOld && TNew) // �������� �� ��������� ������ ����
        {
         m_TOld=TNew;
         return(true); // �������� ����� ���!
        }
      //----
      return(false); // ����� ����� ���� ���!
     };

   //---- ����������� ������    
                     CIsNewBar(){m_TOld=-1;};

protected: datetime m_TOld;
   //---- 
  };
//+------------------------------------------------------------------+
