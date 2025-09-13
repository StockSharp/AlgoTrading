//+------------------------------------------------------------------+
//|                                               LayoutExporter.mqh |
//|                                    Copyright (c) 2020, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//|                           https://www.mql5.com/ru/articles/7795/ |
//+------------------------------------------------------------------+
#include <Controls/Rect.mqh>
#include "LayoutStdLib.mqh"
#include "LayoutColors.mqh"
#include "LayoutConverters.mqh"

class LayoutExporter
{
  protected:
  
    CWndContainer *owner;
    StdLayoutCache *cache;
    int handle;

    string extractClass(const string name) const
    {
      string array[];
      if(StringSplit(name, ' ', array) > 0)
      {
        return array[0];
      }
      return name;
    }

    string extractName(const string id) const
    {
      const int n = StringLen(id);
      for(int i = 0; i < n; i++)
      {
        ushort c = StringGetCharacter(id, i);
        if(c > '9')
        {
          return StringSubstr(id, i);
        }
      }
      return id;
    }

    bool isAligned(CWnd *element)
    {
      const ENUM_WND_ALIGN_FLAGS f = (ENUM_WND_ALIGN_FLAGS)((int)element.Alignment() & 0xF);
      return f != WND_ALIGN_NONE;
    }

    string extractAlign(CWnd *element) const
    {
      ENUM_WND_ALIGN_FLAGS f = element.Alignment();
      string s = EnumToString(f);
      string result = "";
      if(StringFind(s, "::") > 0)
      {
        int count = 0;
        for(int i = 15; i >= 0; i--)
        {
          int mask = ((int)f) & (i);
          if(mask != 0)
          {
            s = EnumToString((ENUM_WND_ALIGN_FLAGS)mask);
            if(StringFind(s, "::") == -1) // exact element of enum
            {
              f = (ENUM_WND_ALIGN_FLAGS)((int)f & ~mask); // clear bits
              if(result != "") result += "|";
              result += s;
              count++;
            }
          }
        }
        if(count > 1)
        {
          result = "(ENUM_WND_ALIGN_FLAGS)(" + result + ")";
        }
      }
    
      if(f == WND_ALIGN_CONTENT)
      {
        if(StringLen(result) > 0)
        {
          return "(ENUM_WND_ALIGN_FLAGS)(WND_ALIGN_CONTENT|" + result + ")";
        }
        else
        {
          return "(ENUM_WND_ALIGN_FLAGS)(WND_ALIGN_CONTENT)";
        }
      }
      
      if(StringLen(result) > 0)
      {
        return result;
      }
      
      return EnumToString(f);
    }

    string extractMargins(CWnd *element) const
    {
      CRect r = element.Margins();
      return StringFormat(" <= PackedRect(%d, %d, %d, %d)", r.left, r.top, r.right, r.bottom);
    }
    
    // TODO: need to detect non-default colors per control type
    string extractColors(CWnd *element) const
    {
      CWndObj *obj = dynamic_cast<CWndObj *>(element);
      if(obj != NULL)
      {
        color clr = obj.ColorBackground();
        if(!LayoutColors::isDefaultGUIcolor(clr))
        {
          return " <= C'" + ColorToString(clr) + "'";
        }
        //obj.ColorBorder();
        //obj.Color();
      }
      else
      {
        CWndClient *client = dynamic_cast<CWndClient *>(element);
        if(client != NULL)
        {
          color clr = client.ColorBackground();
          if(!LayoutColors::isDefaultGUIcolor(clr))
          {
            return " <= C'" + ColorToString(clr) + "'";
          }
          // client.ColorBorder();
        }
      }
      return NULL;
    }

    static color extractBgColorRaw(CWnd *element)
    {
      CWndObj *obj = dynamic_cast<CWndObj *>(element);
      if(obj != NULL)
      {
        color clr = obj.ColorBackground();
        if(!LayoutColors::isDefaultGUIcolor(clr))
        {
          return clr;
        }
      }
      else
      {
        CWndClient *client = dynamic_cast<CWndClient *>(element);
        if(client != NULL)
        {
          color clr = client.ColorBackground();
          if(!LayoutColors::isDefaultGUIcolor(clr))
          {
            return clr;
          }
        }
      }
      return clrNONE;
    }

    int saveElement(CWnd *element)
    {
      static string level = "";
      int count = 0;
      
      if(CheckPointer(element) != POINTER_INVALID)
      {
        if(element == cache.get(0))
        {
          FileWriteString(handle, level + "// GUI Layout for MQL app (standard controls library)\n");
          FileWriteString(handle, level + "// Required hosting window dimensions: (width*height)=" + (string)owner.Width() + "*" + (string)owner.Height() + "\n");
          FileWriteString(handle, level + "// Don't forget to keep a pointer to the main container to call Show() after load and Pack() on refreshes\n");
        }
      
        CWndContainer *container = dynamic_cast<CWndContainer *>(element);
        if(container != NULL)
        {
          const int index = cache.indexOf(container);
          if(index > -1)
          {
            FileWriteString(handle, level + "{\n");
            level += "  ";
          
            FileWriteString(handle, level);
            
            const string name = extractName(container.Name());

            string size;
            if(level == "  ") // this is the first outermost container
            {
              size = "ClientAreaWidth(), ClientAreaHeight()";
            }
            else
            {
              size = (string)container.Width() + ", " + (string)container.Height();
            }
            
            FileWriteString(handle, "_layout<" + extractClass(container._rtti) + "> " + name
              + "(\"" + name + "\", " + size
              + (isAligned(container) ? ", " + extractAlign(container) : "") + ");\n");
            
            string style = "";
            CBox *box = dynamic_cast<CBox *>(container);
            if(box != NULL)
            {
              if(box.HorizontalAlign())
              {
                style = " <= " + EnumToString(box.HorizontalAlign());
              }
              else if(box.VerticalAlign())
              {
                style = " <= " + EnumToString(box.VerticalAlign());
              }
            }
            
            string strclr = extractColors(container);
            FileWriteString(handle, level + name + (strclr != NULL ? "[\"background\"]" + strclr : "") + extractMargins(container) + style + ";\n");
            FileWriteString(handle, level + "{\n");

            count++;
            level += "  ";
          }
      
          int children = 0;
          for(int j = 0; j < container.ControlsTotal(); j++)
          {
            children += saveElement(container.Control(j));
          }
          
          count += children;
            
          if(index > -1)
          {
            if(children == 0)
            {
              FileWriteString(handle, level + "// dummy (feel free to delete)\n");
            }
            
            StringSetLength(level, StringLen(level) - 2);
            FileWriteString(handle, level + "}\n");
            StringSetLength(level, StringLen(level) - 2);
            FileWriteString(handle, level + "}\n");
          }
          
        }
        else
        {
          if(cache.indexOf(element) > -1)
          {
            FileWriteString(handle, level + "{\n");
            level += "  ";

            FileWriteString(handle, level);
            
            const string name = extractName(element.Name());
            
            string text = "";
            string style = "";
            
            CWndObj *ctrl = dynamic_cast<CWndObj *>(element);
            if(ctrl != NULL)
            {
              text = ctrl.Text();
              if(StringLen(text) > 0)
              {
                text = " <= \"" + text + "\"";
              }
              else
              {
                text = "";
              }
              CEdit *edit = dynamic_cast<CEdit *>(element);
              if(edit != NULL)
              {
                style = " <= " + EnumToString(edit.TextAlign());
              }
            }

            FileWriteString(handle, "_layout<" + extractClass(element._rtti) + "> " + name
              + "(\"" + name + "\", " + (string)element.Width() + ", " + (string)element.Height()
              + (isAligned(element) ? ", " + extractAlign(element) : "") + ");\n");

            string strclr = extractColors(element);

            FileWriteString(handle, level + name
              + (strclr != NULL ? "[\"background\"]" + strclr : "")
              + text + extractMargins(element) + style + ";\n");
            count++;

            StringSetLength(level, StringLen(level) - 2);
            FileWriteString(handle, level + "}\n");
          }
        }
      }
    
      return count;
    }

    
    static string HEADER_VERSION;

    int saveElementBinary(CWnd *element)
    {
      int count = 0;
      
      if(CheckPointer(element) != POINTER_INVALID)
      {
        if(element == cache.get(0))
        {
          FileWriteString(handle, HEADER_VERSION);
          FileWriteInteger(handle, owner.Width());
          FileWriteInteger(handle, owner.Height());
        }
      
        CWndContainer *container = dynamic_cast<CWndContainer *>(element);
        if(container != NULL)
        {
          const int index = cache.indexOf(container);
          if(index > -1)
          {
            FileWriteInteger(handle, '{', 1); // delimiter
            
            const string name = extractName(container.Name());
            FileWriteInteger(handle, StringLen(name));
            FileWriteString(handle, name);
            const int type = InspectorDialog::GetTypeByRTTI(container._rtti);
            if(type == -1)
            {
              Print("Unknown type ", container._rtti);
            }
            FileWriteInteger(handle, type);
            FileWriteInteger(handle, container.Width());
            FileWriteInteger(handle, container.Height());
            
            int style = 0;
            CBox *box = dynamic_cast<CBox *>(container);
            if(box != NULL)
            {
              if(type == 0)
              {
                style = box.HorizontalAlign();
              }
              else if(type == 1)
              {
                style = box.VerticalAlign();
              }
            }

            FileWriteInteger(handle, style);
            
            // text is skipped
            FileWriteInteger(handle, 0);
            
            FileWriteInteger(handle, extractBgColorRaw(container));
            FileWriteInteger(handle, LayoutConverters::enum2boxAlignBits(container.Alignment()));
            
            PackedRect r(container.Margins());
            for(int i = 0; i < 4; i++)
            {
              FileWriteInteger(handle, r.parts[i], 2);
            }
            count++;
          }
      
          int children = 0;
          for(int j = 0; j < container.ControlsTotal(); j++)
          {
            children += saveElementBinary(container.Control(j));
          }
          
          count += children;
            
          if(index > -1)
          {
            FileWriteInteger(handle, '}', 1); // delimiter
          }
          
        }
        else
        {
          if(cache.indexOf(element) > -1)
          {
            FileWriteInteger(handle, '[', 1); // delimiter

            const string name = extractName(element.Name());
            FileWriteInteger(handle, StringLen(name));
            FileWriteString(handle, name);
            const int type = InspectorDialog::GetTypeByRTTI(element._rtti);
            FileWriteInteger(handle, type);
            FileWriteInteger(handle, element.Width());
            FileWriteInteger(handle, element.Height());

            CEdit *edit = dynamic_cast<CEdit *>(element);
            if(edit != NULL)
            {
              FileWriteInteger(handle, LayoutConverters::textAlign2style(edit.TextAlign()));
            }
            else
            {
              FileWriteInteger(handle, 0);
            }

            string text = NULL;
            
            CWndObj *ctrl = dynamic_cast<CWndObj *>(element);
            if(ctrl != NULL)
            {
              text = ctrl.Text();
              FileWriteInteger(handle, StringLen(text));
              FileWriteString(handle, text);
            }
            else
            {
              FileWriteInteger(handle, 0);
            }

            FileWriteInteger(handle, extractBgColorRaw(element));
            FileWriteInteger(handle, element.Alignment());
            
            PackedRect r(element.Margins());
            for(int i = 0; i < 4; i++)
            {
              FileWriteInteger(handle, r.parts[i], 2);
            }
            FileWriteInteger(handle, ']', 1); // delimiter
            count++;
          }
        }
      }
    
      return count;
    }

    
  public:
    LayoutExporter(CWndContainer *parent, StdLayoutCache *ptr): owner(parent), cache(ptr) {}

    int saveToFile(const string name)
    {
      int result = 0;
      if((TerminalInfoInteger(TERMINAL_KEYSTATE_SHIFT) & 0x80000000) != 0)
      {
        handle = FileOpen(name + ".mql", FILE_BIN | FILE_WRITE);
        result = saveElementBinary(cache.get(0));
        Print((string)result + " elements saved to binary file " + name + ".mql");
        
      }
      else
      {
        handle = FileOpen(name + ".txt", FILE_TXT | FILE_ANSI | FILE_WRITE);
        result = saveElement(cache.get(0));
        Print((string)result + " elements saved to text file " + name + ".txt");
      }
      FileClose(handle);
      return result;
    }
    
    bool openFileBinary(const string name, int &width, int &height)
    {
      const string nameext = name + ".mql";
      handle = FileOpen(nameext, FILE_BIN | FILE_READ);
      if(handle == INVALID_HANDLE)
      {
        Print("Can't open file ", nameext, " ", GetLastError());
        return false;
      }
      string header = FileReadString(handle, StringLen(HEADER_VERSION));
      if(header != HEADER_VERSION)
      {
        Print("Wrong file header ", nameext);
        FileClose(handle);
        handle = INVALID_HANDLE;
        return false;
      }
      width = FileReadInteger(handle);
      height = FileReadInteger(handle);
      return true;
    }
    
    void closeFileBinary()
    {
      FileClose(handle);
    }
    
    int readDelimiterBinary(void)
    {
      return FileReadInteger(handle, 1);
    }

    bool readNextBinary(Properties &p)
    {
      // having containers side by side with elements is a bad idea (unsupported)
      int namelen = FileReadInteger(handle);
      p.name = FileReadString(handle, namelen);
      p.type = FileReadInteger(handle);
      p.width = FileReadInteger(handle);
      p.height = FileReadInteger(handle);
      p.style = FileReadInteger(handle);
      int textlen = FileReadInteger(handle);
      p.text = FileReadString(handle, textlen);
      p.clr = (color)FileReadInteger(handle);
      p.align = FileReadInteger(handle);
      for(int i = 0; i < 4; i++)
      {
        p.margins[i] = (ushort)FileReadInteger(handle, 2);
      }
      
      return !FileIsEnding(handle);
    }
    
    bool hasNextBinary()
    {
      return !FileIsEnding(handle);
    }
    
};

static string LayoutExporter::HEADER_VERSION = "MQL-Layout binary file/1.0";