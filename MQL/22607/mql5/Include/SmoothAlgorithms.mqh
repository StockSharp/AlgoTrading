//MQL5 Version  25 January, 2015 Final
//+X================================================================X+
//|                                             SmoothAlgorithms.mqh |
//|                               Copyright © 2013, Nikolay Kositsin |
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+X================================================================X+
#property copyright "2013,   Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "3.24"
//+X================================================================X+
//|  Классы для усреднения ценовых рядов                             |
//+X================================================================X+
   enum Smooth_Method
     {
      MODE_SMA_,  //SMA
      MODE_EMA_,  //EMA
      MODE_SMMA_, //SMMA
      MODE_LWMA_, //LWMA
      MODE_JJMA,  //JJMA
      MODE_JurX,  //JurX
      MODE_ParMA, //ParMA
      MODE_T3,     //T3
      MODE_VIDYA,  //VIDYA
      MODE_AMA     //AMA
     };

//+X================================================================X+
//|  Функциональные утилиты для классов алгоритмов усреднения        |
//+X================================================================X+
class CMovSeriesTools
  {
public:
   void              MALengthCheck(string LengthName,int    ExternLength);
   void              MALengthCheck(string LengthName,double ExternLength);

protected:
   bool              BarCheck1(int begin,int bar,bool Set);
   bool              BarCheck2(int begin,int bar,bool Set,int Length);
   bool              BarCheck3(int begin,int bar,bool Set,int Length);

   bool              BarCheck4(int rates_total,int bar,bool Set);
   bool              BarCheck5(int rates_total,int bar,bool Set);
   bool              BarCheck6(int rates_total,int bar,bool Set);

   void              LengthCheck(int    &ExternLength);
   void              LengthCheck(double &ExternLength);

   void              Recount_ArrayZeroPos(
                                          int &count,
                                          int Length,
                                          uint prev_calculated,
                                          uint rates_total,
                                          double series,
                                          int bar,
                                          double &Array[],
                                          bool set
                                          );

   int               Recount_ArrayNumber(int count,int Length,int Number);

   bool              SeriesArrayResize(string FunctionsName,
                                       int Length,
                                       double &Array[],
                                       int &Size_
                                       );

   bool              ArrayResizeErrorPrint(string FunctionsName,int &Size_);
  };
//+X================================================================X+
//|  Функции для классического усреднения ценовых рядов              |
//+X================================================================X+
class CMoving_Average : public CMovSeriesTools
  {
public:
   double            MASeries(uint begin,// номер начала достоверного отсчёта баров
                              uint prev_calculated,// Количество истории в барах на предыдущем тике
                              uint rates_total,// Количество истории в барах на текущем тике
                              int Length,// Период усреднения
                              ENUM_MA_METHOD MA_Method,// Метод усреднения (MODE_SMA, MODE_EMA, MODE_SMMA, MODE_LWMA)
                              double series,// Значение ценового ряда, расчитанное для бара с номером bar
                              uint bar,// Номер бара
                              bool set // Направление индексирования массивов.
                              );

   double            SMASeries(uint begin,// номер начала достоверного отсчёта баров
                               uint prev_calculated,// Количество истории в барах на предыдущем тике
                               uint rates_total,// Количество истории в барах на текущем тике
                               int Length,// Период усреднения
                               double series,// Значение ценового ряда, расчитанное для бара с номером bar
                               uint bar,// Номер бара
                               bool set // Направление индексирования массивов.
                               );

   double            EMASeries(uint begin,// номер начала достоверного отсчёта баров
                               uint prev_calculated,// Количество истории в барах на предыдущем тике
                               uint rates_total,// Количество истории в барах на текущем тике
                               double Length,// Период усреднения
                               double series,// Значение ценового ряда, расчитанное для бара с номером bar
                               uint bar,// Номер бара
                               bool set // Направление индексирования массивов.
                               );

   double            SMMASeries(uint begin,// номер начала достоверного отсчёта баров
                                uint prev_calculated,// Количество истории в барах на предыдущем тике
                                uint rates_total,// Количество истории в барах на текущем тике
                                int Length,// Период усреднения
                                double series,// Значение ценового ряда, расчитанное для бара с номером bar
                                uint bar,// Номер бара
                                bool set // Направление индексирования массивов.
                                );

   double            LWMASeries(uint begin,// номер начала достоверного отсчёта баров
                                uint prev_calculated,// Количество истории в барах на предыдущем тике
                                uint rates_total,// Количество истории в барах на текущем тике
                                int Length,// Период усреднения
                                double series,// Значение ценового ряда, расчитанное для бара с номером bar
                                uint bar,// Номер бара
                                bool set // Направление индексирования массивов.
                                );

protected:
   double            m_SeriesArray[];
   int               m_Size_,m_count,m_weight;
   double            m_Moving,m_MOVING,m_Pr;
   double            m_sum,m_SUM,m_lsum,m_LSUM;
  };
//+X================================================================X+
//|  Алгоритм получения стандартного отклонения                      |
//+X================================================================X+
class CStdDeviation : public CMovSeriesTools
  {
public:
   double            StdDevSeries(uint begin,// номер начала достоверного отсчёта баров
                                  uint prev_calculated,// Количество истории в барах на предыдущем тике
                                  uint rates_total,// Количество истории в барах на текущем тике
                                  int Length,// Период усреднения
                                  double deviation,// Девиация
                                  double series,// Значение ценового ряда, расчитанное для бара с номером bar
                                  double MovSeries,// Значение мувинга, относительно которого расчитывается StdDeviation
                                  uint bar,// Номер бара
                                  bool set // Направление индексирования массивов.
                                  );
protected:
   int               m_Size_,m_count;
   double            m_Sum,m_SUM,m_Sum2,m_SUM2;
   double            m_SeriesArray[];
  };
//+X================================================================X+
//| Алгоритм JMA усреднения произвольных ценовых рядов               |
//+X================================================================X+
class CJJMA : public CMovSeriesTools
  {
public:
   double            JJMASeries(uint begin,// номер начала достоверного отсчёта баров
                                uint prev_calculated,// Количество истории в барах на предыдущем тике
                                uint rates_total,// Количество истории в барах на текущем тике
                                int  Din,// разрешение изменять параметры Length и Phase на каждом баре. 
                                // 0 - запрет изменения параметров,  любое другое значение - разрешение.
                                double Phase,// параметр, изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса усреднения
                                double Length,// глубина сглаживания
                                double series,// Значение ценового ряда, расчитанное для бара с номером bar
                                uint bar,// Номер бара
                                bool set // Направление индексирования массивов.
                                );

   void              JJMALengthCheck(string LengthName,int ExternLength);
   void              JJMAPhaseCheck(string PhaseName,int ExternPhase);

protected:
   void              JJMAInit(uint begin,int Din,double Phase,double Length,double series,uint bar);

   //----+ Объявление глобальных переменных
   bool              m_start;
   //----
   double            m_array[62];
   //----
   double            m_degree,m_Phase,m_sense;
   double            m_Krx,m_Kfd,m_Krj,m_Kct;
   double            m_var1,m_var2;
   //----
   int               m_pos2,m_pos1;
   int               m_Loop1,m_Loop2;
   int               m_midd1,m_midd2;
   int               m_count1,m_count2,m_count3;
   //----
   double            m_ser1,m_ser2;
   double            m_Sum1,m_Sum2,m_JMA;
   double            m_storage1,m_storage2,m_djma;
   double            m_hoop1[128],m_hoop2[11],m_data[128];

   //----+ Переменные для восстановления расчётов на незакрытом баре
   int               m_pos2_,m_pos1_;
   int               m_Loop1_,m_Loop2_;
   int               m_midd1_,m_midd2_;
   int               m_count1_,m_count2_,m_count3_;
   //----
   double            m_ser1_,m_ser2_;
   double            m_Sum1_,m_Sum2_,m_JMA_;
   double            m_storage1_,m_storage2_,m_djma_;
   double            m_hoop1_[128],m_hoop2_[11],m_data_[128];
   //----
   bool              m_bhoop1[128],m_bhoop2[11],m_bdata[128];
  };
//+X================================================================X+
//| Алгоритм усреднения Тильсона произвольных ценовых рядов          |
//+X================================================================X+
class CT3 : public CMovSeriesTools
  {
public:
   double            T3Series(uint begin,// номер начала достоверного отсчёта баров
                              uint prev_calculated,// Количество истории в барах на предыдущем тике
                              uint rates_total,// Количество истории в барах на текущем тике
                              int  Din,// разрешение изменять параметр Length на каждом баре. 
                              // 0 - запрет изменения параметров,  любое другое значение - разрешение.
                              double Curvature,// Коэффициент (для удобства его величина измерения увеличина в сто раз!)
                              double Length,// глубина сглаживания
                              double series,// Значение ценового ряда, расчитанное для бара с номером bar
                              uint bar,// Номер бара
                              bool set // Направление индексирования массивов.
                              );
protected:
   void              T3Init(uint begin,
                            int Din,
                            double Curvature,
                            double Length,
                            double series,
                            uint bar
                            );

   //----+ Объявление глобальных переменных
   double            m_b2,m_b3;
   //----
   double            m_e1,m_e2,m_e3,m_e4,m_e5,m_e6;
   double            m_E1,m_E2,m_E3,m_E4,m_E5,m_E6;
   double            m_c1,m_c2,m_c3,m_c4,m_w1,m_w2;
  };
//+X================================================================X+
//| Алгоритм ультралинейного усреднения произвольных ценовых рядов   |
//+X================================================================X+
class CJurX : public CMovSeriesTools
  {
public:
   double            JurXSeries(uint begin,// номер начала достоверного отсчёта баров
                                uint prev_calculated,// Количество истории в барах на предыдущем тике
                                uint rates_total,// Количество истории в барах на текущем тике
                                int  Din,// разрешение изменять параметр Length на каждом баре. 
                                // 0 - запрет изменения параметров,  любое другое значение - разрешение.
                                double Length,// глубина сглаживания
                                double series,// Значение ценового ряда, расчитанное для бара с номером bar
                                uint bar,// Номер бара
                                bool set // Направление индексирования массивов.
                                );
protected:
   void              JurXInit(uint begin,
                              int Din,
                              double Length,
                              double series,
                              uint bar
                              );

   //----+ Объявление глобальных переменных
   double            m_AB,m_AC;
   double            m_f1,m_f2,m_f3,m_f4,m_f5;
   double            m_f6,m_Kg,m_Hg,m_F1,m_F2;
   double            m_F3,m_F4,m_F5,m_F6,m_w;
  };
//+X================================================================X+
//| Алгоритмы усреднения Тушара Чанда для произвольных ценовых рядов |
//+X================================================================X+
class CCMO : public CMovSeriesTools
  {
public:
   double            VIDYASeries(uint begin,// номер начала достоверного отсчёта баров
                                 uint prev_calculated,// Количество истории в барах на предыдущем тике
                                 uint rates_total,// Количество истории в барах на текущем тике
                                 int CMO_Length,// CMO период
                                 double EMA_Length,
                                 double series,// Значение ценового ряда, расчитанное для бара с номером bar
                                 uint bar,// Номер бара
                                 bool set // Направление индексирования массивов.
                                 );

   double            CMOSeries(uint begin,// номер начала достоверного отсчёта баров
                               uint prev_calculated,// Количество истории в барах на предыдущем тике
                               uint rates_total,// Количество истории в барах на текущем тике
                               int CMO_Length,// CMO период
                               double series,
                               uint bar,// Номер бара
                               bool set // Направление индексирования массивов.
                               );

protected:
   double            m_dSeriesArray[];
   int               m_Size_,m_count;
   double            m_UpSum_,m_UpSum,m_DnSum_,m_DnSum,m_Vidya,m_Vidya_;
   double            m_AbsCMO_,m_AbsCMO,m_series1,m_series1_,m_SmoothFactor;
  };
//+X================================================================X+
//| Алгоритм получения индикатора АМА от произвольных ценовых рядов  |
//+X================================================================X+
class CAMA : public CMovSeriesTools
  {
public:
   double            AMASeries(uint begin,// номер начала достоверного отсчёта баров
                               uint prev_calculated,// Количество истории в барах на предыдущем тике
                               uint rates_total,// Количество истории в барах на текущем тике
                               int Length,// период AMA
                               int Fast_Length, // период быстрой скользящей
                               int Slow_Length, // период медленной скользящей
                               double Rate,// степень, в которую возводится сглаживающая константа
                               double series,// Значение ценового ряда, расчитанное для бара с номером bar
                               uint bar,// Номер бара
                               bool set // Направление индексирования массивов.
                               );
protected:
   //----+  
   double            m_SeriesArray[];
   double            m_dSeriesArray[];
   double            m_NOISE,m_noise;
   double            m_Ama,m_AMA_,m_slowSC,m_fastSC,m_dSC;
   int               m_Size_1,m_Size_2,m_count;
  };
//+X================================================================X+
//| Алгоритм параболического усреднения произвольных ценовых рядов   |
//+X================================================================X+
class CParMA : public CMovSeriesTools
  {
public:
   double            ParMASeries(uint begin,// номер начала достоверного отсчёта баров
                                 uint prev_calculated,// Количество истории в барах на предыдущем тике
                                 uint rates_total,// Количество истории в барах на текущем тике
                                 int Length,// Период усреднения
                                 double series,// Значение ценового ряда, расчитанное для бара с номером bar
                                 uint bar,// Номер бара
                                 bool set // Направление индексирования массивов.
                                 );
protected:
   void              ParMAInit(double Length);

   double            m_SeriesArray[];
   int               m_Size_,m_count;
   int               m_sum_x,m_sum_x2,m_sum_x3,m_sum_x4;
  };
//+X================================================================X+
//| Алгоритм моментума (версия Мерфи!) от произвольных ценовых рядов |
//+X================================================================X+
class CMomentum : public CMovSeriesTools
  {
public:
   double            MomentumSeries(uint begin,// номер начала достоверного отсчёта баров
                                    uint prev_calculated,// Количество истории в барах на предыдущем тике
                                    uint rates_total,// Количество истории в барах на текущем тике
                                    int Length,// Период усреднения
                                    double series,// Значение ценового ряда, расчитанное для бара с номером bar
                                    uint bar,// Номер бара
                                    bool set // Направление индексирования массивов.
                                    );
protected:

   double            m_SeriesArray[];
   int               m_Size_,m_count;
  };
//+X================================================================X+
//| Алгоритм нормированного моментума от произвольных ценовых рядов  |
//+X================================================================X+
class CnMomentum : public CMovSeriesTools
  {
public:
   double            nMomentumSeries(uint begin,// номер начала достоверного отсчёта баров
                                     uint prev_calculated,// Количество истории в барах на предыдущем тике
                                     uint rates_total,// Количество истории в барах на текущем тике
                                     int Length,// Период усреднения
                                     double series,// Значение ценового ряда, расчитанное для бара с номером bar
                                     uint bar,// Номер бара
                                     bool set // Направление индексирования массивов.
                                     );
protected:

   double            m_SeriesArray[];
   int               m_Size_,m_count;
  };
//+X================================================================X+
//| Алгоритм Скорость изменения от произвольных ценовых рядов        |
//+X================================================================X+
class CROC : public CMovSeriesTools
  {
public:
   double            ROCSeries(uint begin,// номер начала достоверного отсчёта баров
                               uint prev_calculated,// Количество истории в барах на предыдущем тике
                               uint rates_total,// Количество истории в барах на текущем тике
                               int Length,// Период усреднения
                               double series,// Значение ценового ряда, расчитанное для бара с номером bar
                               uint bar,// Номер бара
                               bool set // Направление индексирования массивов.
                               );
protected:

   double            m_SeriesArray[];
   int               m_Size_,m_count;
  };
//+X================================================================X+
//|  Функции для усреднения ценовых рядов цифровым фильтром FATL     |
//+X================================================================X+
class CFATL : public CMovSeriesTools
  {
public:
   double            FATLSeries(uint begin,// номер начала достоверного отсчёта баров
                                uint prev_calculated,// Количество истории в барах на предыдущем тике
                                uint rates_total,// Количество истории в барах на текущем тике
                                double series,// Значение ценового ряда, расчитанное для бара с номером bar
                                uint bar,// Номер бара
                                bool set // Направление индексирования массивов.
                                );
                     CFATL();
protected:
   double            m_SeriesArray[39];
   int               m_Size_,m_count;
   double            m_FATL;

   //---- объявление и инициализация массива для коэффициентов цифрового фильтра
   double            m_FATLTable[39];
  };
//+X================================================================X+
//|  Функции для усреднения ценовых рядов цифровым фильтром SATL     |
//+X================================================================X+
class CSATL : public CMovSeriesTools
  {
public:
   double            SATLSeries(uint begin,// номер начала достоверного отсчёта баров
                                uint prev_calculated,// Количество истории в барах на предыдущем тике
                                uint rates_total,// Количество истории в барах на текущем тике
                                double series,// Значение ценового ряда, расчитанное для бара с номером bar
                                uint bar,// Номер бара
                                bool set // Направление индексирования массивов.
                                );
                     CSATL();
protected:
   double            m_SeriesArray[65];
   int               m_Size_,m_count;
   double            m_SATL;

   //---- объявление и инициализация массива для коэффициентов цифрового фильтра
   double            m_SATLTable[65];
  };
//+X================================================================X+
//|  Функции для усреднения ценовых рядов цифровым фильтром RFTL     |
//+X================================================================X+
class CRFTL : public CMovSeriesTools
  {
public:
   double            RFTLSeries(uint begin,// номер начала достоверного отсчёта баров
                                uint prev_calculated,// Количество истории в барах на предыдущем тике
                                uint rates_total,// Количество истории в барах на текущем тике
                                double series,// Значение ценового ряда, расчитанное для бара с номером bar
                                uint bar,// Номер бара
                                bool set // Направление индексирования массивов.
                                );
                     CRFTL();
protected:
   double            m_SeriesArray[44];
   int               m_Size_,m_count;
   double            m_RFTL;

   //---- объявление и инициализация массива для коэффициентов цифрового фильтра
   double            m_RFTLTable[44];
  };
//+X================================================================X+
//|  Функции для усреднения ценовых рядов цифровым фильтром RSTL     |
//+X================================================================X+
class CRSTL : public CMovSeriesTools
  {
public:
   double            RSTLSeries(uint begin,// номер начала достоверного отсчёта баров
                                uint prev_calculated,// Количество истории в барах на предыдущем тике
                                uint rates_total,// Количество истории в барах на текущем тике
                                double series,// Значение ценового ряда, расчитанное для бара с номером bar
                                uint bar,// Номер бара
                                bool set // Направление индексирования массивов.
                                );
                     CRSTL();
protected:
   double            m_SeriesArray[99];
   int               m_Size_,m_count;
   double            m_RSTL;

   //---- объявление и инициализация массива для коэффициентов цифрового фильтра
   double            m_RSTLTable[99];
  };
//+X================================================================X+
//|  Алгоритм универсального усреднения                              |
//+X================================================================X+
class CXMA
  {
public:

   double            XMASeries(uint begin,// номер начала достоверного отсчёта баров
                               uint prev_calculated,// Количество истории в барах на предыдущем тике
                               uint rates_total,// Количество истории в барах на текущем тике
                               // 0 - запрет изменения параметров,  любое другое значение - разрешение.
                               Smooth_Method Method,
                               int Phase,// параметр, изменяющийся в пределах -100 ... +100, 
                               //влияет на качество переходного процесса усреднения
                               int Length,// глубина сглаживания
                               double series,// Значение ценового ряда, расчитанное для бара с номером bar
                               uint bar,// Номер бара
                               bool set // Направление индексирования массивов.
                               );

   int               GetStartBars(Smooth_Method Method,int Length,int Phase);
   string            GetString_MA_Method(Smooth_Method Method);
   void              XMAPhaseCheck(string PhaseName,int ExternPhase,Smooth_Method Method);
   void              XMALengthCheck(string LengthName,int ExternLength);
   void              XMAInit(Smooth_Method Method);
                     CXMA(){m_init=false;};
                    ~CXMA();

protected:

   CMoving_Average *SMA;
   CMoving_Average *EMA;
   CMoving_Average *SMMA;
   CMoving_Average *LWMA;
   CJJMA            *JJMA;
   CJurX            *JurX;
   CParMA           *ParMA;
   CT3              *T3;
   CCMO             *VIDYA;
   CAMA             *AMA;

   bool              m_init;
   Smooth_Method     m_Method;
  };
//+X================================================================X+
//|  расчёт минимального количества необходимых баров алгоритма XMA  |
//+X================================================================X+     
int GetStartBars(Smooth_Method Method,int Length,int Phase)
  {
//----+ 
   switch(Method)
     {
      case MODE_SMA_:  return(Length);
      case MODE_EMA_:  return(0);
      case MODE_SMMA_: return(Length+1);
      case MODE_LWMA_: return(Length);
      case MODE_JJMA:  return(30);
      case MODE_JurX:  return(0);
      case MODE_ParMA: return(Length);
      case MODE_T3:    return(0);
      case MODE_VIDYA: return(Phase+2);
      case MODE_AMA:   return(Length+2);
     }
//----+
   return(0);
  }
//Version  May 1, 2010
//+X================================================================X+
//|                                                 iPriceSeries.mqh |
//|                        Copyright © 2010,        Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+X================================================================X+
/* 
 * Функция iPriceSeries() возвращает входную цену бара по его номеру
 * bar и по номеру цены applied_price:
 * 1-CLOSE, 2-OPEN, 3-HIGH, 4-LOW, 5-MEDIAN, 6-TYPICAL, 7-WEIGHTED,
 * 8-SIMPL, 9-QUARTER, 10-TRENDFOLLOW, 11-0.5 * TRENDFOLLOW, 12-Demark Price.
 * 
 * Пример: 
 * double dPrice = iPriceSeries("GBPJPY", 240, 5, bar, true)
 *                - iPriceSeries("GBPJPY", 240, 5, bar + 1, true);
 */
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
/*
//---- объявление и инициализация перечисления типов ценовых констант
enum Applied_price_ //Тип константы
 {
  PRICE_CLOSE_ = 1,     //Close
  PRICE_OPEN_,          //Open
  PRICE_HIGH_,          //High
  PRICE_LOW_,           //Low
  PRICE_MEDIAN_,        //Median Price (HL/2)
  PRICE_TYPICAL_,       //Typical Price (HLC/3)
  PRICE_WEIGHTED_,      //Weighted Close (HLCC/4)
  PRICE_SIMPL_,         //Simpl Price (OC/2)
  PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
  PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
  PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price 
  PRICE_DEMARK_         //Demark Price
 };
*/
//+X================================================================X+   
//| PriceSeries() function                                           |
//+X================================================================X+ 
double PriceSeries
(
 uint applied_price,// Ценовая константа
 uint   bar,// Индекс сдвига относительно текущего бара на указанное количество периодов назад или вперёд).
 const double &Open[],
 const double &Low[],
 const double &High[],
 const double &Close[]
 )
//PriceSeries(applied_price, bar, open, low, high, close)
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+
   switch(applied_price)
     {
      //----+ Ценовые константы из перечисления ENUM_APPLIED_PRICE
      case  PRICE_CLOSE: return(Close[bar]);
      case  PRICE_OPEN: return(Open [bar]);
      case  PRICE_HIGH: return(High [bar]);
      case  PRICE_LOW: return(Low[bar]);
      case  PRICE_MEDIAN: return((High[bar]+Low[bar])/2.0);
      case  PRICE_TYPICAL: return((Close[bar]+High[bar]+Low[bar])/3.0);
      case  PRICE_WEIGHTED: return((2*Close[bar]+High[bar]+Low[bar])/4.0);

      //----+                            
      case  8: return((Open[bar] + Close[bar])/2.0);
      case  9: return((Open[bar] + Close[bar] + High[bar] + Low[bar])/4.0);
      //----                                
      case 10:
        {
         if(Close[bar]>Open[bar]) return(High[bar]);
         else
           {
            if(Close[bar]<Open[bar]) return(Low[bar]);
            else return(Close[bar]);
           }
        }
      //----         
      case 11:
        {
         if(Close[bar]>Open[bar])return((High[bar]+Close[bar])/2.0);
         else
           {
            if(Close[bar]<Open[bar]) return((Low[bar]+Close[bar])/2.0);
            else return(Close[bar]);
           }
        }
      //----         
      case 12:
        {
         double res=High[bar]+Low[bar]+Close[bar];

         if(Close[bar]<Open[bar]) res=(res+Low[bar])/2;
         if(Close[bar]>Open[bar]) res=(res+High[bar])/2;
         if(Close[bar]==Open[bar]) res=(res+Close[bar])/2;
         return(((res-Low[bar])+(res-High[bar]))/2);
        }
      //----
      default: return(Close[bar]);
     }
//----+
//return(0);
  }
//+X================================================================X+   
//| iPriceSeries() function                                          |
//+X================================================================X+ 
double iPriceSeries
(
 string          symbol,// Символьное имя инструмента. NULL означает текущий символ.
 ENUM_TIMEFRAMES timeframe,// Период. Может быть одним из периодов графика. 0 означает период текущего графика.
 uint            applied_price,// Ценовая константа
 uint            bar,// Индекс сдвига относительно текущего бара на указанное количество периодов назад или вперёд).
 bool            set // Направление индексирования массивов.
 )
//iPriceSeries(symbol, timeframe, applied_price, bar, set)
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+
   uint Bar;
   double diPriceSeries,price[1];
//----
   if(!set)
      Bar=Bars(symbol,timeframe)-1-bar;
   else Bar=bar;
//----
   switch(applied_price)
     {
      case  1: CopyClose(symbol, timeframe, Bar, 1, price); diPriceSeries = price[0]; break;
      case  2: CopyOpen (symbol, timeframe, Bar, 1, price); diPriceSeries = price[0]; break;
      case  3: CopyHigh (symbol, timeframe, Bar, 1, price); diPriceSeries = price[0]; break;
      case  4: CopyLow  (symbol, timeframe, Bar, 1, price); diPriceSeries = price[0]; break;
      //----
      case  5: CopyHigh(symbol,timeframe,Bar,1,price); diPriceSeries=price[0];
      CopyLow(symbol,timeframe,Bar,1,price); diPriceSeries+=price[0];
      diPriceSeries/=2.0;
      break;
      //----
      case  6: CopyClose(symbol,timeframe,Bar,1,price); diPriceSeries=price[0];
      CopyHigh (symbol, timeframe, Bar, 1, price); diPriceSeries += price[0];
      CopyLow  (symbol, timeframe, Bar, 1, price); diPriceSeries += price[0];
      diPriceSeries/=3.0;
      break;
      //----                                                                  
      case  7: CopyClose(symbol,timeframe,Bar,1,price); diPriceSeries=price[0]*2;
      CopyHigh (symbol, timeframe, Bar, 1, price); diPriceSeries += price[0];
      CopyLow  (symbol, timeframe, Bar, 1, price); diPriceSeries += price[0];
      diPriceSeries/=4.0;
      break;

      //----                                 
      case  8: CopyClose(symbol,timeframe,Bar,1,price); diPriceSeries=price[0];
      CopyOpen(symbol,timeframe,Bar,1,price); diPriceSeries+=price[0];
      diPriceSeries/=2.0;
      break;
      //----
      case  9: CopyClose(symbol,timeframe,Bar,1,price); diPriceSeries=price[0];
      CopyOpen (symbol, timeframe, Bar, 1, price); diPriceSeries += price[0];
      CopyHigh (symbol, timeframe, Bar, 1, price); diPriceSeries += price[0];
      CopyLow  (symbol, timeframe, Bar, 1, price); diPriceSeries += price[0];
      diPriceSeries/=4.0;
      break;
      //----                                
      case 10:
        {
         double Open_[1],Low_[1],High_[1],Close_[1];
         //----
         CopyClose(symbol,timeframe,Bar,1,Close_);
         CopyOpen(symbol,timeframe,Bar,1,Open_);
         CopyHigh(symbol,timeframe,Bar,1,High_);
         CopyLow(symbol,timeframe,Bar,1,Low_);
         //----
         if(Close_[0]>Open_[0])diPriceSeries=High_[0];
         else
           {
            if(Close_[0]<Open_[0])
               diPriceSeries=Low_[0];
            else diPriceSeries=Close_[0];
           }
         break;
        }
      //----         
      case 11:
        {
         double Open_[1],Low_[1],High_[1],Close_[1];
         //----
         CopyClose(symbol,timeframe,Bar,1,Close_);
         CopyOpen(symbol,timeframe,Bar,1,Open_);
         CopyHigh(symbol,timeframe,Bar,1,High_);
         CopyLow(symbol,timeframe,Bar,1,Low_);
         //----
         if(Close_[0]>Open_[0])diPriceSeries=(High_[0]+Close_[0])/2.0;
         else
           {
            if(Close_[0]<Open_[0])
               diPriceSeries=(Low_[0]+Close_[0])/2.0;
            else diPriceSeries=Close_[0];
           }
         break;
        }
      //----         
      case 12:
        {
         double Open_[1],Low_[1],High_[1],Close_[1];
         //----
         CopyClose(symbol,timeframe,Bar,1,Close_);
         CopyOpen(symbol,timeframe,Bar,1,Open_);
         CopyHigh(symbol,timeframe,Bar,1,High_);
         CopyLow(symbol,timeframe,Bar,1,Low_);
         //----
         double res=High_[0]+Low_[0]+Close_[0];

         if(Close_[0]<Open_[0]) res=(res+Low_[0])/2;
         if(Close_[0]>Open_[0]) res=(res+High_[0])/2;
         if(Close_[0]==Open_[0]) res=(res+Close_[0])/2;
         diPriceSeries=((res-Low_[0])+(res-High_[0]))/2;
         break;
        }
      //----
      default: CopyClose(symbol,timeframe,Bar,1,price); diPriceSeries=price[0]; break;
     }
//----+
   return(diPriceSeries);
  }
//+X================================================================X+   
//| bPriceSeries() function                                          |
//+X================================================================X+ 
bool bPriceSeries
(
 string          symbol,// Символьное имя инструмента. NULL означает текущий символ.
 ENUM_TIMEFRAMES timeframe,// Период. Может быть одним из периодов графика. 0 означает период текущего графика.
 int             rates_total,// количество истории в барах на текущем тике (если параметр set равен true, 
 // то  значение параметра не нужно в расчёте функции и может быть равным 0)
 uint            applied_price,// Ценовая константа
 uint            bar, // Индекс сдвига относительно текущего бара на указанное количество периодов назад или вперёд).
 bool            set, // Направление индексирования массивов.
 double         &Price_ // возврат по ссылке полученного значения
 )
//bPriceSeries(symbol, timeframe, int rates_total, applied_price, bar, set, Price_)
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+
   uint Bar;
   double series[];
   ArraySetAsSeries(series,true);
//----
   if(!set)
      Bar=rates_total-1-bar;
   else Bar=bar;
//----
   switch(applied_price)
     {
      case  1: if(CopyClose(symbol, timeframe, Bar, 1, series) < 0) return(false); Price_ = series[0]; break;
      case  2: if(CopyOpen (symbol, timeframe, Bar, 1, series) < 0) return(false); Price_ = series[0]; break;
      case  3: if(CopyHigh (symbol, timeframe, Bar, 1, series) < 0) return(false); Price_ = series[0]; break;
      case  4: if(CopyLow  (symbol, timeframe, Bar, 1, series) < 0) return(false); Price_ = series[0]; break;
      //----
      case  5: if(CopyHigh(symbol,timeframe,Bar,1,series)<0) return(false); Price_=series[0];
      if(CopyLow(symbol,timeframe,Bar,1,series)<0) return(false); Price_+=series[0];
      Price_/=2.0;
      break;
      //----
      case  6: if(CopyClose(symbol,timeframe,Bar,1,series)<0) return(false); Price_=series[0];
      if(CopyHigh (symbol, timeframe, Bar, 1, series) < 0) return(false); Price_ += series[0];
      if(CopyLow  (symbol, timeframe, Bar, 1, series) < 0) return(false); Price_ += series[0];
      Price_/=3.0;
      break;
      //----                                                                  
      case  7: if(CopyClose(symbol,timeframe,Bar,1,series)<0) return(false); Price_=series[0]*2;
      if(CopyHigh (symbol, timeframe, Bar, 1, series) < 0) return(false); Price_ += series[0];
      if(CopyLow  (symbol, timeframe, Bar, 1, series) < 0) return(false); Price_ += series[0];
      Price_/=4.0;
      break;

      //----                                 
      case  8: if(CopyClose(symbol,timeframe,Bar,1,series)<0) return(false); Price_=series[0];
      if(CopyOpen(symbol,timeframe,Bar,1,series)<0) return(false); Price_+=series[0];
      Price_/=2.0;
      break;
      //----
      case  9: if(CopyClose(symbol,timeframe,Bar,1,series)<0) return(false); Price_=series[0];
      if(CopyOpen (symbol, timeframe, Bar, 1, series) < 0) return(false); Price_ += series[0];
      if(CopyHigh (symbol, timeframe, Bar, 1, series) < 0) return(false); Price_ += series[0];
      if(CopyLow  (symbol, timeframe, Bar, 1, series) < 0) return(false); Price_ += series[0];
      Price_/=4.0;
      break;
      //----                                
      case 10:
        {
         double Open_[1],Low_[1],High_[1],Close_[1];
         //----
         if(CopyClose(symbol, timeframe, Bar, 1, Close_) < 0) return(false);
         if(CopyOpen (symbol, timeframe, Bar, 1, Open_ ) < 0) return(false);
         if(CopyHigh (symbol, timeframe, Bar, 1, High_ ) < 0) return(false);
         if(CopyLow  (symbol, timeframe, Bar, 1, Low_  ) < 0) return(false);
         //----
         if(Close_[0]>Open_[0])Price_=High_[0];
         else
           {
            if(Close_[0]<Open_[0])
               Price_=Low_[0];
            else Price_=Close_[0];
           }
         break;
        }
      //----         
      case 11:
        {
         double Open_[1],Low_[1],High_[1],Close_[1];
         //----
         if(CopyClose(symbol, timeframe, Bar, 1, Close_) < 0) return(false);
         if(CopyOpen (symbol, timeframe, Bar, 1, Open_ ) < 0) return(false);
         if(CopyHigh (symbol, timeframe, Bar, 1, High_ ) < 0) return(false);
         if(CopyLow  (symbol, timeframe, Bar, 1, Low_  ) < 0) return(false);
         //----
         if(Close_[0]>Open_[0])Price_=(High_[0]+Close_[0])/2.0;
         else
           {
            if(Close_[0]<Open_[0])
               Price_=(Low_[0]+Close_[0])/2.0;
            else Price_=Close_[0];
           }
         break;
        }
      //----         
      case 12:
        {
         double Open_[1],Low_[1],High_[1],Close_[1];
         //----
         if(CopyClose(symbol, timeframe, Bar, 1, Close_) < 0) return(false);
         if(CopyOpen (symbol, timeframe, Bar, 1, Open_ ) < 0) return(false);
         if(CopyHigh (symbol, timeframe, Bar, 1, High_ ) < 0) return(false);
         if(CopyLow  (symbol, timeframe, Bar, 1, Low_  ) < 0) return(false);
         //----
         double res=High_[0]+Low_[0]+Close_[0];

         if(Close_[0]<Open_[0]) res=(res+Low_[0])/2;
         if(Close_[0]>Open_[0]) res=(res+High_[0])/2;
         if(Close_[0]==Open_[0]) res=(res+Close_[0])/2;
         Price_=((res-Low_[0])+(res-High_[0]))/2;
         break;
        }
      //----
      default: if(CopyClose(symbol,timeframe,Bar,1,series)<0) return(false); Price_=series[0]; break;
     }
//----+
   return(true);
  }
//+X================================================================X+   
//| bPriceSeriesOnArray() function                                   |
//+X================================================================X+ 
bool bPriceSeriesOnArray
(
 string          symbol,// Символьное имя инструмента. NULL означает текущий символ.
 ENUM_TIMEFRAMES timeframe,// Период. Может быть одним из периодов графика. 0 означает период текущего графика.
 uint            applied_price,// Ценовая константа
 int             start_pos,// Номер первого копируемого элемента
 int             count,// Количество копируемых элементов
 double         &series[]// массив, куда будут скопированы данные
 )
//bPriceSeriesOnArray(symbol, timeframe, applied_price, start_pos, count, Array)
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+
   ArraySetAsSeries(series,true);

   switch(applied_price)
     {
      case  1: if(CopyClose(symbol, timeframe, start_pos, count, series) < 0) return(false); break;
      case  2: if(CopyOpen (symbol, timeframe, start_pos, count, series) < 0) return(false); break;
      case  3: if(CopyHigh (symbol, timeframe, start_pos, count, series) < 0) return(false); break;
      case  4: if(CopyLow  (symbol, timeframe, start_pos, count, series) < 0) return(false); break;
      //----
      case  5:
        {
         double Low_[];
         ArraySetAsSeries(Low_,true);
         if(CopyHigh(symbol, timeframe, start_pos, count, series) < 0) return(false);
         if(CopyLow (symbol, timeframe, start_pos, count,  Low_ ) < 0) return(false);

         for(int kkk=start_pos; kkk<start_pos+count; kkk++)
            series[kkk]=(series[kkk]+Low_[kkk])/2.0;
         break;
        }
      //----
      case  6:
        {
         double Low_[],High_[];
         ArraySetAsSeries(Low_,true);
         ArraySetAsSeries(High_,true);
         if(CopyClose(symbol, timeframe, start_pos, count, series) < 0) return(false);
         if(CopyHigh (symbol, timeframe, start_pos, count, High_ ) < 0) return(false);
         if(CopyLow  (symbol, timeframe, start_pos, count, Low_  ) < 0) return(false);

         for(int kkk=start_pos; kkk<start_pos+count; kkk++)
            series[kkk]=(series[kkk]+High_[kkk]+Low_[kkk])/3.0;
         break;
        }
      //----                                                                  
      case  7:
        {
         double Low_[],High_[];
         ArraySetAsSeries(Low_,true);
         ArraySetAsSeries(High_,true);
         if(CopyClose(symbol, timeframe, start_pos, count, series) < 0) return(false);
         if(CopyHigh (symbol, timeframe, start_pos, count, High_ ) < 0) return(false);
         if(CopyLow  (symbol, timeframe, start_pos, count, Low_  ) < 0) return(false);

         for(int kkk=start_pos; kkk<start_pos+count; kkk++)
            series[kkk]=(2*series[kkk]+High_[kkk]+Low_[kkk])/4.0;
         break;
        }
      //----                                 
      case  8:
        {
         double Open_[];
         ArraySetAsSeries(Open_,true);
         if(CopyClose(symbol, timeframe, start_pos, count, series) < 0) return(false);
         if(CopyOpen (symbol, timeframe, start_pos, count, Open_ ) < 0) return(false);

         for(int kkk=start_pos; kkk<start_pos+count; kkk++)
            series[kkk]=(series[kkk]+Open_[kkk])/2.0;
         break;
        }
      //----
      case  9:
        {
         double Open_[],Low_[],High_[];
         ArraySetAsSeries(Open_,true);
         ArraySetAsSeries(Low_,true);
         ArraySetAsSeries(High_,true);
         if(CopyOpen (symbol, timeframe, start_pos, count, Open_ ) < 0) return(false);
         if(CopyClose(symbol, timeframe, start_pos, count, series) < 0) return(false);
         if(CopyHigh (symbol, timeframe, start_pos, count, High_ ) < 0) return(false);
         if(CopyLow  (symbol, timeframe, start_pos, count, Low_  ) < 0) return(false);

         for(int kkk=start_pos; kkk<start_pos+count; kkk++)
            series[kkk]=(Open_[kkk]+series[kkk]+High_[kkk]+Low_[kkk])/4.0;
         break;
        }
      //----                                
      case 10:
        {
         double Open_[],Low_[],High_[];
         ArraySetAsSeries(Open_,true);
         ArraySetAsSeries(Low_,true);
         ArraySetAsSeries(High_,true);
         if(CopyClose(symbol, timeframe, start_pos, count, series) < 0) return(false);
         if(CopyOpen (symbol, timeframe, start_pos, count, Open_ ) < 0) return(false);
         if(CopyHigh (symbol, timeframe, start_pos, count, High_ ) < 0) return(false);
         if(CopyLow  (symbol, timeframe, start_pos, count, Low_  ) < 0) return(false);
         //----
         for(int kkk=start_pos; kkk<start_pos+count; kkk++)
           {
            if(series[kkk]>Open_[kkk]) series[kkk]=High_[kkk];
            else
              {
               if(series[kkk]<Open_[kkk])
                  series[kkk]=Low_[kkk];
              }
           }
         break;
        }
      //----         
      case 11:
        {
         double Open_[],Low_[],High_[];
         ArraySetAsSeries(Open_,true);
         ArraySetAsSeries(Low_,true);
         ArraySetAsSeries(High_,true);
         if(CopyClose(symbol, timeframe, start_pos, count, series) < 0) return(false);
         if(CopyOpen (symbol, timeframe, start_pos, count, Open_ ) < 0) return(false);
         if(CopyHigh (symbol, timeframe, start_pos, count, High_ ) < 0) return(false);
         if(CopyLow  (symbol, timeframe, start_pos, count, Low_  ) < 0) return(false);
         //----
         for(int kkk=start_pos; kkk<start_pos+count; kkk++)
           {
            if(series[kkk]>Open_[kkk]) series[kkk]=(High_[kkk]+series[kkk])/2.0;
            else
              {
               if(series[kkk]<Open_[kkk])
                  series[kkk]=(Low_[kkk]+series[kkk])/2.0;
              }
           }
         break;
        }
      //----         
      case 12:
        {
         double Open_[],Low_[],High_[];
         ArraySetAsSeries(Open_,true);
         ArraySetAsSeries(Low_,true);
         ArraySetAsSeries(High_,true);
         if(CopyClose(symbol, timeframe, start_pos, count, series) < 0) return(false);
         if(CopyOpen (symbol, timeframe, start_pos, count, Open_ ) < 0) return(false);
         if(CopyHigh (symbol, timeframe, start_pos, count, High_ ) < 0) return(false);
         if(CopyLow  (symbol, timeframe, start_pos, count, Low_  ) < 0) return(false);
         //----
         for(int kkk=start_pos; kkk<start_pos+count; kkk++)
           {
            double res=High_[kkk]+Low_[kkk]+series[kkk];

            if(series[kkk]<Open_[kkk]) res=(res+Low_[kkk])/2;
            if(series[kkk]>Open_[kkk]) res=(res+High_[kkk])/2;
            if(series[kkk]==Open_[kkk]) res=(res+series[kkk])/2;
            series[kkk]=((res-Low_[kkk])+(res-High_[kkk]))/2;
           }
         break;
        }
      //----
      default: if(CopyClose(symbol,timeframe,start_pos,count,series)<0) return(false);
     }
//----+
   return(true);
  }
//+X================================================================X+   
//| iPriceSeriesAlert() function                                     |
//+X================================================================X+
/*
 * Функция iPriceSeriesAlert() предназначена для индикации  недопустимого 
 * значения параметра applied_price передаваемого в функцию iPriceSeries().
 */
void iPriceSeriesAlert(uchar applied_price)
  {
//----+
   if(applied_price<1)
      Alert("Параметр applied_price должен быть не менее 1. Вы ввели недопустимое ",
            applied_price," будет использовано 1");
//----+                     
   if(applied_price>12)
      Alert("Параметр applied_price должен быть не более 12. Вы ввели недопустимое ",
            applied_price," будет использовано 1");
//----+ 
  }
//+X================================================================X+
//|  Классические алгоритмы усреднения                               |
//+X================================================================X+    
double CMoving_Average::MASeries
(
 uint begin,// номер начала достоверного отсчёта баров
 uint prev_calculated,// Количество истории в барах на предыдущем тике
 uint rates_total,// Количество истории в барах на текущем тике
 int Length,// Период усреднения
 ENUM_MA_METHOD MA_Method,// Метод усреднения (MODE_SMA, MODE_EMA, MODE_SMMA, MODE_LWMA)
 double series,// Значение ценового ряда, расчитанное для бара с номером bar
 uint bar,// Номер бара
 bool set // Направление индексирования массивов.
 )
// MASeries(begin, prev_calculated, Length, MA_Method, series, bar, set) 
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+   
   switch(MA_Method)
     {
      case MODE_SMA:  return(SMASeries (begin, prev_calculated, rates_total, Length, series, bar, set));
      case MODE_EMA:  return(EMASeries (begin, prev_calculated, rates_total, Length, series, bar, set));
      case MODE_SMMA: return(SMMASeries(begin, prev_calculated, rates_total, Length, series, bar, set));
      case MODE_LWMA: return(LWMASeries(begin, prev_calculated, rates_total, Length, series, bar, set));
      default:
        {
         if(bar==begin)
           {
            string word;
            StringConcatenate(word,__FUNCTION__,"():",
                              " Параметр MA_Method должен быть от MODE_SMA до MODE_LWMA.",
                              " Вы ввели недопустимое ",MA_Method," будет использовано MODE_SMA!");
            Print(word);
           }
         return(SMASeries(begin,prev_calculated,rates_total,Length,series,bar,set));
        }
     }
//----+
  }
//+X================================================================X+
//|  Простое усреднение                                              |
//+X================================================================X+    
double CMoving_Average::SMASeries
(
 uint begin,// Номер начала достоверного отсчёта баров
 uint prev_calculated,// Количество истории в барах на предыдущем тике
 uint rates_total,// Количество истории в барах на текущем тике
 int Length,// Период усреднения
 double series,// Значение ценового ряда, расчитанное для бара с номером bar
 uint bar,// Номер бара
 bool set // Направление индексирования массивов
 )
// SMASeries(begin, prev_calculated, rates_total, Length, series, bar, set) 
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+ Проверка начала достоверного отсчёта баров 
   if(BarCheck1(begin,bar,set)) return(EMPTY_VALUE);

//----+ Объявление локальных переменных
   int iii,kkk;
   double sma;

//----+ проверка внешего параметра Length на корректность
   LengthCheck(Length);

//----+ Изменение размеров массива переменных
   if(bar==begin && !SeriesArrayResize(__FUNCTION__,Length,m_SeriesArray,m_Size_))
      return(EMPTY_VALUE);

//----+ перестановка и инициализация ячеек массива m_SeriesArray
   Recount_ArrayZeroPos(m_count,Length,prev_calculated,rates_total,series,bar,m_SeriesArray,set);

//----+ Инициализация нуля
   if(BarCheck2(begin,bar,set,Length))
     {
      m_sum=0.0;

      for(iii=1; iii<Length; iii++)
        {
         kkk=Recount_ArrayNumber(m_count,Length,iii);
         m_sum+=m_SeriesArray[kkk];
        }
     }
   else if(BarCheck3(begin,bar,set,Length)) return(EMPTY_VALUE);

//----+ Вычисление SMA
   m_sum+=series;
   sma = m_sum / Length;
   kkk = Recount_ArrayNumber(m_count, Length, Length - 1);
   m_sum-=m_SeriesArray[kkk];

//----+ Сохранение значений переменных
   if(BarCheck4(rates_total,bar,set))
     {
      m_SUM=m_sum;
     }

//----+ Восстановление значений переменных
   if(BarCheck5(rates_total,bar,set))
     {
      m_sum=m_SUM;
     }
//----+
   return(sma);
  }
//+X================================================================X+
//|  Экспоненциальное усреднение                                     |
//+X================================================================X+    
double CMoving_Average::EMASeries
(
 uint begin,// Номер начала достоверного отсчёта баров
 uint prev_calculated,// Количество истории в барах на предыдущем тике
 uint rates_total,// Количество истории в барах на текущем тике
 double Length, // Период усреднения
 double series, // Значение ценового ряда, расчитанное для бара с номером bar
 uint bar,// Номер бара
 bool set // Направление индексирования массивов
 )
// EMASeries(begin, prev_calculated, Length, series, bar, set) 
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+ Проверка начала достоверного отсчёта баров 
   if(BarCheck1(begin,bar,set)) return(EMPTY_VALUE);

//----+ Объявление локальных переменных
   double ema;

//----+ проверка внешего параметра Length на корректность
   LengthCheck(Length);

//----+ Инициализация нуля
   if(bar==begin)
     {
      m_Pr=2.0/(Length+1.0);
      m_Moving=series;
     }

//----+ Вычисление EMA 
   m_Moving=series*m_Pr+m_Moving *(1-m_Pr);
   ema=m_Moving;

//----+ Сохранение значений переменных
   if(BarCheck4(rates_total,bar,set))
     {
      m_MOVING=m_Moving;
     }

//----+ Восстановление значений переменных
   if(BarCheck5(rates_total,bar,set))
     {
      m_Moving=m_MOVING;
     }
//----+
   return(ema);
  }
//+X================================================================X+
//|  Сглаженное усреднение                                           |
//+X================================================================X+
double CMoving_Average::SMMASeries
(
 uint begin,// Номер начала достоверного отсчёта баров
 uint prev_calculated,// Количество истории в барах на предыдущем тике
 uint rates_total,// Количество истории в барах на текущем тике
 int Length,// Период усреднения
 double series,// Значение ценового ряда, расчитанное для бара с номером bar
 uint bar,// Номер бара
 bool set // Направление индексирования массивов
 )
// SMMASeries(begin, prev_calculated, Length, series, bar, set) 
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+ Проверка начала достоверного отсчёта баров 
   if(BarCheck1(begin,bar,set)) return(EMPTY_VALUE);

//----+ Объявление локальных переменных
   int iii;
   double smma;

//----+ проверка внешего параметра Length на корректность
   LengthCheck(Length);

//----+ Изменение размеров массива переменных
   if(bar==begin && !SeriesArrayResize(__FUNCTION__,Length,m_SeriesArray,m_Size_))
      return(EMPTY_VALUE);

//----+ перестановка и инициализация ячеек массива m_SeriesArray
   Recount_ArrayZeroPos(m_count,Length,prev_calculated,rates_total,series,bar,m_SeriesArray,set);

//----+ Инициализация нуля
   if(BarCheck2(begin,bar,set,Length))
     {
      m_sum=0.0;
      for(iii=0; iii<Length; iii++)
         m_sum+=m_SeriesArray[iii];

      m_Moving=(m_sum-series)/(Length-1);
     }
   else if(BarCheck3(begin,bar,set,Length)) return(EMPTY_VALUE);

//----+ Вычисление SMMA
   m_sum=m_Moving *(Length-1)+series;
   m_Moving=m_sum/Length;
   smma=m_Moving;

//----+ Сохранение значений переменных
   if(BarCheck4(rates_total,bar,set))
     {
      m_SUM=m_sum;
      m_MOVING=m_Moving;
     }

//----+ Восстановление значений переменных
   if(BarCheck5(rates_total,bar,set))
     {
      m_sum=m_SUM;
      m_Moving=m_MOVING;
     }
//----+
   return(smma);
  }
//+X================================================================X+
//|  Линейно-взвешенное усреднение                                   |
//+X================================================================X+
double CMoving_Average::LWMASeries
(
 uint begin,// Номер начала достоверного отсчёта баров
 uint prev_calculated,// Количество истории в барах на предыдущем тике
 uint rates_total,// Количество истории в барах на текущем тике
 int Length,// Период усреднения
 double series,// Значение ценового ряда, расчитанное для бара с номером bar
 uint bar,// Номер бара
 bool set // Направление индексирования массивов
 )
// LWMASeries(begin, prev_calculated, Length, series, bar, set) 
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+ Проверка начала достоверного отсчёта баров 
   if(BarCheck1(begin,bar,set)) return(EMPTY_VALUE);

//----+ Объявление локальных переменных
   double lwma;
   int iii,kkk,Length_=Length+1;

//----+ проверка внешего параметра Length на корректность
   LengthCheck(Length);

//----+ Изменение размеров массива переменных
   if(bar==begin && !SeriesArrayResize(__FUNCTION__,Length_,m_SeriesArray,m_Size_))
      return(EMPTY_VALUE);

//----+ перестановка и инициализация ячеек массива m_SeriesArray
   Recount_ArrayZeroPos(m_count,Length_,prev_calculated,rates_total,series,bar,m_SeriesArray,set);

//----+ Инициализация нуля
   if(BarCheck2(begin,bar,set,Length_))
     {
      m_sum=0.0;
      m_lsum=0.0;
      m_weight=0;
      int rrr=Length;

      for(iii=1; iii<=Length; iii++,rrr--)
        {
         kkk=Recount_ArrayNumber(m_count,Length_,iii);
         m_sum+=m_SeriesArray[kkk]*rrr;
         m_lsum+=m_SeriesArray[kkk];
         m_weight+=iii;
        }
     }
   else if(BarCheck3(begin,bar,set,Length_)) return(EMPTY_VALUE);

//----+ Вычисление LWMA
   m_sum+=series*Length-m_lsum;
   kkk=Recount_ArrayNumber(m_count,Length_,Length);
   m_lsum+=series-m_SeriesArray[kkk];
   lwma=m_sum/m_weight;

//----+ Сохранение значений переменных
   if(BarCheck4(rates_total,bar,set))
     {
      m_SUM  = m_sum;
      m_LSUM = m_lsum;
     }

//----+ Восстановление значений переменных
   if(BarCheck5(rates_total,bar,set))
     {
      m_sum=m_SUM;
      m_lsum=m_LSUM;
     }
//----+
   return(lwma);
  }
//+X================================================================X+
//|  Вычисление стандартного отклонения                              |
//+X================================================================X+    
double CStdDeviation::StdDevSeries
(
 uint begin,// номер начала достоверного отсчёта баров
 uint prev_calculated,// Количество истории в барах на предыдущем тике
 uint rates_total,// Количество истории в барах на текущем тике
 int Length,// Период усреднения
 double deviation,// Девиация
 double series,// Значение ценового ряда, расчитанное для бара с номером bar
 double MovSeries,// Значение мувинга, относительно которого расчитывается StdDeviation
 uint bar,// Номер бара
 bool set // Направление индексирования массивов.
 )
// StdDevSeries(begin, prev_calculated, rates_total, period, deviation, Series, MovSeries, bar, set) 
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+
//---- Проверка начала достоверного отсчёта баров 
   if(BarCheck1(begin,bar,set)) return(EMPTY_VALUE);

//----+ Объявление локальных переменных
   int iii,kkk;
   double StdDev,m_SumX2;

//----+ Изменение размеров массива переменных
   if(bar==begin && !SeriesArrayResize(__FUNCTION__,Length,m_SeriesArray,m_Size_))
      return(EMPTY_VALUE);

//----+ проверка внешего параметра Length на корректность
   LengthCheck(Length);

//----+ перестановка и инициализация ячеек массива m_SeriesArray
   Recount_ArrayZeroPos(m_count,Length,prev_calculated,rates_total,series,bar,m_SeriesArray,set);

//----+ Инициализация нуля
   if(BarCheck2(begin,bar,set,Length))
     {
      m_Sum=0.0;
      m_Sum2=0.0;
      for(iii=1; iii<Length; iii++)
        {
         kkk=Recount_ArrayNumber(m_count,Length,iii);
         m_Sum+=m_SeriesArray[kkk];
         m_Sum2+=MathPow(m_SeriesArray[kkk],2);
        }
     }
   else if(BarCheck3(begin,bar,set,Length)) return(EMPTY_VALUE);

//----+ Вычисление StdDev
   m_Sum+=series;
   m_Sum2 += MathPow(series, 2);
   m_SumX2 = Length * MathPow(MovSeries, 2) - 2 * MovSeries * m_Sum + m_Sum2;

   kkk=Recount_ArrayNumber(m_count,Length,Length-1);
   m_Sum2-=MathPow(m_SeriesArray[kkk],2);
   m_Sum -=m_SeriesArray[kkk];

   StdDev=deviation*MathSqrt(m_SumX2/Length);

//----+ Восстановление значений переменных
   if(BarCheck5(rates_total,bar,set))
     {
      m_Sum=m_SUM;
      m_Sum2=m_SUM2;
     }

//----+ Сохранение значений переменных
   if(BarCheck4(rates_total,bar,set))
     {
      m_SUM=m_Sum;
      m_SUM2=m_Sum2;
     }
//----+
   return(StdDev);
  }
//+X================================================================X+
//| JMA усреднение                                                   |
//+X================================================================X+
double CJJMA::JJMASeries
(
 uint begin,// номер начала достоверного отсчёта баров
 uint prev_calculated,// Количество истории в барах на предыдущем тике
 uint rates_total,// Количество истории в барах на текущем тике
 int  Din,// разрешение изменять параметры Length и Phase на каждом баре. 
 // 0 - запрет изменения параметров,  любое другое значение - разрешение.
 double Phase,// параметр, изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса усреднения
 double Length,// глубина сглаживания
 double series,// Значение ценового ряда, расчитанное для бара с номером bar
 uint bar,// Номер бара
 bool set // Направление индексирования массивов.
 )
// JMASeries(begin, prev_calculated, rates_total, Din, Phase, Length, series, bar, set) 
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+
//---- Проверка начала достоверного отсчёта баров 
   if(BarCheck1(begin,bar,set)) return(EMPTY_VALUE);

//----+ Инициализация коэффициентов
   JJMAInit(begin,Din,Phase,Length,series,bar);

//----+ Объявление локальных переменных
   int posA,posB,back;
   int shift2,shift1,numb;
//----
   double Res,ResPow;
   double dser3,dser4,jjma;
   double ratio,Extr,ser0,resalt;
   double newvel,dSupr,Pow1,hoop1,SmVel;
   double Pow2,Pow2x2,Suprem1,Suprem2;
   double dser1,dser2,extent=0,factor;

//----+
   if(m_Loop1<61)
     {
      m_Loop1++;
      m_array[m_Loop1]=series;
     }
//-x-x-x-x-x-x-x-+  <<< Расчёт функции JMASeries() >>> 
   if(m_Loop1>30)
     {
      if(!m_start)
        {
         m_start= true;
         shift1 = 1;
         back=29;
         //----
         m_ser2 = m_array[1];
         m_ser1 = m_ser2;
        }
      else back=0;
      //-S-S-S-S-+
      for(int rrr=back; rrr>=0; rrr--)
        {
         if(rrr==0)
            ser0=series;
         else ser0=m_array[31-rrr];
         //----
         dser1 = ser0 - m_ser1;
         dser2 = ser0 - m_ser2;
         //----
         if(MathAbs(dser1)>MathAbs(dser2))
            m_var2=MathAbs(dser1);
         else m_var2=MathAbs(dser2);
         //----
         Res=m_var2;
         newvel=Res+0.0000000001;

         if(m_count1<=1)
            m_count1=127;
         else m_count1--;
         //----
         if(m_count2<=1)
            m_count2=10;
         else m_count2--;
         //----
         if(m_count3<128) m_count3++;
         //----      
         m_Sum1+=newvel-m_hoop2[m_count2];
         //----
         m_hoop2[m_count2]=newvel;
         m_bhoop2[m_count2]=true;
         //----
         if(m_count3>10)
            SmVel=m_Sum1/10.0;
         else SmVel=m_Sum1/m_count3;
         //----
         if(m_count3>127)
           {
            hoop1=m_hoop1[m_count1];
            m_hoop1[m_count1]=SmVel;
            m_bhoop1[m_count1]=true;
            numb = 64;
            posB = numb;
            //----
            while(numb>1)
              {
               if(m_data[posB]<hoop1)
                 {
                  numb /= 2.0;
                  posB += numb;
                 }
               else
                  if(m_data[posB]<=hoop1) numb=1;
               else
                 {
                  numb /= 2.0;
                  posB -= numb;
                 }
              }
           }
         else
           {
            m_hoop1[m_count1]=SmVel;
            m_bhoop1[m_count1]=true;
            //----
            if(m_midd1+m_midd2>127)
              {
               m_midd2--;
               posB=m_midd2;
              }
            else
              {
               m_midd1++;
               posB=m_midd1;
              }
            //----
            if(m_midd1>96)
               m_pos2=96;
            else m_pos2=m_midd1;
            //----
            if(m_midd2<32)
               m_pos1=32;
            else m_pos1=m_midd2;
           }
         //----
         numb = 64;
         posA = numb;
         //----
         while(numb>1)
           {
            if(m_data[posA]>=SmVel)
              {
               if(m_data[posA-1]<=SmVel) numb=1;
               else
                 {
                  numb /= 2.0;
                  posA -= numb;
                 }
              }
            else
              {
               numb /= 2.0;
               posA += numb;
              }
            //----
            if(posA==127)
               if(SmVel>m_data[127]) posA=128;
           }
         //----
         if(m_count3>127)
           {
            if(posB>=posA)
              {
               if(m_pos2+1>posA)
                  if(m_pos1-1<posA) m_Sum2+=SmVel;
               //----     
               else if(m_pos1+0>posA)
               if(m_pos1-1<posB)
                  m_Sum2+=m_data[m_pos1-1];
              }
            else
            if(m_pos1>=posA)
              {
               if(m_pos2+1<posA)
                  if(m_pos2+1>posB)
                     m_Sum2+=m_data[m_pos2+1];
              }
            else if(m_pos2+2>posA) m_Sum2+=SmVel;
            //----           
            else if(m_pos2+1<posA)
            if(m_pos2+1>posB)
               m_Sum2+=m_data[m_pos2+1];
            //----    
            if(posB>posA)
              {
               if(m_pos1-1<posB)
                  if(m_pos2+1>posB)
                     m_Sum2-=m_data[posB];
               //----
               else if(m_pos2<posB)
               if(m_pos2+1>posA)
                  m_Sum2-=m_data[m_pos2];
              }
            else
              {
               if(m_pos2+1>posB && m_pos1-1<posB)
                  m_Sum2-=m_data[posB];
               //----                
               else if(m_pos1+0>posB)
               if(m_pos1-0<posA)
                  m_Sum2-=m_data[m_pos1];
              }
           }
         //----
         if(posB<=posA)
           {
            if(posB==posA)
              {
               m_data[posA]=SmVel;
               m_bdata[posA]=true;
              }
            else
              {
               for(numb=posB+1; numb<=posA-1; numb++)
                 {
                  m_data[numb-1]=m_data[numb];
                  m_bdata[numb-1]=true;
                 }
               //----             
               m_data[posA-1]=SmVel;
               m_bdata[posA-1]=true;
              }
           }
         else
           {
            for(numb=posB-1; numb>=posA; numb--)
              {
               m_data[numb+1]=m_data[numb];
               m_bdata[numb+1]=true;
              }
            //----                
            m_data[posA]=SmVel;
            m_bdata[posA]=true;
           }
         //---- 
         if(m_count3<=127)
           {
            m_Sum2=0;
            for(numb=m_pos1; numb<=m_pos2; numb++)
               m_Sum2+=m_data[numb];
           }
         //---- 
         resalt=m_Sum2/(m_pos2-m_pos1+1.0);
         //----
         if(m_Loop2>30)
            m_Loop2=31;
         else m_Loop2++;
         //----
         if(m_Loop2<=30)
           {
            if(dser1>0.0)
               m_ser1=ser0;
            else m_ser1=ser0-dser1*m_Kct;
            //----
            if(dser2<0.0)
               m_ser2=ser0;
            else m_ser2=ser0-dser2*m_Kct;
            //----
            m_JMA=series;
            //----
            if(m_Loop2!=30) continue;
            else
              {
               m_storage1=series;
               if(MathCeil(m_Krx)>=1)
                  dSupr=MathCeil(m_Krx);
               else dSupr=1.0;
               //----
               if(dSupr>0) Suprem2=MathFloor(dSupr);
               else
                 {
                  if(dSupr<0)
                     Suprem2=MathCeil(dSupr);
                  else Suprem2=0.0;
                 }
               //----
               if(MathFloor(m_Krx)>=1)
                  m_var2=MathFloor(m_Krx);
               else m_var2=1.0;
               //----
               if(m_var2>0) Suprem1=MathFloor(m_var2);
               else
                 {
                  if(m_var2<0)
                     Suprem1=MathCeil(m_var2);
                  else Suprem1=0.0;
                 }
               //----
               if(Suprem2==Suprem1) factor=1.0;
               else
                 {
                  dSupr=Suprem2-Suprem1;
                  factor=(m_Krx-Suprem1)/dSupr;
                 }
               //---- 
               if(Suprem1<=29)
                  shift1=(int)Suprem1;
               else shift1=29;
               //----
               if(Suprem2<=29)
                  shift2=(int)Suprem2;
               else shift2=29;

               dser3 = series - m_array[m_Loop1 - shift1];
               dser4 = series - m_array[m_Loop1 - shift2];
               //----
               m_djma=dser3 *(1.0-factor)/Suprem1+dser4*factor/Suprem2;
              }
           }
         else
           {
            if(resalt) ResPow=MathPow(Res/resalt,m_degree);
            else ResPow=0.0;
            //----
            if(m_Kfd>=ResPow)
               m_var1= ResPow;
            else m_var1=m_Kfd;
            //----
            if(m_var1<1.0)m_var2=1.0;
            else
              {
               if(m_Kfd>=ResPow)
                  m_sense=ResPow;
               else m_sense=m_Kfd;

               m_var2=m_sense;
              }
            //---- 
            extent=m_var2;
            Pow1=MathPow(m_Kct,MathSqrt(extent));
            //----
            if(dser1>0.0)
               m_ser1=ser0;
            else m_ser1=ser0-dser1*Pow1;
            //----
            if(dser2<0.0)
               m_ser2=ser0;
            else m_ser2=ser0-dser2*Pow1;
           }
        }
      //---- 
      if(m_Loop2>30)
        {
         Pow2=MathPow(m_Krj,extent);
         //----
         m_storage1 *= Pow2;
         m_storage1 += (1.0 - Pow2) * series;
         m_storage2 *= m_Krj;
         m_storage2 += (series - m_storage1) * (1.0 - m_Krj);
         //----
         Extr=m_Phase*m_storage2+m_storage1;
         //----
         Pow2x2= Pow2 * Pow2;
         ratio = Pow2x2-2.0 * Pow2+1.0;
         m_djma *= Pow2x2;
         m_djma += (Extr - m_JMA) * ratio;
         //----
         m_JMA+=m_djma;
        }
     }
//-x-x-x-x-x-x-x-+

   if(m_Loop1<=30) return(EMPTY_VALUE);
   jjma=m_JMA;

//----+ Восстановление значений переменных
   if(BarCheck5(rates_total,bar,set))
     {
      //---- Восстановление изменённых ячеек массивов из пямяти
      for(numb = 0; numb < 128; numb++) if(m_bhoop1[numb]) m_hoop1[numb] = m_hoop1_[numb];
      for(numb = 0; numb < 11;  numb++) if(m_bhoop2[numb]) m_hoop2[numb] = m_hoop2_[numb];
      for(numb = 0; numb < 128; numb++) if(m_bdata [numb]) m_data [numb] = m_data_ [numb];

      //---- Обнуление номеров изменённых ячеек массивов
      ArrayInitialize(m_bhoop1,false);
      ArrayInitialize(m_bhoop2,false);
      ArrayInitialize(m_bdata,false);

      //---- запись значений переменных из пямяти 
      m_JMA=m_JMA_;
      m_djma = m_djma_;
      m_ser1 = m_ser1_;
      m_ser2 = m_ser2_;
      m_Sum2 = m_Sum2_;
      m_pos1 = m_pos1_;
      m_pos2 = m_pos2_;
      m_Sum1  = m_Sum1_;
      m_Loop1 = m_Loop1_;
      m_Loop2 = m_Loop2_;
      m_count1 = m_count1_;
      m_count2 = m_count2_;
      m_count3 = m_count3_;
      m_storage1 = m_storage1_;
      m_storage2 = m_storage2_;
      m_midd1 = m_midd1_;
      m_midd2 = m_midd2_;
     }

//----+ Сохранение значений переменных
   if(BarCheck4(rates_total,bar,set))
     {
      //---- запись изменённых ячеек массивов в пямять
      for(numb = 0; numb < 128; numb++) if(m_bhoop1[numb]) m_hoop1_[numb] = m_hoop1[numb];
      for(numb = 0; numb < 11;  numb++) if(m_bhoop2[numb]) m_hoop2_[numb] = m_hoop2[numb];
      for(numb = 0; numb < 128; numb++) if(m_bdata [numb]) m_data_ [numb] = m_data [numb];

      //---- Обнуление номеров изменённых ячеек массивов
      ArrayInitialize(m_bhoop1,false);
      ArrayInitialize(m_bhoop2,false);
      ArrayInitialize(m_bdata,false);

      //---- запись значений переменных в пямять
      m_JMA_=m_JMA;
      m_djma_ = m_djma;
      m_Sum2_ = m_Sum2;
      m_ser1_ = m_ser1;
      m_ser2_ = m_ser2;
      m_pos1_ = m_pos1;
      m_pos2_ = m_pos2;
      m_Sum1_  = m_Sum1;
      m_Loop1_ = m_Loop1;
      m_Loop2_ = m_Loop2;
      m_count1_ = m_count1;
      m_count2_ = m_count2;
      m_count3_ = m_count3;
      m_storage1_ = m_storage1;
      m_storage2_ = m_storage2;
      m_midd1_ = m_midd1;
      m_midd2_ = m_midd2;
     }

//----+  Завершение вычислений функции JMASeries()
   return(jjma);
  }
//+X================================================================X+
//|  Инициализация переменных алгоритма JMA                          |
//+X================================================================X+    
void CJJMA::JJMAInit
(
 uint begin,
 int  Din,
 double Phase,
 double Length,
 double series,
 uint bar
 )
// JMAInit(begin, Din, Phase, Length, series, bar) 
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+  
//----+ <<< Расчёт коэффициентов  >>> 
   if(bar==begin || Din!=0)
     {
      if(bar==begin)
        {
         m_midd1 = 63;
         m_midd2 = 64;
         m_start = false;

         //----
         for(int numb = 0;       numb <= m_midd1; numb++) m_data[numb] = -1000000.0;
         for(int numb = m_midd2; numb <= 127;     numb++) m_data[numb] = +1000000.0;

         //----+ Все ячейки массивов должны быть переписаны
         ArrayInitialize(m_bhoop1,true);
         ArrayInitialize(m_bhoop2,true);
         ArrayInitialize(m_bdata,true);

         //----+ Удаление мусора из массивов при повторных инициализациях 
         ArrayInitialize(m_hoop1_, 0.0);
         ArrayInitialize(m_hoop2_, 0.0);
         ArrayInitialize(m_hoop1,  0.0);
         ArrayInitialize(m_hoop2,  0.0);
         ArrayInitialize(m_array,  0.0);
         //----
         m_djma = 0.0;
         m_Sum1 = 0.0;
         m_Sum2 = 0.0;
         m_ser1 = 0.0;
         m_ser2 = 0.0;
         m_pos1 = 0.0;
         m_pos2 = 0.0;
         m_Loop1 = 0.0;
         m_Loop2 = 0.0;
         m_count1 = 0.0;
         m_count2 = 0.0;
         m_count3 = 0.0;
         m_storage1 = 0.0;
         m_storage2 = 0.0;
         m_JMA=series;
        }

      if(Phase>=-100 && Phase<=100)
         m_Phase=Phase/100.0+1.5;
      //----        
      if(Phase > +100) m_Phase = 2.5;
      if(Phase < -100) m_Phase = 0.5;
      //----
      double velA,velB,velC,velD;
      //----
      if(Length>=1.0000000002)
         velA=(Length-1.0)/2.0;
      else velA=0.0000000001;
      //----
      velA *= 0.9;
      m_Krj = velA / (velA + 2.0);
      velC = MathSqrt(velA);
      velD = MathLog(velC);
      m_var1= velD;
      m_var2= m_var1;
      //----
      velB=MathLog(2.0);
      m_sense=(m_var2/velB)+2.0;
      if(m_sense<0.0) m_sense=0.0;
      m_Kfd=m_sense;
      //----
      if(m_Kfd>=2.5)
         m_degree=m_Kfd-2.0;
      else m_degree=0.5;
      //----
      m_Krx = velC * m_Kfd;
      m_Kct = m_Krx / (m_Krx + 1.0);
     }
//----+
  }
//+X================================================================X+
//| Проверка глубины усреднения Lengt на корректность                |
//+X================================================================X+
void CJJMA::JJMALengthCheck(string LengthName,int ExternLength)

// Jm_JMALengthCheck(LengthName, ExternLength)
  {
//----+
//---- сброс сообщений при недопустимых значениях входных параметров  
   if(ExternLength<1)
     {
      string word;
      StringConcatenate
      (word,__FUNCTION__," (): Параметр ",LengthName,
       " должен быть не менее 1. Вы ввели недопустимое ",
       ExternLength," будет использовано  1");
      Print(word);
      return;
     }
//----+
  }
//+X================================================================X+
//| Проверка параметра усреднения Phase на корректность              |
//+X================================================================X+
void CJJMA::JJMAPhaseCheck(string PhaseName,int ExternPhase)

// Jm_JMAPhaseCheck(PhaseName, ExternPhase)
  {
//----+
//---- сброс сообщений при недопустимых значениях входных параметров 
   if(ExternPhase<-100)
     {
      string word;
      StringConcatenate
      (word,__FUNCTION__," (): Параметр ",PhaseName,
       " должен быть не менее -100. Вы ввели недопустимое ",
       ExternPhase," будет использовано  -100");
      Print(word);
      return;
     }
//----
   if(ExternPhase>+100)
     {
      string word;
      StringConcatenate
      (word,__FUNCTION__," (): Параметр ",PhaseName,
       " должен быть не более +100. Вы ввели недопустимое ",
       ExternPhase," будет использовано  +100");
      Print(word);
      return;
     }
//----+
  }
//+X================================================================X+
//|  T3 усреднение                                                   |
//+X================================================================X+
double CT3::T3Series
(
 uint begin,// номер начала достоверного отсчёта баров
 uint prev_calculated,// Количество истории в барах на предыдущем тике
 uint rates_total,// Количество истории в барах на текущем тике
 int  Din,// разрешение изменять параметр Length на каждом баре. 
 // 0 - запрет изменения параметров,  любое другое значение - разрешение.
 double Curvature,// Коэффициент (для удобства его величина измерения увеличина в сто раз!)
 double Length,// глубина сглаживания
 double series,// Значение ценового ряда, расчитанное для бара с номером bar
 uint bar,// Номер бара
 bool set // Направление индексирования массивов.
 )
// T3Series(begin, prev_calculated, rates_total, Din, Curvature, Length, series, bar, set) 
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+
//---- Проверка начала достоверного отсчёта баров 
   if(BarCheck1(begin,bar,set)) return(EMPTY_VALUE);

//----+ Объявление локальных переменных
   double e0,T3_;

//----+ проверка внешего параметра Length на корректность
   LengthCheck(Length);

//----+ Расчёт коэффициентов
   T3Init(begin,Din,Curvature,Length,series,bar);

   e0=series;
//----+ <<< вычисление T3 >>> 
   m_e1 = m_w1 * e0 + m_w2 * m_e1;
   m_e2 = m_w1 * m_e1 + m_w2 * m_e2;
   m_e3 = m_w1 * m_e2 + m_w2 * m_e3;
   m_e4 = m_w1 * m_e3 + m_w2 * m_e4;
   m_e5 = m_w1 * m_e4 + m_w2 * m_e5;
   m_e6 = m_w1 * m_e5 + m_w2 * m_e6;
//----  
   T3_=m_c1*m_e6+m_c2*m_e5+m_c3*m_e4+m_c4*m_e3;

//----+ Восстановление значений переменных
   if(BarCheck5(rates_total,bar,set))
     {
      m_e1 = m_E1;
      m_e2 = m_E2;
      m_e3 = m_E3;
      m_e4 = m_E4;
      m_e5 = m_E5;
      m_e6 = m_E6;
     }

//----+ Сохранение значений переменных
   if(BarCheck4(rates_total,bar,set))
     {

      m_E1 = m_e1;
      m_E2 = m_e2;
      m_E3 = m_e3;
      m_E4 = m_e4;
      m_E5 = m_e5;
      m_E6 = m_e6;
     }

//----+ завершение вычислений значения функции T3Series()
   return(T3_);
  }
//+X================================================================X+
//|  Инициализация переменных алгоритма T3                           |
//+X================================================================X+    
void CT3::T3Init
(
 uint begin,
 int Din,
 double Curvature,
 double Length,
 double series,
 uint bar
 )
// T3InitInit(begin, Din, Curvature, Length, series, bar) 
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+  
//----+ <<< Расчёт коэффициентов >>>
   if(bar==begin || Din!=0)
     {
      double b=Curvature/100.0;
      m_b2 = b * b;
      m_b3 = m_b2 * b;
      m_c1 = -m_b3;
      m_c2 = (3 * (m_b2 + m_b3));
      m_c3 = -3 * (2 * m_b2 + b + m_b3);
      m_c4 = (1 + 3 * b + m_b3 + 3 * m_b2);
      double n=1+0.5 *(Length-1);
      m_w1 = 2 / (n + 1);
      m_w2 = 1 - m_w1;

      if(bar==begin)
        {
         m_e1 = series;
         m_e2 = series;
         m_e3 = series;
         m_e4 = series;
         m_e5 = series;
         m_e6 = series;
        }
     }
//----+
  }
//+X================================================================X+
//| Ультралинейное усреднение                                        |
//+X================================================================X+    
double CJurX::JurXSeries
(
 uint begin,// номер начала достоверного отсчёта баров
 uint prev_calculated,// Количество истории в барах на предыдущем тике
 uint rates_total,// Количество истории в барах на текущем тике
 int  Din,// разрешение изменять параметр Length на каждом баре 
 // 0 - запрет изменения параметров,  любое другое значение - разрешение
 double Length,// глубина сглаживания
 double series,// Значение ценового ряда, расчитанное для бара с номером bar
 uint bar,// Номер бара
 bool set // Направление индексирования массивов.
 )
// JurXSeries(begin, prev_calculated, rates_total, Din, Length, series, bar, set) 
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+
//---- Проверка начала достоверного отсчёта баров 
   if(BarCheck1(begin,bar,set)) return(EMPTY_VALUE);

//----+ Объявление локальных переменных
   double V1,V2,JurX_;

//----+ проверка внешего параметра Length на корректность
   LengthCheck(Length);

//----+ Инициализация коэффициентов
   JurXInit(begin,Din,Length,series,bar);

//---- <<< вычисление JurX >>> 
   m_f1   =  m_Hg * m_f1 + m_Kg * series;
   m_f2   =  m_Kg * m_f1 + m_Hg * m_f2;
   V1     =  m_AC * m_f1 - m_AB * m_f2;
   m_f3   =  m_Hg * m_f3 + m_Kg * V1;
   m_f4   =  m_Kg * m_f3 + m_Hg * m_f4;
   V2     =  m_AC * m_f3 - m_AB * m_f4;
   m_f5   =  m_Hg * m_f5 + m_Kg * V2;
   m_f6   =  m_Kg * m_f5 + m_Hg * m_f6;
   JurX_  =  m_AC * m_f5 - m_AB * m_f6;

//----+ Восстановление значений переменных
   if(BarCheck5(rates_total,bar,set))
     {
      m_f1 = m_F1;
      m_f2 = m_F2;
      m_f3 = m_F3;
      m_f4 = m_F4;
      m_f5 = m_F5;
      m_f6 = m_F6;
     }

//----+ Сохранение значений переменных
   if(BarCheck4(rates_total,bar,set))
     {
      m_F1 = m_f1;
      m_F2 = m_f2;
      m_F3 = m_f3;
      m_F4 = m_f4;
      m_F5 = m_f5;
      m_F6 = m_f6;
     }

//----+ завершение вычислений значения функции JurX.Series
   return(JurX_);

  }
//+X================================================================X+
//|  Инициализация переменных алгоритма JurX                         |
//+X================================================================X+    
void CJurX::JurXInit
(
 uint begin,
 int Din,
 double Length,
 double series,
 uint bar
 )
// JurXInit(begin, Din, Length, series, bar) 
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+  
   if(bar==begin || Din!=0)
     {
      if(Length>=6)
         m_w=Length-1;
      else m_w=5;

      m_Kg = 3 / (Length + 2.0);
      m_Hg = 1.0 - m_Kg;
      //----
      if(bar==begin)
        {
         m_f1 = series;
         m_f2 = series;
         m_f3 = series;
         m_f4 = series;
         m_f5 = series;
         m_f6 = series;

         m_AB = 0.5;
         m_AC = 1.5;
        }
     }
//----+
  }
//+X================================================================X+
//| Параболлическое усреднение                                       |
//+X================================================================X+    
double CParMA::ParMASeries
(
 uint begin,// Номер начала достоверного отсчёта баров
 uint prev_calculated,// Количество истории в барах на предыдущем тике
 uint rates_total,// Количество истории в барах на текущем тике
 int Length,// Период усреднения
 double series,// Значение ценового ряда, расчитанное для бара с номером bar
 uint bar,// Номер бара
 bool set // Направление индексирования массивов
 )
// SMASeries(Number, symbol, timeframe, begin, limit, Length, series, bar, set) 
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+
//---- Проверка начала достоверного отсчёта баров 
   if(BarCheck1(begin,bar,set)) return(EMPTY_VALUE);

//----+ Объявление локальных переменных
   int iii,kkk;
//----
   double S,B0,B1,B2,parma;
   double A,B,C,D,E,F;
   double K,L,M,P,Q,R;
   double sum_y,sum_xy,sum_x2y,var_tmp;

//----+ Изменение размеров массива переменных
   if(bar==begin && !SeriesArrayResize(__FUNCTION__,Length,m_SeriesArray,m_Size_))
      return(EMPTY_VALUE);

//----+ проверка внешего параметра Length на корректность
   LengthCheck(Length);

//----+ перестановка и инициализация ячеек массива m_SeriesArray
   Recount_ArrayZeroPos(m_count,Length,prev_calculated,rates_total,series,bar,m_SeriesArray,set);

//----+ Инициализация нуля
   if(BarCheck2(begin,bar,set,Length)) ParMAInit(Length);
   else if(BarCheck3(begin,bar,set,Length)) return(EMPTY_VALUE);

//----+ Вычисление ParMA
   sum_y   = 0.0;
   sum_xy  = 0.0;
   sum_x2y = 0.0;
//----
   for(iii=1; iii<=Length; iii++)
     {
      kkk=Recount_ArrayNumber(m_count,Length,Length-iii);
      var_tmp  = m_SeriesArray[kkk];
      sum_y   += var_tmp;
      sum_xy  += iii * var_tmp;
      sum_x2y += iii * iii * var_tmp;
     }

// разница между двумя ближайшими барами для sum_x2y: Sum(i=0; i<Length){(2*Length* - 1)*Series[i] + 2*i*Series[i]}

// initialization
   A = Length;
   B = m_sum_x;
   C = m_sum_x2;
   F = m_sum_x3;
   M = m_sum_x4;
   P = sum_y;
   R = sum_xy;
   S = sum_x2y;
// intermediates
   D = B;
   E = C;
   K = C;
   L = F;
   Q = D / A;
   E = E - Q * B;
   F = F - Q * C;
   R = R - Q * P;
   Q = K / A;
   L = L - Q * B;
   M = M - Q * C;
   S = S - Q * P;
   Q = L / E;
// calculate regression coefficients
   B2 = (S - R * Q) / (M - F * Q);
   B1 = (R - F * B2) / E;
   B0 = (P - B * B1 - C * B2) / A;
// value to be returned - parabolic MA
   parma=B0+(B1+B2*A)*A;
//----+
   return(parma);
  }
//+X================================================================X+
//|  Инициализация переменных алгоритма параболлического усреднения  |
//+X================================================================X+    
void CParMA::ParMAInit(double Length)
// ParMAInit(Length)
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+
   int var_tmp;
   m_sum_x=0;
   m_sum_x2 = 0;
   m_sum_x3 = 0;
   m_sum_x4 = 0;

   for(int iii=1; iii<=Length; iii++)
     {
      var_tmp=iii;
      m_sum_x+=var_tmp;
      var_tmp *= iii;
      m_sum_x2+= var_tmp;
      var_tmp *= iii;
      m_sum_x3+= var_tmp;
      var_tmp *= iii;
      m_sum_x4+= var_tmp;
     }
//----+
  }
//+X================================================================X+
//|  CMOSeries() function                                            |
//+X================================================================X+    
double CCMO::CMOSeries
(
 uint begin,// номер начала достоверного отсчёта баров
 uint prev_calculated,// Количество истории в барах на предыдущем тике
 uint rates_total,// Количество истории в барах на текущем тике
 int CMO_Length, // CMO период
 double series,  // Значение ценового ряда, расчитанное для бара с номером bar
 uint bar,// Номер бара
 bool set // Направление индексирования массивов.
 )
// CMOSeries(begin, prev_calculated, rates_total, CMO_Length, series, bar, set) 
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+
//---- Проверка начала достоверного отсчёта баров 
   if(BarCheck1(begin,bar,set)) return(EMPTY_VALUE);

//----+ Объявление локальных переменных;
   double dseries,abcmo;
   int iii,rrr,size=CMO_Length+1;

//----+ Изменение размеров массива переменных
   if(bar==begin && !SeriesArrayResize(__FUNCTION__,size,m_dSeriesArray,m_Size_))
      return(EMPTY_VALUE);

//----+ проверка внешего параметра CMO_Length на корректность
   LengthCheck(CMO_Length);

//---- Проверка начала достаточности баров 
   if(BarCheck1(begin+1,bar,set)) return(EMPTY_VALUE);

//----+ перестановка ячеек массива SeriesArray
   Recount_ArrayZeroPos(m_count,size,prev_calculated,rates_total,series-m_series1,bar,m_dSeriesArray,set);

//----+ Инициализация нуля
   if(BarCheck2(begin,bar,set,CMO_Length+3))
     {
      m_UpSum = 0.0;
      m_DnSum = 0.0;

      for(iii=1; iii<CMO_Length; iii++)
        {
         rrr=Recount_ArrayNumber(m_count,size,iii);
         dseries=m_dSeriesArray[rrr];

         if(dseries > 0) m_UpSum += dseries;
         if(dseries < 0) m_DnSum -= dseries;
        }

      m_AbsCMO=0.000000001;
     }
   else if(BarCheck3(begin,bar,set,CMO_Length+3))
     {
      m_series1=series;
      return(EMPTY_VALUE);
     }

   dseries=m_dSeriesArray[m_count];
   if(dseries > 0) m_UpSum += dseries;
   if(dseries < 0) m_DnSum -= dseries;
   if(m_UpSum+m_DnSum>0)
      m_AbsCMO=MathAbs((m_UpSum-m_DnSum)/(m_UpSum+m_DnSum));
   abcmo=m_AbsCMO;
//----
   rrr=Recount_ArrayNumber(m_count,size,CMO_Length-1);
   dseries=m_dSeriesArray[rrr];
   if(dseries > 0) m_UpSum -= dseries;
   if(dseries < 0) m_DnSum += dseries;

//----+ Восстановление значений переменных
   if(BarCheck5(rates_total,bar,set))
     {
      m_AbsCMO= m_AbsCMO_;
      m_UpSum = m_UpSum_;
      m_DnSum = m_DnSum_;
      m_series1=m_series1_;
     }
   else m_series1=series;

//----+ Сохранение значений переменных
   if(BarCheck4(rates_total,bar,set))
     {
      m_AbsCMO_=m_AbsCMO;
      m_UpSum_ = m_UpSum;
      m_DnSum_ = m_DnSum;
      m_series1_=m_series1;
     }
//----+
   return(abcmo);
  }
//+X================================================================X+
//|  VIDYASeries() function                                          |
//+X================================================================X+    
double CCMO::VIDYASeries
(
 uint begin,// номер начала достоверного отсчёта баров
 uint prev_calculated,// Количество истории в барах на предыдущем тике
 uint rates_total,// Количество истории в барах на текущем тике
 int CMO_Length,// CMO период
 double EMA_Length,// EMA период
 double series,// Значение ценового ряда, расчитанное для бара с номером bar
 uint bar,// Номер бара
 bool set // Направление индексирования массивов.
 )
// VIDYASeries(begin, prev_calculated, rates_total, CMO_Length, EMA_Length, series, bar, set) 
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+
//----+ Объявление локальных переменных
   double vidya,CMO_=CMOSeries(begin,prev_calculated,rates_total,CMO_Length,series,bar,set);

//----+ Инициализация нуля
   if(BarCheck2(begin,bar,set,CMO_Length+3))
     {
      m_Vidya=series;
      //---- Инициализация фактора сглаживания ЕМА
      m_SmoothFactor=2.0/(EMA_Length+1.0);
     }
   else if(BarCheck3(begin,bar,set,CMO_Length+3)) return(EMPTY_VALUE);

//----
   CMO_*=m_SmoothFactor;
   m_Vidya=CMO_*series+(1-CMO_)*m_Vidya;
   vidya=m_Vidya;

//----+ Восстановление значений переменных
   if(BarCheck5(rates_total,bar,set))
     {
      m_Vidya=m_Vidya_;
     }

//----+ Сохранение значений переменных
   if(BarCheck4(rates_total,bar,set))
     {
      m_Vidya_=m_Vidya;
     }
//----+
   return(vidya);
  }
//+X================================================================X+
//|  Усреднение Кауфмана                                             |
//+X================================================================X+    
double CAMA::AMASeries
(
 uint begin,// номер начала достоверного отсчёта баров
 uint prev_calculated,// Количество истории в барах на предыдущем тике
 uint rates_total,// Количество истории в барах на текущем тике
 int Length,// период AMA
 int Fast_Length, // период быстрой скользящей
 int Slow_Length, // период медленной скользящей
 double Rate,// степень, в которую возводится сглаживающая константа
 double series,// Значение ценового ряда, расчитанное для бара с номером bar
 uint bar,// Номер бара
 bool set // Направление индексирования массивов.
 )
// AMASeries(symbol, timeframe, begin, limit, Length, series, bar, set) 
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+
//---- Проверка начала достоверного отсчёта баров 
   if(BarCheck1(begin,bar,set)) return(EMPTY_VALUE);

//----+ Объявление локальных переменных
   double signal,ER,ERSC,SSC,dprice,ama;
   int iii,kkk,rrr,size=Length+1;

//----+ Изменение размеров массивов переменных
   if(bar==begin)
      if(!SeriesArrayResize(__FUNCTION__,size,m_SeriesArray,m_Size_1)
         || !SeriesArrayResize(__FUNCTION__,size,m_dSeriesArray,m_Size_2))
         return(EMPTY_VALUE);

//----+ проверка внешего параметра Length на корректность
   LengthCheck(Length);

//----+ перестановка и инициализация ячеек массива m_SeriesArray
   Recount_ArrayZeroPos(m_count,size,prev_calculated,rates_total,series,bar,m_SeriesArray,set);

//---- Проверка начала достаточности баров 
   if(BarCheck1(begin+1,bar,set)) return(EMPTY_VALUE);

   kkk=Recount_ArrayNumber(m_count,size,1);
   dprice=series-m_SeriesArray[kkk];
   m_dSeriesArray[m_count]=dprice;

//----+ Инициализация нуля
   if(BarCheck2(begin,bar,set,Length+3))
     {
      //---- инициализация констант
      rrr=Recount_ArrayNumber(m_count,size,1);
      m_Ama=m_SeriesArray[rrr];
      m_slowSC = (2.0 / (Slow_Length + 1));
      m_fastSC = (2.0 / (Fast_Length + 1));
      m_dSC=m_fastSC-m_slowSC;
      m_noise=0.000000001;

      for(iii=1; iii<Length; iii++)
        {
         rrr=Recount_ArrayNumber(m_count,size,iii);
         m_noise+=MathAbs(m_dSeriesArray[rrr]);
        }
     }
   else if(BarCheck3(begin,bar,set,Length+3)) return(EMPTY_VALUE);

//----
   m_noise+=MathAbs(dprice);
   rrr=Recount_ArrayNumber(m_count,size,Length);
   signal=MathAbs(series-m_SeriesArray[rrr]);
//----                     
   ER=signal/m_noise;
   ERSC= ER *  m_dSC;
   SSC = ERSC+m_slowSC;
   m_Ama=m_Ama+(MathPow(SSC,Rate) *(series-m_Ama));
   ama =  m_Ama;
   kkk = Recount_ArrayNumber( m_count, size, Length - 1);
   m_noise-=MathAbs(m_dSeriesArray[kkk]);

//----+ Восстановление значений переменных
   if(BarCheck5(rates_total,bar,set))
     {
      m_noise=m_NOISE;
      m_Ama=m_AMA_;
     }

//----+ Сохранение значений переменных
   if(BarCheck4(rates_total,bar,set))
     {
      m_AMA_=m_Ama;
      m_NOISE=m_noise;
     }
//----+
   return(ama);
  }
//+X================================================================X+
//|  Темп изменения цен                                              |
//+X================================================================X+    
double CMomentum::MomentumSeries
(
 uint begin,// Номер начала достоверного отсчёта баров
 uint prev_calculated,// Количество истории в барах на предыдущем тике
 uint rates_total,// Количество истории в барах на текущем тике
 int Length,// Период усреднения
 double series,// Значение ценового ряда, расчитанное для бара с номером bar
 uint bar,// Номер бара
 bool set // Направление индексирования массивов
 )
// MomentumSeries(begin, prev_calculated, rates_total, Length, series, bar, set) 
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+ Проверка начала достоверного отсчёта баров 
   if(BarCheck1(begin,bar,set)) return(EMPTY_VALUE);

//----+ Объявление локальных переменных
   int kkk,Length_=Length+1;
   double Momentum;

//----+ проверка внешего параметра Length на корректность
   LengthCheck(Length);

//----+ Изменение размеров массива переменных
   if(bar==begin && !SeriesArrayResize(__FUNCTION__,Length_,m_SeriesArray,m_Size_))
      return(EMPTY_VALUE);

//----+ перестановка и инициализация ячеек массива m_SeriesArray
   Recount_ArrayZeroPos(m_count,Length_,prev_calculated,rates_total,series,bar,m_SeriesArray,set);

//----+ Инициализация нуля
   if(BarCheck3(begin,bar,set,Length_)) return(EMPTY_VALUE);

//----+ Вычисление темпа изменения цен
   kkk=Recount_ArrayNumber(m_count,Length_,Length);
   Momentum=series-m_SeriesArray[kkk];
//----+
   return(Momentum);
  }
//+X================================================================X+
//|  Нормированный Темп изменения цен                                |
//+X================================================================X+    
double CnMomentum::nMomentumSeries
(
 uint begin,// Номер начала достоверного отсчёта баров
 uint prev_calculated,// Количество истории в барах на предыдущем тике
 uint rates_total,// Количество истории в барах на текущем тике
 int Length,// Период усреднения
 double series,// Значение ценового ряда, расчитанное для бара с номером bar
 uint bar,// Номер бара
 bool set // Направление индексирования массивов
 )
// nMomentumSeries(begin, prev_calculated, rates_total, Length, series, bar, set) 
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+ Проверка начала достоверного отсчёта баров 
   if(BarCheck1(begin,bar,set)) return(EMPTY_VALUE);

//----+ Объявление локальных переменных
   int kkk,Length_=Length+1;
   double nMomentum;

//----+ проверка внешего параметра Length на корректность
   LengthCheck(Length);

//----+ Изменение размеров массива переменных
   if(bar==begin && !SeriesArrayResize(__FUNCTION__,Length_,m_SeriesArray,m_Size_))
      return(EMPTY_VALUE);

//----+ перестановка и инициализация ячеек массива m_SeriesArray
   Recount_ArrayZeroPos(m_count,Length_,prev_calculated,rates_total,series,bar,m_SeriesArray,set);

//----+ Инициализация нуля
   if(BarCheck3(begin,bar,set,Length_)) return(EMPTY_VALUE);

//----+ Вычисление темпа изменения цен
   kkk=Recount_ArrayNumber(m_count,Length_,Length);
   nMomentum=(series-m_SeriesArray[kkk])/m_SeriesArray[kkk];
//----+
   return(nMomentum);
  }
//+X================================================================X+
//|  Темп изменения цен                                              |
//+X================================================================X+    
double CROC::ROCSeries
(
 uint begin,// Номер начала достоверного отсчёта баров
 uint prev_calculated,// Количество истории в барах на предыдущем тике
 uint rates_total,// Количество истории в барах на текущем тике
 int Length,// Период усреднения
 double series,// Значение ценового ряда, расчитанное для бара с номером bar
 uint bar,// Номер бара
 bool set // Направление индексирования массивов
 )
// MomentumSeries(begin, prev_calculated, rates_total, Length, series, bar, set) 
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+ Проверка начала достоверного отсчёта баров 
   if(BarCheck1(begin,bar,set)) return(EMPTY_VALUE);

//----+ Объявление локальных переменных
   int kkk,Length_=Length+1;
   double ROC;

//----+ проверка внешего параметра Length на корректность
   LengthCheck(Length);

//----+ Изменение размеров массива переменных
   if(bar==begin && !SeriesArrayResize(__FUNCTION__,Length_,m_SeriesArray,m_Size_))
      return(EMPTY_VALUE);

//----+ перестановка и инициализация ячеек массива m_SeriesArray
   Recount_ArrayZeroPos(m_count,Length_,prev_calculated,rates_total,series,bar,m_SeriesArray,set);

//----+ Инициализация нуля
   if(BarCheck3(begin,bar,set,Length_)) return(EMPTY_VALUE);

//----+ Вычисление темпа изменения цен
   kkk = Recount_ArrayNumber(m_count, Length_, Length);
   ROC = 100 * series / m_SeriesArray[kkk];
//----+
   return(ROC);
  }
//+X================================================================X+
//|  FATL усреднение                                                 |
//+X================================================================X+    
double CFATL::FATLSeries
(
 uint begin,// Номер начала достоверного отсчёта баров
 uint prev_calculated,// Количество истории в барах на предыдущем тике
 uint rates_total,// Количество истории в барах на текущем тике
 double series,// Значение ценового ряда, расчитанное для бара с номером bar
 uint bar,// Номер бара
 bool set // Направление индексирования массивов
 )
// FATLSeries(begin, prev_calculated, rates_total, Length, series, bar, set) 
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+ Проверка начала достоверного отсчёта баров 
   if(BarCheck1(begin,bar,set)) return(EMPTY_VALUE);

//----+ перестановка и инициализация ячеек массива m_SeriesArray
   Recount_ArrayZeroPos(m_count,m_Size_,prev_calculated,rates_total,series,bar,m_SeriesArray,set);

//----+ Инициализация нуля
   if(BarCheck3(begin,bar,set,m_Size_)) return(EMPTY_VALUE);

//----+ Вычисление FATL
   double FATL=0.0;
   if(BarCheck5(rates_total,bar,set))
     {
      if(prev_calculated!=rates_total)
        {
         m_FATL=0.0;
         for(int iii=1; iii<m_Size_; iii++)
            m_FATL+=m_FATLTable[iii]*m_SeriesArray[Recount_ArrayNumber(m_count,m_Size_,iii)];
        }
      FATL=m_FATL+m_FATLTable[0]*m_SeriesArray[Recount_ArrayNumber(m_count,m_Size_,0)];
     }
   else for(int iii=0; iii<m_Size_; iii++)
                    FATL+=m_FATLTable[iii]*m_SeriesArray[Recount_ArrayNumber(m_count,m_Size_,iii)];
//----+
   return(FATL);
  }
//+X================================================================X+   
//| Конструктор класса CFATL                                         |
//+X================================================================X+
CFATL::CFATL()
  {
//----+
   m_Size_=39;
//----                     
   double FATLTable[]=
     {
      +0.4360409450, +0.3658689069, +0.2460452079, +0.1104506886, -0.0054034585, -0.0760367731,
      -0.0933058722, -0.0670110374, -0.0190795053, +0.0259609206, +0.0502044896, +0.0477818607,
      +0.0249252327, -0.0047706151, -0.0272432537, -0.0338917071, -0.0244141482, -0.0055774838,
      +0.0128149838, +0.0226522218, +0.0208778257, +0.0100299086, -0.0036771622, -0.0136744850,
      -0.0160483392, -0.0108597376, -0.0016060704, +0.0069480557, +0.0110573605, +0.0095711419,
      +0.0040444064, -0.0023824623, -0.0067093714, -0.0072003400, -0.0047717710, +0.0005541115,
      +0.0007860160, +0.0130129076, +0.0040364019
     };

   ArrayCopy(m_FATLTable,FATLTable,0,0,WHOLE_ARRAY);
//----+ 
  }
//+X================================================================X+
//|  SATL усреднение                                                 |
//+X================================================================X+    
double CSATL::SATLSeries
(
 uint begin,// Номер начала достоверного отсчёта баров
 uint prev_calculated,// Количество истории в барах на предыдущем тике
 uint rates_total,// Количество истории в барах на текущем тике
 double series,// Значение ценового ряда, расчитанное для бара с номером bar
 uint bar,// Номер бара
 bool set // Направление индексирования массивов
 )
// FATLSeries(begin, prev_calculated, rates_total, Length, series, bar, set) 
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+ Проверка начала достоверного отсчёта баров 
   if(BarCheck1(begin,bar,set)) return(EMPTY_VALUE);

//----+ перестановка и инициализация ячеек массива m_SeriesArray
   Recount_ArrayZeroPos(m_count,m_Size_,prev_calculated,rates_total,series,bar,m_SeriesArray,set);

//----+ Инициализация нуля
   if(BarCheck3(begin,bar,set,m_Size_)) return(EMPTY_VALUE);

//----+ Вычисление FATL
   double SATL=0.0;
   if(BarCheck5(rates_total,bar,set))
     {
      if(prev_calculated!=rates_total)
        {
         m_SATL=0.0;
         for(int iii=1; iii<m_Size_; iii++)
            m_SATL+=m_SATLTable[iii]*m_SeriesArray[Recount_ArrayNumber(m_count,m_Size_,iii)];
        }
      SATL=m_SATL+m_SATLTable[0]*m_SeriesArray[Recount_ArrayNumber(m_count,m_Size_,0)];
     }
   else for(int iii=0; iii<m_Size_; iii++)
                    SATL+=m_SATLTable[iii]*m_SeriesArray[Recount_ArrayNumber(m_count,m_Size_,iii)];
//----+
   return(SATL);
  }
//+X================================================================X+   
//| Конструктор класса CSATL                                         |
//+X================================================================X+
CSATL::CSATL()
  {
//----+
   m_Size_=65;
//----                     
   double SATLTable[]=
     {
      +0.0982862174,+0.0975682269,+0.0961401078,+0.0940230544,+0.0912437090,+0.0878391006,
      +0.0838544303,+0.0793406350,+0.0743569346,+0.0689666682,+0.0632381578,+0.0572428925,
      +0.0510534242,+0.0447468229,+0.0383959950,+0.0320735368,+0.0258537721,+0.0198005183,
      +0.0139807863,+0.0084512448,+0.0032639979,-0.0015350359,-0.0059060082,-0.0098190256,
      -0.0132507215,-0.0161875265,-0.0186164872,-0.0205446727,-0.0219739146,-0.0229204861,
      -0.0234080863,-0.0234566315,-0.0231017777,-0.0223796900,-0.0213300463,-0.0199924534,
      -0.0184126992,-0.0166377699,-0.0147139428,-0.0126796776,-0.0105938331,-0.0084736770,
      -0.0063841850,-0.0043466731,-0.0023956944,-0.0005535180,+0.0011421469,+0.0026845693,
      +0.0040471369,+0.0052380201,+0.0062194591,+0.0070340085,+0.0076266453,+0.0080376628,
      +0.0083037666,+0.0083694798,+0.0082901022,+0.0080741359,+0.0077543820,+0.0073260526,
      +0.0068163569,+0.0062325477,+0.0056078229,+0.0049516078,+0.0161380976
     };

   ArrayCopy(m_SATLTable,SATLTable,0,0,WHOLE_ARRAY);
//----+ 
  }
//+X================================================================X+
//|  RFTL усреднение                                                 |
//+X================================================================X+    
double CRFTL::RFTLSeries
(
 uint begin,// Номер начала достоверного отсчёта баров
 uint prev_calculated,// Количество истории в барах на предыдущем тике
 uint rates_total,// Количество истории в барах на текущем тике
 double series,// Значение ценового ряда, расчитанное для бара с номером bar
 uint bar,// Номер бара
 bool set // Направление индексирования массивов
 )
// FATLSeries(begin, prev_calculated, rates_total, Length, series, bar, set) 
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+ Проверка начала достоверного отсчёта баров 
   if(BarCheck1(begin,bar,set)) return(EMPTY_VALUE);

//----+ перестановка и инициализация ячеек массива m_SeriesArray
   Recount_ArrayZeroPos(m_count,m_Size_,prev_calculated,rates_total,series,bar,m_SeriesArray,set);

//----+ Инициализация нуля
   if(BarCheck3(begin,bar,set,m_Size_)) return(EMPTY_VALUE);

//----+ Вычисление FATL
   double RFTL=0.0;
   if(BarCheck5(rates_total,bar,set))
     {
      if(prev_calculated!=rates_total)
        {
         m_RFTL=0.0;
         for(int iii=1; iii<m_Size_; iii++)
            m_RFTL+=m_RFTLTable[iii]*m_SeriesArray[Recount_ArrayNumber(m_count,m_Size_,iii)];
        }
      RFTL=m_RFTL+m_RFTLTable[0]*m_SeriesArray[Recount_ArrayNumber(m_count,m_Size_,0)];
     }
   else for(int iii=0; iii<m_Size_; iii++)
                    RFTL+=m_RFTLTable[iii]*m_SeriesArray[Recount_ArrayNumber(m_count,m_Size_,iii)];
//----+
   return(RFTL);
  }
//+X================================================================X+   
//| Конструктор класса CRFTL                                         |
//+X================================================================X+
CRFTL::CRFTL()
  {
//----+
   m_Size_=44;
//----                     
   double RFTLTable[]=
     {
      -0.0025097319, +0.0513007762, +0.1142800493, +0.1699342860, +0.2025269304,
      +0.2025269304, +0.1699342860, +0.1142800493, +0.0513007762, -0.0025097319,
      -0.0353166244, -0.0433375629, -0.0311244617, -0.0088618137, +0.0120580088,
      +0.0233183633, +0.0221931304, +0.0115769653, -0.0022157966, -0.0126536111,
      -0.0157416029, -0.0113395830, -0.0025905610, +0.0059521459, +0.0105212252,
      +0.0096970755, +0.0046585685, -0.0017079230, -0.0063513565, -0.0074539350,
      -0.0050439973, -0.0007459678, +0.0032271474, +0.0051357867, +0.0044454862,
      +0.0018784961, -0.0011065767, -0.0031162862, -0.0033443253, -0.0022163335,
      +0.0002573669, +0.0003650790, +0.0060440751, +0.0018747783
     };

   ArrayCopy(m_RFTLTable,RFTLTable,0,0,WHOLE_ARRAY);
//----+ 
  }
//+X================================================================X+
//|  RSTL усреднение                                                 |
//+X================================================================X+    
double CRSTL::RSTLSeries
(
 uint begin,// Номер начала достоверного отсчёта баров
 uint prev_calculated,// Количество истории в барах на предыдущем тике
 uint rates_total,// Количество истории в барах на текущем тике
 double series,// Значение ценового ряда, расчитанное для бара с номером bar
 uint bar,// Номер бара
 bool set // Направление индексирования массивов
 )
// FATLSeries(begin, prev_calculated, rates_total, Length, series, bar, set) 
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+ Проверка начала достоверного отсчёта баров 
   if(BarCheck1(begin,bar,set)) return(EMPTY_VALUE);

//----+ перестановка и инициализация ячеек массива m_SeriesArray
   Recount_ArrayZeroPos(m_count,m_Size_,prev_calculated,rates_total,series,bar,m_SeriesArray,set);

//----+ Инициализация нуля
   if(BarCheck3(begin,bar,set,m_Size_)) return(EMPTY_VALUE);

//----+ Вычисление FATL
   double RSTL=0.0;
   if(BarCheck5(rates_total,bar,set))
     {
      if(prev_calculated!=rates_total)
        {
         m_RSTL=0.0;
         for(int iii=1; iii<m_Size_; iii++)
            m_RSTL+=m_RSTLTable[iii]*m_SeriesArray[Recount_ArrayNumber(m_count,m_Size_,iii)];
        }
      RSTL=m_RSTL+m_RSTLTable[0]*m_SeriesArray[Recount_ArrayNumber(m_count,m_Size_,0)];
     }
   else for(int iii=0; iii<m_Size_; iii++)
                    RSTL+=m_RSTLTable[iii]*m_SeriesArray[Recount_ArrayNumber(m_count,m_Size_,iii)];
//----+
   return(RSTL);
  }
//+X================================================================X+   
//| Конструктор класса CRSTL                                         |
//+X================================================================X+
CRSTL::CRSTL()
  {
//----+
   m_Size_=99;
//----                     
   double RSTLTable[]=
     {
      -0.00514293,-0.00398417,-0.00262594,-0.00107121,+0.00066887,+0.00258172,+0.00465269,
      +0.00686394,+0.00919334,+0.01161720,+0.01411056,+0.01664635,+0.01919533,+0.02172747,
      +0.02421320,+0.02662203,+0.02892446,+0.03109071,+0.03309496,+0.03490921,+0.03651145,
      +0.03788045,+0.03899804,+0.03984915,+0.04042329,+0.04071263,+0.04071263,+0.04042329,
      +0.03984915,+0.03899804,+0.03788045,+0.03651145,+0.03490921,+0.03309496,+0.03109071,
      +0.02892446,+0.02662203,+0.02421320,+0.02172747,+0.01919533,+0.01664635,+0.01411056,
      +0.01161720,+0.00919334,+0.00686394,+0.00465269,+0.00258172,+0.00066887,-0.00107121,
      -0.00262594,-0.00398417,-0.00514293,-0.00609634,-0.00684602,-0.00739452,-0.00774847,
      -0.00791630,-0.00790940,-0.00774085,-0.00742482,-0.00697718,-0.00641613,-0.00576108,
      -0.00502957,-0.00423873,-0.00340812,-0.00255923,-0.00170217,-0.00085902,-0.00004113,
      +0.00073700,+0.00146422,+0.00213007,+0.00272649,+0.00324752,+0.00368922,+0.00405000,
      +0.00433024,+0.00453068,+0.00465046,+0.00469058,+0.00466041,+0.00457855,+0.00442491,
      +0.00423019,+0.00399201,+0.00372169,+0.00342736,+0.00311822,+0.00280309,+0.00249088,
      +0.00219089,+0.00191283,+0.00166683,+0.00146419,+0.00131867,+0.00124645,+0.00126836,
      -0.00401854
     };

   ArrayCopy(m_RSTLTable,RSTLTable,0,0,WHOLE_ARRAY);
//----+ 
  }
//+X================================================================X+
//|  расчёт минимального количества необходимых баров алгоритма XMA  |
//+X================================================================X+     
int CXMA::GetStartBars(Smooth_Method Method,int Length,int Phase)
  {
//----+ 
   switch(Method)
     {
      case MODE_SMA_:  return(Length);
      case MODE_EMA_:  return(0);
      case MODE_SMMA_: return(Length+1);
      case MODE_LWMA_: return(Length);
      case MODE_JJMA:  return(30);
      case MODE_JurX:  return(0);
      case MODE_ParMA: return(Length);
      case MODE_T3:    return(0);
      case MODE_VIDYA: return(Phase+2);
      case MODE_AMA:   return(Length+2);
     }
//----+
   return(0);
  }
//+X================================================================X+
//|  Инициализация переменных алгоритма XMA                          |
//+X================================================================X+    
double CXMA::XMASeries
(uint begin,// номер начала достоверного отсчёта баров
 uint prev_calculated,// Количество истории в барах на предыдущем тике
 uint rates_total,// Количество истории в барах на текущем тике
 // 0 - запрет изменения параметров,  любое другое значение - разрешение.
 Smooth_Method Method,
 int Phase,// параметр, изменяющийся в пределах -100 ... +100(для JJMA), влияет на качество переходного процесса усреднения
 int Length,// глубина сглаживания
 double series,// Значение ценового ряда, расчитанное для бара с номером bar
 uint bar,// Номер бара
 bool set // Направление индексирования массивов.
 )
// XMASeries(begin,prev_calculated,rates_total,Smooth_Method Method,Phase,Length,series,bar,set) 
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+ 
   XMAInit(Method);

   switch(Method)
     {
      case MODE_SMA_:  return(SMA.SMASeries(begin,prev_calculated,rates_total,Length,series,bar,set));
      case MODE_EMA_:  return(EMA.EMASeries(begin,prev_calculated,rates_total,Length,series,bar,set));
      case MODE_SMMA_: return(SMMA.SMMASeries(begin,prev_calculated,rates_total,Length,series,bar,set));
      case MODE_LWMA_: return(LWMA.LWMASeries(begin,prev_calculated,rates_total,Length,series,bar,set));
      case MODE_JJMA:  return(JJMA.JJMASeries(begin,prev_calculated,rates_total,0,Phase,Length,series,bar,set));
      case MODE_JurX:  return(JurX.JurXSeries(begin,prev_calculated,rates_total,0,Length,series,bar,set));
      case MODE_ParMA: return(ParMA.ParMASeries(begin,prev_calculated,rates_total,Length,series,bar,set));
      case MODE_T3:    return(T3.T3Series(begin,prev_calculated,rates_total,0,Phase,Length,series,bar,set));
      case MODE_VIDYA: return(VIDYA.VIDYASeries(begin,prev_calculated,rates_total,Phase,Length,series,bar,set));
      case MODE_AMA:   return(AMA.AMASeries(begin,prev_calculated,rates_total,Length,2,Phase,2.0,series,bar,set));
     }
//----+
   return(0.0);
  }
//+X================================================================X+
//|  Инициализация переменных алгоритма XMA                          |
//+X================================================================X+    
void CXMA::XMAInit(Smooth_Method Method)
// XMAInit(Method) 
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+
   if(m_init)return;
   else
     {
      m_init=true;
      m_Method=Method;
     }

   switch(Method)
     {
      case MODE_SMA_:  SMA   = new CMoving_Average; break;
      case MODE_EMA_:  EMA   = new CMoving_Average; break;
      case MODE_SMMA_: SMMA  = new CMoving_Average; break;
      case MODE_LWMA_: LWMA  = new CMoving_Average; break;
      case MODE_JJMA:  JJMA  = new CJJMA;           break;
      case MODE_JurX:  JurX  = new CJurX;           break;
      case MODE_ParMA: ParMA = new CParMA;          break;
      case MODE_T3:    T3    = new CT3;             break;
      case MODE_VIDYA: VIDYA = new CCMO;            break;
      case MODE_AMA:   AMA   = new CAMA;            break;
      default:                                      break;
     }
//----+
  }
//+X================================================================X+
//|  Деинициализация переменных алгоритма XMA                        |
//+X================================================================X+   
void CXMA::~CXMA()
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+ 
   switch(m_Method)
     {
      case MODE_SMA_:  if (GetPointer(SMA)!=NULL)   delete SMA;   break;
      case MODE_EMA_:  if (GetPointer(EMA)!=NULL)   delete EMA;   break;
      case MODE_SMMA_: if (GetPointer(SMMA)!=NULL)  delete SMMA;  break;
      case MODE_LWMA_: if (GetPointer(LWMA)!=NULL)  delete LWMA;  break;
      case MODE_JJMA:  if (GetPointer(JJMA)!=NULL)  delete JJMA;  break;
      case MODE_JurX:  if (GetPointer(JurX)!=NULL)  delete JurX;  break;
      case MODE_ParMA: if (GetPointer(ParMA)!=NULL) delete ParMA; break;
      case MODE_T3:    if (GetPointer(T3)!=NULL)    delete T3;    break;
      case MODE_VIDYA: if (GetPointer(VIDYA)!=NULL) delete VIDYA; break;
      case MODE_AMA:   if (GetPointer(AMA)!=NULL)   delete AMA;   break;
     }
//----+
  }
//+X================================================================X+
//|  Получение стрингового имени алгоритма усреднения XMA            |
//+X================================================================X+   
string CXMA::GetString_MA_Method(Smooth_Method Method)
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+  
   switch(Method)
     {
      case MODE_SMA_:  return("SMA");
      case MODE_EMA_:  return("EMA");
      case MODE_SMMA_: return("SMMA");
      case MODE_LWMA_: return("LWMA");
      case MODE_JJMA:  return("JJMA");
      case MODE_JurX:  return("JurX");
      case MODE_ParMA: return("ParMA");
      case MODE_T3:    return("T3");
      case MODE_VIDYA: return(" VIDYA");
      case MODE_AMA:   return("AMA");
     }
//----+
   return("");
  }
//+X================================================================X+
//| Проверка параметра усреднения Phase на корректность              |
//+X================================================================X+
void CXMA::XMAPhaseCheck(string PhaseName,int ExternPhase,Smooth_Method Method)

// XMAPhaseCheck(PhaseName, ExternPhase, Method)
  {
//---- сброс сообщений при недопустимых значениях входных параметров  
   switch(Method)
     {
      case MODE_SMA_:  break;
      case MODE_EMA_:  break;
      case MODE_SMMA_: break;
      case MODE_LWMA_: break;
      case MODE_JJMA:
         //----
         if(ExternPhase<-100)
           {
            string word;
            StringConcatenate(word,__FUNCTION__," (): Параметр ",PhaseName,
                              " должен быть не менее -100. Вы ввели недопустимое ",ExternPhase," будет использовано -100");
            Print(word);
            break;
           }
         //----
         if(ExternPhase>+100)
           {
            string word;
            StringConcatenate(word,__FUNCTION__," (): Параметр ",PhaseName,
                              " должен быть не более +100. Вы ввели недопустимое ",ExternPhase," будет использовано +100");
            Print(word);
            break;;
           }
         break;

      case MODE_JurX:  break;
      case MODE_ParMA: break;

      case MODE_T3:    break;
      if(ExternPhase<1)
        {
         string word;
         StringConcatenate(word,__FUNCTION__," (): Параметр ",PhaseName,
                           " должен быть не менее 1. Вы ввели недопустимое ",ExternPhase," будет использовано 1");
         Print(word);
         break;
        }

      case MODE_VIDYA:

         if(ExternPhase<1)
           {
            string word;
            StringConcatenate(word,__FUNCTION__," (): Параметр ",PhaseName,
                              " должен быть не менее 1. Вы ввели недопустимое ",ExternPhase," будет использовано 1");
            Print(word);
            break;
           }

      case MODE_AMA:

         if(ExternPhase<1)
           {
            string word;
            StringConcatenate(word,__FUNCTION__," (): Параметр ",PhaseName,
                              " должен быть не менее 1. Вы ввели недопустимое ",ExternPhase," будет использовано 1");
            Print(word);
            break;
           }
     }

//----+
  }
//+X================================================================X+
//| Проверка глубины усреднения Lengt на корректность                |
//+X================================================================X+
void CXMA::XMALengthCheck(string LengthName,int ExternLength)

// XMALengthCheck(LengthName, ExternLength)
  {
//----+
//---- сброс сообщений при недопустимых значениях входных параметров  
   if(ExternLength<1)
     {
      string word;
      StringConcatenate
      (word,__FUNCTION__," (): Параметр ",LengthName,
       " должен быть не менее 1. Вы ввели недопустимое ",
       ExternLength," будет использовано  1");
      Print(word);
      return;
     }
//----+
  }
//+X================================================================X+
//| Проверка периода усреднения на корректность                      |
//+X================================================================X+
void CMovSeriesTools::MALengthCheck(string LengthName,int ExternLength)
// MALengthCheck(LengthName, ExternLength)
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+ 
   if(ExternLength<1)
     {
      string word;
      StringConcatenate
      (word,__FUNCTION__," (): Параметр ",LengthName,
       " должен быть не менее 1. Вы ввели недопустимое ",
       ExternLength," будет использовано  1");
      Print(word);
      return;
     }
//----+
  }
//+X================================================================X+
//| Проверка периода усреднения на корректность                      |
//+X================================================================X+
void CMovSeriesTools::MALengthCheck(string LengthName,double ExternLength)

// MALengthCheck(LengthName, ExternLength)
  {
//----+ 
   if(ExternLength<1)
     {
      string word;
      StringConcatenate
      (word,__FUNCTION__," (): Параметр ",LengthName,
       " должен быть не менее 1. Вы ввели недопустимое ",
       ExternLength," будет использовано  1");
      Print(word);
      return;
     }
//----+
  }
//+X================================================================X+
//| Проверка бара на его присутствие в диапозоне расчёта             |
//+X================================================================X+
bool CMovSeriesTools::BarCheck1(int begin,int bar,bool Set)

// BarCheck1(begin, bar, Set)
  {
//----+
   if((!Set && bar<begin) || (Set && bar>begin)) return(true);
//----+
   return(false);
  }
//+X================================================================X+
//| Проверка бара на старт расчёта                                   |
//+X================================================================X+
bool CMovSeriesTools::BarCheck2(int begin,int bar,bool Set,int Length)

// BarCheck2(begin, bar, Set, Length)
  {
//----+
   if((!Set && bar==begin+Length-1) || (Set && bar==begin-Length+1))
      return(true);
//----+
   return(false);
  }
//+X================================================================X+
//| Проверка бара на отсутствие баров для усреднения                 |
//+X================================================================X+
bool CMovSeriesTools::BarCheck3(int begin,int bar,bool Set,int Length)

// BarCheck3(begin, bar, Set, Length)
  {
//----+
   if((!Set && bar<begin+Length-1) || (Set && bar>begin-Length+1))
      return(true);
//----+
   return(false);
  }
//+X================================================================X+
//| Проверка бара на момент сохранения данных                        |
//+X================================================================X+
bool CMovSeriesTools::BarCheck4(int rates_total,int bar,bool Set)

// BarCheck4(rates_total, bar, Set)
  {
//----+
//----+ Сохранение значений переменных
   if((!Set && bar==rates_total-2) || (Set && bar==1)) return(true);
//----+
   return(false);
  }
//+X================================================================X+
//| Проверка бара на момент восстановления данных                    |
//+X================================================================X+
bool CMovSeriesTools::BarCheck5(int rates_total,int bar,bool Set)

// BarCheck5(rates_total, begin, bar, set)
  {
//----+
//----+ Восстановление значений переменных
   if((!Set && bar==rates_total-1) || (Set && bar==0)) return(true);
//----+
   return(false);
  }
//+X================================================================X+
//| Изменение некорректного периода усреднения                       |
//+X================================================================X+
void CMovSeriesTools::LengthCheck(int &ExternLength)

// LengthCheck(LengthName, ExternLength)
  {
//----+
   if(ExternLength<1) ExternLength=1;
//----+
  }
//+X================================================================X+
//| Изменение некорректного периода усреднения                       |
//+X================================================================X+
void CMovSeriesTools::LengthCheck(double &ExternLength)

// LengthCheck(ExternLength)
  {
//----+
   if(ExternLength<1) ExternLength=1;
//----+
  }
//+X================================================================X+
//|  пересчёт позиции самого нового элемента в массиве               |
//+X================================================================X+    
void CMovSeriesTools::Recount_ArrayZeroPos
(
 int &count,// Возврат по ссылке номера текущего значения ценового ряда
 int Length,
 uint prev_calculated,// Количество истории в барах на предыдущем тике
 uint rates_total,// Количество истории в барах на текущем тике
 double series,// Значение ценового ряда, расчитанное для бара с номером bar
 int bar,
 double &Array[],
 bool set // Направление индексирования массивов
 )
// Recount_ArrayZeroPos(count, Length, prev_calculated, rates_total, series, bar, Array[], set)
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+
   if(set)
     {
      if(bar!=rates_total-prev_calculated)
        {
         count--;
         if(count<0) count=Length-1;
        }
     }
   else
     {
      if(bar!=prev_calculated-1)
        {
         count--;
         if(count<0) count=Length-1;
        }
     }

   Array[count]=series;
//----+
  }
//+X================================================================X+
//|  Преобразование номера таймсерии в позицию в массиве             |
//+X================================================================X+    
int CMovSeriesTools::Recount_ArrayNumber
(
 int count,// Номер текущего значения ценового ряда
 int Length,
 int Number // Позиция запрашиваемого значения относительно текущего бара bar
 )
// Recount_ArrayNumber(count, Length, Number)
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+
   int ArrNumber=Number+count;

   if(ArrNumber>Length-1) ArrNumber-=Length;
//----+
   return(ArrNumber);
  }
//+X================================================================X+
//|  Изменение размера массива Array[]                               |
//+X================================================================X+    
bool CMovSeriesTools::SeriesArrayResize
(
 string FunctionsName,// Название функции, внутри которой меняется размер
 int Length,// новый размер массива
 double &Array[],// Изменяемый массив
 int &Size_ // Получившийся размер массива
 )
// SeriesArrayResize(FunctionsName, Length, Array, Size_) 
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+    

//----+ Изменение размеров массива переменных
   if(Length>Size_)
     {
      int Size=Length+1;

      if(ArrayResize(Array,Size)==-1)
        {
         ArrayResizeErrorPrint(FunctionsName,Size_);
         return(false);
        }

      Size_=Size;
     }
//----+
   return(true);
  }
//+X================================================================X+
//|  Сброс в логфайл ошибки изменения размера массива                |
//+X================================================================X+    
bool CMovSeriesTools::ArrayResizeErrorPrint
(
 string FunctionsName,
 int &Size_
 )
// ArrayResizeErrorPrint(FunctionsName, Size_) 
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+    
   string lable,word;
   StringConcatenate(lable,FunctionsName,"():");
   StringConcatenate(word,lable," Ошибка!!! Не удалось изменить",
                     " размеры массива переменных функции ",FunctionsName,"()!");
   Print(word);
//----             
   int error=GetLastError();
   ResetLastError();
//----
   if(error>4000)
     {
      StringConcatenate(word,lable,"(): Код ошибки ",error);
      Print(word);
     }

   Size_=-2;
   return(false);
//----+
   return(true);
  }
//+X----------------------+ <<< The End >>> +-----------------------X+
