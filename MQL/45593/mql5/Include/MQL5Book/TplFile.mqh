//+------------------------------------------------------------------+
//|                                                      TplFile.mqh |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/MapArray.mqh>

#define PUSH(A,V) (A[ArrayResize(A, ArraySize(A) + 1) - 1] = V)

//+------------------------------------------------------------------+
//| Class for matching xpath-like selector with path of nested tags  |
//| and attributes, syntax "/path/element[attribute=value]"          |
//+------------------------------------------------------------------+
class Selector
{
   const string selector;
   string path[];
   int cursor;
public:
   Selector(const string s): selector(s), cursor(0)
   {
      StringSplit(selector, '/', path);
   }
   
   string operator[](int i) const
   {
      if(i < 0 || i >= ArraySize(path)) return NULL;
      if(StringLen(path[i]) > 0)
      {
         const int param = StringFind(path[i], "[");
         if(param > 0)
         {
            return StringSubstr(path[i], 0, param);
         }
      }
      return path[i];
   }
   
   bool accept(const string tag, MapArray<string,string> &properties)
   {
      const string name = this[cursor] == "*" ? tag : this[cursor];
      // compare requested and actual tag names
      if(!(name == "" && tag == NULL) && (name != tag))
      {
         return false;
      }
      
      // NB! Only single condition for specific attribute is currenly supported
      // TODO: support multiple conditions "tag[attribute1@value1][attribute2@value2]..."
      const int start = StringLen(path[cursor]) > 0 ? StringFind(path[cursor], "[") : 0;
      if(start > 0)
      {
         const int stop = StringFind(path[cursor], "]");
         const string prop = StringSubstr(path[cursor], start + 1, stop - start - 1);
         
         // NB! Only equality operator '=' is currently supported
         // TODO: support more stuff ('!=', '<', '>', '@' etc.)
         string kv[]; // key and value
         if(StringSplit(prop, '=', kv) == 2)
         {
            const string value = properties[kv[0]];
            if(kv[1] != value)
            {
               return false;
            }
         }
      }
      
      cursor++;
      return true;
   }
   
   bool isComplete() const
   {
      return cursor == ArraySize(path);
   }
   
   int level() const
   {
      return cursor;
   }
   
   string name() const
   {
      return this[cursor];
   }
   
   int size() const
   {
      return ArraySize(path);
   }

   bool unwind()
   {
      if(cursor > 0)
      {
         cursor--;
         return true;
      }
      return false;
   }
};

//+------------------------------------------------------------------+
//| Class for container element in TPL-file.                         |
//| Holds nested containers (children) and attributes.               |
//+------------------------------------------------------------------+
class Container
{
   MapArray<string,string> properties;
   Container *children[];
   const string tag;
   const int handle;
   
   struct Level
   {
      static int current;
      const int level;
      Level(): level(++current) { }
      ~Level() { --current; }
   };

public:
   Container(const int h, const string t = NULL/*ROOT*/): handle(h), tag(t) { }
   ~Container()
   {
      for(int i = 0; i < ArraySize(children); ++i)
      {
         if(CheckPointer(children[i]) == POINTER_DYNAMIC) delete children[i];
      }
   }
   
   void assign(const string key, const string value)
   {
      properties.put(key, value);
   }
   
   void remove(const string key)
   {
      properties.remove(key);
   }
   
   Container *add(const string subtag)
   {
      return PUSH(children, new Container(handle, subtag));
   }
   
   string get(const string key) const
   {
      return properties[key];
   }
   
   string name() const
   {
      return tag;
   }
   
   // find an element matching xpath-like selector (1-st match is returned)
   // TODO: implement finding all matches (there can be many blocks with the same tag)
   Container *find(const string selector)
   {
      Selector s(selector);
      return find(&s);
   }
   
   Container *find(Selector *selector)
   {
      const string element = StringFormat("%*s", 2 * selector.level(), " ")
         + "<" + tag + "> " + (string)ArraySize(children);
      if(selector.accept(tag, properties))
      {
         Print(element + " accepted");
         
         if(selector.isComplete())
         {
            return &this;
         }
         
         for(int i = 0; i < ArraySize(children); ++i)
         {
            Container *c = children[i].find(selector);
            if(c) return c;
         }
         selector.unwind();
      }
      else
      {
         Print(element);
      }
      
      return NULL;
   }
   
   bool save(const int h)
   {
      Level level;
      const string margin = level.level > 1 ? "\n" : "";
      // opening tag
      if(tag != NULL)
      {
         if(FileWriteString(h, margin + "<" + tag + ">\n") <= 0) return false;
      }
      // all properties
      for(int i = 0; i < properties.getSize(); ++i)
      {
         if(FileWriteString(h, properties.getKey(i) + "=" + properties[i] + "\n") <= 0) return false;
      }
      // all nested containers
      for(int i = 0; i < ArraySize(children); ++i)
      {
         children[i].save(h);
      }
      // closing tag
      if(tag != NULL)
      {
         if(FileWriteString(h, "</" + tag + ">\n") <= 0) return false;
      }
      return true;
   }
   
   bool write(int h = 0)
   {
      bool rewriting = false;
      if(h == 0)
      {
         h = handle;
         rewriting = true;
      }
      if(!FileGetInteger(h, FILE_IS_WRITABLE))
      {
         Print("File is not writable");
         return false;
      }
      
      if(rewriting)
      {
         // NB! After rewinding to the beginning of the text file,
         // we need to write BOM manually, because MQL5 does not do it.
         // Without BOM terminal will not apply tpl-file
         ushort u[1] = {0xFEFF};
         FileSeek(h, SEEK_SET, 0);
         FileWriteString(h, ShortArrayToString(u));
      }
      // otherwise (if an external file handle is specified),
      // calling code is responsible for FileSeek and writing BOM
      
      bool result = save(h);
      
      if(rewriting)
      {
         // NB! MQL5 does not allow to change (especially shrink) file size
         // so we need to write zeros until the end of file to wipe out old data
         while(FileTell(h) < FileSize(h) && !IsStopped())
         {
            FileWriteString(h, " ");
         }
      }
      return result;
   }
   
   bool read(const bool verbose = false)
   {
      while(!FileIsEnding(handle))
      {
         string text = FileReadString(handle);
         const int len = StringLen(text);
         if(len > 0)
         {
            if(text[0] == '<' && text[len - 1] == '>')
            {
               const string subtag = StringSubstr(text, 1, len - 2);
               if(subtag[0] == '/' && StringFind(subtag, tag) == 1)
               {
                  if(verbose)
                  {
                     print();
                  }
                  return true;       // complete
               }
               
               PUSH(children, new Container(handle, subtag)).read(verbose);
            }
            else
            {
               string pair[];
               if(StringSplit(text, '=', pair) == 2)
               {
                  properties.put(pair[0], pair[1]);
               }
            }
         }
      }
      return false;
   }
   
   void print()
   {
      Print("Tag: ", tag);
      properties.print();
   }
};

static int Container::Level::current = -1;
//+------------------------------------------------------------------+
