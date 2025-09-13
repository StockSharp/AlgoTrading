class LayoutConverters
{
  public:
    static ENUM_ALIGN_MODE style2textAlign(const int style)
    {
      ENUM_ALIGN_MODE align = ALIGN_LEFT;
      // center, (justify), left, right, (stack)
      switch(style)
      {
        case 0: align = ALIGN_CENTER;
                break;
        case 2: align = ALIGN_LEFT;
                break;
        case 3: align = ALIGN_RIGHT;
                break;
      }
      return align;
    }

    static int textAlign2style(const ENUM_ALIGN_MODE align)
    {
      switch(align)
      {
        case ALIGN_CENTER: return 0;
        case ALIGN_LEFT: return 2;
        case ALIGN_RIGHT: return 3;
      }
      return 0;
    }

    static ENUM_WND_ALIGN_FLAGS boxAlignBits2enum(const int align)
    {
      int result = align & 0xF;
      if((align & 0x10) != 0) result |= WND_ALIGN_CONTENT;
      return (ENUM_WND_ALIGN_FLAGS)result;
    }

    static int enum2boxAlignBits(const ENUM_WND_ALIGN_FLAGS flags)
    {
      int result = flags;
      if((result & WND_ALIGN_CONTENT) != 0)
      {
        result &= 0xF;
        result |= 0x10;
      }
      return result;
    }

};