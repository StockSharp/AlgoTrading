//+------------------------------------------------------------------+
//|                                               IFS_Fern_color.mq5 |
//|                        Copyright 2011, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#include <cIntBMP.mqh>
//-- Barnsley Fern IFS coefficients
double IFS_a[4] = {0.00,  0.85,  0.20,  -0.15};
double IFS_b[4] = {0.00,  0.04, -0.26,   0.28};
double IFS_c[4] = {0.00, -0.04,  0.23,   0.26};
double IFS_d[4] = {0.16,  0.85,  0.22,   0.24};
double IFS_e[4] = {0.00,  0.00,  0.00,   0.00};
double IFS_f[4] = {0.00,  1.60,  1.60,   0.00};
double IFS_p[4] = {10,     850,    70,     70};
//-- Palette
uchar Palette[23*3]=
  {
   0x00,0x00,0x00,0x02,0x0A,0x06,0x03,0x11,0x0A,0x0B,0x1E,0x0F,0x0C,0x4C,0x2C,0x1C,0x50,0x28,
   0x2C,0x54,0x24,0x3C,0x58,0x20,0x4C,0x5C,0x1C,0x70,0x98,0x6C,0x38,0xBC,0xB0,0x28,0xCC,0xC8,
   0x4C,0xB0,0x98,0x5C,0xA4,0x84,0xBC,0x68,0x14,0xA8,0x74,0x28,0x84,0x8C,0x54,0x94,0x80,0x40,
   0x87,0x87,0x87,0x9F,0x9F,0x9F,0xC7,0xC7,0xC7,0xDF,0xDF,0xDF,0xFC,0xFC,0xFC
  };
//+------------------------------------------------------------------+
//| CIFS class                                                       |
//+------------------------------------------------------------------+
class CIFS
  {
protected:
   cIntBMP           m_bmp;
   int               m_xsize;
   int               m_ysize;
   uchar             m_virtual_screen[];
   double            m_scale;
   double            m_probs[8];

public:
                    ~CIFS()                          { m_bmp.Delete("bmpimg",true); };
   void              Create(int x_size,int y_size,uchar col);
   void              Render(double scale,bool back);
   void              ShowBMP(bool back);
protected:
   void              VS_Prepare(int x_size,int y_size,uchar col);
   void              VS_Fill(uchar col);
   void              VS_PutPixel(int px,int py,uchar col);
   uchar             VS_GetPixel(int px,int py);
   int               GetPalColor(uchar index);
   int               RGB256(int r,int g,int b) const {return(r+256*g+65536*b);      }
   void              PrepareProbabilities();
   void              RenderIFSToVirtualScreen();
   void              VirtualScreenToBMP();
  };
//+------------------------------------------------------------------+
//| Create method                                                    |
//+------------------------------------------------------------------+
void CIFS::Create(int x_size,int y_size,uchar col)
  {
   m_bmp.Create(x_size,y_size,col);
   VS_Prepare(x_size,y_size,col);
   PrepareProbabilities();
  }
//+------------------------------------------------------------------+
//| Prepares virtual screen                                          |
//+------------------------------------------------------------------+
void CIFS::VS_Prepare(int x_size,int y_size,uchar col)
  {
   m_xsize=x_size;
   m_ysize=y_size;
   ArrayResize(m_virtual_screen,m_xsize*m_ysize);
   VS_Fill(col);
  }
//+------------------------------------------------------------------+
//| Fills the virtual screen with specified color                    |
//+------------------------------------------------------------------+
void CIFS::VS_Fill(uchar col)
  {
   for(int i=0; i<m_xsize*m_ysize; i++) {m_virtual_screen[i]=col;}
  }
//+------------------------------------------------------------------+
//| Returns the color from palette                                   |
//+------------------------------------------------------------------+
int CIFS::GetPalColor(uchar index)
  {
   int ind=index;
   if(ind<=0) {ind=0;}
   if(ind>22) {ind=22;}
   uchar r=Palette[3*(ind)];
   uchar g=Palette[3*(ind)+1];
   uchar b=Palette[3*(ind)+2];
   return(RGB256(r,g,b));
  }
//+------------------------------------------------------------------+
//| Draws a pixel on the virtual screen                              |
//+------------------------------------------------------------------+
void CIFS::VS_PutPixel(int px,int py,uchar col)
  {
   if (px<0) return;
   if (py<0) return;
   if (px>m_xsize) return;
   if (py>m_ysize) return;
    int pos=m_xsize*py+px;
   if(pos>=ArraySize(m_virtual_screen)) return;
   m_virtual_screen[pos]=col;
  }
//+------------------------------------------------------------------+
//| Gets the pixel "color" from the virtual screen                   |
//+------------------------------------------------------------------+
uchar CIFS::VS_GetPixel(int px,int py)
  {
   if (px<0) return(0);
   if (py<0) return(0);
   if (px>m_xsize) return(0);
   if (py>m_ysize) return(0);
    int pos=m_xsize*py+px;
   if(pos>=ArraySize(m_virtual_screen)) return(0);
   return(m_virtual_screen[pos]);
  }
//+------------------------------------------------------------------+
//| Prepare the cumulative probabilities array                       |
//+------------------------------------------------------------------+
void CIFS::PrepareProbabilities()
  {
   double m=0;
   for(int i=0; i<ArraySize(IFS_p); i++)
     {
      m_probs[i]=IFS_p[i]+m;
      m=m+IFS_p[i];
     }
  }
//+------------------------------------------------------------------+
//| Renders the IFS set to the virtual screen                        |
//+------------------------------------------------------------------+
void CIFS::RenderIFSToVirtualScreen()
  {
   double x=0,y=0;
   double x0=0;
   double y0=0;
   uint iterations= uint (MathRound(100000+100*MathPow(m_scale,2)));

   for(uint i=0; i<iterations; i++)
     {
      double prb=1000*(rand()/32767.0);

      for(int k=0; k<ArraySize(IFS_p); k++)
        {
         if(prb<=m_probs[k])
           {
            x = IFS_a[k] * x0 + IFS_b[k] * y0 + IFS_e[k];
            y = IFS_c[k] * x0 + IFS_d[k] * y0 + IFS_f[k];

            int scX = int (MathRound(m_xsize/2 + (x-0)*m_scale));
            int scY = int (MathRound(m_ysize/2 + (y-5)*m_scale));

            if(scX>=0 && scX<m_xsize && scY>=0 && scY<m_ysize)
              {
               uchar c=VS_GetPixel(scX,scY);
               if(c<255) c=c+1;
               VS_PutPixel(scX,scY,c);
              }
            break;
           }
         x0 = x;
         y0 = y;
        }
     }
  }
//+------------------------------------------------------------------+
//| Copies virtual screen to BMP                                     |
//+------------------------------------------------------------------+
void CIFS::VirtualScreenToBMP()
  {
   for(int i=0; i<m_xsize; i++)
     {
      for(int j=0; j<m_ysize; j++)
        {
         uchar colind=VS_GetPixel(i,j);
         int xcol=GetPalColor(colind);
         if(colind==0) xcol=0x00;
         //if(colind==0) xcol=0xFFFFFF;
         m_bmp.DrawDot(i,j,xcol);
        }
     }
  }
//+------------------------------------------------------------------+
//| Shows BMP image on the chart                                     |
//+------------------------------------------------------------------+
void CIFS::ShowBMP(bool back)
  {
   m_bmp.Save("bmpimg",true);
   m_bmp.Show(0,0,"bmpimg","Fern");
   ObjectSetInteger(0,"Fern",OBJPROP_BACK,back);
  }
//+------------------------------------------------------------------+
//| Render method                                                    |     
//+------------------------------------------------------------------+
void CIFS::Render(double scale,bool back)
  {
   m_scale=scale;
   VS_Fill(0);
   RenderIFSToVirtualScreen();
   VirtualScreenToBMP();
   ShowBMP(back);
  }

static int gridmode;
CIFS fern;
int currentscale=50;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
void OnInit()
  {
//-- get grid mode
   gridmode= int (ChartGetInteger(0,CHART_SHOW_GRID,0));
//-- disable grid
   ChartSetInteger(0,CHART_SHOW_GRID,0);
//-- create bmp
   fern.Create(800,800,0x00);
//-- show as backround image
   fern.Render(currentscale,true);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int r)
  {
//-- restore grid mode
   ChartSetInteger(0,CHART_SHOW_GRID,gridmode); 
//-- delete Fern object
   ObjectDelete(0,"Fern");
 }
//+------------------------------------------------------------------+
//| Expert OnChart event handler                                     |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,         // Event identifier  
                  const long& lparam,   // Event parameter of long type
                  const double& dparam, // Event parameter of double type
                  const string& sparam  // Event parameter of string type
                  )
  {
//--- click on the graphic object
   if(id==CHARTEVENT_OBJECT_CLICK)
     {
      Print("Click event on graphic object with name '"+sparam+"'");
      if(sparam=="Fern")
        {
         // increase scale coefficient (zoom)
         currentscale=int (currentscale*1.1);
         fern.Render(currentscale,true);
        }
     }
  }
//+------------------------------------------------------------------+