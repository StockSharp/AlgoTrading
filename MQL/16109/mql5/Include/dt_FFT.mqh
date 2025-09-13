//+------------------------------------------------------------------+
//|                                                       dt_FFT.mqh |
//|                                           Copyright © 2006, klot |
//|                                                     klot@mail.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2006, klot"
#property link      "http://alglib.sources.ru/fft/"
#property description "FFT for MQL5"
#property library
//+-----------------------------------+
//| Объявление констант               |
//+-----------------------------------+
#define pi 3.14159265358979323846     // константа для числа pi
//+------------------------------------------------------------------+
//| Быстрое преобразование Фурье                                     |
//|                                                                  |
//| Алгоритм проводит быстрое преобразование Фурье комплексной       |
//| функции, заданной nn отсчетами на действительной оси.            |
//|                                                                  |
//| В зависимости от  переданных параметров, может выполняться       |
//| как прямое, так и обратное преобразование.                       |
//|                                                                  |
//| Входные параметры:                                               |
//|     nn  -   Число значений функции. Должно  быть  степенью       |
//|             двойки. Алгоритм   не  проверяет  правильность       |
//|             переданного значения.                                |
//|     a   -   array [0 .. 2*nn-1] of Real                          |
//|             Значения функции. I-ому значению соответствуют       |
//|             элементы   a[2*I]     (вещественная     часть)       |
//|             и a[2*I+1] (мнимая часть).                           |
//|     InverseFFT                                                   |
//|         -   направление преобразования.                          |
//|             True, если обратное, False, если прямое.             |
//|                                                                  |
//| Выходные параметры:                                              |
//|     a   -   результат   преобразования.   Подробнее    см.       |
//|             описание на сайте.  http://alglib.sources.ru/fft/    |
//+------------------------------------------------------------------+
void fastfouriertransform(double &a[],int nn,bool inversefft)
  {
//---
   int ii;
   int jj;
   int n;
   int mmax;
   int m;
   int j;
   int istep;
   int i;
   int isign;
   double wtemp;
   double wr;
   double wpr;
   double wpi;
   double wi;
   double theta;
   double tempr;
   double tempi;
//---
   if(inversefft) isign=-1;
   else isign=1;
//---
   n = 2*nn;
   j = 1;
   for(ii=1; ii<=nn; ii++)
     {
      i=2*ii-1;
      if(j>i)
        {
         tempr = a[j-1];
         tempi = a[j];
         a[j-1]= a[i-1];
         a[j]=a[i];
         a[i-1]=tempr;
         a[i]=tempi;
        }
      m=n/2;
      while(m>=2 && j>m)
        {
         j-=m;
         m/=2;
        }
      j+=m;
     }
   mmax=2;
   while(n>mmax)
     {
      istep = 2*mmax;
      theta = 2.0*pi/(isign*mmax);
      wpr = -2.0*MathPow(MathSin(0.5*theta),2);
      wpi = MathSin(theta);
      wr = 1.0;
      wi = 0.0;
      for(ii=1; ii<=mmax/2; ii++)
        {
         m=2*ii-1;
         for(jj=0; jj<=(n-m)/istep; jj++)
           {
            i = m+jj*istep;
            j = i+mmax;
            tempr = wr*a[j-1]-wi*a[j];
            tempi = wr*a[j]+wi*a[j-1];
            a[j-1]-=tempr;
            a[j]-=tempi;
            a[i-1]+=tempr;
            a[i]+=tempi;
           }
         wtemp=wr;
         wr = wr*wpr-wi*wpi+wr;
         wi = wi*wpr+wtemp*wpi+wi;
        }
      mmax=istep;
     }
   if(inversefft) for(i=1; i<=2*nn; i++) a[i-1]/=nn;
//---
   return;
  }
//+------------------------------------------------------------------+
//| Быстрое преобразование Фурье                                     |
//|                                                                  |
//| Алгоритм проводит быстрое преобразование Фурье вещественной      |
//| функции, заданной n отсчетами на действительной оси.             |
//|                                                                  |
//| В зависимости от  переданных параметров, может выполняться       |
//| как прямое, так и обратное преобразование.                       |
//|                                                                  |
//| Входные параметры:                                               |
//|     tnn  -   Число значений функции. Должно  быть  степенью      |
//|             двойки. Алгоритм   не  проверяет  правильность       |
//|             переданного значения.                                |
//|     a   -   array [0 .. nn-1] of Real                            |
//|             Значения функции.                                    |
//|     InverseFFT                                                   |
//|         -   направление преобразования.                          |
//|             True, если обратное, False, если прямое.             |
//|                                                                  |
//| Выходные параметры:                                              |
//|     a   -   результат   преобразования.   Подробнее    см.       |
//|             описание на сайте. http://alglib.sources.ru/fft/     |
//+------------------------------------------------------------------+
void realfastfouriertransform(double &a[],int tnn,bool inversefft)
  {
//---
   double twr;
   double twi;
   double twpr;
   double twpi;
   double twtemp;
   double ttheta;
   int i;
   int i1;
   int i2;
   int i3;
   int i4;
   double c1;
   double c2;
   double h1r;
   double h1i;
   double h2r;
   double h2i;
   double wrs;
   double wis;
   int nn;
   int ii;
   int jj;
   int n;
   int mmax;
   int m;
   int j;
   int istep;
   int isign;
   double wtemp;
   double wr;
   double wpr;
   double wpi;
   double wi;
   double theta;
   double tempr;
   double tempi;
//---
   if(tnn==1) return;
//---
   if(!inversefft)
     {
      ttheta=2.0*pi/tnn;
      c1 = 0.5;
      c2 = -0.5;
     }
   else
     {
      ttheta=2.0*pi/tnn;
      c1 = 0.5;
      c2 = 0.5;
      ttheta=-ttheta;
      twpr = -2.0*MathPow(MathSin(0.5*ttheta),2);
      twpi = MathSin(ttheta);
      twr = 1.0+twpr;
      twi = twpi;
      for(i=2; i<=tnn/4+1; i++)
        {
         i1 = i+i-2;
         i2 = i1+1;
         i3 = tnn+1-i2;
         i4 = i3+1;
         wrs = twr;
         wis = twi;
         h1r = c1*(a[i1]+a[i3]);
         h1i = c1*(a[i2]-a[i4]);
         h2r = -c2*(a[i2]+a[i4]);
         h2i = c2*(a[i1]-a[i3]);
         a[i1] = h1r+wrs*h2r-wis*h2i;
         a[i2] = h1i+wrs*h2i+wis*h2r;
         a[i3] = h1r-wrs*h2r+wis*h2i;
         a[i4] = -h1i+wrs*h2i+wis*h2r;
         twtemp= twr;
         twr = twr*twpr-twi*twpi+twr;
         twi = twi*twpr+twtemp*twpi+twi;
        }
      h1r=a[0];
      a[0] = c1*(h1r+a[1]);
      a[1] = c1*(h1r-a[1]);
     }
//---
   if(inversefft) isign=-1;
   else isign=1;
   n=tnn;
   nn= tnn/2;
   j = 1;
   for(ii=1; ii<=nn; ii++)
     {
      i=2*ii-1;
      if(j>i)
        {
         tempr = a[j-1];
         tempi = a[j];
         a[j-1]= a[i-1];
         a[j]=a[i];
         a[i-1]=tempr;
         a[i]=tempi;
        }
      m=n/2;
      while(m>=2 && j>m)
        {
         j -= m;
         m /= 2;
        }
      j+=m;
     }
   mmax=2;
   while(n>mmax)
     {
      istep = 2*mmax;
      theta = 2.0*pi/(isign*mmax);
      wpr = -2.0*MathPow(MathSin(0.5*theta),2);
      wpi = MathSin(theta);
      wr = 1.0;
      wi = 0.0;
      for(ii=1; ii<=mmax/2; ii++)
        {
         m=2*ii-1;
         for(jj=0; jj<=(n-m)/istep; jj++)
           {
            i = m+jj*istep;
            j = i+mmax;
            tempr = wr*a[j-1]-wi*a[j];
            tempi = wr*a[j]+wi*a[j-1];
            a[j-1]= a[i-1]-tempr;
            a[j]=a[i]-tempi;
            a[i-1]+=tempr;
            a[i]+=tempi;
           }
         wtemp=wr;
         wr = wr*wpr-wi*wpi+wr;
         wi = wi*wpr+wtemp*wpi+wi;
        }
      mmax=istep;
     }
   if(inversefft)
     {
      for(i=1; i<=2*nn; i++)
        {
         a[i-1]/=nn;
        }
     }
   if(!inversefft)
     {
      twpr = -2.0*MathPow(MathSin(0.5*ttheta),2);
      twpi = MathSin(ttheta);
      twr = 1.0+twpr;
      twi = twpi;
      for(i=2; i<=tnn/4+1; i++)
        {
         i1 = i+i-2;
         i2 = i1+1;
         i3 = tnn+1-i2;
         i4 = i3+1;
         wrs = twr;
         wis = twi;
         h1r = c1*(a[i1]+a[i3]);
         h1i = c1*(a[i2]-a[i4]);
         h2r = -c2*(a[i2]+a[i4]);
         h2i = c2*(a[i1]-a[i3]);
         a[i1] = h1r+wrs*h2r-wis*h2i;
         a[i2] = h1i+wrs*h2i+wis*h2r;
         a[i3] = h1r-wrs*h2r+wis*h2i;
         a[i4] = -h1i+wrs*h2i+wis*h2r;
         twtemp= twr;
         twr = twr*twpr-twi*twpi+twr;
         twi = twi*twpr+twtemp*twpi+twi;
        }
      h1r=a[0];
      a[0] = h1r+a[1];
      a[1] = h1r-a[1];
     }
//---
   return;
  }
//+--------------------------------------------------------------------------+
//| Корелляция с использованием БПФ                                          |
//|                                                                          |
//| На входе:                                                                |
//|     Signal      -   массив сигнал, с которым проводим корелляцию.        |
//|                     Нумерация элементов от 0 до SignalLen-1              |
//|     SignalLen   -   длина сигнала.                                       |
//|                                                                          |
//|     Pattern     -   массив образец, корелляцию сигнала с которым мы ищем |
//|                     Нумерация элементов от 0 до PatternLen-1             |
//|     PatternLen  -   длина образца                                        |
//|                                                                          |
//| На выходе:                                                               |
//|     Signal      -   значения корелляции в точках от 0 до                 |
//|                     SignalLen-1. http://alglib.sources.ru/fft/           |
//+--------------------------------------------------------------------------+                    
void fastcorellation(double &signal[],int signallen,double &pattern[],int patternlen)
  {
//---
//ap::real_1d_array a1;
//ap::real_1d_array a2;
   double a1[],a2[];
//---
   int nl;
   int i;
   double t1;
   double t2;
//---
   nl= signallen+patternlen;
   i = 1;
   while(i<nl)
     {
      i=i*2;
     }
   nl=i;
//a1.setbounds(0, nl-1);
//a2.setbounds(0, nl-1);
   ArrayResize(a1,nl);
   ArrayResize(a2,nl);
//---
   for(i=0; i<=signallen-1; i++) a1[i]=signal[i];
   for(i=signallen; i<=nl-1; i++) a1[i]=0;
   for(i=0; i<=patternlen-1; i++) a2[i]=pattern[i];
   for(i=patternlen; i<=nl-1; i++) a2[i]=0;
//---
   realfastfouriertransform(a1,nl,false);
   realfastfouriertransform(a2,nl,false);
//---
   a1[0] *= a2[0];
   a1[1] *= a2[1];
   for(i=1; i<=(nl/2)-1; i++)
     {
      t1 = a1[2*i];
      t2 = a1[2*i+1];
      a1[2*i]=t1*a2[2*i]+t2*a2[2*i+1];
      a1[2*i+1]=t2*a2[2*i]-t1*a2[2*i+1];
     }
   realfastfouriertransform(a1,nl,true);
   for(i=0; i<=signallen-1; i++) signal[i]=a1[i];
//---
   return;
  }
//+------------------------------------------------------------------+
//| Свертка с использованием БПФ                                     |
//|                                                                  |
//| Одна из сворачиваемых функций трактуется, как сигнал,            |
//| с которым проводим свертку. Вторая считается откликом.           |
//|                                                                  |
//| На входе:                                                        |
//|     Signal      -   сигнал, с которым проводим свертку. Массив   |
//|                     вещественных  чисел,  нумерация  элементов   |
//|                     от 0 до SignalLen-1.                         |
//|     SignalLen   -   длина сигнала.                               |
//|     Response    -   функция отклика. Состоит из  двух  частей,   |
//|                     соответствующих положительным и отрицательным|
//|                     значениям аргумента.                         |
//|                                                                  |
//|                     Элементам    массива  с   номерами от 0 до   |
//|                     NegativeLen соответствуют значения отклика   |
//|                     в точках от -NegativeLen до 0 соответственно.|
//|                                                                  |
//|                     Элементам      массива   с   номерами   от   |
//|                     NegativeLen+1  до  NegativeLen+PositiveLen   |
//|                     соответствуют  значения  отклика в  точках   |
//|                     от 1 до PositiveLen соответственно.          |
//|                                                                  |
//|     NegativeLen -   "Отрицательная длина" отклика.               |
//|     PositiveLen -   "Положительная длина" отклика.               |
//|                     За   пределами [-NegativeLen, PositiveLen]   |
//|                     отклик равен нолю.                           |
//|                                                                  |
//| На выходе:                                                       |
//|     Signal      -   значения свертки функции в точках от 0  до   |
//|                     SignalLen-1. http://alglib.sources.ru/fft/   |
//+------------------------------------------------------------------+                    
void fastconvolution(double &signal[],int signallen,double &response[],int negativelen,int positivelen)
  {
//---
//ap::real_1d_array a1;
//ap::real_1d_array a2;
   double a1[],a2[];
//----
   int nl;
   int i;
   double t1;
   double t2;
//---
   nl=signallen;
   if(negativelen>positivelen) nl+=negativelen;
   else nl+=positivelen;
   if(negativelen+1+positivelen>nl) nl=negativelen+1+positivelen;
   i=1;
   while(i<nl)
     {
      i=i*2;
     }
   nl=i;
//a1.setbounds(0, nl-1);
//a2.setbounds(0, nl-1);
   ArrayResize(a1,nl);
   ArrayResize(a2,nl);
//---
   for(i=0; i<=signallen-1; i++) a1[i]=signal[i];
   for(i=signallen; i<=nl-1; i++) a1[i]=0;
   for(i=0; i<=nl-1; i++) a2[i]=0;
   for(i=1; i<=negativelen; i++) a2[nl-i]=response[negativelen-i];
//---
   realfastfouriertransform(a1,nl,false);
   realfastfouriertransform(a2,nl,false);
//---
   a1[0] *= a2[0];
   a1[1] *= a2[1];
   for(i=1; i<=nl/2-1; i++)
     {
      t1 = a1[2*i];
      t2 = a1[2*i+1];
      a1[2*i]=t1*a2[2*i]-t2*a2[2*i+1];
      a1[2*i+1]=t2*a2[2*i]+t1*a2[2*i+1];
     }
   realfastfouriertransform(a1,nl,true);
   for(i=0; i<=signallen-1; i++) signal[i]=a1[i];
//---
   return;
  }
//+------------------------------------------------------------------+
//| Быстрое дискретное синусное преобразование                       |
//|                                                                  |
//| Алгоритм проводит быстрое синусное преобразование вещественной   |
//| функции, заданной nn отсчетами на действительной оси.            |
//|                                                                  |
//| В зависимости от  переданных параметров, может выполняться       |
//| как прямое, так и обратное преобразование.                       |
//|                                                                  |
//| Входные параметры:                                               |
//|     nn  -   Число значений функции. Должно  быть  степенью       |
//|             двойки. Алгоритм   не  проверяет  правильность       |
//|             переданного значения.                                |
//|     a   -   array [0 .. nn-1] of Real                            |
//|             Значения функции.                                    |
//|     InverseFST                                                   |
//|         -   направление преобразования.                          |
//|             True, если обратное, False, если прямое.             |
//|                                                                  |
//| Выходные параметры:                                              |
//|     a   -   результат   преобразования.   Подробнее    см.       |
//|             описание на сайте. http://alglib.sources.ru/fft/     |
//+------------------------------------------------------------------+            
void fastsinetransform(double &a[],int tnn,bool inversefst)
  {
//---
   int jj;
   int j;
   int tm;
   int n2;
   double sum;
   double y1;
   double y2;
   double theta;
   double wi;
   double wr;
   double wpi;
   double wpr;
   double wtemp;
   double twr;
   double twi;
   double twpr;
   double twpi;
   double twtemp;
   double ttheta;
   int i;
   int i1;
   int i2;
   int i3;
   int i4;
   double c1;
   double c2;
   double h1r;
   double h1i;
   double h2r;
   double h2i;
   double wrs;
   double wis;
   int nn;
   int ii;
   int n;
   int mmax;
   int m;
   int istep;
   int isign;
   double tempr;
   double tempi;
//---
   if(tnn==1)
     {
      a[0]=0;
      return;
     }
   theta=pi/tnn;
   wr = 1.0;
   wi = 0.0;
   wpr = -2.0*MathPow(MathSin(0.5*theta),2);
   wpi = MathSin(theta);
   a[0]= 0.0;
   tm = tnn/2;
   n2 = tnn+2;
   for(j=2; j<=tm+1; j++)
     {
      wtemp=wr;
      wr = wr*wpr-wi*wpi+wr;
      wi = wi*wpr+wtemp*wpi+wi;
      y1 = wi*(a[j-1]+a[n2-j-1]);
      y2 = 0.5*(a[j-1]-a[n2-j-1]);
      a[j-1]=y1+y2;
      a[n2-j-1]=y1-y2;
     }
   ttheta=2.0*pi/tnn;
   c1 = 0.5;
   c2 = -0.5;
   isign=1;
   n=tnn;
   nn= tnn/2;
   j = 1;
   for(ii=1; ii<=nn; ii++)
     {
      i=2*ii-1;
      if(j>i)
        {
         tempr = a[j-1];
         tempi = a[j];
         a[j-1]= a[i-1];
         a[j]=a[i];
         a[i-1]=tempr;
         a[i]=tempi;
        }
      m=n/2;
      while(m>=2 && j>m)
        {
         j = j-m;
         m = m/2;
        }
      j=j+m;
     }
   mmax=2;
   while(n>mmax)
     {
      istep = 2*mmax;
      theta = 2.0*pi/(isign*mmax);
      wpr = -2.0*MathPow(MathSin(0.5*theta),2);
      wpi = MathSin(theta);
      wr = 1.0;
      wi = 0.0;
      for(ii=1; ii<=mmax/2; ii++)
        {
         m=2*ii-1;
         for(jj=0; jj<=(n-m)/istep; jj++)
           {
            i = m+jj*istep;
            j = i+mmax;
            tempr = wr*a[j-1]-wi*a[j];
            tempi = wr*a[j]+wi*a[j-1];
            a[j-1]= a[i-1]-tempr;
            a[j]=a[i]-tempi;
            a[i-1]+=tempr;
            a[i]+=tempi;
           }
         wtemp=wr;
         wr = wr*wpr-wi*wpi+wr;
         wi = wi*wpr+wtemp*wpi+wi;
        }
      mmax=istep;
     }
   twpr = -2.0*MathPow(MathSin(0.5*ttheta),2);
   twpi = MathSin(ttheta);
   twr = 1.0+twpr;
   twi = twpi;
   for(i=2; i<=tnn/4+1; i++)
     {
      i1 = i+i-2;
      i2 = i1+1;
      i3 = tnn+1-i2;
      i4 = i3+1;
      wrs = twr;
      wis = twi;
      h1r = c1*(a[i1]+a[i3]);
      h1i = c1*(a[i2]-a[i4]);
      h2r = -c2*(a[i2]+a[i4]);
      h2i = c2*(a[i1]-a[i3]);
      a[i1] = h1r+wrs*h2r-wis*h2i;
      a[i2] = h1i+wrs*h2i+wis*h2r;
      a[i3] = h1r-wrs*h2r+wis*h2i;
      a[i4] = -h1i+wrs*h2i+wis*h2r;
      twtemp= twr;
      twr = twr*twpr-twi*twpi+twr;
      twi = twi*twpr+twtemp*twpi+twi;
     }
   h1r=a[0];
   a[0] = h1r+a[1];
   a[1] = h1r-a[1];
   sum=0.0;
   a[0]*=0.5;
   a[1]=0.0;
   for(jj=0; jj<=tm-1; jj++)
     {
      j=2*jj+1;
      sum+=a[j-1];
      a[j-1]=a[j];
      a[j]=sum;
     }
   if(inversefst) for(j=1; j<=tnn; j++) a[j-1]*=2/tnn;
//---
   return;
  }
//+------------------------------------------------------------------+
//| Быстрое дискретное косинусное преобразование                     |
//|                                                                  |
//| Алгоритм проводит быстрое косинусное преобразование вещественной |
//| функции, заданной nn отсчетами на действительной оси.            |
//|                                                                  |
//| В зависимости от  переданных параметров, может выполняться       |
//| как прямое, так и обратное преобразование.                       |
//|                                                                  |
//| Входные параметры:                                               |
//|     tnn  -  Число значений функции минус один. Должно быть       |
//|             степенью   двойки.   Алгоритм   не   проверяет       |
//|             правильность переданного значения.                   |
//|     a   -   array [0 .. nn] of Real                              |
//|             Значения функции.                                    |
//|     InverseFCT                                                   |
//|         -   направление преобразования.                          |
//|             True, если обратное, False, если прямое.             |
//|                                                                  |
//| Выходные параметры:                                              |
//|     a   -   результат   преобразования.   Подробнее    см.       |
//|             описание на сайте. http://alglib.sources.ru/fft/     |
//+------------------------------------------------------------------+            
void fastcosinetransform(double &a[],int tnn,bool inversefct)
  {
//---
   int j;
   int n2;
   double sum;
   double y1;
   double y2;
   double theta;
   double wi;
   double wpi;
   double wr;
   double wpr;
   double wtemp;
   double twr;
   double twi;
   double twpr;
   double twpi;
   double twtemp;
   double ttheta;
   int i;
   int i1;
   int i2;
   int i3;
   int i4;
   double c1;
   double c2;
   double h1r;
   double h1i;
   double h2r;
   double h2i;
   double wrs;
   double wis;
   int nn;
   int ii;
   int jj;
   int n;
   int mmax;
   int m;
   int istep;
   int isign;
   double tempr;
   double tempi;
//---
   if(tnn==1)
     {
      y1 = a[0];
      y2 = a[1];
      a[0] = 0.5*(y1+y2);
      a[1] = 0.5*(y1-y2);
      if(inversefct)
        {
         a[0] *= 2;
         a[1] *= 2;
        }
      return;
     }
   wi = 0;
   wr = 1;
   theta = pi/tnn;
   wtemp = MathSin(theta*0.5);
   wpr = -2.0*wtemp*wtemp;
   wpi = MathSin(theta);
   sum = 0.5*(a[0]-a[tnn]);
   a[0]= 0.5*(a[0]+a[tnn]);
   n2=tnn+2;
   for(j=2; j<=tnn/2; j++)
     {
      wtemp=wr;
      wr = wtemp*wpr-wi*wpi+wtemp;
      wi = wi*wpr+wtemp*wpi+wi;
      y1 = 0.5*(a[j-1]+a[n2-j-1]);
      y2 = a[j-1]-a[n2-j-1];
      a[j-1]=y1-wi*y2;
      a[n2-j-1]=y1+wi*y2;
      sum=sum+wr*y2;
     }
   ttheta=2.0*pi/tnn;
   c1 = 0.5;
   c2 = -0.5;
   isign=1;
   n=tnn;
   nn= tnn/2;
   j = 1;
   for(ii=1; ii<=nn; ii++)
     {
      i=2*ii-1;
      if(j>i)
        {
         tempr = a[j-1];
         tempi = a[j];
         a[j-1]= a[i-1];
         a[j]=a[i];
         a[i-1]=tempr;
         a[i]=tempi;
        }
      m=n/2;
      while(m>=2 && j>m)
        {
         j = j-m;
         m = m/2;
        }
      j=j+m;
     }
   mmax=2;
   while(n>mmax)
     {
      istep = 2*mmax;
      theta = 2.0*pi/(isign*mmax);
      wpr = -2.0*MathPow(MathSin(0.5*theta),2);
      wpi = MathSin(theta);
      wr = 1.0;
      wi = 0.0;
      for(ii=1; ii<=mmax/2; ii++)
        {
         m=2*ii-1;
         for(jj=0; jj<=(n-m)/istep; jj++)
           {
            i = m+jj*istep;
            j = i+mmax;
            tempr = wr*a[j-1]-wi*a[j];
            tempi = wr*a[j]+wi*a[j-1];
            a[j-1]= a[i-1]-tempr;
            a[j]=a[i]-tempi;
            a[i-1]+=tempr;
            a[i]+=tempi;
           }
         wtemp=wr;
         wr = wr*wpr-wi*wpi+wr;
         wi = wi*wpr+wtemp*wpi+wi;
        }
      mmax=istep;
     }
   twpr = -2.0*MathPow(MathSin(0.5*ttheta),2);
   twpi = MathSin(ttheta);
   twr = 1.0+twpr;
   twi = twpi;
   for(i=2; i<=tnn/4+1; i++)
     {
      i1 = i+i-2;
      i2 = i1+1;
      i3 = tnn+1-i2;
      i4 = i3+1;
      wrs = twr;
      wis = twi;
      h1r = c1*(a[i1]+a[i3]);
      h1i = c1*(a[i2]-a[i4]);
      h2r = -c2*(a[i2]+a[i4]);
      h2i = c2*(a[i1]-a[i3]);
      a[i1] = h1r+wrs*h2r-wis*h2i;
      a[i2] = h1i+wrs*h2i+wis*h2r;
      a[i3] = h1r-wrs*h2r+wis*h2i;
      a[i4] = -h1i+wrs*h2i+wis*h2r;
      twtemp= twr;
      twr = twr*twpr-twi*twpi+twr;
      twi = twi*twpr+twtemp*twpi+twi;
     }
   h1r=a[0];
   a[0] = h1r+a[1];
   a[1] = h1r-a[1];
   a[tnn]=a[1];
   a[1]=sum;
   j=4;
   while(j<=tnn)
     {
      sum=sum+a[j-1];
      a[j-1]=sum;
      j=j+2;
     }
   if(inversefct) for(j=0; j<=tnn; j++) a[j]=a[j]*2/tnn;
//----
   return;
  }
//+------------------------------------------------------------------+
//| Быстрое преобразование Фурье двух вещественных функций           |
//|                                                                  |
//| Алгоритм проводит   быстрое   преобразование   Фурье  двух       |
//| вещественных    функций,  каждая   из  которых  задана  tn       |
//| отсчетами на действительной оси.                                 |
//|                                                                  |
//| Алгоритм  позволяет  сэкономить  время, но проводит только       |
//| прямое преобразование.                                           |
//|                                                                  |
//| Входные параметры:                                               |
//|     tn  -   Число значений функций. Должно  быть  степенью       |
//|             двойки. Алгоритм   не  проверяет  правильность       |
//|             переданного значения.                                |
//|     a1  -   array [0 .. nn-1] of Real                            |
//|             Значения первой функции.                             |
//|     a2  -   array [0 .. nn-1] of Real                            |
//|             Значения второй функции.                             |
//|                                                                  |
//| Выходные параметры:                                              |
//|     a   -   Преобразование Фурье первой функции                  |
//|     b   -   Преобразование Фурье второй функции                  |
//| (подробнее см. на сайте) http://alglib.sources.ru/fft/           |
//+------------------------------------------------------------------+
void tworealffts(double &a1[],double &a2[],double &a[],double &b[],int tn)
  {
//----
   int jj;
   int j;
   double rep;
   double rem;
   double aip;
   double aim;
   int ii;
   int n;
   int nn;
   int mmax;
   int m;
   int istep;
   int i;
   int isign;
   double wtemp;
   double wr;
   double wpr;
   double wpi;
   double wi;
   double theta;
   double tempr;
   double tempi;
//---
   nn=tn;
//a.setbounds(0, 2*tn-1);
//b.setbounds(0, 2*tn-1);
   ArrayResize(a,2*tn);
   ArrayResize(b,2*tn);
//---
   for(j=1; j<=tn; j++)
     {
      jj=j+j;
      a[jj-2] = a1[j-1];
      a[jj-1] = a2[j-1];
     }
   isign=1;
   n = 2*nn;
   j = 1;
   for(ii=1; ii<=nn; ii++)
     {
      i=2*ii-1;
      if(j>i)
        {
         tempr = a[j-1];
         tempi = a[j];
         a[j-1]= a[i-1];
         a[j]=a[i];
         a[i-1]=tempr;
         a[i]=tempi;
        }
      m=n/2;
      while(m>=2 && j>m)
        {
         j = j-m;
         m = m/2;
        }
      j=j+m;
     }
   mmax=2;
   while(n>mmax)
     {
      istep = 2*mmax;
      theta = 2.0*pi/(isign*mmax);
      wpr = -2.0*MathPow(MathSin(0.5*theta),2);
      wpi = MathSin(theta);
      wr = 1.0;
      wi = 0.0;
      for(ii=1; ii<=mmax/2; ii++)
        {
         m=2*ii-1;
         for(jj=0; jj<=(n-m)/istep; jj++)
           {
            i = m+jj*istep;
            j = i+mmax;
            tempr = wr*a[j-1]-wi*a[j];
            tempi = wr*a[j]+wi*a[j-1];
            a[j-1]= a[i-1]-tempr;
            a[j]=a[i]-tempi;
            a[i-1]+=tempr;
            a[i]+=tempi;
           }
         wtemp=wr;
         wr = wr*wpr-wi*wpi+wr;
         wi = wi*wpr+wtemp*wpi+wi;
        }
      mmax=istep;
     }
   b[0] = a[1];
   a[1] = 0.0;
   b[1] = 0.0;
   for(jj=1; jj<=tn/2; jj++)
     {
      j=2*jj+1;
      rep = 0.5*(a[j-1]+a[2*tn+1-j]);
      rem = 0.5*(a[j-1]-a[2*tn+1-j]);
      aip = 0.5*(a[j]+a[2*tn+2-j]);
      aim = 0.5*(a[j]-a[2*tn+2-j]);
      a[j-1]=rep;
      a[j]=aim;
      a[2*tn+1-j] = rep;
      a[2*tn+2-j] = -aim;
      b[j-1]=aip;
      b[j]=-rem;
      b[2*tn+1-j] = aip;
      b[2*tn+2-j] = rem;
     }
//----
   return;
  }
//+------------------------------------------------------------------+
