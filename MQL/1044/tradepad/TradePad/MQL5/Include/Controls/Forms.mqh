//+------------------------------------------------------------------+
//|                                                        Forms.mqh |
//|                                                  2011, KTS Group |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "2011, KTS Group" 

#define  CNTR_MAIN                   (0)
#define  CNTR_HANDLER                (1)
//---
#define  REPAINT_REASON_MOVE         (0)
#define  REPAINT_REASON_DRAG         (1)
#define  REPAINT_REASON_RESIZECHART  (2)
//---
#define  FRM_TYPE_MINIMIZE           (0)
#define  FRM_TYPE_MAXIMIZE           (1)
#define  FRM_TYPE_DRAG               (2)
//---
#define  WAVS_CTRLS                  (0)
#define  WAVS_EVNTS                  (1)

#include <Controls\Controls.mqh>
//+------------------------------------------------------------------+
//|Base class for all forms                                          |
//+------------------------------------------------------------------+
class CForm: public CFrame
  {
private:
   CObject          *m_Handle;
   int               m_Width;
   int               m_Height;
   string            m_FrameName;
   int               m_ChartWidth;
   int               m_ChartHeight;
   int               m_hWnd;
   Align             m_FrameAlign;
   string            m_FileNameMaximize;
   string            m_FileNameMinimize;
   string            m_FileNameDrag;
   string            m_FileNameDisabled;
   uchar             m_LastPanelType;
   uchar             m_PanelType;
   bool              m_Enabled;
   bool              m_Visible;
   CImage           *m_BkgObj;
   CArrayString     *ProtectedObjs;                      // contained 
   CResources       *Resources;                          // contained images for all controls,Wav-player,font settings
   CContainer       *MainContainer;                      // main container for all objects 
   CContainer       *HandlerContainer;                   // contained objects for "EventsHandler" function
   CWAVbox          *CTRLS_WAVbox;                       // player WAV-files for controls
   CWAVbox          *EVNTS_WAVbox;                       // player WAV-files for events
   FontSettings      m_Font;
   int               m_SetupFile_Handle;
protected:
   void              Destroy(void);
   int               GetLeft(Align &p_align) const;
   int               GetTop(Align &p_align) const;
   bool              ChangeBackground(uchar f_FrameType);

public:
                     CForm(void);
                    ~CForm(void);
   virtual bool      Create(long f_SubWin,string f_FrameName,Align &f_Align,CResources *p_Resources=NULL);
   virtual bool      Create(long f_SubWin,string f_FrameName,CResources *p_Resources=NULL);
   uchar             FrameState(void) {return(m_PanelType);}
   virtual void      OnChartEvent(const int id,const long &lparam,const double &dparam,const string &sparam);
   virtual bool      Repaint(uchar f_Reason);
   CContainer       *GetContainer(uchar f_Type);
   //---
   CTabControl      *CreateTabs(string f_ControlName,int f_Top,int f_Left,uchar f_TabsCount,string f_RS_SectionName,string f_RS_Font,CContainer *p_Parent=NULL);
   CRadioGroup      *CreateRadioGroup(string f_ControlName,int f_Size,string f_RS_SectionName,string f_RS_Font,CContainer *p_Parent=NULL);
   CCheckbox        *CreateCheckBox(string f_ControlName,int f_Top,int f_Left,string f_Caption,string f_RS_SectionName,string f_RS_Font,CContainer *p_Parent=NULL);
   CButton          *CreateButton(string f_ControlName,int f_Top,int f_Left,string f_Caption,string f_RS_SectionName=" ",string f_RS_Font=" ",CContainer *p_Parent=NULL);
   CTradeButton     *CreateCMDButton(string f_ControlName,int f_Top,int f_Left,string f_RS_SectionName=" ",string f_RS_Font=" ",CContainer *p_Parent=NULL);
   CLabel           *CreateLabel(string f_ControlName,int f_Top,int f_Left,string f_Caption,string f_RS_Font=" ",CContainer *p_Parent=NULL);
   CImage           *CreateImage(string f_ControlName,int f_Top,int f_Left,string f_RS_SectionName=" ",CContainer *p_Parent=NULL);
   CEdit            *CreateEdit(string f_ControlName,int f_Top,int f_Left,int f_Width,int f_Height,string f_RS_Font=" ",CContainer *p_Parent=NULL);
   CSpinEdit        *CreateSpin(string f_ControlName,int f_Top,int f_Left,int f_Width,int f_Heigth,uchar f_Type,string f_RS_SectionName,string f_RS_Font,CContainer *p_Parent=NULL);
   //---
   CControl         *OnObjectClick(string f_sparam,int &f_Type);
   CWAVbox          *GetWAVbox(const uchar f_WavBox);
   virtual void      OnVisible(void)  {}
   virtual void      OnEnable(void)   {}
   virtual void      EventsHandlerDispatcher(CControl *p_Control,int f_ControlType,const long &lparam,const double &dparam,const string &sparam);
   virtual void      ButtonClickEvent(CButton *p_Obj,string f_ObjName)       {}
   virtual void      CMDButtonClickEvent(CButton *p_Obj,string f_ObjName)    {}
   virtual void      SpinBtnsClickEvent(CSpinButton *p_Obj)                  {}
   virtual void      CheckBoxClickEvent(CCheckbox *p_Obj,string f_ObjName)   {}
   virtual void      TabHeaderClickEvent(CTabHeader *p_Obj,string f_ObjName) {}
   virtual void      RadioBtnClickEvent(CRadioButton *p_Obj,string f_ObjName){}
   virtual void      CaptionClickEvent(CCaption *p_Obj,string f_ObjName)     {}
   int               Type() {return(CTRL_FORM);}
  };
//+------------------------------------------------------------------+
//|constructor                                                       |
//+------------------------------------------------------------------+
CForm::CForm(void)
  {
   m_Handle=NULL;
   m_Width=m_Height=0;
   m_FileNameDrag=" ";
   m_FileNameMaximize=" ";
   m_FileNameMinimize=" ";
   m_FileNameDisabled=" ";
   m_Enabled=m_Visible=true;
   m_FrameName=" ";
   m_PanelType=m_LastPanelType=FRM_TYPE_MAXIMIZE;
   ProtectedObjs    =NULL;
   MainContainer    =NULL;
   HandlerContainer =NULL;
   CTRLS_WAVbox     =NULL;
   EVNTS_WAVbox     =NULL;
   Resources        =NULL;
   m_BkgObj         =NULL;
   m_hWnd           =-1;
   m_SetupFile_Handle=INVALID_HANDLE;
//--- default position frame-center on the chart
   m_FrameAlign.Top=0;
   m_FrameAlign.Left=0;
   m_FrameAlign.h_Align=H_ALIGN_MIDLLE_POS;
   m_FrameAlign.v_Align=V_ALIGN_MIDLLE_POS;
//---   
  }
//+------------------------------------------------------------------+
//|destructor                                                        |
//+------------------------------------------------------------------+
CForm::~CForm(void)
  {
   Destroy();
  }
//+------------------------------------------------------------------+
//|Main function                                                     |
//+------------------------------------------------------------------+
//| INPUT:  &f_Align  - form position structure                      |
//|         *p_Resources  - pointer of global resources object       |
//|         f_RS_SectionName - name of graphical section on resource |
//|         f_RS_Font - name of font section on resource             |
//| OUTPUT: true if successful, else if not.                         |
//+------------------------------------------------------------------+
bool CForm::Create(long f_SubWin,string f_FrameName,Align &f_Align,CResources *p_Resources=NULL)
  {
   bool result=((Resources=p_Resources)!=NULL && Resources.CreateResources());

   m_Chart=f_SubWin;
   m_FrameAlign=f_Align;
   m_ChartWidth=(int)ChartGetInteger(m_Chart,CHART_WIDTH_IN_PIXELS);
   m_ChartHeight=(int)ChartGetInteger(m_Chart,CHART_HEIGHT_IN_PIXELS);
   m_hWnd=(int)ChartGetInteger(m_Chart,CHART_WINDOW_HANDLE);

   if(result)
     {
      result&=CFrame::Create(new CContainer,p_Resources);
      if((MainContainer=GetContainer())==NULL) return(false);
      CImgResource *rs_Imgs=Resources.GetImgsResource(FRAME_SECTION);
      if(rs_Imgs!=NULL)
        {
         CArrayString *Images=rs_Imgs.GetResList();
         m_FileNameMaximize =Images.At(0);
         m_FileNameMinimize =Images.At(1);
         m_FileNameDrag     =Images.At(2);
         m_FileNameDisabled =Images.At(3);
         if((m_BkgObj=new CImage)!=NULL)
           {
            GetContainer().AddItem(m_BkgObj);
            m_BkgObj.Create("bkg"+f_FrameName,0,0);
            m_BkgObj.SetImgs(m_FileNameMaximize,m_FileNameDisabled);
            m_BkgObj.Image(m_FileNameMaximize);
            m_Width=m_BkgObj.Width();
            m_Height=m_BkgObj.Height();
            m_Top=GetTop(m_FrameAlign);
            m_Left=GetLeft(m_FrameAlign);
            m_BkgObj.SetTop(m_Top);
            m_BkgObj.SetLeft(m_Left);
            if((ProtectedObjs=new CArrayString)!=NULL)
              {
               ProtectedObjs.Add(m_BkgObj.Name());
              }
           }
         else return(false);
        }

      CFontResource *rs_Font=Resources.GetFontResource("COMMON_FONT");

      if(rs_Font!=NULL)
        {
         rs_Font.GetFont(m_Font);
        }

      if((HandlerContainer=new CContainer)==NULL)
        {
         Destroy();
         return(false);
        }

      HandlerContainer.FreeMode(false);

      if((CTRLS_WAVbox=Resources.GetWavResource("CTRLS"))!=NULL)
        {
         CTRLS_WAVbox.On(false);
        }

      if((EVNTS_WAVbox=Resources.GetWavResource("EVNTS"))!=NULL)
        {
         EVNTS_WAVbox.On(false);
        }

      m_FrameName=m_BkgObj.Name();
      m_Handle=GetPointer(this);
     }
   else Print("CFrame::Create()- resources creation error");

   return(result);
  }
//+------------------------------------------------------------------+
//|Creating frame on center of the chart                             |
//+------------------------------------------------------------------+
//|INPUT:   *p_Resources  - pointer of global resources object       |
//|         f_RS_SectionName - name of graphical section on resource |
//|         f_RS_Font - name of font section on resource             |
//| OUTPUT: true if successful, else if not.                         |
//+------------------------------------------------------------------+
bool CForm::Create(long f_SubWin,string f_FrameName,CResources *p_Resources=NULL)
  {
   return((bool)Create(f_SubWin,f_FrameName,m_FrameAlign,p_Resources));
  }
//+------------------------------------------------------------------+
//|Destroy all containers and resources                              |
//+------------------------------------------------------------------+
void CForm::Destroy(void)
  {
//if(m_BkgObj!=NULL) delete m_BkgObj;
   if(HandlerContainer!=NULL) delete HandlerContainer;
   if(ProtectedObjs!=NULL) delete ProtectedObjs;
  }
//+------------------------------------------------------------------+
//|Get pointer for Container object                                  |
//|  0 - get main container                                          |
//|  1 - get handler container                                       |
//+------------------------------------------------------------------+
CContainer *CForm::GetContainer(uchar f_Type)
  {
   CContainer *obj=NULL;
   switch(f_Type)
     {
      case CNTR_MAIN:    obj=MainContainer;     break;
      case CNTR_HANDLER: obj=HandlerContainer;  break;
      default: break;
     }
   return(obj);
  }
//+------------------------------------------------------------------+
//|Calculate X coordinate                                            |
//+------------------------------------------------------------------+
int CForm::GetLeft(Align &p_align) const
  {
   if(m_Chart==-1) return(0);
   int f_Left=0;
   int ChartMX=(int)round(m_ChartWidth/2);
   switch(p_align.h_Align)
     {
      case H_ALIGN_CUSTOM_POS:     f_Left=p_align.Left;                      break;
      case H_ALIGN_LEFT_POS:       f_Left=1;                                 break;
      case H_ALIGN_RIGHT_POS:      f_Left=m_ChartWidth-m_Width;              break;
      case H_ALIGN_MIDLLE_POS:     f_Left=ChartMX-(int)round(m_Width/2);     break;
      default:break;
     }
   return(f_Left);
  }
//+------------------------------------------------------------------+
//|Calculate Y coordinate                                            |
//+------------------------------------------------------------------+
int CForm::GetTop(Align &p_align) const
  {
   if(m_Chart==-1) return(0);
   int f_Top=0;
   int ChartMY=(int)round(m_ChartHeight/2);
   switch(p_align.v_Align)
     {
      case V_ALIGN_CUSTOM_POS:     f_Top=p_align.Top;                        break;
      case V_ALIGN_TOP_POS:        f_Top=1;                                  break;
      case V_ALIGN_BOTTOM_POS:     f_Top=m_ChartHeight-m_Height;             break;
      case V_ALIGN_MIDLLE_POS:     f_Top=ChartMY-(int)round(m_Height/2);     break;
      default:break;
     }

   return(f_Top);
  }
//+------------------------------------------------------------------+
//|Changing background bitmap of frame                               |
//+------------------------------------------------------------------+
bool CForm::ChangeBackground(uchar f_FrameType)
  {
   bool result=true;
   switch(f_FrameType)
     {
      case FRM_TYPE_MINIMIZE:
        {
         result=(m_FileNameMinimize!=" " && m_BkgObj.Image(m_FileNameMinimize));
        }
      break;

      case FRM_TYPE_MAXIMIZE:
        {
         result=(m_FileNameMaximize!=" " && m_BkgObj.Image(m_FileNameMaximize));
        }
      break;

      case FRM_TYPE_DRAG:
        {
         result=(m_FileNameDrag!=" " && m_BkgObj.Image(m_FileNameDrag));
        }
      break;

      default:
         result=false;
         break;
     }
   if(result)
     {
      m_LastPanelType=m_PanelType;
      m_PanelType=f_FrameType;
     }
   return(result);
  }
//+------------------------------------------------------------------+
//|Repaint frame method                                              |
//+------------------------------------------------------------------+
bool CForm::Repaint(uchar f_Reason)
  {
   bool result=true;
   switch(f_Reason)
     {
      case REPAINT_REASON_MOVE:
        {
         result&=Hide(ProtectedObjs);
         m_LastTop=m_Top;
         m_LastLeft=m_Left;
         if(m_FileNameDrag!=" ") result&=ChangeBackground(FRM_TYPE_DRAG);
         result&=m_BkgObj.Selectable(true);
         result&=m_BkgObj.Selected(true);
        }
      break;

      case REPAINT_REASON_DRAG:
        {
         result&=ChangeBackground(m_LastPanelType);
         m_Top=m_BkgObj.Top();
         m_Left=m_BkgObj.Left();
         result&=m_BkgObj.Selectable(false);
         result&=m_BkgObj.Selected(false);
         result&=Restore();
         if(result)
           {
            m_FrameAlign.Top=m_Top;
            m_FrameAlign.Left=m_Left;
            m_FrameAlign.h_Align=H_ALIGN_CUSTOM_POS;
            m_FrameAlign.v_Align=V_ALIGN_CUSTOM_POS;
           }
        }
      break;

      case REPAINT_REASON_RESIZECHART:
        {

        }
      break;
     }
   ChartRedraw();
   return(result);
  }
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//|Create RadioGroup control                                         |
//+------------------------------------------------------------------+
//| INPUT:  f_ControlName  - control name                            |
//|         f_Size  - size of GroupControl container                 |
//|         f_RS_SectionName - resource section name for images      |
//|         f_RS_Font - resource section name for Font               |
//| OUTPUT: pointer of control                                       |
//+------------------------------------------------------------------+
CRadioGroup *CForm::CreateRadioGroup(string f_ControlName,int f_Size,string f_RS_SectionName,string f_RS_Font,CContainer *p_Parent=NULL)
  {
   CRadioGroup *rGroup=NULL;
   CContainer  *Parent=MainContainer;
   if(p_Parent!=NULL) Parent=p_Parent;

   if((rGroup=new CRadioGroup)!=NULL)
     {
      bool result=rGroup.Create(f_ControlName,0,0,f_Size,Resources,f_RS_SectionName,f_RS_Font);
      if(result) Parent.AddItem(rGroup);
     }
   return(rGroup);
  }
//+------------------------------------------------------------------+
//|Create Tabs control                                               |
//+------------------------------------------------------------------+
//| INPUT:  f_ControlName  - control name                            |
//|         f_Top   - Y distance                                     |
//|         f_Left  - X distance                                     |
//|         f_TabsCount  - size of GroupControl container            |
//|         f_RS_SectionName - resource section name for images      |
//|         f_RS_Font - resource section name for Font               |
//| OUTPUT: pointer of control                                       |
//+------------------------------------------------------------------+
CTabControl *CForm::CreateTabs(string f_ControlName,int f_Top,int f_Left,uchar f_TabsCount,string f_RS_SectionName,string f_RS_Font,CContainer *p_Parent=NULL)
  {
   CTabControl *Tab=NULL;
   CContainer  *Parent=MainContainer;
   if(p_Parent!=NULL) Parent=p_Parent;

   if((Tab=new CTabControl)!=NULL)
     {
      bool result=Tab.Create(f_ControlName,f_Top,f_Left,f_TabsCount,TO_BOTTOM,Resources,f_RS_SectionName,f_RS_Font);
      if(result) Parent.AddItem(Tab);
     }
   return(Tab);
  }
//+------------------------------------------------------------------+
//|Create CheckBox control                                           |
//+------------------------------------------------------------------+
CCheckbox *CForm::CreateCheckBox(string f_ControlName,int f_Top,int f_Left,string f_Caption,string f_RS_SectionName,string f_RS_Font,CContainer *p_Parent=NULL)
  {
   CCheckbox *box=NULL;
   CContainer  *Parent=MainContainer;
   if(p_Parent!=NULL) Parent=p_Parent;

   if((box=new CCheckbox)!=NULL)
     {
      bool result=box.Create(f_ControlName,f_Top,f_Left,m_SetupFile_Handle,Resources,f_RS_SectionName,f_RS_Font);
      if(f_Caption!=" ")
        {
         box.Caption(f_Caption);
         HandlerContainer.AddItem(box.objCaption());
        }
      box.Enabled(true);
      if(m_SetupFile_Handle==INVALID_HANDLE) result&=box.Checked(false);
      if(result)
        {
         HandlerContainer.AddItem(box);
         Parent.AddItem(box);
        }
     }
   return(box);
  }
//+------------------------------------------------------------------+
//|Create button control                                             |
//+------------------------------------------------------------------+
CButton *CForm::CreateButton(string f_ControlName,int f_Top,int f_Left,string f_Caption,string f_RS_SectionName=" ",string f_RS_Font=" ",CContainer *p_Parent=NULL)
  {
   CButton *Button=NULL;
   CContainer  *Parent=MainContainer;
   if(p_Parent!=NULL) Parent=p_Parent;

   if((Button=new CButton)!=NULL)
     {
      bool result=Button.Create(f_ControlName,f_Top,f_Left,Resources,f_RS_SectionName,f_RS_Font);
      result&=Button.Down(false);
      Button.Enabled(true);
      if(f_Caption!=" ")
        {
         result&=Button.SetCaption(f_Caption);
         HandlerContainer.AddItem(Button.objCaption());
        }
      if(result)
        {
         HandlerContainer.AddItem(Button);
         Parent.AddItem(Button);
        }
     }
   return(Button);
  }
//+------------------------------------------------------------------+
//|Create command button (Sell/Buy)                                  |
//+------------------------------------------------------------------+
CTradeButton *CForm::CreateCMDButton(string f_ControlName,int f_Top,int f_Left,string f_RS_SectionName=" ",string f_RS_Font=" ",CContainer *p_Parent=NULL)
  {
   CTradeButton *Button=NULL;
   CContainer  *Parent=MainContainer;
   if(p_Parent!=NULL) Parent=p_Parent;

   if((Button=new CTradeButton)!=NULL)
     {
      bool result=Button.Create(f_ControlName,f_Top,f_Left,Resources,f_RS_SectionName,f_RS_Font,HandlerContainer);
      Button.Clickable(true);
      result&=Button.OnBkgChange(BTN_FACE_PRICENONE,0,UCHAR_MAX,PRICE_CHANGE_NONE);
      result&=Button.Down(false);
      if(result)
        {
         HandlerContainer.AddItem(Button);
         Parent.AddItem(Button);
        }
     }
   return(Button);
  }
//+------------------------------------------------------------------+
//|Create Label control                                              |
//+------------------------------------------------------------------+
CLabel  *CForm::CreateLabel(string f_ControlName,int f_Top,int f_Left,string f_Caption,string f_RS_Font=" ",CContainer *p_Parent=NULL)
  {
   CLabel *Label=NULL;
   CContainer  *Parent=MainContainer;
   if(p_Parent!=NULL) Parent=p_Parent;

   if((Label=new CLabel)!=NULL)
     {
      bool result=Label.Create(f_ControlName,f_Top,f_Left,Resources,f_RS_Font);
      result&=Label.SetAnchorPoint(ANCHOR_CENTER);
      result&=Label.SetCaption(f_Caption);
      if(result) Parent.AddItem(Label);

     }
   return(Label);
  }
//+------------------------------------------------------------------+
//|Create Image control                                              |
//+------------------------------------------------------------------+
CImage *CForm::CreateImage(string f_ControlName,int f_Top,int f_Left,string f_RS_SectionName=" ",CContainer *p_Parent=NULL)
  {
   CImage *bmp=NULL;
   CContainer  *Parent=MainContainer;
   if(p_Parent!=NULL) Parent=p_Parent;

   if((bmp=new CImage)!=NULL)
     {
      bool result=bmp.Create(f_ControlName,f_Top,f_Left,Resources,f_RS_SectionName);
      if(result) Parent.AddItem(bmp);
     }
   return(bmp);
  }
//+------------------------------------------------------------------+
//|Create Edit control                                               |
//+------------------------------------------------------------------+
CEdit *CForm::CreateEdit(string f_ControlName,int f_Top,int f_Left,int f_Width,int f_Height,string f_RS_Font=" ",CContainer *p_Parent=NULL)
  {
   CEdit *Edit=NULL;
   CContainer  *Parent=MainContainer;
   if(p_Parent!=NULL) Parent=p_Parent;

   if((Edit=new CEdit)!=NULL)
     {
      bool result=Edit.Create(f_ControlName,f_Top,f_Left,f_Width,f_Height,m_SetupFile_Handle,Resources,f_RS_Font);
      if(result) Parent.AddItem(Edit);
     }
   return(Edit);
  }
//+------------------------------------------------------------------+
//|Create SpinEdit control                                                                   |
//+------------------------------------------------------------------+
CSpinEdit *CForm::CreateSpin(string f_ControlName,int f_Top,int f_Left,int f_Width,int f_Heigth,uchar f_Type,string f_RS_SectionName,string f_RS_Font,CContainer *p_Parent=NULL)
  {
   bool result=true;
   CSpinEdit *SpinEdit=NULL;
   CContainer  *Parent=MainContainer;
   if(p_Parent!=NULL) Parent=p_Parent;

   if((SpinEdit=new CSpinEdit)!=NULL)
     {
      switch(f_Type)
        {
         case BP_RIGHT:
           {
            if(SpinEdit.Create(f_ControlName,f_Top,f_Left,f_Type,HandlerContainer.Container(),m_SetupFile_Handle,Resources,f_RS_SectionName,f_RS_Font))
              {
               if(SpinEdit.CreateField(0,0,f_Width,f_Heigth))
                 {
                  if(SpinEdit.CreateIncButton(0,0))
                    {
                     result&=SpinEdit.CreateDecButton(1,0);
                    }
                  else Print("CreateSpin:: Create Increment button failed");
                 }
               else Print("CreateSpin:: Create Numeric field failed");
              }
           }
         break;

         case BP_LEFT_RIGHT:
           {
            if(SpinEdit.Create(f_ControlName,f_Top,f_Left,f_Type,HandlerContainer.Container(),m_SetupFile_Handle,Resources,f_RS_SectionName,f_RS_Font))
              {
               if(SpinEdit.CreateDecButton(1,1))
                 {
                  if(SpinEdit.CreateField(0,1,f_Width,f_Heigth))
                    {
                     result&=SpinEdit.CreateIncButton(1,0);
                    }
                  else Print("CreateSpin:: Create Numeric field failed");
                 }
               else Print("CreateSpin:: Create Decrement button failed");
              }
           }
         break;
        }
      if(result)
        {
         SpinEdit.ReadOnly(false);
         Parent.AddItem(SpinEdit);
        }
     }
   return(SpinEdit);
  }
//+------------------------------------------------------------------+
//|return pointer of control object from "handler's" container       |
//+------------------------------------------------------------------+
CControl *CForm::OnObjectClick(string f_sparam,int &f_Type)
  {
   CControl    *Control;
   for(int i=0;i<HandlerContainer.TotalItems();i++)
     {
      Control=HandlerContainer.Item(i);
      if(Control.Name()==f_sparam)
        {
         f_Type=Control.Type();
         return(Control);
        }
     }
   return(NULL);
  }
CWAVbox *CForm::GetWAVbox(const uchar f_WavBox)
  {
    CWAVbox *res=NULL;
    switch(f_WavBox)
      {
       case WAVS_CTRLS :
           res=CTRLS_WAVbox;
         break;
       
       case WAVS_EVNTS :
           res=EVNTS_WAVbox;
         break;
      }
    return(res);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CForm::OnChartEvent(const int id,const long &lparam,const double &dparam,const string &sparam)
  {
   CControl *CTRL=NULL;
   int CtrlType=CTRL_UNKNOW;
   if(id==CHARTEVENT_OBJECT_DRAG)
     {
      if(sparam==m_FrameName) {Repaint(REPAINT_REASON_DRAG);return;}
     }

   if(id==CHARTEVENT_OBJECT_CLICK)
     {
      if(sparam==m_FrameName && m_PanelType==FRM_TYPE_DRAG) {Repaint(REPAINT_REASON_DRAG);return;}
      if((CTRL=OnObjectClick(sparam,CtrlType))!=NULL)
        {
         EventsHandlerDispatcher(CTRL,CtrlType,lparam,dparam,sparam);
        }
     }
   if(id==CHARTEVENT_CHART_CHANGE)
     {
      int _ChrW=(int)ChartGetInteger(m_Chart,CHART_WIDTH_IN_PIXELS);
      int _ChrH=(int)ChartGetInteger(m_Chart,CHART_HEIGHT_IN_PIXELS);
      if(m_ChartWidth!=_ChrW || m_ChartHeight!=_ChrH)
        {
         m_ChartWidth=_ChrW;
         m_ChartHeight=_ChrH;
         Repaint(REPAINT_REASON_RESIZECHART);
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void  CForm::EventsHandlerDispatcher(CControl *p_Control,int f_ControlType,const long &lparam,const double &dparam,const string &sparam)
  {
   switch(f_ControlType)
     {
      case CTRL_UP_SPINBUTTON:
      case CTRL_DN_SPINBUTTON:
        {
         CSpinButton *obj=p_Control;
         if(obj!=NULL)
           {
            if(obj.OnClick((int)dparam,(int)lparam,DELAY_BTN_CLICK)) SpinBtnsClickEvent(obj);
           }
        }
      break;

      case CTRL_BUTTON:
        {
         CButton *obj=p_Control;
         if(obj.OnClick((int)dparam,(int)lparam,DELAY_BTN_CLICK))
           {
            ButtonClickEvent(obj,sparam);
           }
        }
      break;

      case CTRL_CMD_BUTTON:
        {
         CTradeButton *obj=p_Control;
         if(obj.OnClick((int)dparam,(int)lparam,DELAY_BTN_CLICK))
           {
            CMDButtonClickEvent(obj,sparam);
           }
        }
      break;

      case CTRL_CHECKBOX:
        {
         CCheckbox *obj=p_Control;
         if(obj.Enabled() && obj.OnStateChange())
           {
            CheckBoxClickEvent(obj,sparam);
           }
        }
      break;

      case CTRL_TAB_HEADER:
        {
         CTabHeader *obj=p_Control;
         if(obj.OnClick((int)dparam,(int)lparam))
           {
            TabHeaderClickEvent(obj,sparam);
           }
        }
      break;

      case CTRL_RADIOBUTTON:
        {
         CRadioButton *obj=p_Control;
         if(obj.OnClick((int)dparam,(int)lparam))
           {
            RadioBtnClickEvent(obj,sparam);
           }
        }
      break;

      case CTRL_CAPTION:
        {
         CCaption *obj=p_Control;
         CControl *parent=obj.Parent();
         switch(parent.Type())
           {
            case CTRL_BUTTON:
              {
               CButton *_obj=parent;
               if(_obj!=NULL && _obj.IsEnabled && _obj.OnClick((int)dparam,(int)lparam,DELAY_BTN_CLICK))
                 {
                  ButtonClickEvent(_obj,_obj.Name());
                 }
              }
            break;

            case CTRL_CMD_BUTTON:
              {
               CTradeButton *_obj=parent;
               if(_obj!=NULL && _obj.OnClick((int)dparam,(int)lparam,DELAY_BTN_CLICK))
                 {
                  CMDButtonClickEvent(_obj,_obj.Name());
                 }
              }
            break;

            case CTRL_TAB_HEADER:
              {
               CTabHeader *_obj=parent;
               if(_obj!=NULL && _obj.OnClick((int)dparam,(int)lparam))
                 {
                  TabHeaderClickEvent(_obj,_obj.Name());
                 }
              }
            break;

            case CTRL_CHECKBOX:
              {
               CCheckbox *_obj=parent;
               if(_obj!=NULL && _obj.OnClick())
                 {
                  CheckBoxClickEvent(_obj,_obj.Name());
                 }
              }
            break;

            case CTRL_RADIOBUTTON:
              {
               CRadioButton *_obj=parent;
               if(_obj!=NULL && _obj.OnClick())
                 {
                  RadioBtnClickEvent(_obj,_obj.Name());
                 }
              }
            break;

            default:  CaptionClickEvent(obj,obj.Name());  break;
           }
        }
      break;

      default:
         break;
     }
  }

//+------------------------------------------------------------------+
