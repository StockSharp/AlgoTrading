//+------------------------------------------------------------------+
//|                                                     Controls.mqh |
//|                                                  2011, KTS Group |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "2011, KTS Group"


#define DELAY_BTN_CLICK     (50) //(ms), for clickable buttons only
#define BTN_CAPTION_SHIFT   (1)
#define INC_EVENT           (0)
#define DEC_EVENT           (1)
//---SpinEdit buttons position
#define BP_RIGHT            (0)
#define BP_LEFT_RIGHT       (1)
//---SpinButton state defines
#define BS_ENABLED          (1)
#define BS_DISABLED         (2)
#define BS_LASTPRESSED      (3)
//---Trade buttons defines
#define LBL_LARGE           (0)
#define LBL_SMALL           (1)
#define BTN_FACE_PRICEUP    (0)
#define BTN_FACE_PRICEDOWN  (1)
#define BTN_FACE_PRICENONE  (2)
#define PRICE_CHANGE_NONE   (0)
#define PRICE_CHANGE_UP     (1)
#define PRICE_CHANGE_DOWN   (2)
//---Default font settings
#define DEFAULT_FONTNAME    "Tahoma"
#define DEFAULT_FONTSIZE    (8)
#define DEFAULT_FONTCOLOR   Red
//---Object type defines
#define CTRL_UNKNOW         (0)
#define CTRL_CHECKBOX       (1)
#define CTRL_BUTTON         (2)
#define CTRL_RADIOBUTTON    (11)
#define CTRL_RADIOGROUP     (24)
#define CTRL_UP_SPINBUTTON  (8)
#define CTRL_DN_SPINBUTTON  (9)
#define CTRL_PANEL          (3)
#define CTRL_EDIT           (4)
#define CTRL_SPINEDIT       (5)
#define CTRL_LABEL          (6)
#define CTRL_IMAGE          (10)
#define CTRL_GAUGE          (7)
#define CTRL_TAB_HEADER     (12)
#define CTRL_TABCONTROL     (13)
#define CTRL_FRAME          (14)
#define CTRL_FORM           (15)
#define RS_IMGS             (16)
#define RS_WAVS             (17)
#define RS_FONT             (18)
#define CTRL_CAPTION        (19)
#define CTRL_CMD_BUTTON     (20)
#define SKIN_IMAGE_SECTION  (21)
#define SKIN_FONT_SECTION   (22)
#define SKIN                (23)
//---Skin sections defines
#define CHECKBOX_SECTION    "rs_chb"
#define RADIOBTN_SECTION    "rs_rbtn"
#define BUTTON_SECTION      "rs_btn"
#define SPIN_SECTION        "rs_spin"
#define SPIN1_SECTION       "rs_spin1"
#define FRAME_SECTION       "rs_frame"
#define PANEL_SECTION       "rs_pnl"
#define TABS_SECTION        "rs_tabs"
#define CMD_BTNBUY_SECTION  "rs_cmdbuy"
#define CMD_BTNSELL_SECTION "rs_cmdsell"
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum ENUM_VPANEL_POS
  {
   V_ALIGN_CUSTOM_POS =0,        //Custom
   V_ALIGN_TOP_POS    =1,        //Top 
   V_ALIGN_MIDLLE_POS =2,        //Midlle
   V_ALIGN_BOTTOM_POS =3,        //Bottom
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum ENUM_HPANEL_POS
  {
   H_ALIGN_CUSTOM_POS  =0,       //Custom
   H_ALIGN_RIGHT_POS   =1,       //Right 
   H_ALIGN_MIDLLE_POS  =2,       //Midlle
   H_ALIGN_LEFT_POS    =3,       //Left
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
struct Align
  {
   ENUM_VPANEL_POS   v_Align;
   ENUM_HPANEL_POS   h_Align;
   int               Top;
   int               Left;
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
struct WAVList
  {
   string            WAVFile;
   int               Index;     // any integer value
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
struct FontSettings
  {
   string            FontName;
   int               FontSize;
   color             Clr1;
   color             Clr2;
   color             Clr3;
   color             Clr4;
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum ENUM_TABS_ORIENTATION
  {
   TO_TOP            =0,
   TO_BOTTOM         =1,
   TO_LEFT           =2,
   TO_RIGHT          =3,
  };

#include <Object.mqh>
#include <Arrays\ArrayObj.mqh>
#include <Arrays\ArrayString.mqh>

//forward declaration
class CWAVbox;
class CContainer;
//---RESOURCE RUTINES

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CResource: public CObject
  {
private:
   string            m_ResourcesName;
public:
                     CResource(void);
   void              ResourcesName(string f_Name) {m_ResourcesName=f_Name;}
   string            ResourcesName() {return(m_ResourcesName);}
   virtual int       Type() {return(CTRL_UNKNOW);}
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CResource::CResource(void)
  {
   m_ResourcesName=" ";
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CFontResource:public CResource
  {
private:
   FontSettings      Font;
public:
                     CFontResource(void);
   void              AddResource(string f_FontName,
                                 int f_FontSize,
                                 color f_Clr1,
                                 color f_Clr2=CLR_NONE,
                                 color f_Clr3=CLR_NONE,
                                 color f_Clr4=CLR_NONE);
   void              GetFont(FontSettings &s_Font);
   int               Type() {return(RS_FONT);}
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CFontResource::CFontResource(void)
  {
   Font.FontName=DEFAULT_FONTNAME;
   Font.FontSize=DEFAULT_FONTSIZE;
   Font.Clr1    =DEFAULT_FONTCOLOR;
   Font.Clr2    =CLR_NONE;
   Font.Clr3    =CLR_NONE;
   Font.Clr4    =CLR_NONE;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CFontResource::AddResource(string f_FontName,
                                int f_FontSize,
                                color f_Clr1,
                                color f_Clr2=-1,
                                color f_Clr3=-1,
                                color f_Clr4=-1)
  {
   Font.FontName=f_FontName;
   Font.FontSize=f_FontSize;
   Font.Clr1    =f_Clr1;
   Font.Clr2    =f_Clr2;
   Font.Clr3    =f_Clr3;
   Font.Clr4    =f_Clr4;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CFontResource::GetFont(FontSettings &s_Font)
  {
   s_Font.FontName =Font.FontName;
   s_Font.FontSize =Font.FontSize;
   s_Font.Clr1     =Font.Clr1;
   s_Font.Clr2     =Font.Clr2;
   s_Font.Clr3     =Font.Clr3;
   s_Font.Clr4     =Font.Clr4;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CImgResource: public CResource
  {
private:
   CArrayString     *ResourcesList;
public:
                     CImgResource(void);
                    ~CImgResource(void);
   bool              AddResource(string f_ResDescription);
   CArrayString      *GetResList(void) {return(ResourcesList);}
   int               Type() {return(RS_IMGS);}
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CImgResource::CImgResource(void)
  {
   if((ResourcesList=new CArrayString)==NULL)
     {
      Print("::CResource create error"); return;
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CImgResource::~CImgResource(void)
  {
   if(ResourcesList!=NULL) delete ResourcesList;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CImgResource::AddResource(string f_ResDescription)
  {
   bool result=false;
   if(ResourcesList!=NULL)
     {
      result=ResourcesList.Add(f_ResDescription);
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CWavResource: public CResource
  {
protected:
   CWAVbox          *Player;
public:
                     CWavResource(void);
                    ~CWavResource(void);
   void              AddResource(WAVList &a_List[]);
   CWAVbox           *GetPlayer() {return(Player);}
   int               Type() {return(RS_WAVS);}
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CWavResource::CWavResource(void)
  {
   Player=NULL;
   if((Player=new CWAVbox)==NULL) Print("::CWavResource create error");
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CWavResource::~CWavResource(void)
  {
   if(Player!=NULL) delete Player;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CWavResource::AddResource(WAVList &a_List[])
  {
   if(Player!=NULL)
     {
      Player.SetPlayList(a_List);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CResources: public CObject
  {
private:
   CArrayObj        *ResourcesList;
public:
                     CResources(void);
                    ~CResources(void);
   CWavResource     *AddWavResource(string f_Name,CWavResource *p_Res);
   CFontResource    *AddFontResource(string f_Name,CFontResource *p_Res);
   CImgResource     *AddImgsResource(string f_Name,CImgResource *p_Res);
   CFontResource    *GetFontResource(string f_Name);
   CImgResource     *GetImgsResource(string f_Name);
   CWAVbox          *GetWavResource(string f_Name);
   virtual bool      CreateResources(uchar f_SkinSection=0) {return(false);}
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CResources::CResources(void)
  {
   if((ResourcesList=new CArrayObj)==NULL) Print("::CResources create error");
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CResources::~CResources(void)
  {
   if(ResourcesList!=NULL) delete ResourcesList;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CWavResource *CResources::AddWavResource(string f_Name,CWavResource *p_Res)
  {
   CWavResource *res=p_Res;
   if(res!=NULL)
     {
      res.ResourcesName(f_Name);
      ResourcesList.Add(res);
      return(res);
     }
   return(NULL);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CFontResource *CResources::AddFontResource(string f_Name,CFontResource *p_Res)
  {
   CFontResource *res=p_Res;
   if(res!=NULL)
     {
      res.ResourcesName(f_Name);
      ResourcesList.Add(res);
      return(res);
     }
   return(NULL);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CImgResource *CResources::AddImgsResource(string f_Name,CImgResource *p_Res)
  {
   CImgResource *res=p_Res;
   if(res!=NULL)
     {
      res.ResourcesName(f_Name);
      ResourcesList.Add(res);
      return(res);
     }
   return(NULL);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CFontResource *CResources::GetFontResource(string f_Name)
  {
   CObject       *current=NULL;
   CFontResource *res=NULL;
   for(int i=0;i<ResourcesList.Total();i++)
     {
      current=ResourcesList.At(i);
      if(current.Type()!=RS_FONT) continue;
      else
        {
         res=current;
         if(res.ResourcesName()==f_Name) return(res);
        }
     }
   return(NULL);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CImgResource *CResources::GetImgsResource(string f_Name)
  {
   CObject      *current=NULL;
   CImgResource *res=NULL;
   for(int i=0;i<ResourcesList.Total();i++)
     {
      current=ResourcesList.At(i);
      if(current.Type()!=RS_IMGS) continue;
      else
        {
         res=current;
         if(res.ResourcesName()==f_Name) return(res);
        }
     }
   return(NULL);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CWAVbox *CResources::GetWavResource(string f_Name="CTRLS")
  {
   CObject      *current=NULL;
   CWavResource *res=NULL;
   for(int i=0;i<ResourcesList.Total();i++)
     {
      current=ResourcesList.At(i);
      if(current.Type()!=RS_WAVS) continue;
      else
        {
         res=current;
         if(res.ResourcesName()==f_Name) return(res.GetPlayer());
        }
     }
   return(NULL);
  }  
//+------------------------------------------------------------------+
//| NOT SUPPORTED                                                    |
//+------------------------------------------------------------------+
class CSkinSection:public CObject
  {
public:
   string            m_SectionName;
   void              SkinSectionName(string f_SectionName) {m_SectionName=f_SectionName;}
   string            SkinSectionName() {return(m_SectionName);}
   virtual int       Type() {return(CTRL_UNKNOW);}
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CSkinImgSection:public CSkinSection
  {
private:
   CArrayString     *Imgs;
public:
                     CSkinImgSection(void);
   bool              AddImage(string f_ImagePath);
   CArrayString     *GetSection(void) {return(Imgs);}
   int               Type() {return(SKIN_IMAGE_SECTION);}
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CSkinImgSection::CSkinImgSection(void)
  {
   Imgs=NULL;
   if((Imgs=new CArrayString)==NULL)
     {
      printf("Object: 'CSkinImgSection imgs array' create error");
      return;
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CSkinImgSection::AddImage(string f_ImagePath)
  {
   return((bool)Imgs.Add(f_ImagePath));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CSkinFontSection:public CSkinSection
  {
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CSkin: public CObject
  {
private:
   string            m_SkinName;
   CArrayObj        *Sections;
public:
                     CSkin(void);
                    ~CSkin(void);
   void              SkinName(string f_Name) {m_SkinName=f_Name;}
   string            SkinName() {return(m_SkinName);}
   int               Type() {return(SKIN);}
   CSkinImgSection *AddImgsSkinSection(string f_skin_section_name);
   CArrayString     *GetSkinImgsSection(string f_skin_section_name);
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CSkin::CSkin(void)
  {
   m_SkinName="default";
   Sections=NULL;
   if((Sections=new CArrayObj)==NULL)
     {
      printf("Object: 'Skin sections array' create error");
      return;
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CSkin::~CSkin(void)
  {
   if(Sections!=NULL)
     {
      delete Sections;
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CSkinImgSection *CSkin::AddImgsSkinSection(string f_skin_section_name)
  {
   CSkinImgSection *Section=new CSkinImgSection;
   if(Section!=NULL)
     {
      Section.SkinSectionName(f_skin_section_name);
      Sections.Add(Section);
      return(Section);
     }
   return(NULL);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CArrayString *CSkin::GetSkinImgsSection(string f_skin_section_name)
  {
   int _total=Sections.Total();
   if(_total>0)
     {
      for(int i=0;i<_total;i++)
        {
         CSkinImgSection *Section=Sections.At(i);
         if(Section!=NULL && (Section.Type()==SKIN_IMAGE_SECTION && Section.SkinSectionName()==f_skin_section_name))
           {
            return(Section.GetSection());
           }
        }
     }
   return(NULL);
  }
//+------------------------------------------------------------------+
//|Base class for interface objects                                  |
//+------------------------------------------------------------------+
class CControl: public CObject
  {
private:

protected:
   string            m_Name;
   ENUM_OBJECT       m_Type;
   int               m_Top;
   int               m_Left;
   bool              m_Selected;
   bool              m_Selectable;
   ENUM_BASE_CORNER  m_Corner;
   long              m_Chart;
   int               m_TimeFramesFlag;
   int               m_LastTimeFramesFlag;
   int               m_Window;
   bool              m_DrawOnBackground;
   int               m_FileHandle;
   int               m_Index;
public:
                     CControl(void);
                    ~CControl(void);

   virtual void      InitControl(long Chart=0,
                                 int  Window=0,
                                 int  Flags=WRONG_VALUE,
                                 ENUM_BASE_CORNER Corner=CORNER_LEFT_UPPER,
                                 bool Selected=false,
                                 bool Selectable=false,
                                 bool DrawOnBackground=false);

   virtual bool      Create(string Name,ENUM_OBJECT Type,int Top,int Left,bool f_DefaultInit=true);
   int               Width() const;
   int               Height()const;
   virtual bool      SetWidth(const  int Size)  {return(false);}
   virtual bool      SetHeight(const int Size)  {return(false);}
   virtual bool      SetTop(const  int Size);
   virtual bool      SetLeft(const int Size);
   void              SetName(const string f_Name) {m_Name=f_Name;}
   virtual bool      ChangeControlPosition(int f_TopShift,int f_LeftShift);
   virtual bool      SetColor(color f_new_color);
   int               Top() const;
   int               Left() const;
   bool              DrawAsBackGround(bool Draw);
   bool              Selected(bool Select);
   bool              Selectable(bool Select);
   virtual bool      TimeFrames(int Flag);
   virtual bool      Visible() {return((bool)(m_TimeFramesFlag!=OBJ_NO_PERIODS));}
   virtual bool      Visible(bool f_State);
   virtual bool      Enabled(bool f_State) {return(false);}
   bool              SetCorner(const ENUM_BASE_CORNER Corner);
   virtual void      SetIndex(const int f_index) {m_Index=f_index;}
   virtual int       Index(void) const {return((int)m_Index);}
   string            Name() const {return(m_Name);}
   virtual bool      SaveControlState(int f_file_handle);
   virtual bool      LoadControlState(int f_file_handle,string f_Name,ENUM_OBJECT f_Type);
   virtual int       Type() {return(CTRL_UNKNOW);}
   long              Chart(){return((long)m_Chart);}
   void              Chart(long f_Chart){m_Chart=f_Chart;}
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CControl::CControl(void)
  {
   m_Name=NULL;
   m_Top=0;
   m_Left=0;
   m_Selected=false;
   m_Selectable=false;
   m_Corner=CORNER_LEFT_UPPER;
   m_Chart=-1;
   m_LastTimeFramesFlag=m_TimeFramesFlag=WRONG_VALUE;
   m_Window=-1;
   m_DrawOnBackground=false;
   m_FileHandle=NULL;
   m_Index=-1;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CControl::~CControl(void)
  {
   bool result;
   if(result=ObjectDelete(m_Chart,m_Name))
     {
      m_Chart=-1;
      m_Window=-1;
      m_Name=NULL;
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CControl::InitControl(long Chart=0,
                           int Window=0,
                           int Flags=WRONG_VALUE,
                           ENUM_BASE_CORNER Corner=CORNER_LEFT_UPPER,
                           bool Selected=false,
                           bool Selectable=false,
                           bool DrawOnBackground=false)
  {
   m_Corner=Corner;
   m_LastTimeFramesFlag=m_TimeFramesFlag=Flags;
   m_Window=Window;
   m_Chart=Chart;
   m_Selected=Selected;
   m_Selectable=Selectable;
   m_DrawOnBackground=DrawOnBackground;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CControl::Create(string Name,ENUM_OBJECT Type,int Top,int Left,bool f_DefaultInit=true)
  {
   if(f_DefaultInit) InitControl();
   m_Name  =Name;
   m_Type  =Type;
   m_Top=Top;
   m_Left=Left;
   bool result=ObjectCreate(m_Chart,m_Name,m_Type,m_Window,0,0.0);
   if(result)
     {
      result&=ObjectSetInteger(m_Chart,m_Name,OBJPROP_TIMEFRAMES,m_TimeFramesFlag);
      result&=ObjectSetInteger(m_Chart,m_Name,OBJPROP_SELECTABLE,m_Selectable);
      result&=ObjectSetInteger(m_Chart,m_Name,OBJPROP_SELECTED,m_Selected);
      result&=ObjectSetInteger(m_Chart,m_Name,OBJPROP_BACK,m_DrawOnBackground);
      result&=ObjectSetInteger(m_Chart,m_Name,OBJPROP_CORNER,m_Corner);
      result&=ObjectSetInteger(m_Chart,m_Name,OBJPROP_YDISTANCE,Top);
      result&=ObjectSetInteger(m_Chart,m_Name,OBJPROP_XDISTANCE,Left);
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int CControl::Width(void) const
  {
   if(m_Chart==-1) return(0);
   return((int)ObjectGetInteger(m_Chart,m_Name,OBJPROP_XSIZE));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int CControl::Height(void) const
  {
   if(m_Chart==-1) return(0);
   return((int)ObjectGetInteger(m_Chart,m_Name,OBJPROP_YSIZE));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int CControl::Top(void) const
  {
   if(m_Chart==-1) return(0);
   return((int)ObjectGetInteger(m_Chart,m_Name,OBJPROP_YDISTANCE));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CControl::SetTop(const int Size)
  {
   bool result=ObjectSetInteger(m_Chart,m_Name,OBJPROP_YDISTANCE,Size);
   m_Top=Size;
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CControl::SetLeft(const int Size)
  {
   bool result=ObjectSetInteger(m_Chart,m_Name,OBJPROP_XDISTANCE,Size);
   m_Left=Size;
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CControl::ChangeControlPosition(int f_TopShift,int f_LeftShift)
  {
   return(SetTop(m_Top+f_TopShift)&SetLeft(m_Left+f_LeftShift));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CControl::SetColor(color f_new_color)
  {
   if(m_Chart==-1) return(false);
   return(ObjectSetInteger(m_Chart,m_Name,OBJPROP_COLOR,f_new_color));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CControl::Visible(bool f_State)
  {
   bool result=true;

   if(f_State) result&=TimeFrames(m_LastTimeFramesFlag);
   else
     {
      m_LastTimeFramesFlag=m_TimeFramesFlag;
      result&=TimeFrames(OBJ_NO_PERIODS);
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int CControl::Left(void) const
  {
   if(m_Chart==-1) return(0);
   return((int)ObjectGetInteger(m_Chart,m_Name,OBJPROP_XDISTANCE));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CControl::DrawAsBackGround(bool Draw)
  {
   if(m_Chart==-1) return(false);
   return(ObjectSetInteger(m_Chart,m_Name,OBJPROP_BACK,Draw));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CControl::Selectable(bool Select)
  {
   if(m_Chart==-1) return(false);
   return(ObjectSetInteger(m_Chart,m_Name,OBJPROP_SELECTABLE,Select));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CControl::Selected(bool Select)
  {
   if(m_Chart==-1) return(false);
   return(ObjectSetInteger(m_Chart,m_Name,OBJPROP_SELECTED,Select));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CControl::TimeFrames(int Flag)
  {
   if(m_Chart==-1) return(false);
   if(ObjectSetInteger(m_Chart,m_Name,OBJPROP_TIMEFRAMES,Flag))
     {
      m_TimeFramesFlag=Flag;
      return(true);
     }
   return(false);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CControl::SetCorner(const ENUM_BASE_CORNER Corner)
  {
   if(m_Chart==-1) return(false);
   return(ObjectSetInteger(m_Chart,m_Name,OBJPROP_CORNER,Corner));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CControl::SaveControlState(int f_file_handle)
  {
   int len;
   string str;

   if(f_file_handle<=0)                                                                  return(false);
   if(m_Chart==-1)                                                                       return(false);

   if(FileWriteLong(f_file_handle,-1)!=sizeof(long))                                     return(false);
   if(FileWriteInteger(f_file_handle,m_Type,INT_VALUE)!=INT_VALUE)                       return(false);

   str=m_Name;
   len=StringLen(str);
   if(FileWriteInteger(f_file_handle,len,INT_VALUE)!=INT_VALUE)                          return(false);
   if(len!=0) if(FileWriteString(f_file_handle,str,len)!=len)                            return(false);

   if(FileWriteInteger(f_file_handle,(int)m_DrawOnBackground,CHAR_VALUE)!=sizeof(char))  return(false);
   if(FileWriteInteger(f_file_handle,(int)m_Selectable,CHAR_VALUE)!=sizeof(char))        return(false);
   if(FileWriteInteger(f_file_handle,(int)m_TimeFramesFlag,INT_VALUE)!=sizeof(int))      return(false);
   if(FileWriteInteger(f_file_handle,(int)m_Corner,INT_VALUE)!=sizeof(int))              return(false);
   return(true);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CControl::LoadControlState(int f_file_handle,string f_Name,ENUM_OBJECT f_Type)
  {
   int    len;
   string str;
   bool   result;
   int  g_Chart=0;
   int  g_Window=0;
   int  g_Timeframes=WRONG_VALUE;
   bool g_DrawAsBcg;
   bool g_Selectable;
   ENUM_BASE_CORNER g_Corner;
   bool g_Selected=false;

   if(FileReadLong(f_file_handle)!=-1) return(false);
   if(FileReadInteger(f_file_handle,INT_VALUE)!=f_Type) return(false);
   len=FileReadInteger(f_file_handle,INT_VALUE);
   if(len!=0) str=FileReadString(f_file_handle,len);  else str="";
   if(str!=f_Name) return(false);

   g_DrawAsBcg=(char)FileReadInteger(f_file_handle,CHAR_VALUE);
   g_Selectable= (char)FileReadInteger(f_file_handle,CHAR_VALUE);
   g_Timeframes= (int)FileReadInteger(f_file_handle,INT_VALUE);
   g_Corner=(ENUM_BASE_CORNER)FileReadInteger(f_file_handle,INT_VALUE);

   InitControl(g_Chart,g_Window,g_Timeframes,g_Corner,g_Selected,g_Selectable,g_DrawAsBcg);
   result=Create(f_Name,f_Type,0,0,false);

   return(result);
  }
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CFrame:public CControl
  {
private:
   bool              m_Enabled;
   bool              m_Visible;
   CResources       *Resources;
   CContainer       *CtrlsContainer;
public:
                     CFrame(void);
                    ~CFrame(void);
   int               m_LastTop;
   int               m_LastLeft;
   CResources       *GetResObj(void){return(Resources);}
   CContainer       *GetContainer(void){return(CtrlsContainer);}
   bool              Create(CContainer *p_Container=NULL,CResources *p_rs=NULL);
   int               Top(void) {return(m_Top);}
   int               Left(void){return(m_Left);}
   virtual bool      Visible(bool f_State);
   virtual bool      Visible() const { return(m_Visible);}
   virtual bool      Enabled(bool f_State);
   virtual bool      Enabled() const {return(m_Enabled);}
   virtual bool      Hide(CArrayString *p_Exclusion=NULL);
   virtual bool      Restore();
   virtual bool      TimeFrames(int f_Flag);
   virtual bool      ChangePosition(int f_TopShift,int f_LeftShift);
   virtual void      OnVisible(void)  {}
   virtual void      OnEnable(void)   {}
   int               Type() {return(CTRL_FRAME);}
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CFrame::CFrame(void)
  {
   m_Top=m_LastTop=0;
   m_Left=m_LastLeft=0;
   m_Enabled=m_Visible=true;
   Resources=NULL;
   CtrlsContainer=NULL;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CFrame::~CFrame(void)
  {
   if(CtrlsContainer!=NULL) delete CtrlsContainer;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CFrame::Create(CContainer *p_Container=NULL,CResources *p_rs=NULL)
  {
   bool result=true;
   if(p_Container==NULL)
     {
      if((CtrlsContainer=new CContainer)==NULL)return(false);
     }
   else CtrlsContainer=p_Container;

   if(p_rs!=NULL) Resources=p_rs;
   return(result);
  }
//+------------------------------------------------------------------+
//|Changing visibility for all controls                              |
//+------------------------------------------------------------------+
bool CFrame::Visible(bool f_State)
  {
   bool result=true;
   int _total=CtrlsContainer.TotalItems();
   if(_total>0) result&=CtrlsContainer.Visible(f_State);
   if(result)
     {
      m_Visible=f_State;
      OnVisible();
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|Changing enabling/disabling mode for all controls                 |
//+------------------------------------------------------------------+
bool CFrame::Enabled(bool f_State)
  {
   bool result=true;
   int _total=CtrlsContainer.TotalItems();
   if(_total>0) result&=CtrlsContainer.Enabled(f_State);
   if(result)
     {
      m_Enabled=f_State;
      OnEnable();
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CFrame::Hide(CArrayString *p_Exclusion=NULL)
  {
   bool result=true;
   int _total=CtrlsContainer.TotalItems();
   if(_total>0) result&=(p_Exclusion!=NULL)?CtrlsContainer.Visible(false,p_Exclusion):CtrlsContainer.Visible(false);
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CFrame::Restore(void)
  {
   bool result=true;
   int _total=CtrlsContainer.TotalItems();
   if(_total>0)
     {
      if(m_Top!=m_LastTop || m_Left!=m_LastLeft)
        {
         result&=ChangePosition(m_Top-m_LastTop,m_Left-m_LastLeft);
        }
      result&=CtrlsContainer.Visible(true);
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CFrame::TimeFrames(int f_Flag)
  {
   bool result=CtrlsContainer.TimeFrames(f_Flag);
   result&=TimeFrames(f_Flag);
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CFrame::ChangePosition(int f_TopShift,int f_LeftShift)
  {
   return((bool)CtrlsContainer.ChangeControlPosition(f_TopShift,f_LeftShift));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CWAVbox:public CObject
  {
private:
   bool              m_UseSound;
   WAVList           m_PlayList[];
public:
                     CWAVbox(void);
   void              Play(int f_ControlType);
   void              SetPlayList(WAVList &p_PlayList[]);
   bool              On()const {return(m_UseSound);}
   void              On(bool f_State) {m_UseSound=f_State;}
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CWAVbox::CWAVbox(void)
  {
   m_UseSound=false;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CWAVbox::Play(int f_ControlType)
  {
   if(m_UseSound)
     {
      string WAVFile=" ";
      for(int i=0;i<ArraySize(m_PlayList);i++)
        {
         if(m_PlayList[i].Index==f_ControlType)
           {
            WAVFile=m_PlayList[i].WAVFile;
            break;
           }
        }
      if(WAVFile!=" ") PlaySound(WAVFile);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CWAVbox::SetPlayList(WAVList &p_PlayList[])
  {
   int _Size=ArraySize(p_PlayList);
   if(ArrayResize(m_PlayList,_Size)>0)
     {
      for(int i=0;i<_Size;i++)
        {
         m_PlayList[i].Index=p_PlayList[i].Index;
         m_PlayList[i].WAVFile=p_PlayList[i].WAVFile;
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CContainer:public CControl
  {
private:
   CArrayObj        *ControlsList;
   int               m_Flag;
   bool              m_Visible;
   bool              m_Enabled;
   bool              IsControlProtected(string f_name);
   CArrayString     *Exclusion;
protected:
   virtual bool      DestroyItem(CControl *Control);
public:
                     CContainer(void);
                    ~CContainer(void);
   CArrayObj        *Container() {return(ControlsList);}
   virtual bool      AddItem(CObject *p_Object=NULL);
   bool              Resize(int f_NewSize) {return((bool)ControlsList.Resize(f_NewSize));}
   CControl         *Item(int f_Index);
   bool              Visible(void) const {return(m_Visible);}
   bool              Visible(bool f_State);
   bool              Visible(bool f_State,CArrayString *arExclusion);
   bool              Enabled(void) const {return(m_Enabled);}
   bool              Enabled(bool f_State);
   void              FreeMode(bool f_State) {ControlsList.FreeMode(f_State);}
   int               TotalItems(void) {return((int)ControlsList.Total());}
   virtual bool      ChangeControlPosition(int f_TopShift,int f_LeftShift);
   virtual bool      TimeFrames(int f_Flag);
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CContainer::CContainer(void)
  {
   ControlsList=new CArrayObj;
   m_Flag=OBJ_ALL_PERIODS;
   m_Visible=true;
   m_Enabled=true;
   Exclusion=NULL;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CContainer::~CContainer(void)
  {
   bool result=true;
   int _total=ControlsList.Total();
   if(ControlsList!=NULL)
     {
      for(int i=0;i<_total;i++)
        {
         result&=DestroyItem(ControlsList.At(i));
        }
      delete ControlsList;
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CContainer::AddItem(CObject *p_Object=NULL)
  {
   bool result=false;
   CControl *obj=p_Object;
   if(p_Object!=NULL)
     {
      obj.Visible(m_Visible);
      result=ControlsList.Add(p_Object);
      obj.SetIndex(ControlsList.Total()-1);
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CControl *CContainer::Item(int f_Index)
  {
   return(ControlsList.At(f_Index));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CContainer::DestroyItem(CControl *Control)
  {
   if(CheckPointer(Control)!=POINTER_INVALID)
     {
      if(CheckPointer(Control)==POINTER_DYNAMIC)
        {
         delete Control;
         Control=NULL;
         return(true);
        }
     }
   return(false);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CContainer::Visible(bool f_State)
  {
   bool result=true;
   CControl *Control=NULL;
   for(int i=0;i<ControlsList.Total();i++)
     {
      if((Control=ControlsList.At(i))!=NULL) result&=Control.Visible(f_State);
     }
   if(result) m_Visible=f_State;
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CContainer::IsControlProtected(string f_name)
  {
   if(Exclusion!=NULL)
     {
      for(int i=0;i<Exclusion.Total();i++)
        {
         if(Exclusion.At(i)==f_name) return(true);
        }
     }
   return(false);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CContainer::Visible(bool f_State,CArrayString *arExclusion)
  {
   bool result=true;
   CControl *Control=NULL;
   string _ControlName=" ";
   Exclusion=arExclusion;
   int _size=(Exclusion!=NULL)?Exclusion.Total():0;
   int _counter=0;
   for(int i=0;i<ControlsList.Total();i++)
     {
      if((Control=ControlsList.At(i))!=NULL)
        {
         if(_size>0 && _counter!=_size)
           {
            if(IsControlProtected(Control.Name()))
              {
               _counter++;
               continue;
              }
           }
         result&=Control.Visible(f_State);
        }
     }
   if(result) m_Visible=f_State;
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CContainer::Enabled(bool f_State)
  {
   bool result=true;
   CControl *Control=NULL;
   int _total=ControlsList.Total();
   for(int i=0;i<_total;i++)
     {
      if((Control=ControlsList.At(i))!=NULL) result&=Control.Enabled(f_State);
     }
   if(result)m_Enabled=f_State;
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CContainer::ChangeControlPosition(int f_TopShift,int f_LeftShift)
  {
   bool result=true;
   CControl *Control=NULL;
   int _total=ControlsList.Total();
   if(_total>0)
     {
      for(int i=0;i<_total;i++)
        {
         if((Control=ControlsList.At(i))!=NULL) result&=Control.ChangeControlPosition(f_TopShift,f_LeftShift);
        }
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CContainer::TimeFrames(int f_Flag)
  {
   bool result=true;
   int _total=ControlsList.Total();
   for(int i=0; i<_total;i++)
     {
      CControl *obj=ControlsList.At(i);
      if(obj!=NULL) result&=obj.TimeFrames(f_Flag);
     }
   if(result) m_Flag=f_Flag;
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CGroup:public CContainer
  {
public:
   virtual bool      Active(int f_Index);
   ushort            Active();
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CGroup::Active(int f_Index)
  {
   CArrayObj  *Controls=Container();
   CRadioControl *obj=NULL;
   for(int i=0;i<Controls.Total();i++)
     {
      obj=Controls.At(i);
      if(obj!=NULL && obj.Index()!=f_Index && obj.Active() && obj.Active(false)) break;
     }
   return(true);
  }
  
ushort CGroup::Active(void)
  {
   CArrayObj  *Controls=Container();
   CRadioControl *obj=NULL;
   for(ushort i=0;i<Controls.Total();i++)
     {
      obj=Controls.At(i);
      if(obj!=NULL && obj.Active()) return(i);
     }
   return(USHORT_MAX);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CGroupControl:public CControl
  {
private:
   CGroup           *m_Items;
   int               m_Size;
   CWAVbox          *Player;
   CArrayString     *Images;
   FontSettings      m_Font;
public:
   int               m_FontSize;
   string            m_FontName;
   string            m_Enb_ActiveFileName;
   string            m_Enb_DisActiveFileName;
   string            m_Dsb_ActiveFileName;
   string            m_Dsb_DisActiveFileName;
   color             m_ActiveClr;
   color             m_DisActiveClr;
   color             m_DisabledClr;
public:
                     CGroupControl(void);
                    ~CGroupControl(void);
   virtual bool      Create(string f_ControlName,
                            int    f_Top,
                            int    f_Left,
                            int    f_Size,
                            CResources *p_RSGlobal=NULL,
                            string f_RSName=" ",
                            string f_RSFont=" ");
   CGroup            *Items() {return(m_Items);}
   CWAVbox           *WAVbox() {return(Player);}
   CArrayString      *Imgs()   {return(Images);}
   virtual int       Top() const {return(m_Top);}
   virtual int       Left() const {return(m_Left);}
   void              SetFont(string f_FontName,
                             int f_FontSize,
                             color f_ActiveClr,
                             color f_DisActiveClr,
                             color f_DisabledClr);
   void              SetImgs(string f_Enb_ActiveFileName,
                             string f_Enb_DisActiveFileName,
                             string f_Dsb_ActiveFileName=" ",
                             string f_Dsb_DisActiveFileName=" ");
   virtual bool      Enabled(bool f_State)  {return((bool)m_Items.Enabled(f_State));}
   virtual bool      Enabled(void) const    {return((bool)m_Items.Enabled());}
   virtual bool      Visible(void) const    {return((bool)m_Items.Visible());}
   virtual bool      Visible(bool f_State)  {return((bool)m_Items.Visible(f_State));}
   virtual ushort    ActiveIndex() {return(m_Items.Active());}
   virtual bool      InitWAVbox(CWAVbox *p_WAVbox=NULL) {return((Player=p_WAVbox)!=NULL);}
   virtual bool      TimeFrames(int f_Flag) {return((bool)m_Items.TimeFrames(f_Flag));}
   virtual bool      ChangeControlPosition(int f_TopShift,int f_LeftShift);
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CGroupControl::CGroupControl(void)
  {
   m_Items=NULL;
   Player=NULL;
   Images=NULL;
   m_Top=0;
   m_Left=0;
   m_FontSize=8;
   m_FontName="Tahoma";
   m_Enb_ActiveFileName=" ";
   m_Enb_DisActiveFileName=" ";
   m_Dsb_ActiveFileName=" ";
   m_Dsb_DisActiveFileName=" ";
   m_ActiveClr=Red;
   m_DisActiveClr=CLR_NONE;
   m_DisabledClr=CLR_NONE;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CGroupControl::~CGroupControl(void)
  {
   if(m_Items!=NULL) delete m_Items;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CGroupControl::Create(string f_Name,
                           int f_Top,
                           int f_Left,
                           int f_Size,
                           CResources *p_RSGlobal=NULL,
                           string f_RSName=" ",
                           string f_RSFont=" ")
  {
   CResources *rs=p_RSGlobal;
   CFontResource *rs_Font=NULL;
   if(rs!=NULL && f_RSName!=" ")
     {
      Images=rs.GetImgsResource(f_RSName).GetResList();
      SetImgs(Images.At(1),Images.At(2),Images.At(3),Images.At(4));
      Player=rs.GetWavResource();
      if(f_RSFont!=" " && (rs_Font=rs.GetFontResource(f_RSFont))!=NULL)
        {
         rs_Font.GetFont(m_Font);
         SetFont(m_Font.FontName,m_Font.FontSize,m_Font.Clr1,m_Font.Clr2,m_Font.Clr3);
        }
     }

   bool result=((m_Items=new CGroup)!=NULL);
   if(result)
     {
      result&=m_Items.Resize(f_Size);
      m_Top=f_Top;
      m_Left=f_Left;
      m_Size=f_Size;
     }
   SetName(f_Name);
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CGroupControl::SetFont(string f_FontName,
                            int f_FontSize,
                            color f_ActiveClr,
                            color f_DisActiveClr,
                            color f_DisabledClr)
  {
   m_FontName=f_FontName;
   m_FontSize=f_FontSize;
   m_ActiveClr=f_ActiveClr;
   m_DisActiveClr=f_DisActiveClr;
   m_DisabledClr=f_DisabledClr;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CGroupControl::SetImgs(string f_Enb_ActiveFileName,
                            string f_Enb_DisActiveFileName,
                            string f_Dsb_ActiveFileName=" ",
                            string f_Dsb_DisActiveFileName=" ")
  {
   m_Enb_ActiveFileName=f_Enb_ActiveFileName;
   m_Enb_DisActiveFileName=f_Enb_DisActiveFileName;
   m_Dsb_ActiveFileName=(f_Dsb_ActiveFileName!=" ")?f_Dsb_ActiveFileName:f_Enb_ActiveFileName;
   m_Dsb_DisActiveFileName=(f_Dsb_DisActiveFileName!=" ")?f_Dsb_DisActiveFileName:f_Enb_DisActiveFileName;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CGroupControl::ChangeControlPosition(int f_TopShift,int f_LeftShift)
  {
   bool result=m_Items.ChangeControlPosition(f_TopShift,f_LeftShift);
   if(result)
     {
      m_Top=m_Top+f_TopShift;
      m_Left=m_Left+f_LeftShift;
     }
   return(result);
  }
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+

class CLabel:public CControl
  {
private:
   color             m_Enabled_Clr;
   color             m_Disabled_Clr;
   bool              m_Enabled;
   FontSettings      m_Font;
   CResources       *Resources;
public:
                     CLabel(void);
   double            Angle;
   string            Caption;
   string            FontName;
   int               FontSize;
   color             Color;
   ENUM_ANCHOR_POINT AnchorPoint;
   bool              Create(string f_Name,int f_Top,int f_Left,CResources *p_ResObj=NULL,string f_RS_Font=" ");
   bool              SetFont(const string f_FontName,const int f_FontSize);
   bool              SetCaption(const string f_Text);
   bool              SetColor(const color f_Color=Red);
   void              SetDisabledColor(const color f_Color=CLR_NONE) {m_Disabled_Clr=f_Color;}
   bool              SetAngle(const double f_Angle=0.0);
   bool              SetAnchorPoint(ENUM_ANCHOR_POINT f_point=ANCHOR_LEFT_UPPER);
   virtual bool      Enabled(bool f_State);
   virtual bool      Enabled()const {return(m_Enabled);}
   virtual bool      SaveControlState(int f_file_handle);
   virtual bool      LoadControlState(int f_file_handle,string f_Name,ENUM_OBJECT f_Type);
   virtual int       Type() {return(CTRL_LABEL);}
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CLabel::CLabel(void)
  {
   Color=Red;
   FontName="MS Sans Serif";
   FontSize=8;
   Caption="Label";
   Angle=0.0;
   AnchorPoint=ANCHOR_LEFT_UPPER;
   m_Enabled_Clr=CLR_NONE;
   m_Disabled_Clr=CLR_NONE;
   m_Enabled=true;
   Resources=NULL;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CLabel::Create(string f_Name,int f_Top,int f_Left,CResources *p_ResObj=NULL,string f_RS_Font=" ")
  {
   Resources=p_ResObj;
   bool result=CControl::Create(f_Name,OBJ_LABEL,f_Top,f_Left);
   if(result)
     {
      result&=ObjectSetDouble(m_Chart,m_Name,OBJPROP_ANGLE,Angle);
      result&=ObjectSetInteger(m_Chart,m_Name,OBJPROP_ANCHOR,AnchorPoint);
     }
   if(Resources!=NULL && f_RS_Font!=" ")
     {
      CFontResource *rs_Font=Resources.GetFontResource(f_RS_Font);
      rs_Font.GetFont(m_Font);
      m_Enabled_Clr=m_Font.Clr1;
      m_Disabled_Clr=m_Font.Clr3;
      result&=SetColor(m_Enabled_Clr);
      result&=SetFont(m_Font.FontName,m_Font.FontSize);
     }
   else
     {
      result&=ObjectSetInteger(m_Chart,m_Name,OBJPROP_COLOR,Color);
      result&=ObjectSetString(m_Chart,m_Name,OBJPROP_FONT,FontName);
      result&=ObjectSetInteger(m_Chart,m_Name,OBJPROP_FONTSIZE,FontSize);
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CLabel::SetFont(const string f_FontName,const int f_FontSize)
  {
   if(m_Chart==-1) return(false);

   bool result=ObjectSetInteger(m_Chart,m_Name,OBJPROP_FONTSIZE,f_FontSize);
   result&=ObjectSetString(m_Chart,m_Name,OBJPROP_FONT,f_FontName);
   if(result)
     {
      FontName=f_FontName;
      FontSize=f_FontSize;
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CLabel::SetCaption(const string f_Text)
  {
   bool result=ObjectSetString(m_Chart,m_Name,OBJPROP_TEXT,f_Text);
   if(result) Caption=f_Text;
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CLabel::SetColor(const color f_Color=Red)
  {
   bool result=ObjectSetInteger(m_Chart,m_Name,OBJPROP_COLOR,f_Color);
   m_Enabled_Clr=f_Color;
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CLabel::SetAngle(const double f_Angle=0.000000)
  {
   bool result=ObjectSetDouble(m_Chart,m_Name,OBJPROP_ANGLE,f_Angle);
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CLabel::SetAnchorPoint(ENUM_ANCHOR_POINT f_point=0)
  {
   bool result=ObjectSetInteger(m_Chart,m_Name,OBJPROP_ANCHOR,f_point);
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CLabel::Enabled(bool f_State)
  {
   bool result=(m_Disabled_Clr!=CLR_NONE);
   if(result)
     {
      if(f_State) Color=m_Enabled_Clr;
      else Color=m_Disabled_Clr;
      result&=ObjectSetInteger(m_Chart,m_Name,OBJPROP_COLOR,Color);
      m_Enabled=f_State;
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CLabel::SaveControlState(int f_file_handle)
  {
   string str;
   int    len;
   bool result=CControl::SaveControlState(f_file_handle);
   if(result)
     {
      str=Caption;
      len=StringLen(str);
      if(FileWriteInteger(f_file_handle,len,INT_VALUE)!=INT_VALUE)                          return(false);
      if(len!=0) if(FileWriteString(f_file_handle,str,len)!=len)                            return(false);
      if(FileWriteInteger(f_file_handle,Color,INT_VALUE)!=INT_VALUE)                        return(false);
      if(FileWriteDouble(f_file_handle,Angle)!=sizeof(double))                              return(false);
      if(FileWriteInteger(f_file_handle,AnchorPoint,INT_VALUE)!=INT_VALUE)                  return(false);
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CLabel::LoadControlState(int f_file_handle,string f_Name,ENUM_OBJECT f_Type)
  {
   int    len;
   bool result=CControl::LoadControlState(f_file_handle,f_Name,f_Type);

   if(FileReadLong(f_file_handle)!=-1) return(false);
   if(FileReadInteger(f_file_handle,INT_VALUE)!=f_Type) return(false);
   len=FileReadInteger(f_file_handle,INT_VALUE);
   if(len!=0) Caption=FileReadString(f_file_handle,len);  else Caption="";
   result&=ObjectSetString(m_Chart,f_Name,OBJPROP_TEXT,Caption);

   Color=(color)FileReadInteger(f_file_handle,INT_VALUE);
   result&=ObjectSetInteger(m_Chart,f_Name,OBJPROP_COLOR,Color);

   Angle=(double)FileReadDouble(f_file_handle);
   result&=ObjectSetDouble(m_Chart,m_Name,OBJPROP_ANGLE,Angle);

   AnchorPoint=(ENUM_ANCHOR_POINT)FileReadInteger(f_file_handle,INT_VALUE);
   result&=ObjectSetInteger(m_Chart,m_Name,OBJPROP_ANCHOR,AnchorPoint);
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CCaption:public CLabel
  {
private:
   CControl         *m_Parent;
public:
                     CCaption(void) {m_Parent=NULL;}
   void              SetParentObj(CControl *p_Obj=NULL) {m_Parent=p_Obj;}
   CControl         *Parent(void) {return(m_Parent);}
   int               Type() {return(CTRL_CAPTION);}
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CRadioControl:public CControl
  {
private:
   bool              m_Active;
   bool              m_Enabled;
   CGroup           *Group;
   CCaption         *m_objCaption;
   CWAVbox          *Player;
   string            m_EnabledBmpActive;
   string            m_EnabledBmpDisActive;
   string            m_DisabledBmpActive;
   string            m_DisabledBmpDisActive;
   int               m_ControlHeight;
   int               m_ControlWidth;
   string            m_Font;
   uint              m_FontSize;
   color             m_FontColorActive;
   color             m_FontColorDisActive;
   color             m_FontColorDisabled;
   double            m_Angle;
protected:
   virtual bool      ClickArea(int f_Top_Click,int f_Left_Click);
   virtual void      WAVPlay(void);
public:
                     CRadioControl(void);
                    ~CRadioControl(void);
   virtual bool      Create(const string f_Name,int f_Top,int f_Left,string f_Caption,CGroup *p_Group=NULL);
   virtual bool      InitWAVbox(CWAVbox *p_WAVbox=NULL) {return((Player=p_WAVbox)!=NULL);}
   virtual bool      Active(bool f_State);
   bool              Active() const {return(m_Active);}
   virtual bool      Enabled(bool f_State);
   bool              Enabled() const {return(m_Enabled);}
   virtual bool      SetCaptionAngle(double   f_Angle=0.0);
   virtual void      Font(string f_FontName,
                          int f_FontSize,
                          color f_FontColorActive=Red,
                          color f_FontColorDisActive=CLR_NONE,
                          color f_FontColorDisabled=CLR_NONE
                          );
   virtual bool      SetImgs(const string f_FileNameActive,
                             const string f_FileNameDisActive,
                             const string f_DisabledFileNameActive=" ",
                             const string f_DisabledFileNameDisActive=" ");
   CCaption          *objCaption() {return(m_objCaption);}
   virtual bool      OnClick(int f_Top_Click,int f_Left_Click);
   virtual bool      OnClick(void);
   virtual bool      TimeFrames(int f_Flag);
   virtual bool      ChangeControlPosition(int f_TopShift,int f_LeftShift);
   virtual int       Type() {return(0);}
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CRadioControl::CRadioControl(void)
  {
   m_Active=false;
   m_Enabled=true;
   m_EnabledBmpActive=" ";
   m_EnabledBmpDisActive=" ";
   m_DisabledBmpActive=" ";
   m_DisabledBmpDisActive=" ";
   m_Font="Tahoma";
   m_FontSize=8;
   m_FontColorActive=Red;
   m_FontColorDisActive=CLR_NONE;
   m_FontColorDisabled=CLR_NONE;
   m_Angle=0.0;
   m_objCaption=NULL;
   Group     =NULL;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CRadioControl::~CRadioControl(void)
  {
   if(m_objCaption!=NULL) delete m_objCaption;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CRadioControl::Create(const string f_Name,int f_Top,int f_Left,string f_Caption,CGroup *p_Group=NULL)
  {
   bool result=CControl::Create(f_Name,OBJ_BITMAP_LABEL,f_Top,f_Left);
   if(result) result&=((m_objCaption=new CCaption)!=NULL);
   if(result)
     {
      result&=m_objCaption.Create("lbl"+Name(),f_Top,f_Left);
      result&=m_objCaption.SetCaption(f_Caption);
      m_objCaption.SetParentObj(GetPointer(this));
      Group=p_Group;
      Group.AddItem(GetPointer(this));
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CRadioControl::Active(bool f_State)
  {
   bool result=true;
   if(f_State)
     {
      result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,0,m_EnabledBmpActive);
      result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,1,m_EnabledBmpActive);
      if(Group!=NULL) Group.Active(Index());
     }
   else
     {
      result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,0,m_EnabledBmpDisActive);
      result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,1,m_EnabledBmpDisActive);
     }
   color _clr=(f_State)?m_FontColorActive:m_FontColorDisActive;
   result&=m_objCaption.SetColor(_clr);
   m_Active=f_State;
   m_ControlHeight=Height();
   m_ControlWidth=Width();
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CRadioControl::Enabled(bool f_State)
  {
   bool result=(m_DisabledBmpActive!=" " && 
                m_DisabledBmpDisActive!=" " && 
                m_EnabledBmpActive!=" " && 
                m_EnabledBmpDisActive!=" ");
   if(result)
     {
      if(f_State)
        {
         if(m_Active)
           {
            result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,0,m_EnabledBmpActive);
            result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,1,m_EnabledBmpActive);
           }
         else
           {
            result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,0,m_EnabledBmpDisActive);
            result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,1,m_EnabledBmpDisActive);
           }
        }
      else
        {
         if(m_Active)
           {
            result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,0,m_DisabledBmpActive);
            result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,1,m_DisabledBmpActive);
           }
         else
           {
            result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,0,m_DisabledBmpDisActive);
            result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,1,m_DisabledBmpDisActive);
           }
        }
      if(result)
        {
         m_Enabled=f_State;
         if(m_objCaption!=NULL && m_FontColorDisabled!=CLR_NONE) m_objCaption.Enabled(m_Enabled);
        }
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CRadioControl::SetImgs(const string f_FileNameActive,
                            const string f_FileNameDisActive,
                            const string f_DisabledFileNameActive=" ",
                            const string f_DisabledFileNameDisActive=" ")
  {
   bool result=true;
   m_EnabledBmpActive            =f_FileNameActive;
   m_EnabledBmpDisActive         =f_FileNameDisActive;
   m_DisabledBmpActive           =f_DisabledFileNameActive;
   m_DisabledBmpDisActive        =f_DisabledFileNameDisActive;
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CRadioControl::Font(string f_FontName,
                         int f_FontSize,
                         color f_FontColorActive=Red,
                         color f_FontColorDisActive=CLR_NONE,
                         color f_FontColorDisabled=CLR_NONE)
  {
   m_Font=f_FontName;
   m_FontSize=f_FontSize;
   m_FontColorActive=f_FontColorActive;
   m_FontColorDisActive=f_FontColorDisActive;
   m_FontColorDisabled=f_FontColorDisabled;

   if(m_objCaption!=NULL)
     {
      bool result=m_objCaption.SetFont(m_Font,m_FontSize);
      result&=m_objCaption.SetColor(m_FontColorActive);
      m_objCaption.SetDisabledColor(m_FontColorDisabled);
      if(!result) Print("CRadioControl::Set font error");
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CRadioControl::SetCaptionAngle(double f_Angle=0.000000)
  {
   m_Angle=f_Angle;
   bool result=(m_objCaption!=NULL && m_objCaption.SetAngle(m_Angle));
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CRadioControl::ClickArea(int f_Top_Click,int f_Left_Click)
  {
   int objTop=Top();
   int objLeft=Left();

   if((f_Top_Click>=objTop+5 && f_Top_Click<=objTop+m_ControlHeight) && 
      (f_Left_Click>=objLeft+5 && f_Left_Click<=objLeft+m_ControlWidth)) return(true);
   else return(false);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CRadioControl::WAVPlay(void)
  {
   if(Player!=NULL)
     {
      Player.Play(Type());
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CRadioControl::OnClick(int f_Top_Click,int f_Left_Click)
  {
   bool result=false;
   if(m_Enabled && !m_Active && ClickArea(f_Top_Click,f_Left_Click))
     {
      result=Active(true);
      WAVPlay();
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CRadioControl::OnClick(void)
  {
   bool result=false;
   if(m_Enabled && !m_Active)
     {
      result=Active(true);
      WAVPlay();
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CRadioControl::TimeFrames(int f_Flag)
  {
   if(m_Chart==-1) return(false);
   bool result=CControl::TimeFrames(f_Flag);
   if(m_objCaption!=NULL)
     {
      result&=m_objCaption.TimeFrames(f_Flag);
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CRadioControl::ChangeControlPosition(int f_TopShift,int f_LeftShift)
  {
   bool result=CControl::ChangeControlPosition(f_TopShift,f_LeftShift);
   if(result && m_objCaption!=NULL)
     {
      result&=m_objCaption.ChangeControlPosition(f_TopShift,f_LeftShift);
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|????? need code optimization                                      |
//+------------------------------------------------------------------+
class CCheckbox :public CControl
  {
private:
   string            m_Caption;
   CCaption         *m_objCaption;
   int               m_Width;
   int               m_Height;
   bool              LastState;
   bool              m_Enabled;
   FontSettings      _Font;
   CArrayString     *Images;
   CResources       *Resources;
   CWAVbox          *Player;
   string            m_EnabledBmpActive;
   string            m_EnabledBmpDisActive;
   string            m_DisabledBmpActive;
   string            m_DisabledBmpDisActive;
   string            m_Font;
   uint              m_FontSize;
   color             m_FontColorActive;
   color             m_FontColorDisActive;
   color             m_FontColorDisabled;
protected:
   void              WAVPlay(void);
public:
                     CCheckbox(void);
                    ~CCheckbox(void);
   bool              Create(string f_Name,int f_Top,int f_Left,int f_handle,CResources *p_ResObj,string f_RS_Name,string f_RS_Font);
   bool              IsChecked;
   bool              Checked(bool f_State);
   bool              Enabled(bool f_State);
   bool              Enabled() const {return(m_Enabled);}
   void              Caption(string f_Text) {m_Caption=f_Text; m_objCaption.SetCaption(f_Text);}
   CCaption          *objCaption() {return(m_objCaption);}
   bool              OnStateChange();
   bool              OnClick();
   virtual bool      ChangeControlPosition(int f_TopShift,int f_LeftShift);
   virtual bool      TimeFrames(int f_Flag);
   virtual bool      SaveControlState(int f_file_handle);
   virtual bool      LoadControlState(int f_file_handle,string f_Name,ENUM_OBJECT f_Type);
   virtual  int      Type() {return(CTRL_CHECKBOX);}
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CCheckbox::CCheckbox(void)
  {
   m_EnabledBmpActive    =" ";
   m_EnabledBmpDisActive =" ";
   m_DisabledBmpActive   =" ";
   m_DisabledBmpDisActive=" ";
   m_Font                ="Tahoma";
   m_FontSize            =8;
   m_FontColorActive     =Red;
   m_FontColorDisActive  =CLR_NONE;
   m_FontColorDisabled   =CLR_NONE;
   LastState=false;
   IsChecked=false;
   m_Enabled=false;
   m_Caption="";
   m_objCaption=NULL;
   m_Width=0;
   m_Height=0;
   Resources=NULL;
   Images=NULL;
   Player=NULL;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CCheckbox::~CCheckbox(void)
  {
   if(m_objCaption!=NULL)
     {
      delete m_objCaption;
      m_objCaption=NULL;
     }
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CCheckbox::Create(string f_Name,int f_Top,int f_Left,int f_handle,CResources *p_ResObj,string f_RS_Name,string f_RS_Font)
  {
   bool result=false;
   if(f_handle!=INVALID_HANDLE)
     {
      result=LoadControlState(f_handle,f_Name,OBJ_BITMAP_LABEL);
      if(result)
        {
         result&=ObjectSetInteger(m_Chart,m_Name,OBJPROP_YDISTANCE,f_Top);
         result&=ObjectSetInteger(m_Chart,m_Name,OBJPROP_XDISTANCE,f_Left);
        }
     }

   if((Resources=p_ResObj)!=NULL && f_RS_Name!=" " && f_RS_Font!=" ")
     {
      Player=Resources.GetWavResource();
      if(!result) result=CControl::Create(f_Name,OBJ_BITMAP_LABEL,f_Top,f_Left);
      Images=Resources.GetImgsResource(f_RS_Name).GetResList();
      if(Images!=NULL)
        {
         m_EnabledBmpActive     =Images.At(0);
         m_EnabledBmpDisActive  =Images.At(1);
         m_DisabledBmpActive    =Images.At(2);
         m_DisabledBmpDisActive =Images.At(3);
         result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,0,m_EnabledBmpActive);
         result=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,1,m_EnabledBmpDisActive);

         m_Width=Width();
         m_Height=Height();
         CFontResource *rs_Font=Resources.GetFontResource(f_RS_Font);
         if(rs_Font!=NULL)
           {
            rs_Font.GetFont(_Font);
            m_Font               =_Font.FontName;
            m_FontSize           =_Font.FontSize;
            m_FontColorActive    =_Font.Clr1;
            m_FontColorDisActive =_Font.Clr2;
            m_FontColorDisabled  =_Font.Clr3;
            result&=((m_objCaption=new CCaption)!=NULL);
            if(result)
              {
               int X_pos=f_Left+m_Width+10;
               int Y_pos=f_Top+(int)ceil(m_Height/2);
               result&=m_objCaption.Create("lbl"+f_Name,Y_pos,X_pos,p_ResObj,f_RS_Font);
               if(result)
                 {
                  result&=m_objCaption.SetAnchorPoint(ANCHOR_LEFT);
                  m_objCaption.SetParentObj(GetPointer(this));
                 }
              }
           }
        }
     }
   else result=false;

   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CCheckbox::WAVPlay(void)
  {
   if(Player!=NULL)
     {
      Player.Play(Type());
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CCheckbox::Checked(bool f_State)
  {
   if(m_Chart==-1) return(false);
   bool result=ObjectSetInteger(m_Chart,m_Name,OBJPROP_STATE,f_State);
   if(result)
     {
      IsChecked=f_State;
      LastState=IsChecked;
     }
   color _clr=(f_State)?m_FontColorActive:m_FontColorDisActive;
   result&=m_objCaption.SetColor(_clr);
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CCheckbox::Enabled(bool f_State)
  {
   bool result=(m_DisabledBmpActive!=" " && 
                m_DisabledBmpDisActive!=" " && 
                m_EnabledBmpActive!=" " && 
                m_EnabledBmpDisActive!=" ");
   if(result)
     {
      if(f_State)
        {
         result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,0,m_EnabledBmpActive);
         result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,1,m_EnabledBmpDisActive);
        }
      else
        {
         if(IsChecked)
           {
            result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,0,m_DisabledBmpActive);
            result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,1,m_DisabledBmpActive);
           }
         else
           {
            result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,0,m_DisabledBmpDisActive);
            result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,1,m_DisabledBmpDisActive);
           }
        }
      if(result)
        {
         m_Enabled=f_State;
         if(m_objCaption!=NULL && m_FontColorDisabled!=CLR_NONE) m_objCaption.Enabled(m_Enabled);
        }
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CCheckbox::OnStateChange(void)
  {
   IsChecked=ObjectGetInteger(m_Chart,m_Name,OBJPROP_STATE);
   if(LastState!=IsChecked)
     {
      LastState=IsChecked;
      color _clr=(IsChecked)?m_FontColorActive:m_FontColorDisActive;
      m_objCaption.SetColor(_clr);
      WAVPlay();
      return(true);
     }
   return(false);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CCheckbox::OnClick(void)
  {
   bool result=true;
   if(m_Enabled)
     {
      if(IsChecked) Checked(false);
      else Checked(true);
      WAVPlay();
      ChartRedraw();
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CCheckbox::ChangeControlPosition(int f_TopShift,int f_LeftShift)
  {
   bool result=CControl::ChangeControlPosition(f_TopShift,f_LeftShift);
   if(result && m_objCaption!=NULL)
     {
      result&=m_objCaption.ChangeControlPosition(f_TopShift,f_LeftShift);
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CCheckbox::TimeFrames(int f_Flag)
  {
   if(m_Chart==-1) return(false);
   bool result=CControl::TimeFrames(f_Flag);
   if(m_objCaption!=NULL)
     {
      result&=m_objCaption.TimeFrames(f_Flag);
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CCheckbox::SaveControlState(int f_file_handle)
  {
   bool result;

   if(f_file_handle<=0)                                                                  return(false);
   if(m_Chart==-1)                                                                       return(false);

   result=CControl::SaveControlState(f_file_handle);
   if(result)
     {
      if(FileWriteLong(f_file_handle,ObjectGetInteger(m_Chart,m_Name,OBJPROP_STATE))!=sizeof(long)) return(false);
     }
   else return(false);

   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CCheckbox::LoadControlState(int f_file_handle,string f_Name,ENUM_OBJECT f_Type)
  {

   bool result=CControl::LoadControlState(f_file_handle,f_Name,f_Type);
   if(result)
     {
      if(!Checked(FileReadLong(f_file_handle))) return(false);
     }
   else  return(false);

   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CButton: public CControl
  {
private:
   string            m_Caption;
   string            m_Font;
   uint              m_FontSize;
   color             m_FontColor;
   color             m_FontColorDown;
   color             m_FontColorDisabled;
   CCaption         *m_objCaption;
   int               m_btnWidth;
   int               m_btnHeight;
   string            BmpOn;
   string            BmpOff;
   string            BmpDis;
   bool              LastState;
   bool              m_Clickable;
   FontSettings      _Font;
   CArrayString     *Images;
   CResources       *Resources;
protected:
   virtual bool      ClickArea(int f_Top_Click,int f_Left_Click);
   void              WAVPlay(void);
public:
                     CButton(void);
                    ~CButton(void);
   CWAVbox          *Player;
   CCaption         *objCaption() {return(m_objCaption);}
   virtual bool      Create(string f_Name,int f_Top,int f_Left,CResources *p_ResObj=NULL,string f_RS_Name=" ",string f_RS_Font=" ");
   virtual bool      SetImgs(string f_FileNameOn,string f_FileNameOff,string f_FileNameDis="");
   virtual bool      SetCaption(string f_Caption);
   virtual void      Font(string f_FontName,int f_FontSize,color f_FontColor=255,color f_FontColorDown=-1,color f_FontColorDisabled=-1);
   bool              IsDown;
   bool              IsEnabled;
   virtual bool      Down(bool f_State);
   virtual bool      Enabled(bool f_State);
   virtual void      Clickable(bool f_State) {m_Clickable=f_State;}
   bool              OnClick(int f_Top_Click,int f_Left_Click,int delay=30);
   virtual bool      OnStateChange();
   virtual bool      TimeFrames(int f_Flag);
   virtual bool      ChangeControlPosition(int f_TopShift,int f_LeftShift);
   virtual bool      SaveControlState(int f_file_handle)                                  {return(false);}
   virtual bool      LoadControlState(int f_file_handle,string f_Name,ENUM_OBJECT f_Type) {return(false);}
   virtual   int     Type() {return(CTRL_BUTTON);}
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CButton::CButton(void)
  {
   BmpOn  =" ";
   BmpOff =" ";
   BmpDis =" ";
   m_objCaption=NULL;
   m_Caption=" ";
   m_Font="Tahoma";
   m_FontSize=8;
   m_FontColor=Red;
   m_btnWidth=0;
   m_btnHeight=0;
   IsDown   =false;
   IsEnabled=true;
   LastState=false;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CButton::~CButton(void)
  {
   if(m_objCaption!=NULL)
     {
      delete m_objCaption;
      m_objCaption=NULL;
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CButton::Create(string f_Name,int f_Top,int f_Left,CResources *p_ResObj=NULL,string f_RS_Name=" ",string f_RS_Font=" ")
  {
   bool result=CControl::Create(f_Name,OBJ_BITMAP_LABEL,f_Top,f_Left);

   if(result && (Resources=p_ResObj)!=NULL && f_RS_Name!=" ")
     {
      Player=Resources.GetWavResource();
      if((Images=Resources.GetImgsResource(f_RS_Name).GetResList())!=NULL)
        {
         result&=SetImgs(Images.At(0),Images.At(1),Images.At(2));
        }

      if(f_RS_Font!=" ")
        {
         CFontResource *rs_Font=Resources.GetFontResource(f_RS_Font);
         if(rs_Font!=NULL)
           {
            rs_Font.GetFont(_Font);
            Font(_Font.FontName,_Font.FontSize,_Font.Clr1,_Font.Clr2,_Font.Clr3);
           }
        }
     }

   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CButton::SetImgs(string f_FileNameOn,string f_FileNameOff=" ",string f_FileNameDis="")
  {
   if(m_Chart==-1) return(false);
   bool result=true;

   BmpOn=f_FileNameOn;
   if(f_FileNameOff!=" ") BmpOff=f_FileNameOff;
   if(f_FileNameDis!=" ") BmpDis=f_FileNameDis;

   if(!m_Clickable)
     {
      result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,0,BmpOn);
      result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,1,BmpOff);
     }
   else
     {
      result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,0,BmpOn);
      result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,1,BmpOn);
     }

   m_btnWidth=Width();
   m_btnHeight=Height();

   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CButton::SetCaption(string f_Caption)
  {
   bool result;

   if(m_objCaption!=NULL)
     {
      result=m_objCaption.SetCaption(f_Caption);
     }
   else
     {
      int X_pos=Left()+(int)round(m_btnWidth/2);
      int Y_pos=Top()+(int)round(m_btnHeight/2);
      result=((m_objCaption=new CCaption)!=NULL);
      if(!result) return(result);
      result&=m_objCaption.Create("lbl"+Name(),Y_pos,X_pos);
      if(result)
        {
         result&=m_objCaption.SetFont(m_Font,m_FontSize);
         result&=m_objCaption.SetColor(m_FontColor);
         result&=m_objCaption.SetAnchorPoint(ANCHOR_CENTER);
         result&=m_objCaption.SetCaption(f_Caption);
         m_objCaption.SetParentObj(GetPointer(this));
        }
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CButton::Font(string f_FontName,int f_FontSize,color f_FontColor=255,color f_FontColorDown=-1,color f_FontColorDisabled=-1)
  {
   m_Font=f_FontName;
   m_FontSize=f_FontSize;
   m_FontColor=f_FontColor;
   m_FontColorDown=f_FontColorDown;
   m_FontColorDisabled=f_FontColorDisabled;
   if(m_objCaption!=NULL)
     {
      bool result=m_objCaption.SetFont(f_FontName,f_FontSize);
      result&=m_objCaption.SetColor(f_FontColor);
      if(!result) Print("Set font error");
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CButton::Down(bool f_State)
  {
   if(m_Chart==-1) return(false);
   bool result=ObjectSetInteger(m_Chart,m_Name,OBJPROP_STATE,!f_State);
   if(result)IsDown=f_State;
   if(m_objCaption!=NULL)
     {
      color _clr=(IsDown)?m_FontColorDown:m_FontColor;
      if(IsDown) m_objCaption.ChangeControlPosition(BTN_CAPTION_SHIFT,BTN_CAPTION_SHIFT);
      else     m_objCaption.ChangeControlPosition(-BTN_CAPTION_SHIFT,-BTN_CAPTION_SHIFT);
      result&=m_objCaption.SetColor(_clr);
     }
   LastState=IsDown;
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CButton::Enabled(bool f_State)
  {
   if(m_Chart==-1) return(false);
   bool result=true;
   if(f_State)
     {
      if(!m_Clickable)
        {
         result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,1,BmpOff);
         result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,0,BmpOn);
        }
      else
        {
         result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,1,BmpOn);
         result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,0,BmpOn);
        }
     }
   else
     {
      if(BmpDis!=" ")
        {
         result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,1,BmpDis);
         result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,0,BmpDis);
        }
      else
        {
         if(!m_Clickable)
           {
            if(IsDown)
              {
               result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,1,BmpOff);
               result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,0,BmpOff);
              }
            else
              {
               result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,1,BmpOn);
               result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,0,BmpOn);
              }
           }
         else
           {
            result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,1,BmpOn);
            result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,0,BmpOn);
           }
        }
     }

   if(result)
     {
      IsEnabled=f_State;
      if(m_objCaption!=NULL) m_objCaption.Enabled(IsEnabled);
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CButton::OnClick(int f_Top_Click,int f_Left_Click,int delay=30)
  {
   bool result=true;
   if(IsEnabled && m_Clickable && ClickArea(f_Top_Click,f_Left_Click))
     {
      result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,1,BmpOff);
      Down(true);
      ChartRedraw(m_Chart);
      Sleep(delay);
      WAVPlay();
      result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,1,BmpOn);
      Down(false);
      ChartRedraw(m_Chart);
      return(result);
     }
   else return(result=false);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CButton::OnStateChange(void)
  {
   IsDown=!ObjectGetInteger(m_Chart,m_Name,OBJPROP_STATE);
   if(LastState!=IsDown)
     {
      LastState=IsDown;
      return(true);
     }
   return(false);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CButton::WAVPlay(void)
  {
   if(Player!=NULL)
     {
      Player.Play(Type());
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CButton::TimeFrames(int f_Flag)
  {
   if(m_Chart==-1) return(false);
   bool result=CControl::TimeFrames(f_Flag);
   if(m_objCaption!=NULL)
     {
      result&=m_objCaption.TimeFrames(f_Flag);
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CButton::ChangeControlPosition(int f_TopShift,int f_LeftShift)
  {
   bool result=CControl::ChangeControlPosition(f_TopShift,f_LeftShift);
   if(result && m_objCaption!=NULL)
     {
      result&=m_objCaption.ChangeControlPosition(f_TopShift,f_LeftShift);
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CButton::ClickArea(int f_Top_Click,int f_Left_Click)
  {
   int objTop=this.Top();
   int objLeft=this.Left();

   if((f_Top_Click>=objTop+5 && f_Top_Click<=objTop+m_btnHeight) && 
      (f_Left_Click>=objLeft+5 && f_Left_Click<=objLeft+m_btnWidth)) return(true);
   else return(false);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CTradeButton: public CButton
  {
private:
   uchar             m_SmallDigitWidth;
   uchar             m_DilimiterWidth;
   uchar             m_LargeDigitWidth;
   int               m_LastTop;
   int               m_LastLeft;
   int               m_Width;
   int               m_Height;
   uchar             m_CaptionWidth;   //Pixels
   uchar             m_TotalDigits;
   uchar             m_PriceDigits;
   uchar             m_Digits;
   bool              m_LastState;
   color             m_LastColor;
   color             m_NormalFontColor;
   color             m_PNFontColor;
   color             m_DisabledFontColor;
   string            m_FontName;
   int               m_FontSizeS;
   int               m_FontSizeL;
   uchar             m_LastPriceChange;

private:
   bool              fg_Creating;
   bool              fg_Int;         //IsInteger flag
   struct BkgBmps
     {
      string            NormalState;
      string            DownState;
     };

   struct BtnBkgs
     {
      BkgBmps           PriceUp;
      BkgBmps           PriceDown;
      BkgBmps           PriceNone;
      BkgBmps           TradeDisabled;
     };

   struct CaptionMatrix
     {
      CCaption         *objPrice;
      CCaption         *objPoints;
      CCaption         *objPips;       // pips=point/10
     };

   BtnBkgs           btnFace;
   CaptionMatrix     Caption;
   FontSettings      _Font;
   CArrayString     *Images;
   CResources       *Resources;
   CContainer       *Handler;
   bool              isInteger(double value) {return((bool)(ceil(value)==floor(value)));}
protected:
   bool              SetCaption(double f_Price,uchar f_digits);
   bool              SetCaptionMatrix(CCaption *p_objCaption,string f_Value);
   bool              ChangeFontColor(color f_Clr);
   void              InitCaptionMatrix(double f_price,uchar f_digits);
   bool              ChangeMatrixPosition(int f_TopShift,int f_LeftShift);
   CCaption         *CreateMatrixLabel(const string f_ControlName,int f_Top,int f_Left,ENUM_ANCHOR_POINT f_Anchor,uchar f_Size);
public:
                     CTradeButton(void);
                    ~CTradeButton(void);
   bool              Create(string f_Name,int f_Top,int f_Left,CResources *p_ResObj=NULL,string f_RS_Name=" ",string f_RS_Font=" ",CContainer *p_Handler=NULL);
   bool              Down(bool f_State);
   bool              IsDown;
   bool              Enabled(bool f_State);
   bool              TimeFrames(int f_Flag);
   bool              ChangeControlPosition(int f_TopShift,int f_LeftShift);
   void              SetDigitsSizes(uchar f_SmallDigit,
                                    uchar f_Dilimiter,
                                    uchar f_LargeDigit);
   bool              OnBkgChange(uchar f_btnFace,double f_price,uchar f_digits,uchar f_PriceChange);
   int               Type() {return(CTRL_CMD_BUTTON);}
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CTradeButton::CTradeButton(void)
  {
   m_SmallDigitWidth=0;
   m_DilimiterWidth=0;
   m_LargeDigitWidth=0;
   fg_Creating=false;
   fg_Int=false;
   btnFace.PriceUp.DownState=" ";
   btnFace.PriceUp.NormalState=" ";
   btnFace.PriceDown.DownState=" ";
   btnFace.PriceDown.NormalState=" ";
   btnFace.PriceNone.DownState=" ";
   btnFace.PriceNone.NormalState=" ";
   btnFace.TradeDisabled.DownState=" ";
   btnFace.TradeDisabled.NormalState=" ";
   Caption.objPrice=NULL;
   Caption.objPoints=NULL;
   Caption.objPips=NULL;
   m_CaptionWidth=0;
   m_TotalDigits=0;
   m_PriceDigits=0;
   m_Digits=0;
   IsDown=false;
   m_LastState=false;
   m_NormalFontColor=DEFAULT_FONTCOLOR;
   m_PNFontColor=DEFAULT_FONTCOLOR;
   m_LastColor=DEFAULT_FONTCOLOR;
   m_DisabledFontColor=DEFAULT_FONTCOLOR;
   m_FontName  =DEFAULT_FONTNAME;
   m_FontSizeS =DEFAULT_FONTSIZE;
   m_FontSizeL =2*m_FontSizeS;
   m_LastPriceChange=PRICE_CHANGE_NONE;
   Images    = NULL;
   Resources = NULL;
   Handler   = NULL;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CTradeButton::~CTradeButton(void)
  {
   if(Caption.objPrice!=NULL)  delete Caption.objPrice;
   if(Caption.objPoints!=NULL) delete Caption.objPoints;
   if(Caption.objPips!=NULL)   delete Caption.objPips;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CTradeButton::Create(string f_Name,int f_Top,int f_Left,CResources *p_ResObj=NULL,string f_RS_Name=" ",string f_RS_Font=" ",CContainer *p_Handler=NULL)
  {
   bool result=CButton::Create(f_Name,f_Top,f_Left);
   if(result)
     {
      if((Resources=p_ResObj)!=NULL && f_RS_Name!=" " && f_RS_Font!=" ")
        {
         Player=Resources.GetWavResource();
         Images=Resources.GetImgsResource(f_RS_Name).GetResList();
         if(Images!=NULL)
           {
            btnFace.PriceUp.NormalState         =Images.At(0);
            btnFace.PriceUp.DownState           =Images.At(1);
            btnFace.PriceDown.NormalState       =Images.At(2);
            btnFace.PriceDown.DownState         =Images.At(3);
            btnFace.PriceNone.NormalState       =Images.At(4);
            btnFace.PriceNone.DownState         =Images.At(5);
            btnFace.TradeDisabled.NormalState   =Images.At(6);
            btnFace.TradeDisabled.DownState     =Images.At(6);
           }

         CFontResource *rs_Font=Resources.GetFontResource(f_RS_Font);
         if(rs_Font!=NULL)
           {
            rs_Font.GetFont(_Font);
            m_FontName        =_Font.FontName;
            m_FontSizeS       =_Font.FontSize;
            m_FontSizeL       =2*m_FontSizeS;
            m_NormalFontColor =_Font.Clr1;
            m_PNFontColor     =_Font.Clr2;
            m_LastColor       =m_PNFontColor;
            m_DisabledFontColor=_Font.Clr3;
           }
        }
      else return(false);
      m_Top=m_LastTop=f_Top;
      m_Left=m_LastLeft=f_Left;
      Handler=p_Handler;
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CTradeButton::SetCaption(double f_price,uchar f_digits)
  {
   if(!fg_Creating)
     {
      InitCaptionMatrix(f_price,f_digits);
      fg_Creating=true;
     }

   bool   result=true;
   string rests;
   string ResultStr;
   if(!fg_Int)
     {
      rests=DoubleToString(NormalizeDouble(fmod(f_price,1),f_digits),f_digits);
      StringConcatenate(ResultStr,floor(f_price),".");
     }
   else
     {
      rests=DoubleToString(f_price,f_digits);
     }
   switch(f_digits)
     {

      case 0:
        {
         result&=SetCaptionMatrix(Caption.objPrice,StringSubstr(rests,0,StringLen(rests)-2));
         result&=SetCaptionMatrix(Caption.objPoints,StringSubstr(rests,StringLen(rests)-2,2));
        }
      break;

      case 2:
        {
         result&=SetCaptionMatrix(Caption.objPrice,ResultStr);
         result&=SetCaptionMatrix(Caption.objPoints,StringSubstr(rests,2,2));
        }
      break;

      case 4:
        {
         StringConcatenate(ResultStr,ResultStr,StringSubstr(rests,2,2));
         result&=SetCaptionMatrix(Caption.objPrice,ResultStr);
         result&=SetCaptionMatrix(Caption.objPoints,StringSubstr(rests,2,2));
        }
      break;

      case 3:
        {
         result&=SetCaptionMatrix(Caption.objPrice,ResultStr);
         result&=SetCaptionMatrix(Caption.objPoints,StringSubstr(rests,2,2));
         result&=SetCaptionMatrix(Caption.objPips,StringSubstr(rests,4,-1));
        }
      break;

      case 5:
        {
         StringConcatenate(ResultStr,ResultStr,StringSubstr(rests,2,2));
         result&=SetCaptionMatrix(Caption.objPrice,ResultStr);
         result&=SetCaptionMatrix(Caption.objPoints,StringSubstr(rests,4,2));
         result&=SetCaptionMatrix(Caption.objPips,StringSubstr(rests,6,-1));
        }
      break;
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CTradeButton::SetCaptionMatrix(CCaption *p_objCaption,string f_Value)
  {
   return((bool)p_objCaption.SetCaption(f_Value));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CTradeButton::ChangeFontColor(color f_Clr)
  {
   bool result=true;
   if(Caption.objPrice!=NULL) result&=Caption.objPrice.SetColor(f_Clr);
   if(Caption.objPoints!=NULL) result&=Caption.objPoints.SetColor(f_Clr);
   if(Caption.objPips!=NULL) result&=Caption.objPips.SetColor(f_Clr);
   if(result) m_LastColor=f_Clr;
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CTradeButton::InitCaptionMatrix(double f_price,uchar f_digits)
  {
   uchar pricedigits=0;
   fg_Int=(isInteger(f_price))?true:false;
   int   int_value=(fg_Int)?(int)f_price:(int)MathFloor(f_price);
   m_Width=Width();
   m_Height=Height();
   int   TopPos=0,LeftPos=0;

   int step=10;
   for(uchar i=1;i<=10;i++)
     {
      if(int_value/step<1)
        {
         pricedigits=i;
         break;
        }
      step*=10;
     }
   m_PriceDigits=pricedigits;
   m_Digits=f_digits;
   m_TotalDigits=(fg_Int)?m_PriceDigits:m_PriceDigits+m_Digits+1;
   m_CaptionWidth=(m_TotalDigits-2)*m_SmallDigitWidth+2*m_LargeDigitWidth;

   switch(m_Digits)
     {
      case 0:
        {
         TopPos=m_Top+m_Height-3;
         LeftPos=m_Left+(int)floor(m_Width/2-m_CaptionWidth/2);
         Caption.objPrice=CreateMatrixLabel("lblPrice"+m_Name,TopPos,LeftPos,ANCHOR_LEFT_LOWER,LBL_SMALL);

         TopPos+=3;
         LeftPos+=(m_PriceDigits-2)*m_SmallDigitWidth;
         Caption.objPoints=CreateMatrixLabel("lblPoint"+m_Name,TopPos,LeftPos,ANCHOR_LEFT_LOWER,LBL_LARGE);
        }
      break;

      case 2:
      case 4:
        {
         TopPos=m_Top+m_Height-3;
         LeftPos=m_Left+(int)floor(m_Width/2-m_CaptionWidth/2);
         Caption.objPrice=CreateMatrixLabel("lblPrice"+m_Name,TopPos,LeftPos,ANCHOR_LEFT_LOWER,LBL_SMALL);

         TopPos+=3;
         LeftPos+=(m_PriceDigits+(m_Digits-2))*m_SmallDigitWidth+m_DilimiterWidth+1;
         Caption.objPoints=CreateMatrixLabel("lblPoint"+m_Name,TopPos,LeftPos,ANCHOR_LEFT_LOWER,LBL_LARGE);
        }
      break;

      case 3:
      case 5:
        {
         TopPos=m_Top+m_Height-3;
         LeftPos=m_Left+(int)floor(m_Width/2-m_CaptionWidth/2);
         Caption.objPrice=CreateMatrixLabel("lblPrice"+m_Name,TopPos,LeftPos,ANCHOR_LEFT_LOWER,LBL_SMALL);

         TopPos+=3;
         LeftPos+=(m_PriceDigits+(m_Digits-3))*m_SmallDigitWidth+m_DilimiterWidth+1;
         Caption.objPoints=CreateMatrixLabel("lblPoint"+m_Name,TopPos,LeftPos,ANCHOR_LEFT_LOWER,LBL_LARGE);

         TopPos=m_Top+(int)floor(m_Height/2-3);
         LeftPos=m_Left+(int)floor(m_Width/2+m_CaptionWidth/2)+1;
         Caption.objPips=CreateMatrixLabel("lblPips"+m_Name,TopPos,LeftPos,ANCHOR_RIGHT_UPPER,LBL_SMALL);
        }
      break;
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CCaption *CTradeButton::CreateMatrixLabel(const string f_ControlName,int f_Top,int f_Left,ENUM_ANCHOR_POINT f_Anchor,uchar f_Size)
  {
   CCaption *Label=NULL;
   if((Label=new CCaption)!=NULL)
     {
      bool result=Label.Create(f_ControlName,f_Top,f_Left);
      result&=Label.SetAnchorPoint(f_Anchor);
      result&=Label.SetColor(m_LastColor);
      if(f_Size==LBL_LARGE) result&=Label.SetFont(m_FontName,m_FontSizeL);
      else                  result&=Label.SetFont(m_FontName,m_FontSizeS);
      result&=Label.SetCaption(" ");
      if(!Visible()) result&=Label.TimeFrames(OBJ_NO_PERIODS);
      if(!result)
        {
         Print("CTradeButton:: Caption initilize error");
        }
      else
        {
         Label.SetParentObj(GetPointer(this));
         if(Handler!=NULL) Handler.AddItem(Label);
        }
      return(Label);
     }
   return(NULL);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CTradeButton::ChangeMatrixPosition(int f_TopShift,int f_LeftShift)
  {
   bool result=true;

   if(Caption.objPrice!=NULL)
     {
      result&=Caption.objPrice.ChangeControlPosition(f_TopShift,f_LeftShift);
     }

   if(Caption.objPoints!=NULL)
     {
      result&=Caption.objPoints.ChangeControlPosition(f_TopShift,f_LeftShift);
     }

   if(Caption.objPips!=NULL)
     {
      result&=Caption.objPips.ChangeControlPosition(f_TopShift,f_LeftShift);
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CTradeButton::Down(bool f_State)
  {
   if(m_Chart==-1) return(false);
   bool result=ObjectSetInteger(m_Chart,m_Name,OBJPROP_STATE,!f_State);
   if(result)IsDown=f_State;

   if(IsDown) result&=ChangeMatrixPosition(BTN_CAPTION_SHIFT,BTN_CAPTION_SHIFT);
   else result&=ChangeMatrixPosition(-BTN_CAPTION_SHIFT,-BTN_CAPTION_SHIFT);

   m_LastState=IsDown;
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CTradeButton::Enabled(bool f_State)
  {
   string Off=" ",On=" ",Dis=btnFace.TradeDisabled.NormalState;
   if(m_Chart==-1) return(false);
   bool result=true;

   if(f_State)
     {
      switch(m_LastPriceChange)
        {
         case PRICE_CHANGE_NONE:
           {
            Off=btnFace.PriceNone.DownState;
            On=btnFace.PriceNone.NormalState;
            result&=ChangeFontColor(m_PNFontColor);
           }
         break;

         case PRICE_CHANGE_UP:
           {
            Off=btnFace.PriceUp.DownState;
            On=btnFace.PriceUp.NormalState;
            result&=ChangeFontColor(m_NormalFontColor);
           }
         break;

         case PRICE_CHANGE_DOWN:
           {
            Off=btnFace.PriceDown.DownState;
            On=btnFace.PriceDown.NormalState;
            result&=ChangeFontColor(m_NormalFontColor);
           }
         break;
        }
      result&=SetImgs(On,Off,Dis);
     }
   else result&=ChangeFontColor(m_DisabledFontColor);
   result&=CButton::Enabled(f_State);

   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CTradeButton::TimeFrames(int f_Flag)
  {
   bool result;
   if(f_Flag==OBJ_NO_PERIODS)
     {
      m_LastTop=m_Top;
      m_LastLeft=m_Left;
     }

   if(result=CControl::TimeFrames(f_Flag))
     {
      if(Caption.objPrice!=NULL)
        {
         result&=Caption.objPrice.TimeFrames(f_Flag);
        }
      if(Caption.objPoints!=NULL)
        {
         result&=Caption.objPoints.TimeFrames(f_Flag);
        }
      if(Caption.objPips!=NULL)
        {
         result&=Caption.objPips.TimeFrames(f_Flag);
        }
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CTradeButton::ChangeControlPosition(int f_TopShift,int f_LeftShift)
  {
   bool result=CControl::ChangeControlPosition(f_TopShift,f_LeftShift);
   if(result)
     {
      result&=ChangeMatrixPosition(f_TopShift,f_LeftShift);
      m_Top=Top();
      m_Left=Left();
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CTradeButton::SetDigitsSizes(uchar f_SmallDigit,uchar f_Dilimiter,uchar f_LargeDigit)
  {
   m_SmallDigitWidth=f_SmallDigit;
   m_DilimiterWidth=f_Dilimiter;
   m_LargeDigitWidth=f_LargeDigit;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CTradeButton::OnBkgChange(uchar f_btnFace,double f_price,uchar f_digits,uchar f_PriceChange)
  {
   bool result=false;
   switch(f_btnFace)
     {
      case BTN_FACE_PRICEUP:
        {
         result=SetImgs(btnFace.PriceUp.NormalState,btnFace.PriceUp.DownState,btnFace.TradeDisabled.NormalState);
         if(m_LastColor!=m_NormalFontColor)result&=ChangeFontColor(m_NormalFontColor);
        }
      break;

      case BTN_FACE_PRICEDOWN:
        {
         result=SetImgs(btnFace.PriceDown.NormalState,btnFace.PriceDown.DownState,btnFace.TradeDisabled.NormalState);
         if(m_LastColor!=m_NormalFontColor)result&=ChangeFontColor(m_NormalFontColor);
        }
      break;

      case BTN_FACE_PRICENONE:
        {
         result=SetImgs(btnFace.PriceNone.NormalState,btnFace.PriceNone.DownState,btnFace.TradeDisabled.NormalState);
         if(m_LastColor!=m_PNFontColor) result&=ChangeFontColor(m_PNFontColor);
        }
      break;

      default: if(!IsEnabled && m_LastColor!=m_DisabledFontColor) result&=ChangeFontColor(m_DisabledFontColor); break;

     }
   m_LastPriceChange=f_PriceChange;
   if(f_digits!=UCHAR_MAX) result&=SetCaption(f_price,f_digits);
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CImage:public CControl
  {
private:
   string            m_BmpEnabled;
   string            m_BmpDisabled;
   bool              m_Enabled;
public:
                     CImage(void);
   virtual bool      Create(const string f_Name,int f_Top,int f_Left=0,CResources *p_ResObj=NULL,string f_RS_Name=" ");
   virtual bool      Enabled(bool f_State);
   virtual bool      Enabled() const {return(m_Enabled);}
   virtual bool      Image(string f_FileName);
   virtual void      SetImgs(string f_FileNameEnabled,string f_FileNameDisabled=" ");
   virtual bool      SaveControlState(int f_file_handle)                                  {return(false);}
   virtual bool      LoadControlState(int f_file_handle,string f_Name,ENUM_OBJECT f_Type) {return(false);}
   virtual int       Type() {return(CTRL_IMAGE);}
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CImage::CImage(void)
  {
   m_BmpDisabled=" ";
   m_BmpEnabled =" ";
   m_Enabled=true;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CImage::SetImgs(string f_FileNameEnabled,string f_FileNameDisabled=" ")
  {
   if(f_FileNameEnabled!=" ") m_BmpEnabled=f_FileNameEnabled;
   if(f_FileNameDisabled!=" ") m_BmpDisabled=f_FileNameDisabled;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CImage::Image(string f_FileName)
  {
   if(m_Chart==-1) return(false);
   bool result=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,0,f_FileName);
   result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,1,f_FileName);
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CImage::Create(const string f_Name,int f_Top,int f_Left=0,CResources *p_ResObj=NULL,string f_RS_Name=" ")
  {
   bool result=CControl::Create(f_Name,OBJ_BITMAP_LABEL,f_Top,f_Left);
   if(result)
     {
      result&=SetColor((color)ChartGetInteger(m_Chart,CHART_COLOR_BACKGROUND));
      CResources *rs=p_ResObj;
      if(rs!=NULL && f_RS_Name!=" ")
        {
         CArrayString *Images=NULL;
         CImgResource *rs_Img=rs.GetImgsResource(f_RS_Name);
         if(rs_Img!=NULL && (Images=rs_Img.GetResList())!=NULL)
           {
            SetImgs(Images.At(0),Images.At(1));
            Image(m_BmpEnabled);
           }
        }
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CImage::Enabled(bool f_State)
  {
   bool result=(m_BmpEnabled!=" " && m_BmpDisabled!=" ");
   if(result)
     {
      if(f_State) result&=Image(m_BmpEnabled);
      else        result&=Image(m_BmpDisabled);
     }
   if(result) m_Enabled=f_State;
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CPanel:public CControl
  {
private:
   int               GetX(Align  &p_align) const;
   int               GetY(Align  &p_align) const;
protected:
public:
   bool              Create(string Name);
   bool              Background(const string FileName,Align &p_align);
   bool              ChangeBackground(const string FileName);
   virtual bool      SaveControlState(int f_file_handle)                                  {return(false);}
   virtual bool      LoadControlState(int f_file_handle,string f_Name,ENUM_OBJECT f_Type) {return(false);}
   virtual int       Type() {return(CTRL_PANEL);}
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CPanel::Create(string Name)
  {
   bool result=CControl::Create(Name,OBJ_BITMAP_LABEL,0,0);
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int CPanel::GetX(Align &p_align) const
  {
   if(m_Chart==-1) return(0);
   int f_Left=0;
   int ChartWidth=(int)ChartGetInteger(m_Chart,CHART_WIDTH_IN_PIXELS);
   int ChartMX=(int)round(ChartWidth/2);
   switch(p_align.h_Align)
     {
      case H_ALIGN_CUSTOM_POS:     f_Left=p_align.Left;                  break;
      case H_ALIGN_LEFT_POS:       f_Left=1;                             break;
      case H_ALIGN_RIGHT_POS:      f_Left=ChartWidth-Width();            break;
      case H_ALIGN_MIDLLE_POS:     f_Left=ChartMX-(int)round(Width()/2); break;
      default:break;
     }
   return(f_Left);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int CPanel::GetY(Align &p_align) const
  {
   if(m_Chart==-1) return(0);
   int f_Top=0;

   int ChartHeigth=(int)ChartGetInteger(m_Chart,CHART_HEIGHT_IN_PIXELS);
   int ChartMY=(int)round(ChartHeigth/2);
   switch(p_align.v_Align)
     {
      case V_ALIGN_CUSTOM_POS:     f_Top=p_align.Top;                    break;
      case V_ALIGN_TOP_POS:        f_Top=1;                              break;
      case V_ALIGN_BOTTOM_POS:     f_Top=ChartHeigth-Height();           break;
      case V_ALIGN_MIDLLE_POS:     f_Top=ChartMY-(int)round(Height()/2); break;
      default:break;
     }
   return(f_Top);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CPanel::Background(const string FileName,Align &PanelAlign)
  {
   if(m_Chart==-1) return(false);
   bool result=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,0,FileName);
   result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,1,FileName);
   if(result)
     {
      result&=SetColor((color)ChartGetInteger(m_Chart,CHART_COLOR_BACKGROUND));
      result&=ObjectSetInteger(m_Chart,m_Name,OBJPROP_XDISTANCE,GetX(PanelAlign));
      result&=ObjectSetInteger(m_Chart,m_Name,OBJPROP_YDISTANCE,GetY(PanelAlign));
     }

   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CPanel::ChangeBackground(const string FileName)
  {
   if(m_Chart==-1) return(false);
   bool result=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,0,FileName);
   result&=ObjectSetString(m_Chart,m_Name,OBJPROP_BMPFILE,1,FileName);
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CEdit: public CControl
  {
private:
   color             m_BackgroundColor;
   color             m_Color;
   color             t_BackgroundColor;
   color             t_Color;
   color             t_ReadOnlyColor;
   color             t_ReadOnlyBgColor;
   string            m_FontName;
   int               m_FontSize;
protected:
   bool              ChangeColors();
public:
                     CEdit(void);
   bool              IsReadOnly;
   virtual bool      Create(string f_Name,int f_Top,int f_Left,int f_Width,int f_Heigth,int f_file_handle,CResources *p_ResObj=NULL,string f_RS_Font=" ");
   bool              ReadOnly(bool f_State);
   void              ReadOnlyColors(color f_clr=CLR_NONE,color f_clr_bg=CLR_NONE);
   void              Colors(color f_clr=CLR_NONE,color f_clr_bg=CLR_NONE);
   bool              SetFont(const string f_FontName,const int f_FontSize);
   string            Text();
   bool              IsEnabled;
   bool              Enabled(bool f_State);
   bool              IsNumeric(void);
   bool              IsDecimal(void);
   bool              SetText(string f_text);
   virtual bool      SaveControlState(int f_file_handle);
   virtual bool      LoadControlState(int f_file_handle,string f_Name,ENUM_OBJECT f_Type);
   virtual int       Type() {return(CTRL_EDIT);}
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CEdit::CEdit(void)
  {
   IsReadOnly=false;
   IsEnabled =true;
   m_BackgroundColor=Black;
   m_Color=Black;
   t_ReadOnlyColor=CLR_NONE;
   t_ReadOnlyBgColor=CLR_NONE;
   t_BackgroundColor=CLR_NONE;
   t_Color=CLR_NONE;
   m_FontName="MS Sans Serif";
   m_FontSize=8;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CEdit::Create(string f_Name,int f_Top,int f_Left,int f_Width,int f_Heigth,int f_file_handle,CResources *p_ResObj=NULL,string f_RS_Font=" ")
  {
   bool result=false;
   FontSettings _Font;
   bool _default=true;
   if(f_file_handle!=INVALID_HANDLE)
     {
      result=LoadControlState(f_file_handle,f_Name,OBJ_EDIT);
      if(result)
        {
         result&=ObjectSetInteger(m_Chart,m_Name,OBJPROP_YDISTANCE,f_Top);
         result&=ObjectSetInteger(m_Chart,m_Name,OBJPROP_XDISTANCE,f_Left);
        }

     }

   if(!result)
     {
      result=CControl::Create(f_Name,OBJ_EDIT,f_Top,f_Left);
     }

   if(result)
     {
      CResources *rs=p_ResObj;
      if(rs!=NULL && f_RS_Font!=" ")
        {
         CFontResource *rs_Font=rs.GetFontResource(f_RS_Font);
         if(rs_Font!=NULL)
           {
            rs_Font.GetFont(_Font);
            m_FontName        =_Font.FontName;
            m_FontSize        =_Font.FontSize;
            t_Color           =_Font.Clr1;
            t_BackgroundColor =_Font.Clr2;
            t_ReadOnlyColor   =_Font.Clr3;
            t_ReadOnlyBgColor =_Font.Clr4;
            _default=false;
            result&=ChangeColors();
           }
        }

      result&=ObjectSetInteger(m_Chart,m_Name,OBJPROP_YSIZE,f_Heigth);
      result&=ObjectSetInteger(m_Chart,m_Name,OBJPROP_XSIZE,f_Width);
      result&=ObjectSetString(m_Chart,m_Name,OBJPROP_FONT,m_FontName);
      result&=ObjectSetInteger(m_Chart,m_Name,OBJPROP_FONTSIZE,m_FontSize);
      result&=ObjectSetInteger(m_Chart,m_Name,OBJPROP_READONLY,IsReadOnly);

      if(_default)
        {
         result&=ObjectSetInteger(m_Chart,m_Name,OBJPROP_BGCOLOR,m_BackgroundColor);
         result&=ObjectSetInteger(m_Chart,m_Name,OBJPROP_COLOR,m_Color);
        }

     }

   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CEdit::ReadOnlyColors(color f_clr=CLR_NONE,color f_clr_bg=CLR_NONE)
  {
   t_ReadOnlyColor=f_clr;
   t_ReadOnlyBgColor=f_clr_bg;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CEdit::Colors(color f_clr=CLR_NONE,color f_clr_bg=CLR_NONE)
  {
   t_Color=f_clr;
   t_BackgroundColor=f_clr_bg;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CEdit::ChangeColors()
  {
   bool result=true;
   if(m_Chart==-1) return(false);
   if(!IsReadOnly)
     {
      m_BackgroundColor=t_BackgroundColor;
      m_Color=t_Color;
     }
   else
     {
      m_BackgroundColor=t_ReadOnlyBgColor;
      m_Color=t_ReadOnlyColor;
     }
   result&=ObjectSetInteger(m_Chart,m_Name,OBJPROP_COLOR,m_Color);
   result&=ObjectSetInteger(m_Chart,m_Name,OBJPROP_BGCOLOR,m_BackgroundColor);
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CEdit::ReadOnly(bool f_State)
  {
   if(m_Chart==-1) return(false);
   ResetLastError();
   bool result=ObjectSetInteger(m_Chart,m_Name,OBJPROP_READONLY,f_State);
   if(result)
     {
      IsReadOnly=f_State;
      result&=ChangeColors();
     }
   else  Print("Error "+DoubleToString(GetLastError(),0));
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CEdit::SetFont(const string f_FontName,const int f_FontSize)
  {
   if(m_Chart==-1) return(false);
   m_FontName=f_FontName;
   m_FontSize=f_FontSize;
   bool result=ObjectSetInteger(m_Chart,m_Name,OBJPROP_FONTSIZE,m_FontSize);
   result&=ObjectSetString(m_Chart,m_Name,OBJPROP_FONT,m_FontName);
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string CEdit::Text(void)
  {
   if(m_Chart==-1) return("");
   return(ObjectGetString(m_Chart,m_Name,OBJPROP_TEXT));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool  CEdit::SetText(string f_text)
  {
   if(m_Chart==-1) return(false);
   return(ObjectSetString(m_Chart,m_Name,OBJPROP_TEXT,f_text));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CEdit::IsNumeric(void)
  {
   bool result=false;
   string _text=Text();
   int _length=StringLen(_text);
   ushort _char;
   if(_length>0)
     {
      for(int i=0;i<_length;i++)
        {
         _char=StringGetCharacter(_text,i);
         if(_char>=48 && _char<=57)
           {
            result=true;
            continue;
           }
         else return(false);
        }
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CEdit::IsDecimal(void)
  {
   bool result=false;
   string _text=Text();
   int _length=StringLen(_text);
   ushort _char;
   if(_length>0)
     {
      for(int i=0;i<_length;i++)
        {
         _char=StringGetCharacter(_text,i);
         if(_char==46 || (_char>=48 && _char<=57))
           {
            result=true;
            continue;
           }
         else return(false);
        }
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CEdit::Enabled(bool f_State)
  {
   bool result=true;
   result&=ReadOnly(!f_State);
   if(result) IsEnabled=f_State;
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CEdit::SaveControlState(int f_file_handle)
  {
   bool result;

   if(f_file_handle<=0)                                                                  return(false);
   if(m_Chart==-1)                                                                       return(false);

   result=CControl::SaveControlState(f_file_handle);
   if(result)
     {
      string str;
      int len;
      if(FileWriteInteger(f_file_handle,(int)ObjectGetInteger(m_Chart,m_Name,OBJPROP_READONLY),CHAR_VALUE)!=sizeof(char)) return(false);
      str=Text();
      len=StringLen(str);
      if(FileWriteInteger(f_file_handle,len,INT_VALUE)!=INT_VALUE)                        return(false);
      if(len!=0) if(FileWriteString(f_file_handle,str,len)!=len)                          return(false);
     }
   else return(false);

   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CEdit::LoadControlState(int f_file_handle,string f_Name,ENUM_OBJECT f_Type)
  {
   bool result=CControl::LoadControlState(f_file_handle,f_Name,f_Type);
   if(result)
     {
      string str="";
      int len=0;
      bool g_ReadOnly=(char)FileReadInteger(f_file_handle,CHAR_VALUE);

      ReadOnly(g_ReadOnly);
      len=(int)FileReadInteger(f_file_handle,INT_VALUE);
      if(len!=0) str=FileReadString(f_file_handle,len);  else str="";
      if(!ObjectSetString(m_Chart,m_Name,OBJPROP_TEXT,str)) return(false);
     }
   else  {Print("");return(false);}
   return(result);
  }
//+------------------------------------------------------------------+

class CSpinButton:public CButton
  {
public:
                     CSpinButton(void) {ButtonState=BS_ENABLED;}
   int               ButtonState;
   void              SetButtonState(int f_State) {ButtonState=f_State;}
   virtual int       Type() {return(0);}
   virtual bool      SaveControlState(int f_file_handle)                                  {return(false);}
   virtual bool      LoadControlState(int f_file_handle,string f_Name,ENUM_OBJECT f_Type) {return(false);}
   virtual bool      GetData(string &f_ParentName,double &f_Value)                        {return(false);}
  };
//+------------------------------------------------------------------+
//|?????                                                             |
//+------------------------------------------------------------------+
class CSpinEdit:public CControl
  {

private:
   class CIncSpinButton:public CSpinButton
     {
   private:
      CSpinEdit        *ParentObject;
   public:
      virtual   int  Type() {return(CTRL_UP_SPINBUTTON);}
      virtual bool      GetData(string &f_ParentName,double &f_Value);
      virtual bool      Parent(CSpinEdit *p_ParentObject) {return((ParentObject=p_ParentObject)!=NULL);}
     };

   class CDecSpinButton:public CSpinButton
     {
   private:
      CSpinEdit        *ParentObject;
   public:
      virtual   int  Type() {return(CTRL_DN_SPINBUTTON);}
      virtual bool      GetData(string &f_ParentName,double &f_Value);
      virtual bool      Parent(CSpinEdit *p_ParentObject) {return((ParentObject=p_ParentObject)!=NULL);}
     };
   CImage           *Bkg;
   CEdit            *edField;
   CIncSpinButton   *btnInc;
   CDecSpinButton   *btnDec;
   CArrayObj        *HandlerList;   //Global handler's container
   CArrayString     *Images;
   CResources       *rs;
private:
   int               m_Top;
   int               m_Left;
   string            m_Name;
   int               m_Width;
   double            m_Max;
   double            m_Min;
   double            m_Last;
   double            m_Step;
   int               m_Digits;
   string            m_UpBmpNormal;
   string            m_UpBmpPressed;
   string            m_DnBmpNormal;
   string            m_DnBmpPressed;
   bool              Loaded;
   uchar             m_ButtonsPosition;
   int               m_file_handle;
   string            m_RS_Font;
protected:
   bool              Inc(double &f_Value);
   bool              Dec(double &f_Value);
   bool              SetText(string f_Text);
   bool              ChangeStateImg(CSpinButton *p_Button,const int f_StateType);
   void              ButtonsCheckState(const int f_StateType);
public:
                     CSpinEdit(void);
                    ~CSpinEdit(void);
   bool              Create(const string f_Name,
                            int f_Top,
                            int f_Left,
                            uchar f_ButtonsPosition,
                            CArrayObj *f_Handler=NULL,
                            int f_file_handle=INVALID_HANDLE,
                            CResources *p_ResObj=NULL,
                            string f_RS_Name=" ",
                            string f_RS_Font=" ");
   bool              CreateField(int f_Top,int f_Left,int f_Width,int f_Heigth);
   bool              CreateIncButton(int f_Top,
                                     int f_Left,
                                     string f_NormalFileName=" ",
                                     string f_DownFileName=" ",
                                     string f_DisabledFileName=" ",
                                     string f_LPFileName=" ");
   bool              CreateDecButton(int f_Top,
                                     int f_Left,
                                     string f_NormalFileName=" ",
                                     string f_DownFileName=" ",
                                     string f_DisabledFileName=" ",
                                     string f_LPFileName=" ");
   bool              TimeFrames(int f_Flag);
   bool              SetFont(const string f_FontName,const int f_FontSize);
   bool              ReadOnly(bool f_State) {return((edField!=NULL && edField.ReadOnly(f_State)));}
   void              Colors(color f_clr=CLR_NONE,color f_clr_bg=CLR_NONE) {if(edField!=NULL) edField.Colors(f_clr,f_clr_bg);}
   void              ReadOnlyColors(color f_clr=CLR_NONE,color f_clr_bg=CLR_NONE) {if(edField!=NULL) edField.ReadOnlyColors(f_clr,f_clr_bg);}
   bool              OnIncrement(double &f_Value);
   bool              OnDecrement(double &f_Value);
   bool              KeysAssigned();
   bool              OnEdit() {return(false);}
   bool              IsEnabled;
   bool              Enabled(bool f_State);
   void              SetImgs(string f_BkgFileName,string f_DisBkgFileName);
   bool              ChangeControlPosition(int f_TopShift,int f_LeftShift);
   void              SetRange(double f_Max,double f_Min,double f_Step,double Default=0.0);
   double            Value() const    {return(NormalizeDouble(m_Last,m_Digits));}
   bool              Value(const double f_Value);
   virtual bool      SaveControlState(int f_file_handle);
   virtual bool      LoadControlState(int f_file_handle,string f_Name,ENUM_OBJECT f_Type);
   virtual   int     Type() {return(CTRL_SPINEDIT);}
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CIncSpinButton::GetData(string &f_ParentName,double &f_Value)
  {
   bool result=ParentObject.OnIncrement(f_Value);
   f_ParentName=ParentObject.Name();
   ChartRedraw(m_Chart);
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CDecSpinButton::GetData(string &f_ParentName,double &f_Value)
  {
   bool result=ParentObject.OnDecrement(f_Value);
   f_ParentName=ParentObject.Name();
   ChartRedraw(m_Chart);
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CSpinEdit::CSpinEdit(void)
  {
   btnInc=NULL;
   btnDec=NULL;
   edField=NULL;
   Bkg=NULL;
   HandlerList=NULL;
   Images=NULL;
   rs=NULL;
   m_Top=0;
   m_Left=0;
   m_Name=" ";
   m_Width=0;
   m_Max=0.0;
   m_Min=0.0;
   m_Last=NULL;
   m_Step=0.0;
   m_Digits=0;
   m_UpBmpPressed=" ";
   m_DnBmpPressed=" ";
   Loaded=false;
   IsEnabled=true;
   m_ButtonsPosition=BP_RIGHT;
   m_file_handle=INVALID_HANDLE;
   m_RS_Font=" ";
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CSpinEdit::~CSpinEdit(void)
  {
   if(btnInc!=NULL)
     {
      delete btnInc;
      btnInc=NULL;
     }
   if(btnDec!=NULL)
     {
      delete btnDec;
      btnDec=NULL;
     }
   if(edField!=NULL)
     {
      delete edField;
      edField=NULL;
     }
   if(Bkg!=NULL)
     {
      delete Bkg;
      Bkg=NULL;
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CSpinEdit::Create(const string f_Name,
                       int f_Top,
                       int f_Left,
                       uchar f_ButtonsPosition,
                       CArrayObj *f_Handler=NULL,
                       int f_file_handle=INVALID_HANDLE,
                       CResources *p_ResObj=NULL,
                       string f_RS_Name=" ",
                       string f_RS_Font=" ")
  {
   bool result=true;

   m_ButtonsPosition=f_ButtonsPosition;

   SetName(f_Name);
   SetTop(f_Top);
   SetLeft(f_Left);

   rs=p_ResObj;

   if(rs!=NULL && f_RS_Name!=" " && f_RS_Font!=" ")
     {
      m_RS_Font=f_RS_Font;
      CImgResource *rs_Img=rs.GetImgsResource(f_RS_Name);
      if(rs_Img!=NULL) Images=rs_Img.GetResList();
     }

   if(m_ButtonsPosition==BP_LEFT_RIGHT && (Bkg=new CImage)!=NULL)
     {
      result&=Bkg.Create("bkg"+f_Name,f_Top,f_Left);
      m_Width=Bkg.Width();
      if(Images!=NULL)
        {
         Bkg.SetImgs(Images.At(0),Images.At(1));
         Bkg.Image(Images.At(0));
        }
     }
   if(result)
     {
      m_Top=f_Top;
      m_Left=f_Left;
      m_Name=f_Name;
      m_ButtonsPosition=f_ButtonsPosition;
      m_file_handle=f_file_handle;
      HandlerList=f_Handler;
     }
   else Print("CSpinEdit:: Create SpinEdit failed");
   return(result);
  }
//+------------------------------------------------------------------+
//|f_Top and f_Left determine the shift relative to the global anchor    |
//|point of the object with the coordinates (m_Top,m_Left)                    |
//+------------------------------------------------------------------+
bool CSpinEdit::CreateField(int f_Top,int f_Left,int f_Width,int f_Heigth)
  {
   int _Top=0;
   int _Left=0;

   bool result=((edField=new CEdit)!=NULL);
   if(result)
     {
      switch(m_ButtonsPosition)
        {
         case BP_RIGHT:
           {
            _Top=m_Top+f_Top;
            _Left=m_Left+f_Left;
           }
         break;

         case BP_LEFT_RIGHT:
           {
            _Top=m_Top+f_Top;
            if(btnDec!=NULL)
              {
               _Left=m_Left+btnDec.Width()+f_Left;
              }
            else _Left=m_Left+f_Left;
           }
         break;
        }

      Loaded=(m_file_handle!=INVALID_HANDLE);
      result&=edField.Create("ed"+m_Name,_Top,_Left,f_Width,f_Heigth,m_file_handle,rs,m_RS_Font);
      if(result) HandlerList.Add(GetPointer(edField));
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|It is supposed that the "+" button is always on the right. Parameters  |
//|f_Top and f_Left set the button shift relative to the top right |
//|text box corner.                                                  |
//+------------------------------------------------------------------+
bool CSpinEdit::CreateIncButton(int f_Top,
                                int f_Left,
                                string f_NormalFileName=" ",
                                string f_DownFileName=" ",
                                string f_DisabledFileName=" ",
                                string f_LPFileName=" ")
  {
   int _Top=0;
   int _Left=0;
   bool result=true;
   if(edField!=NULL)
     {
      result=((btnInc=new CIncSpinButton)!=NULL);
      if(result)
        {
         _Top=edField.Top()+f_Top;
         _Left=edField.Left()+edField.Width()+f_Left;
         result&=btnInc.Create("sup"+m_Name,_Top,_Left);
         if(Images!=NULL)
           {
            result&=btnInc.SetImgs(Images.At(2),Images.At(3),Images.At(4));
            m_UpBmpNormal =Images.At(2);
            m_UpBmpPressed=Images.At(5);
           }
         else
           {
            result&=btnInc.SetImgs(f_NormalFileName,f_DownFileName,f_DisabledFileName);
            m_UpBmpNormal =f_NormalFileName;
            m_UpBmpPressed=f_LPFileName;
           }
         btnInc.Clickable(true);
         btnInc.Player=rs.GetWavResource();
         btnInc.Down(false);
         result&=btnInc.Parent(GetPointer(this));
         if(result) HandlerList.Add(GetPointer(btnInc));
        }
     }
   else
     {
      result=false;
      Print("CreateIncButton:: Before creating this button, you must create numeric field");
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|It is supposed that the "-" button can be both to the right and   |
//|to the left of the text box. Parameters f_Top and f_Left set       |
//|the button shift relative to the global anchor point, if the button|
//|is on the left(BP_LEFT_RIGHT mode). If the button is on |
//|the right, the parameters f_Top and f_Left set the shift relative to the     |
//|bottom left corner of the "+" button for the BP_RIGHT mode                |
//+------------------------------------------------------------------+
bool CSpinEdit::CreateDecButton(int f_Top,
                                int f_Left,
                                string f_NormalFileName=" ",
                                string f_DownFileName=" ",
                                string f_DisabledFileName=" ",
                                string f_LPFileName=" ")
  {
   int _Top=0;
   int _Left=0;
   bool result=true;

   switch(m_ButtonsPosition)
     {
      case BP_LEFT_RIGHT:
        {
         _Top=m_Top+f_Top;
         _Left=m_Left+f_Left;
        }
      break;

      case BP_RIGHT:
        {
         if(btnInc!=NULL)
           {
            _Top=btnInc.Top()+btnInc.Height()+f_Top;
            _Left=btnInc.Left()+f_Left;
           }
         else
           {
            result=false;
            Print("CreatDecButton:: Before creating this button, you must create Increment button");
           }
        }
      break;
     }

   if(result)
     {
      result&=((btnDec=new CDecSpinButton)!=NULL);
      result&=btnDec.Create("sdn"+m_Name,_Top,_Left);
      if(Images!=NULL)
        {
         result&=btnDec.SetImgs(Images.At(6),Images.At(7),Images.At(8));
         m_DnBmpNormal =Images.At(6);
         m_DnBmpPressed=Images.At(9);
        }
      else
        {
         result&=btnDec.SetImgs(f_NormalFileName,f_DownFileName,f_DisabledFileName);
         m_DnBmpNormal =f_NormalFileName;
         m_DnBmpPressed=f_LPFileName;
        }
      btnDec.Clickable(true);
      btnDec.Down(false);
      btnDec.Player=rs.GetWavResource();
      result&=btnDec.Parent(GetPointer(this));
      if(result) HandlerList.Add(GetPointer(btnDec));
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CSpinEdit::SetRange(double f_Max,double f_Min,double f_Step,double Default=0.0)
  {
   m_Step=f_Step;
   double rests=(double)MathMod(m_Step,1);
   if(rests!=0)
     {
      
      int step=10;
      for(int i=1;i<=5;i++)
        {
         if(rests*step>=1)
           {
            m_Digits=i;
            break;
           }
         step*=10;
        }
     }
     else m_Digits=0;
   m_Max=NormalizeDouble(f_Max,m_Digits);
   m_Min=NormalizeDouble(f_Min,m_Digits);
   //if(!Loaded)m_Last=(Default==0.0)?m_Min:Default;
   m_Last=(Default==0.0)?m_Min:Default;
   if(edField!=NULL) edField.SetText(DoubleToString(m_Last,m_Digits));
   if(IsEnabled && btnDec!=NULL)ChangeStateImg(btnDec,(m_Last==m_Min)?BS_DISABLED:BS_ENABLED);
   if(IsEnabled && btnInc!=NULL)ChangeStateImg(btnInc,(m_Last==m_Max)?BS_DISABLED:BS_ENABLED);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CSpinEdit::SetFont(const string f_FontName,const int f_FontSize)
  {
   if(edField!=NULL)
     {
      return(edField.SetFont(f_FontName,f_FontSize));
     }
   return(false);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CSpinEdit::Value(const double f_Value)
  {
   if(f_Value<m_Min || f_Value>m_Max) return(false);
   m_Last=f_Value;
   if(edField!=NULL) edField.SetText(DoubleToString(m_Last,m_Digits));
   
   if(btnDec!=NULL)
     {
      if(m_Last==m_Min) {if(IsEnabled)ChangeStateImg(btnDec,BS_DISABLED);}
     }
     
   if(btnInc!=NULL)
     {
      if(m_Last==m_Max) {if(IsEnabled)ChangeStateImg(btnInc,BS_DISABLED);}
     }
   return(true);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CSpinEdit::SetText(string f_Text)
  {
   if(edField!=NULL)return(edField.SetText(f_Text));
   return(false);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CSpinEdit::Inc(double &f_Value)
  {
   bool result=true;
   double Value=(m_Last!=m_Max && m_Last+m_Step<m_Max)?(m_Last+m_Step):m_Max;
   if(NormalizeDouble(Value,m_Digits)!=m_Max)
     {
      m_Last=Value;
      NormalizeDouble(m_Last,m_Digits);
      result&=(m_Last<m_Max);
     }
   else
     {
      if(m_Last!=m_Max) {m_Last=m_Max; result=true;}
      else result=false;
     }
   f_Value=NormalizeDouble(m_Last,m_Digits);
   if(edField!=NULL) edField.SetText(DoubleToString(m_Last,m_Digits));
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CSpinEdit::Dec(double &f_Value)
  {
   bool result=true;
   double Value=(m_Last!=m_Min && m_Last-m_Step>m_Min)?(m_Last-m_Step):m_Min;
   if(NormalizeDouble(Value,m_Digits)!=m_Min)
     {
      m_Last=Value;
      NormalizeDouble(m_Last,m_Digits);
      result&=(m_Last>m_Min);
     }
   else
     {
      if(m_Last!=m_Min) {m_Last=m_Min; result=true;}
      else result=false;
     }
   f_Value=NormalizeDouble(m_Last,m_Digits);
   if(edField!=NULL) edField.SetText(DoubleToString(m_Last,m_Digits));
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CSpinEdit::KeysAssigned(void)
  {
   bool result=false;
   string _text=edField.Text();
   int _length=StringLen(_text);
   double value=0.0;

   if(_length>0)
     {
      result=edField.IsDecimal();
      value=StringToDouble(_text);
      if(result)
        {
         if(value>=m_Min && value<=m_Max)
           {
            if(value>m_Last)
              {
               m_Last=value;
               NormalizeDouble(m_Last,m_Digits);
               ButtonsCheckState(INC_EVENT);
              }
            else
              {
               m_Last=value;
               NormalizeDouble(m_Last,m_Digits);
               ButtonsCheckState(DEC_EVENT);
              }
           }
         else
           {
            if(value<m_Min)
              {
               m_Last=m_Min;
               NormalizeDouble(m_Last,m_Digits);
               ButtonsCheckState(DEC_EVENT);
              }
            else
              {
               if(value>m_Max)
                 {
                  m_Last=m_Max;
                  NormalizeDouble(m_Last,m_Digits);
                  ButtonsCheckState(INC_EVENT);
                 }
              }
           }
        }
      else result=false;
     }
   edField.SetText(DoubleToString(m_Last,m_Digits));
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CSpinEdit::Enabled(bool f_State)
  {
   bool result=true;
   if(btnInc!=NULL)
     {
      if(m_Last==m_Max) result&=btnInc.Enabled(false);
      else result&=btnInc.Enabled(f_State);
     }
   if(btnDec!=NULL)
     {
      if(m_Last==m_Min) result&=btnDec.Enabled(false);
      else result&=btnDec.Enabled(f_State);
     }

   if(edField!=NULL) result&=edField.ReadOnly(!f_State);
   if(Bkg!=NULL) result&=Bkg.Enabled(f_State);

   if(result) IsEnabled=f_State;
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CSpinEdit::SetImgs(string f_BkgFileName,string f_DisBkgFileName)
  {
   if(Bkg!=NULL)
     {
      Bkg.SetImgs(f_BkgFileName,f_DisBkgFileName);
      Bkg.Image(f_BkgFileName);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CSpinEdit::TimeFrames(int f_Flag)
  {
   bool result=true;
   if(Bkg!=NULL)
     {
      result&=Bkg.TimeFrames(f_Flag);
     }
   if(btnInc!=NULL)
     {
      result&=btnInc.TimeFrames(f_Flag);
     }
   if(btnDec!=NULL)
     {
      result&=btnDec.TimeFrames(f_Flag);
     }
   if(edField!=NULL)
     {
      result&=edField.TimeFrames(f_Flag);
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CSpinEdit::ChangeControlPosition(int f_TopShift,int f_LeftShift)
  {

   bool result=true;
   if(result && Bkg!=NULL)
     {
      result&=Bkg.ChangeControlPosition(f_TopShift,f_LeftShift);
     }
   if(result && btnInc!=NULL)
     {
      result&=btnInc.ChangeControlPosition(f_TopShift,f_LeftShift);
     }
   if(result && btnDec!=NULL)
     {
      result&=btnDec.ChangeControlPosition(f_TopShift,f_LeftShift);
     }
   if(result && edField!=NULL)
     {
      result&=edField.ChangeControlPosition(f_TopShift,f_LeftShift);
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CSpinEdit::ChangeStateImg(CSpinButton *p_Button,const int f_StateType)
  {
   bool result=true;
   string bmp="";
   switch(f_StateType)
     {
      case BS_ENABLED:
        {
         if(!p_Button.IsEnabled) p_Button.Enabled(true);
         else
           {
            if(p_Button==btnInc) bmp=m_UpBmpNormal;
            else                 bmp=m_DnBmpNormal;
            p_Button.SetImgs(bmp," "," ");
           }
        }
      break;

      case BS_DISABLED:
        {
         if(p_Button==btnInc) bmp=m_UpBmpNormal;
         else                 bmp=m_DnBmpNormal;

         p_Button.SetImgs(bmp," "," ");
         p_Button.Enabled(false);
        }
      break;

      case BS_LASTPRESSED:
        {
         if(p_Button==btnInc) bmp=m_UpBmpPressed;
         else                 bmp=m_DnBmpPressed;
         p_Button.SetImgs(bmp," "," ");
        }
      break;
     }
   if(result) p_Button.SetButtonState(f_StateType);
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CSpinEdit::ButtonsCheckState(const int f_EventType)
  {
   if(m_Last!=m_Max && m_Last!=m_Min)
     {
      if(f_EventType==INC_EVENT)
        {
         if(!btnDec.IsEnabled)
           {
            ChangeStateImg(btnDec,BS_ENABLED);
           }

         if(m_UpBmpPressed!=" ")
           {
            if(btnInc.ButtonState==BS_ENABLED)      ChangeStateImg(btnInc,BS_LASTPRESSED);
            if(btnDec.ButtonState==BS_LASTPRESSED)  ChangeStateImg(btnDec,BS_ENABLED);
           }
        }
      else
        {
         if(!btnInc.IsEnabled)
           {
            ChangeStateImg(btnInc,BS_ENABLED);
           }

         if(m_DnBmpPressed!=" ")
           {
            if(btnDec.ButtonState==BS_ENABLED)     ChangeStateImg(btnDec,BS_LASTPRESSED);
            if(btnInc.ButtonState==BS_LASTPRESSED) ChangeStateImg(btnInc,BS_ENABLED);
           }
        }
     }
   else
     {
      if(f_EventType==INC_EVENT)
        {
         if(m_Last==m_Max && btnInc.IsEnabled) ChangeStateImg(btnInc,BS_DISABLED);
          ChangeStateImg(btnDec,BS_ENABLED);
        }
      else
        {
         if(m_Last==m_Min && btnDec.IsEnabled) ChangeStateImg(btnDec,BS_DISABLED);
          ChangeStateImg(btnInc,BS_ENABLED);
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CSpinEdit::OnIncrement(double &f_Value)
  {
   bool result=Inc(f_Value);
   if(result && btnDec!=NULL && btnInc!=NULL)
     {
      ButtonsCheckState(INC_EVENT);
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CSpinEdit::OnDecrement(double &f_Value)
  {
   bool result=Dec(f_Value);
   if(result && btnDec!=NULL && btnInc!=NULL)
     {
      ButtonsCheckState(DEC_EVENT);
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CSpinEdit::LoadControlState(int f_file_handle,string f_Name,ENUM_OBJECT f_Type)
  {
   bool result=edField.LoadControlState(f_file_handle,f_Name,f_Type);
   if(result) m_Last=(double)FileReadDouble(f_file_handle);
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CSpinEdit::SaveControlState(int f_file_handle)
  {
   bool result=edField.SaveControlState(f_file_handle);
   if(result)
     {
      if(FileWriteDouble(f_file_handle,(m_Last==NULL)?(double)m_Min:(double)m_Last)!=sizeof(double)) return(false);

     }
   return(result);
  }
//+------------------------------------------------------------------+  

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CGauge:public CControl
  {
private:
   CEdit            *ProgressBar;
   CLabel           *Text;
   CImage           *Background;
   string            Suffix;
   string            m_Gauge_Name;
   int               m_Top;
   int               m_Left;
   int               m_Bar_Left;
   int               m_Bar_Width;
   int               m_Bar_Heigth;
   int               m_Range;
   double            m_StepSize;
   int               m_LastPercent;
   double            m_LastRests;
   bool              m_ShowText;
   ushort            m_GaugeStep;
protected:
   void              Repaint(const int f_Points);
public:
                     CGauge(void);
                    ~CGauge(void);
   bool              Create(const string f_Name,const string f_BmpFile,int f_Top,int f_Left);
   void              SetSuffix(const string f_Suffix="%") {Suffix=f_Suffix;}
   bool              SetColors(color f_clBorder,color f_clBkg,color f_clText);
   void              SetRange(const int f_Max,const int f_Min) {m_Range=f_Max-f_Min;}
   void              ShowText(bool f_Show);
   void              Progress(const int f_Value);
   void              SetProgressStep(const ushort f_GaugeStep=1);
   virtual bool      SaveControlState(int f_file_handle)                                  {return(false);}
   virtual bool      LoadControlState(int f_file_handle,string f_Name,ENUM_OBJECT f_Type) {return(false);}
   virtual   int     Type() {return(CTRL_GAUGE);}
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CGauge::CGauge(void)
  {
   ProgressBar=NULL;
   Text=NULL;
   Background=NULL;
   Suffix="%";
   m_Gauge_Name="";
   m_Top=0;
   m_Left=0;
   m_Bar_Left=0;
   m_Bar_Width=0;
   m_Bar_Heigth=0;
   m_Range=0;
   m_StepSize=0.0;
   m_LastPercent=0;
   m_LastRests=0;
   m_ShowText=true;
   m_GaugeStep=1;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CGauge::~CGauge(void)
  {
   if(ProgressBar!=NULL)
     {
      delete ProgressBar;
      ProgressBar=NULL;
     }
   if(Text!=NULL)
     {
      delete Text;
      Text=NULL;
     }
   if(Background!=NULL)
     {
      delete Background;
      Background=NULL;
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CGauge::Create(const string f_Name,const string f_BmpFile,int f_Top,int f_Left)
  {
   m_Gauge_Name=f_Name;
   bool result=((Background=new CImage)!=NULL);
   if(result)
     {
      m_Top=f_Top;
      m_Left=f_Left;
      m_Bar_Left=f_Left;
      result&=Background.Create("bcg"+m_Gauge_Name,m_Top,m_Left);
      result&=Background.Image(f_BmpFile);
     }

   m_Bar_Width=Background.Width();
   m_Bar_Heigth=Background.Height();
   m_StepSize=(double)m_Bar_Width/100;

   result&=((ProgressBar=new CEdit)!=NULL);
   if(result)
     {
      result&=ProgressBar.Create("pBar"+m_Gauge_Name,m_Top,m_Left,m_Bar_Width,m_Bar_Heigth,-1);
     }

   result&=((Text=new CLabel)!=NULL);
   if(result)
     {
      int X_pos=m_Left+m_Bar_Width+10;
      int Y_pos=m_Top+(int)round(m_Bar_Heigth/2);
      result&=Text.Create("lbl"+m_Gauge_Name,Y_pos,X_pos);
      if(result)
        {
         result&=Text.SetAnchorPoint(ANCHOR_LEFT);
         result&=Text.SetCaption(IntegerToString(0)+Suffix);
        }
     }
   if(!result)
     {
      if(ProgressBar!=NULL)
        {
         delete ProgressBar;
         ProgressBar=NULL;
        }

      if(Text!=NULL)
        {
         delete Text;
         Text=NULL;
        }

      if(Background!=NULL)
        {
         delete Background;
         Background=NULL;
        }
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CGauge::SetColors(color f_clBorder,color f_clBkg,color f_clText)
  {
   ProgressBar.ReadOnlyColors(f_clBorder,f_clBkg);
   bool result=ProgressBar.ReadOnly(true);
   result&=Text.SetColor(f_clText);
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CGauge::Progress(const int f_Value)
  {
   int Percent=(int)MathFloor(f_Value*100/m_Range);
   if(Percent<=100 && Percent-m_LastPercent>=1)
     {
      if(m_ShowText) Text.SetCaption(IntegerToString(Percent)+Suffix);
      Repaint(Percent-m_LastPercent);
      m_LastPercent=Percent;
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CGauge::SetProgressStep(const ushort f_GaugeStep=1)
  {
   m_GaugeStep=((int)MathMod(100,f_GaugeStep)==0)?f_GaugeStep:1;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CGauge::Repaint(const int f_Step)
  {
   double StepValue=f_Step*m_StepSize+m_LastRests;
   int Shift=(int)MathFloor(StepValue);
   if(Shift>=1 && Shift>=m_GaugeStep)
     {
      bool result=ProgressBar.SetWidth(m_Bar_Width-Shift)&ProgressBar.SetLeft(m_Bar_Left+Shift);
      if(result)
        {
         m_Bar_Width=ProgressBar.Width();
         m_Bar_Left=ProgressBar.Left();
         m_LastRests=StepValue-Shift;
         ChartRedraw();
        }
     }
   else m_LastRests=StepValue;

  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CGauge::ShowText(bool f_Show)
  {
   m_ShowText=f_Show;
   if(!f_Show && Text!=NULL)
     {
      delete Text;
      Text=NULL;
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CTabSheet:public CContainer
  {
private:
   string            m_Name;
   bool              m_Active;
public:
                     CTabSheet(void);
   bool              CreateSheet(string f_Name);
   bool              IsActive(void) {return((bool)m_Active);}
   bool              Active(bool f_State);
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CTabSheet::CTabSheet(void)
  {
   m_Name=" ";
   m_Active=false;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CTabSheet::CreateSheet(string f_Name)
  {
   m_Name=f_Name;
   return(true);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CTabSheet::Active(bool f_State)
  {
   bool result=Visible(f_State);
   if(result)
     {
      m_Active=f_State;
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CTabHeader: public CRadioControl
  {
private:
   CTabSheet        *ManagedSheet;
   CCaption         *Caption;
   CGroupControl    *Parent;
   ENUM_TABS_ORIENTATION m_Align;
public:
                     CTabHeader(void);
   void              SetParent(CTabSheet *p_TabSheet,ENUM_TABS_ORIENTATION f_Align,CGroupControl *p_Parent)
     {
      ManagedSheet=p_TabSheet;
      m_Align=f_Align;
      Parent=p_Parent;
     }
   CTabSheet        *TabSheet() {return(ManagedSheet);}
   virtual bool      Active(bool f_State);
   int               Type() {return(CTRL_TAB_HEADER);}
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CTabHeader::CTabHeader(void)
  {
   Caption=NULL;
   ManagedSheet=NULL;
   m_Align=TO_BOTTOM;
   Parent=NULL;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CTabHeader::Active(bool f_State)
  {
   bool result=CRadioControl::Active(f_State);
   if(result && ManagedSheet!=NULL && ManagedSheet.Active(f_State))
     {
      switch(m_Align)
        {
         case TO_BOTTOM: break;
         case TO_RIGHT: break;
         case TO_LEFT: SetLeft(Parent.Left()-Width()); break;
         case TO_TOP: SetTop(Parent.Top()-Height()); break;
        }

      int X_pos=Left()+(int)round(Width()/2);
      int Y_pos=Top()+(int)round(Height()/2);

      if(Caption!=NULL)
        {
         result&=Caption.SetTop(Y_pos);
         result&=Caption.SetLeft(X_pos);
        }
      else
        {
         result&=((Caption=objCaption())!=NULL);
         if(result)
           {
            result&=Caption.SetTop(Y_pos);
            result&=Caption.SetLeft(X_pos);
            result&=Caption.SetAnchorPoint(ANCHOR_CENTER);
           }
        }
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CTabControl: public CGroupControl
  {
private:
   int               m_HeaderTop;
   int               m_HeaderLeft;
   CContainer       *Sheets;
   CImage           *Bkg;
   ENUM_TABS_ORIENTATION m_Align;
   string            m_BkgFileName;
   bool              m_Visible;
   bool              m_Enabled;
public:
                     CTabControl(void);
                    ~CTabControl(void);
   bool              Create(string f_ControlName,
                            int f_Top,
                            int f_Left,
                            uchar f_TabsCount,
                            ENUM_TABS_ORIENTATION f_Align=TO_BOTTOM,
                            CResources *p_RSGlobal=NULL,
                            string f_RSName=" ",
                            string f_RSFont=" ");
   CTabSheet        *AddItem(string f_Name,string f_HeaderCaption,bool f_HeaderState,CArrayObj *p_HandlerList=NULL);
   virtual bool      Enabled(bool f_State);
   virtual bool      Enabled(void) const {return(m_Enabled);}
   virtual bool      Visible(void) const {return(m_Visible);}
   virtual bool      Visible(bool f_State);
   bool              TimeFrames(int f_Flag);
   bool              ChangeControlPosition(int f_TopShift,int f_LeftShift);
   virtual int       Type() {return(CTRL_TABCONTROL);}
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CTabControl::CTabControl(void)
  {
   m_HeaderTop=0;
   m_HeaderLeft=0;
   Bkg=NULL;
   Sheets=NULL;
   m_Align=TO_BOTTOM;
   m_BkgFileName=" ";
   m_Visible=true;
   m_Enabled=true;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CTabControl::~CTabControl(void)
  {
   if(Bkg!=NULL) delete Bkg;
   if(Sheets!=NULL) delete Sheets;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CTabControl::Create(string f_ControlName,
                         int f_Top,
                         int f_Left,
                         uchar f_TabsCount,
                         ENUM_TABS_ORIENTATION f_Align=TO_BOTTOM,
                         CResources *p_RSGlobal=NULL,
                         string f_RSName=" ",
                         string f_RSFont=" ")
  {
   bool result=(CGroupControl::Create(f_ControlName,f_Top,f_Left,f_TabsCount,p_RSGlobal,f_RSName,f_RSFont)) && (f_TabsCount!=0) && ((Sheets=new CContainer)!=NULL);
   if(result)
     {

      result&=Sheets.Resize(f_TabsCount);
      m_Align=f_Align;
      m_BkgFileName=Imgs().At(0);
      if(m_BkgFileName!=" ")
        {
         result&=((Bkg = new CImage)!=NULL);
         result&=Bkg.Create("bkg"+f_ControlName,f_Top,f_Left);
         result&=Bkg.Image(m_BkgFileName);

         switch(m_Align)
           {
            case TO_BOTTOM:
              {
               m_HeaderTop=Bkg.Top()+Bkg.Height()-1;
               m_HeaderLeft=Bkg.Left();
              }
            break;

            case TO_TOP:
              {
               m_HeaderTop=Bkg.Top()+1;
               m_HeaderLeft=Bkg.Left();
              }
            break;

            case TO_LEFT:
              {
               m_HeaderTop=Bkg.Top();
               m_HeaderLeft=Bkg.Left()+1;
              }
            break;

            case TO_RIGHT:
              {
               m_HeaderTop=Bkg.Top();
               m_HeaderLeft=Bkg.Left()+Bkg.Width()-1;
              }
            break;
           }
        }
      else
        {
         Print("CTabControl::Create()-common error");
         return(false);
        }
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CTabSheet *CTabControl::AddItem(string f_Name,string f_HeaderCaption,bool f_HeaderState,CArrayObj *p_HandlerList=NULL)
  {
   bool result=true;
   CTabSheet  *Tab=new CTabSheet;
   CTabHeader *Header=NULL;
   CArrayObj  *Handler=p_HandlerList;
   CCaption   *objCaption=NULL;
   if(Tab!=NULL)
     {
      result&=Tab.CreateSheet(f_Name);
      result&=Sheets.AddItem(Tab);
      if(result)
        {
         result&=((Header=new CTabHeader)!=NULL);
         result&=Header.Create("tab"+f_Name,m_HeaderTop,m_HeaderLeft,f_HeaderCaption,Items());
         result&=Header.SetImgs(m_Enb_ActiveFileName,m_Enb_DisActiveFileName,m_Dsb_ActiveFileName,m_Dsb_DisActiveFileName);
         Header.Font(m_FontName,m_FontSize,m_ActiveClr,m_DisActiveClr,m_DisabledClr);
         if(result)
           {
            objCaption=Header.objCaption();
            Header.InitWAVbox(WAVbox());
            Header.SetParent(Tab,m_Align,GetPointer(this));
            Header.Active(f_HeaderState);
            switch(m_Align)
              {
               case TO_TOP:
               case TO_BOTTOM:
                 {
                  m_HeaderLeft+=Header.Width()+1;
                 }
               break;

               case TO_LEFT:
                 {
                  m_HeaderTop+=Header.Height()+1;
                  Header.SetCaptionAngle(90.0);
                 }
               break;

               case TO_RIGHT:
                 {
                  m_HeaderTop+=Header.Height()+1;
                  Header.SetCaptionAngle(-90.0);
                 }
               break;
              }

           }
         if(result && Handler!=NULL)
           {
            Handler.Add(Header);
            Handler.Add(objCaption);
           }
        }
     }
   if(result)
     {
      return(Tab);
     }
   else return(NULL);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CTabControl::Visible(bool f_State)
  {
   bool result=Bkg.Visible(m_Visible=f_State);
   result&=CGroupControl::Visible(f_State);
   CTabSheet *obj;
   int i;
   for(i=0;i<Sheets.TotalItems();i++)
     {
      obj=Sheets.Item(i);
      if(obj.IsActive())
        {
         result&=obj.Visible(f_State);
         break;
        }
     }
   if(result) m_Visible=f_State;
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CTabControl::Enabled(bool f_State)
  {
   bool result=CGroupControl::Enabled(f_State);
   CTabSheet *obj;
   int i;
   for(i=0;i<Sheets.TotalItems();i++)
     {
      obj=Sheets.Item(i);
      if(obj.IsActive())
        {
         obj.Enabled(f_State);
         break;
        }
     }
   m_Enabled=f_State;
   return(true);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CTabControl::TimeFrames(int f_Flag)
  {
   if(Bkg!=NULL) Bkg.TimeFrames(f_Flag);
   bool result=CGroupControl::TimeFrames(f_Flag);;
   CTabSheet *obj;
   int i;
   for(i=0;i<Sheets.TotalItems();i++)
     {
      obj=Sheets.Item(i);
      if(obj.IsActive()) result&=obj.TimeFrames(f_Flag);
      else result&=obj.TimeFrames(OBJ_NO_PERIODS);
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CTabControl::ChangeControlPosition(int f_TopShift,int f_LeftShift)
  {
   bool result=CGroupControl::ChangeControlPosition(f_TopShift,f_LeftShift);
   if(Bkg!=NULL) result&=Bkg.ChangeControlPosition(f_TopShift,f_LeftShift);
   result&=Sheets.ChangeControlPosition(f_TopShift,f_LeftShift);
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CRadioButton: public CRadioControl
  {
public:
   virtual bool      Active(bool f_State);
   int               Type() {return(CTRL_RADIOBUTTON);}
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CRadioButton::Active(bool f_State)
  {
   bool result=CRadioControl::Active(f_State);
   CLabel *Caption=objCaption();
   if(result && Caption!=NULL)
     {
      int X_pos=Left()+Width()+10;
      int Y_pos=Top()+(int)ceil(Height()/2);
      result&=Caption.SetTop(Y_pos);
      result&=Caption.SetLeft(X_pos);
      result&=Caption.SetAnchorPoint(ANCHOR_LEFT);
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CRadioGroup:public CGroupControl
  {
public:
   CRadioButton     *AddItem(string f_Name,int f_Top,int f_Left,string f_Caption,bool f_Checked,CArrayObj *p_HandlerList=NULL);
   virtual int       Type() {return(CTRL_RADIOGROUP);}
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CRadioButton *CRadioGroup::AddItem(string f_Name,int f_Top,int f_Left,string f_Caption,bool f_Checked,CArrayObj *p_HandlerList=NULL)
  {
   CRadioButton *rb=NULL;
   CArrayObj    *Handler=p_HandlerList;
   CCaption     *objCaption=NULL;

   bool result=(Items()!=NULL && ((rb=new CRadioButton)!=NULL));
   if(result)
     {
      result&=rb.Create(f_Name,f_Top,f_Left,f_Caption,Items());
      if(result)
        {
         result&=rb.SetImgs(m_Enb_ActiveFileName,m_Enb_DisActiveFileName,m_Dsb_ActiveFileName,m_Dsb_DisActiveFileName);
         rb.Font(m_FontName,m_FontSize,m_ActiveClr,m_DisActiveClr,m_DisabledClr);
         result&=rb.Active(f_Checked);
         rb.InitWAVbox(WAVbox());
         objCaption=rb.objCaption();
        }
      if(result && Handler!=NULL)
        {
         Handler.Add(rb);
         Handler.Add(objCaption);
        }
     }
   return(rb);
  }

//+------------------------------------------------------------------+
