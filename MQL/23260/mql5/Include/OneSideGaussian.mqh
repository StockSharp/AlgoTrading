//+------------------------------------------------------------------+
//|                                              OneSideGaussian.mqh |
//|                                       Copyright © 2007, Tinytjan |
//|                                                 tinytjan@mail.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2007, Tinytjan"
#property link      "tinytjan@mail.ru"
//----
#define MaxLength 22

// Buffer for one side gaussian blurring. Works like MA but i think
// this method will work better

// double GaussianBuffer_N
// N -- number of old ticks used with current one
// The buffer consists of coefs for Gaussian blurring

// Note: use only N + 1 first values of the buffer
// The rest will be zero

double GaussianBuffer_1[MaxLength];
double GaussianBuffer_2[MaxLength];
double GaussianBuffer_3[MaxLength];
double GaussianBuffer_5[MaxLength];
double GaussianBuffer_8[MaxLength];
double GaussianBuffer_13[MaxLength];
double GaussianBuffer_21[MaxLength];
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum Applied_price_
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
//+------------------------------------------------------------------+
//| Gaussian function                                                |
//+------------------------------------------------------------------+
// Counts function Exp((x - x0)^2/s^2)
// x0 - higher point of function
// x  - point function is counted at
// s  - width of function // don't forget about 3-sigma rule

double Gaussian(int Size,int X)
  {
//----
   return(MathExp(-X*X*9/((Size+1)*(Size+1))));
//----
  }
//+------------------------------------------------------------------+
//| Buffers initialization function                                  |
//+------------------------------------------------------------------+
// Note: Place this function into Ur custom initialization function
// of Ur indicator or expert

void BuffersInit()
  {
//----
   int i=0;

// starting init with zeros
   for(i=0; i<MaxLength-1; i++)
     {
      GaussianBuffer_1[i] = 0.0;
      GaussianBuffer_2[i] = 0.0;
      GaussianBuffer_3[i] = 0.0;
      GaussianBuffer_5[i] = 0.0;
      GaussianBuffer_8[i] = 0.0;
      GaussianBuffer_13[i] = 0.0;
      GaussianBuffer_21[i] = 0.0;
     }

// init with function coefs
   for(i = 0; i < 1; i++)    GaussianBuffer_1[i]  = Gaussian(1, i);
   for(i = 0; i < 2; i++)    GaussianBuffer_2[i]  = Gaussian(2, i);
   for(i = 0; i < 3; i++)    GaussianBuffer_3[i]  = Gaussian(3, i);
   for(i = 0; i < 5; i++)    GaussianBuffer_5[i]  = Gaussian(5, i);
   for(i = 0; i < 8; i++)    GaussianBuffer_8[i]  = Gaussian(8, i);
   for(i = 0; i < 13; i++)   GaussianBuffer_13[i] = Gaussian(13, i);
   for(i = 0; i < 21; i++)   GaussianBuffer_21[i] = Gaussian(21, i);

   double sum;

//normalization
   sum=0.0;
   for(i = 0; i < 1; i++)    sum += GaussianBuffer_1[i];
   for(i = 0; i < 1; i++)    GaussianBuffer_1[i] /= sum;

   sum=0.0;
   for(i = 0; i < 2; i++)    sum += GaussianBuffer_2[i];
   for(i = 0; i < 2; i++)    GaussianBuffer_2[i] /= sum;

   sum=0.0;
   for(i = 0; i < 3; i++)    sum += GaussianBuffer_3[i];
   for(i = 0; i < 3; i++)    GaussianBuffer_3[i] /= sum;

   sum=0.0;
   for(i = 0; i < 5; i++)    sum += GaussianBuffer_5[i];
   for(i = 0; i < 5; i++)    GaussianBuffer_5[i] /= sum;

   sum=0.0;
   for(i = 0; i < 8; i++)    sum += GaussianBuffer_8[i];
   for(i = 0; i < 8; i++)    GaussianBuffer_8[i] /= sum;

   sum=0.0;
   for(i = 0; i < 13; i++)    sum += GaussianBuffer_13[i];
   for(i = 0; i < 13; i++)    GaussianBuffer_13[i] /= sum;

   sum=0.0;
   for(i = 0; i < 21; i++)    sum += GaussianBuffer_21[i];
   for(i = 0; i < 21; i++)    GaussianBuffer_21[i] /= sum;
//----
  }
//+------------------------------------------------------------------+   
//| Получение значения ценовой таймсерии                             |
//+------------------------------------------------------------------+ 
double CountPrice(
                  int PriceMode,// Ценовая константа
                  int   index,// Индекс сдвига относительно текущего бара на указанное количество периодов назад или вперёд
                  const double &Open[],
                  const double &Low[],
                  const double &High[],
                  const double &Close[]
                  )
//CountPrice(PriceMode, index, open, low, high, close)
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----
   switch(PriceMode)
     {
      //---- Ценовые константы из перечисления ENUM_APPLIED_PRICE
      case  PRICE_CLOSE: return(Close[index]);
      case  PRICE_OPEN: return(Open[index]);
      case  PRICE_HIGH: return(High[index]);
      case  PRICE_LOW: return(Low[index]);
      case  PRICE_MEDIAN: return((High[index]+Low[index])/2.0);
      case  PRICE_TYPICAL: return((Close[index]+High[index]+Low[index])/3.0);
      case  PRICE_WEIGHTED: return((2*Close[index]+High[index]+Low[index])/4.0);

      //----                            
      case  8: return((Open[index]+Close[index])/2.0);
      case  9: return((Open[index]+Close[index]+High[index]+Low[index])/4.0);
      //----                                
      case 10:
        {
         if(Close[index]>Open[index])return(High[index]);
         else
           {
            if(Close[index]<Open[index])
               return(Low[index]);
            else return(Close[index]);
           }
        }
      //----         
      case 11:
        {
         if(Close[index]>Open[index])return((High[index]+Close[index])/2.0);
         else
           {
            if(Close[index]<Open[index])
               return((Low[index]+Close[index])/2.0);
            else return(Close[index]);
           }
         break;
        }
      //----         
      case 12:
        {
         double res=High[index]+Low[index]+Close[index];

         if(Close[index]<Open[index]) res=(res+Low[index])/2;
         if(Close[index]>Open[index]) res=(res+High[index])/2;
         if(Close[index]==Open[index]) res=(res+Close[index])/2;
         return(((res-Low[index])+(res-High[index]))/2);
        }
      //----
      default: return(Close[index]);
     }
//----
//return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double Smooth_1(
                int Rates_total,      // максимальное количество баров
                int PriceMode,        // Ценовая константа
                int   index,          // Индекс сдвига относительно текущего бара на указанное количество периодов назад или вперёд
                const double &Open[],
                const double &Low[],
                const double &High[],
                const double &Close[]
                )
  {
//----
   if(Rates_total<=index+1) return(0);

   double sum=0;

   for(int i=0; i<=1; i++) sum+=GaussianBuffer_1[i]*CountPrice(PriceMode,i+index,Open,Low,High,Close);
//----
   return(sum);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double Smooth_2(
                int Rates_total,      // максимальное количество баров
                int PriceMode,        // Ценовая константа
                int   index,          // Индекс сдвига относительно текущего бара на указанное количество периодов назад или вперёд
                const double &Open[],
                const double &Low[],
                const double &High[],
                const double &Close[]
                )
  {
//----
   if(Rates_total<=index+2) return(0);

   double sum=0;

   for(int i=0; i<=2; i++) sum+=GaussianBuffer_2[i]*CountPrice(PriceMode,i+index,Open,Low,High,Close);
//----
   return(sum);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double Smooth_3(
                int Rates_total,      // максимальное количество баров
                int PriceMode,        // Ценовая константа
                int   index,          // Индекс сдвига относительно текущего бара на указанное количество периодов назад или вперёд
                const double &Open[],
                const double &Low[],
                const double &High[],
                const double &Close[]
                )
  {
//----
   if(Rates_total<=index+3) return(0);

   double sum=0;

   for(int i=0; i<=3; i++) sum+=GaussianBuffer_3[i]*CountPrice(PriceMode,i+index,Open,Low,High,Close);
//----
   return(sum);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double Smooth_5(
                int Rates_total,      // максимальное количество баров
                int PriceMode,        // Ценовая константа
                int   index,          // Индекс сдвига относительно текущего бара на указанное количество периодов назад или вперёд
                const double &Open[],
                const double &Low[],
                const double &High[],
                const double &Close[]
                )
  {
//----
   if(Rates_total<=index+5) return(0);

   double sum=0;

   for(int i=0; i<=5; i++) sum+=GaussianBuffer_5[i]*CountPrice(PriceMode,i+index,Open,Low,High,Close);
//----
   return(sum);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double Smooth_8(
                int Rates_total,      // максимальное количество баров
                int PriceMode,        // Ценовая константа
                int   index,          // Индекс сдвига относительно текущего бара на указанное количество периодов назад или вперёд
                const double &Open[],
                const double &Low[],
                const double &High[],
                const double &Close[]
                )
  {
//----
   if(Rates_total<=index+8) return(0);

   double sum=0;

   for(int i=0; i<=8; i++) sum+=GaussianBuffer_8[i]*CountPrice(PriceMode,i+index,Open,Low,High,Close);
//----
   return(sum);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double Smooth_13(
                 int Rates_total,      // максимальное количество баров
                int PriceMode,        // Ценовая константа
                int   index,          // Индекс сдвига относительно текущего бара на указанное количество периодов назад или вперёд
                const double &Open[],
                const double &Low[],
                const double &High[],
                const double &Close[]
                 )
  {
//----
   if(Rates_total<=index+13) return(0);

   double sum=0;

   for(int i=0; i<=13; i++) sum+=GaussianBuffer_13[i]*CountPrice(PriceMode,i+index,Open,Low,High,Close);
//----
   return(sum);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double Smooth_21(
                 int Rates_total,      // максимальное количество баров
                int PriceMode,        // Ценовая константа
                int   index,          // Индекс сдвига относительно текущего бара на указанное количество периодов назад или вперёд
                const double &Open[],
                const double &Low[],
                const double &High[],
                const double &Close[]
                 )
  {
//----
   if(Rates_total<=index+21) return(0);

   double sum=0;

   for(int i=0; i<=21; i++) sum+=GaussianBuffer_21[i]*CountPrice(PriceMode,i+index,Open,Low,High,Close);
//----
   return(sum);
  }
//+------------------------------------------------------------------+
