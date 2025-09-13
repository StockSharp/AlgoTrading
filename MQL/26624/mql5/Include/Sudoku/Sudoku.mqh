//+------------------------------------------------------------------+
//|                                                       Sudoku.mqh |
//|                                    Copyright (c) 2019, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//+------------------------------------------------------------------+

#include "RubbArray.mqh"
#include "AutoPtr.mqh"

class SudokuVector: public BaseArray<ulong>
{
  private:
    bool temporary;

  protected:
    bool isTemporary() const
    {
      return temporary;
    }

  public:
    SudokuVector(): temporary(false) {}
    SudokuVector(const bool temp): temporary(temp) {}

    void fill(const uint d)
    {
      clear();
      for(uint i = 0; i < d; i++)
      {
        this << (i + 1);
      }
    }
    
    void wipe(const uint i, const ulong value = 0)
    {
      data[i] = value;
    }

    SudokuVector *merge(const SudokuVector *other) const
    {
      SudokuVector *result = new SudokuVector(true);
      for(uint i = 0; i < this.size(); i++)
      {
        for(uint j = 0; j < other.size(); j++)
        {
          if((this[i] & 0xFF) == (other[j] & 0xFF))
          {
            result << (this[i] & 0xFF);
            break;
          }
        }
      }
      if(other.isTemporary()) delete other;
      return result;
    }
};

class Sudoku;

class SudokuStructure
{
  protected:
    const uint index;
    Sudoku *owner;

  public:
    SudokuStructure() {}
    SudokuStructure(Sudoku *host, const uint _index): owner(host), index(_index) {}

    virtual ulong operator[](uint i) const
    {
      return 0;
    }

    virtual uint size() const
    {
      return 0;
    }
    
    virtual string getType() const = 0;
    
    virtual uint getIndex(const uint i) const = 0;

    virtual bool clearCandidate(const uchar c = '0') const
    {
      return false;
    }

    uint firstEmptyIndex(const uchar c /* candidate */ = '0') const
    {
      for(uint i = 0; i < this.size(); i++)
      {
        if((this[i] & 0xFF) == 0)
        {
          if(c == '0')
          {
            return i;
          }
          else
          {
            const uint shift = c - '0';
            if((this[i] & ((1 << shift) << 8)) > 0)
            {
              return i;
            }
          }
        }
      }
      
      return -1;
    }
    
    SudokuVector *getVector(const bool auto = false) const
    {
      SudokuVector *result;
      static SudokuVector temp;
      
      temp.clear();
      
      if(!auto) result = new SudokuVector();
      else result = &temp;
      
      for(uint i = 0; i < size(); i++)
      {
        result << this[i];
      }
      return result;
    }
    
    SudokuVector *getEmpties(const uint max = 0) const
    {
      SudokuVector temp;
      SudokuVector *result = new SudokuVector();
      uint _max = max == 0 ? this.size() : max;
      temp.fill(_max);

      for(uint i = 0; i < size(); i++)
      {
        if((this[i] & 0xFF) > 0)
        {
          temp.wipe((uint)(this[i] & 0xFF) - 1);
        }
      }
      
      for(uint i = 0; i < _max; i++)
      {
        if(temp[i] != 0)
        {
          result << temp[i];
        }
      }
      
      return result;
    }

    uint filledCount() const
    {
      uint count = 0;
      for(uint i = 0; i < this.size(); i++)
      {
        if((this[i] & 0xFF) > 0) count++;
      }
      return count;
    }
    
    bool isSolved(const bool validate = false) const // also means "isValid", when not all cells are filled
    {
      const uint n = size();

      int counter[];
      ArrayResize(counter, n);
      ArrayInitialize(counter, 0);

      uint x;
      for(uint i = 0; i < n; i++)
      {
        x = (uint)(this[i] & 0xFF) - 1;
        if(x >= n)
        {
          if(validate) continue; // skip 0s
          return false;
        }
        if(counter[x] > 0)
        {
          if(validate) Print(getType(), " ", index, " conflict: ", getVector().toString());
          return false;
        }
        counter[x]++;
      }
      // should be an array of [1,1,1...] for solution, or [1,0,...] for validation
      return true;
    }

};

class Row: public SudokuStructure
{
  public:
    Row(Sudoku *host, const uint _index): SudokuStructure(host, _index) {}
    virtual ulong operator[](uint i) const override;
    virtual uint size() const override;
    virtual bool clearCandidate(const uchar c = '0') const override;
    virtual uint getIndex(const uint i) const override;
    virtual string getType() const override {return typename(this);}
};

class Column: public SudokuStructure
{
  public:
    Column(Sudoku *host, const uint _index): SudokuStructure(host, _index) {}
    virtual ulong operator[](uint i) const override;
    virtual uint size() const override;
    virtual bool clearCandidate(const uchar c = '0') const override;
    virtual uint getIndex(const uint i) const override;
    virtual string getType() const override {return typename(this);}
};

class Block: public SudokuStructure
{
  protected:
    uint y;
    uint x;

  public:
    Block(Sudoku *host, const uint _index);
    virtual ulong operator[](uint i) const override;
    virtual uint size() const override;
    virtual bool clearCandidate(const uchar c = '0') const override;
    virtual uint getIndex(const uint i) const override;
    virtual string getType() const override {return typename(this);}
};

interface SudokuNotify
{
  void onChange(const uint index, const ulong value);
};

class Sudoku
{
  protected:
    ulong field[];
    uint size;
    uint dimension;
    uint blocks;
    uint blockSize;
    uint iteration;
    uint completed;
    uint filled; // initial state

    uint free[][2]; // [rows, columns, blocks][empty count, original index]
    
    static const string SUDOKU_HEADER;
    
    class State
    {
      public:
        State(const ulong &f[], const uint c)
        {
          ArrayCopy(field, f);
          completed = c;
        }
        ulong field[];
        uint completed;
    };
    
    Stack<State *> stack;
    int backtrackCount;
    List<State *> solutions;
    
    bool multipleCheck;
    uint maxNestingLevel;
    
    enum STATUS
    {
      UNKNOWN,
      ERROR,
      CORRECT,
      NONUNIQUE
    };
    
    STATUS status;
    bool mute;
    
    SudokuNotify *callback;
    SudokuNotify *enabledCallback;

  public:
    Sudoku(const uint sizePerSide, const uint dimensions = 2, const uint blocking = 9)
    {
      dimension = sizePerSide < 10 ? sizePerSide : 9;
      if(sizePerSide >= 10)
      {
        Print("Maximal allowed size is 9, overrides ", sizePerSide);
      }
      
      if(dimension < 4)
      {
        Print("Minimal allowed size is 4, overrides ", dimension);
        dimension = 4;
      }
      
      if(dimensions < 2 || dimensions > 3)
      {
        Print("Allowed dimensions: 2 or 3, use default 2");
        size = (int)MathPow(dimension, 2);
      }
      else
      {
        size = (int)MathPow(dimension, dimensions);
      }
      
      blocks = blocking;
      ArrayResize(field, size);
      ArrayResize(free, 2 * dimension + blocking);
      ArrayInitialize(field, 0);
      ArrayInitialize(free, 0);
      multipleCheck = false;
    }
    
    ~Sudoku()
    {
      ArrayFree(field);
      ArrayFree(free);
    }
    
    void enableMultipleCheck(const bool m = true)
    {
      multipleCheck = m;
    }
    
    uint getDimension() const
    {
      return dimension;
    }
    
    uint getBlockNumber() const
    {
      return blocks;
    }
    
    uint getBlockSize() const
    {
      return blockSize;
    }
    
    STATUS getStatus() const
    {
      return status;
    }
    
    uint getNestingLevel() const
    {
      return maxNestingLevel;
    }
    
    uint getIterations() const
    {
      return iteration;
    }
    
    void setMuteMode(const bool m)
    {
      mute = m;
    }
    
    void bind(SudokuNotify *ptr)
    {
      callback = ptr;
    }
    
    bool hasCallback() const
    {
      return callback != NULL;
    }
    
    virtual uint offset(const uint row, const uint column, const uint plane = 0) const
    {
      return plane * (dimension * dimension) + row * dimension + column;
    }

    virtual ulong cellByIndex(const uint index) const
    {
      if(index < size) return field[index];
      
      Print("Offset ", index, " is out of bound: ", size);
      
      return 0;
    }
    
    virtual ulong cell(const uint row, const uint column, const uint plane = 0) const
    {
      uint index = offset(row, column, plane);
      
      if(index < size) return field[index];
      
      Print("Offset ", index, " [", row, ",", column, ",", plane, "] is out of bound: ", size);
      
      return 0;
    }
    
    virtual string cellAsString(const uint index) const
    {
      ulong v = cellByIndex(index);
      if((v & 0xFF) > 0) return (string)(v & 0xFF);
      if(v > 0) return getCandidatesString(index);
      return "";
    }
    
    virtual string rawValueAsString(const ulong value) const
    {
      string candidates = "", label = "0";
      if((value & 0xFF) > 0) label = (string)(value & 0xFF);
      
      for(uint i = 1; i < sizeof(ulong) * 8 - 8; i++)
      {
        if(((value >> (8 + i)) & 1) > 0)
        {
          candidates += (string)i;
        }
      }
      if(candidates == "") candidates = "0";

      return candidates + "/" + label;
    }
    
    virtual bool setValue(const uint row, const uint column, const uint plane = 0, const uchar value = '0')
    {
      if(callback)
      {
        SudokuNotify *temp;
        temp = enabledCallback;
        enabledCallback = callback;
        bool result = assign(row, column, plane, value);
        enabledCallback = temp;
        return result;
      }

      uint index = offset(row, column, plane);
      
      if(index < size)
      {
        field[index] = (value - '0');
        return true;
      }
      return false;
    }

  protected:
    virtual bool assign(const uint row, const uint column, const uint plane = 0, const uchar value = '0')
    {
      uint index = offset(row, column, plane);
      
      if(index < size)
      {
        field[index] =   // (field[index] & 0xFFFFFFFFFFFFFF00) |
          (value - '0'); // current implementation specific: clear guesses/candidates!

        if(field[index] > 0)
        {
          AutoPtr<SudokuStructure> r(row(row));
          AutoPtr<SudokuStructure> c(column(column));
          AutoPtr<SudokuStructure> b(block(coordinates2block(row, column)));
          (~r).clearCandidate(value);
          (~c).clearCandidate(value);
          (~b).clearCandidate(value);
            
          #ifdef SUDOKU_DEBUG
          Print("MOVE: ", row, " ", column, " ", plane, " ", CharToString(value));
          Print(exportAsText('.', index));
          Print(exportCandidates());
          #endif
        }

        return true;
      }
      return false;
    }

  public:
    virtual void clearCandidates()
    {
      for(uint i = 0; i < size; i++)
      {
        field[i] = field[i] & 0xFF;
      }
    }
    
    virtual bool clearCandidates(const uint row, const uint column, const uint plane = 0)
    {
      uint index = offset(row, column, plane);
      if(index < size)
      {
        field[index] = field[index] & 0xFF;
        return true;
      }
      return false;
    }

    virtual bool setCandidate(const uint row, const uint column, const uint plane = 0, const uchar value = '0')
    {
      uint index = offset(row, column, plane);
      
      if(index < size)
      {
        uint mask = (value - '0');
        if(mask > 0)
        {
          field[index] = (field[index] & ~0xFF) | ((1 << mask) << 8); // note: low byte is reserved for completed moves
          return true;
        }
      }
      return false;
    }

    virtual bool clearCandidate(const uint row, const uint column, const uint plane = 0, const uchar value = '0')
    {
      uint index = offset(row, column, plane);
      
      if(index < size)
      {
        uint mask = (value - '0');
        if(mask > 0)
        {
          ulong prev = field[index];
          field[index] = field[index] & ~((1 << mask) << 8);
          if(enabledCallback && prev != field[index]) enabledCallback.onChange(index, prev);
          return true;
        }
      }
      return false;
    }

    virtual bool isCandidate(const uint row, const uint column, const uint plane = 0, const uchar value = '0') const
    {
      uint index = offset(row, column, plane);
      
      if(index < size)
      {
        uint mask = (value - '0');
        if(mask > 0)
        {
          if((field[index] & ((1 << mask) << 8)) > 0) return true;
        }
      }
      return false;
    }

    virtual SudokuVector *getCandidates(const uint index) const
    {
      SudokuVector *result = new SudokuVector();
      for(uint i = 0; i < dimension; i++)
      {
        if(((field[index] >> 8) & (1 << (i + 1))) > 0)
        {
          result << (i + 1);
        }
      }
      return result;
    }
    
    virtual SudokuVector *getCandidates(const uint row, const uint column, const uint plane = 0) const
    {
      uint index = offset(row, column, plane);
      if(index >= size) return NULL;

      return getCandidates(index);
    }

    virtual uint getCandidatesCount(const uint index) const
    {
      uint count = 0;      
      for(uint i = 0; i < dimension; i++)
      {
        if(((field[index] >> 8) & (1 << (i + 1))) > 0)
        {
          count++;
        }
      }
      return count;
    }

    virtual uint getCandidatesCount(const uint row, const uint column, const uint plane = 0) const
    {
      uint index = offset(row, column, plane);
      if(index >= size) return 0;

      return getCandidatesCount(index);
    }

    virtual string getCandidatesString(const uint index) const
    {
      string result = "";      
      for(uint i = 0; i < dimension; i++)
      {
        if(((field[index] >> 8) & (1 << (i + 1))) > 0)
        {
          result += (string)(i + 1);
        }
      }
      return result;
    }

    virtual string valueAsString(const ulong value) const
    {
      if((value & 0xFF) > 0) return (string)(value & 0xFF);

      string result = "";      
      for(uint i = 0; i < dimension; i++)
      {
        if(((value >> 8) & (1 << (i + 1))) > 0)
        {
          result += (string)(i + 1);
        }
      }
      return result;
    }
    
    virtual void initialize() = 0;
    virtual void debug() {}
    virtual void transpose(const uint axis0 = 0, const uint axis1 = 1) = 0;
    virtual void randomize(const int seed = -1, const uint shuffleCycles = 100) = 0;
    virtual uint generate(const int seed = -1, const uint hiddenclue = 0, const uint minclues = 0) = 0;
    virtual bool checkMinimality() = 0;
    virtual bool solve(const uint nesting = 0) = 0;
    virtual bool speculate() = 0;
    virtual bool isSolved() const = 0;

    virtual uint coordinates2block(const uint row, const uint column) = 0;
    virtual void block2coordinates(const uint block, const uint index, uint &row, uint &column) = 0;
    
    virtual SudokuStructure *row(const uint index) const = 0;
    virtual SudokuStructure *column(const uint index) const = 0;
    virtual SudokuStructure *block(const uint index) const = 0;// for 3D-sudoku this is a plane

    virtual SudokuVector *shuffle(const SudokuVector *source, const int seed = -1) const
    {
      MathSrand(seed == -1 ? GetTickCount() : seed);
      ulong array[][2];
      const uint length = source.size();
      ArrayResize(array, length);
      for(uint i = 0; i < length; i++)
      {
        array[i][0] = rand();
        array[i][1] = source[i];
      }

      ArraySort(array);
      
      SudokuVector *result = new SudokuVector();
      
      for(uint i = 0; i < length; i++)
      {
        result << array[i][1];
      }
      return result;
    }

    virtual SudokuVector *relabel(const int seed = -1) const
    {
      MathSrand(seed == -1 ? GetTickCount() : seed);
      ulong array[][2];
      const uint length = dimension + 1;
      ArrayResize(array, length);
      array[0][0] = 0;
      for(uint i = 1; i < length; i++) // 0-th element is not relocated
      {
        array[i][0] = rand();
        array[i][1] = i;
      }

      ArraySort(array);
      
      SudokuVector *result = new SudokuVector();
      
      for(uint i = 0; i < length; i++)
      {
        result << array[i][1];
      }
      return result;
    }
    
    uint countFilled() const
    {
      uint count = 0;
      
      for(uint i = 0; i < size; i++)
      {
        if((field[i] & 0xFF) != 0) count++;
      }

      return count;
    }
    
    bool importField(const ulong &array[])
    {
      if(ArraySize(array) == size)
      {
        ArrayCopy(field, array);
        filled = countFilled();
        return true;
      }
      
      return false;
    }
    
    int exportField(ulong &array[]) const
    {
      return ArrayCopy(array, field);
    }

    virtual bool loadFromText(const string text)
    {
      string code = text;
      StringReplace(code, "\n", "");
      StringReplace(code, "\r", "");
      StringReplace(code, " ", "");
      StringTrimLeft(code);
      StringTrimRight(code);
      
      const int n = StringLen(code);
      if(n != size) return false;

      for(uint i = 0; i < size; i++)
      {
        ushort c = code[i];
        field[i] = c >= '0' && c <= '9' ? c - '0' : 0;
      }
      
      filled = countFilled();
      
      Print("Clues/Givens: ", filled);
      
      return true;
    }

    virtual bool loadFromFile(const string filename, const bool common = false)
    {
      string text = "";
      int handle = FileOpen(filename, FILE_ANSI|FILE_READ|FILE_SHARE_READ|FILE_SHARE_WRITE|(common ? FILE_COMMON : 0));
      if(handle != INVALID_HANDLE)
      {
        while(!FileIsEnding(handle))
        {
          text += FileReadString(handle) + "\n";
        }
        FileClose(handle);
        
        Print("Reading ", filename);
        
        return loadFromText(text);
      }
      return false;
    }

    virtual bool saveToFile(const string filename, const bool common = false)
    {
      int handle = FileOpen(filename, FILE_ANSI|FILE_WRITE|FILE_SHARE_READ|FILE_SHARE_WRITE|(common ? FILE_COMMON : 0));
      if(handle != INVALID_HANDLE)
      {
        FileWriteString(handle, exportAsText());
        FileClose(handle);
        
        return true;
      }
      return false;
    }
    
    virtual string exportAsText(const uchar space = '0', const uint index = -1) const = 0;
    virtual string exportCandidates(void) const = 0;
    
    virtual bool exportPosition(const string filename, const bool common = false) const
    {
      int handle = FileOpen(filename, FILE_BIN|FILE_WRITE|FILE_SHARE_READ|FILE_SHARE_WRITE|(common ? FILE_COMMON : 0));
      if(handle != INVALID_HANDLE)
      {
        FileWriteString(handle, SUDOKU_HEADER, StringLen(SUDOKU_HEADER));
        FileWriteInteger(handle, size);
        FileWriteInteger(handle, dimension);
        FileWriteInteger(handle, blocks);
        FileWriteArray(handle, field);
        FileClose(handle);
        
        return true;
      }
      return false;
    }

    virtual bool importPosition(const string filename, const bool common = false)
    {
      int handle = FileOpen(filename, FILE_BIN|FILE_READ|FILE_SHARE_READ|FILE_SHARE_WRITE|(common ? FILE_COMMON : 0));
      if(handle != INVALID_HANDLE)
      {
        bool result = false;
        string header = FileReadString(handle, StringLen(SUDOKU_HEADER));
        if(header == SUDOKU_HEADER)
        {
          size = FileReadInteger(handle);
          dimension = FileReadInteger(handle);
          blocks = FileReadInteger(handle);
          Print("Reading board: ", size, " [", dimension, "*", dimension, "], ", blocks, " blocks");
          uint read = FileReadArray(handle, field, 0, size);
          if(read != size)
          {
            Print("File '", filename, "'reading error, elements read: ", read, ", expected: ", size);
          }
          else
          {
            result = true;
          }
        }
        else
        {
          Print("File '", filename, "' not loaded, wrong header: ", header);
        }
        FileClose(handle);
        
        Print("Position restored from ", filename);
        
        return result;
      }
      return false;
    }
};

class ClassicSudoku: public Sudoku
{
  public:
    ClassicSudoku(): Sudoku(9, 2, 9)
    {
      blockSize = 3;
    }

    ClassicSudoku(const uint sizePerSide, const uint blocking = 9): Sudoku(sizePerSide, 2, blocking)
    {
      blockSize = (uint)MathSqrt(size / blocks);
    }
    
    // creates starting board, filled in a straight way, ready for randomization
    virtual void initialize() override
    {
      ushort c = 0;
      const ushort bandsize = (ushort)(dimension / sqrt(blocks));
      for(uint i = 0; i < size; i++)
      {
        if(i > 0 && i % dimension == 0) c += bandsize;
        if(i > 0 && i % (bandsize * dimension) == 0) c++;
        c = c % (ushort)dimension + 1;
        field[i] = c;
      }
    }

    virtual void debug() override
    {
      for(uint i = 0; i < size; i++)
      {
        field[i] = (ushort)(i / dimension) + 1;
      }
    }

    virtual void transpose(const uint axis0 = 0, const uint axis1 = 1) override
    {
      for(uint i = 0; i < dimension; i++)
      {
        for(uint j = i + 1; j < dimension; j++)
        {
          ulong temp = field[offset(i, j)];
          field[offset(i, j)] = field[offset(j, i)];
          field[offset(j, i)] = temp;
        }
      }
      //#ifdef SUDOKU_DEBUG_TRANSPOSE
      //Print("Transpose");
      //Print(exportAsText());
      //#endif
    }
    
    bool swapBands(const uint b1, const uint b2)
    {
      // number of bands = sqrt(blocks) for classic sudoku
      if(b1 == b2) return false;

      const uint bands = (uint)sqrt(blocks);
      
      if(b1 >= bands || b2 >= bands) return false;
      
      const uint rows = (uint)(dimension / bands);
      
      for(uint i = b1 * rows, k = b2 * rows; i < (b1 + 1) * rows; i++, k++)
      {
        for(uint j = 0; j <  dimension; j++)
        {
          ulong temp = field[offset(i, j)];
          field[offset(i, j)] = field[offset(k, j)];
          field[offset(k, j)] = temp;
        }
      }
      #ifdef SUDOKU_DEBUG_SWAPS
      Print("SwapBands ", b1, " ", b2);
      //Print(exportAsText());
      #endif
      return true;
    }

    bool swapRows(const uint b, const uint r1, const uint r2)
    {
      // number of rows = dimension / sqrt(blocks) for classic sudoku
      if(r1 == r2) return false;

      const uint bands = (uint)sqrt(blocks);
      const uint rows = (uint)(dimension / bands);

      if(r1 >= rows || r2 >= rows || b >= bands) return false;

      uint i = b * rows + r1;
      uint k = b * rows + r2;

      for(uint j = 0; j <  dimension; j++)
      {
        ulong temp = field[offset(i, j)];
        field[offset(i, j)] = field[offset(k, j)];
        field[offset(k, j)] = temp;
      }
      #ifdef SUDOKU_DEBUG_SWAPS
      Print("SwapRows ", r1, " ", r2, " in ", b);
      //Print(exportAsText());
      #endif
      return true;
    }
    
    // shuffle starting plain board with allowed permutations
    virtual void randomize(const int seed = -1, const uint shuffleCycles = 100) override
    {
      Print("Shuffling...");
      // available modifications:
      //   relabelling
      //   swap bands
      //   swap rows inside a band
      //   * swap stacks - not needed due to transposition
      //   * swap columns inside a stack - not needed due to transposition
      //   transposition

      AutoPtr<SudokuVector> namer(relabel(seed));
      for(uint i = 0; i < size; i++)
      {
        uint u = (uint)(field[i] > dimension ? 0 : field[i]); // only clues are processed at this stage, guesses are discarded
        field[i] = (~namer)[u];
      }
      
      const uint bands = (uint)sqrt(blocks);
      const uint rows = (uint)(dimension / bands);
      
      uint success = 0;
      for(uint i = 0; i < shuffleCycles; i++)
      {
        if(swapBands((uint)((double)rand() * bands / 32767), (uint)((double)rand() * bands / 32767))) success++;
        if(swapRows((uint)((double)rand() * bands / 32767), (uint)((double)rand() * rows / 32767), (uint)((double)rand() * rows / 32767))) success++;
        transpose(); // success++; always ok
      }
      
      Print("Permutations done: ", success, " of ", 2 * shuffleCycles);
    }

    virtual uint generate(const int seed = -1, const uint hiddenclue = 0, const uint minclues = 0) override
    {
      Print("Generating composition...");
      // difficulty can be measured by
      //   number of guesses (speculation level) required to solve puzzle
      //   average minimal number of candidates on every position
      //   number of labels
      //   number of iterations/backtracks required to solve puzzle
      // integral difficulty suggested here: size / 2 / number of clues * sqrt(guessing points)
      
      // const uint low = size / 5;
      // const uint high = size / 2;
      // uint givens = clues == 0 ? low : clues; // for standard 9*9 sudoku the low bound is 17
      // if(givens > high) givens = high;        // and the high bound is 40
      // NB! it may take forever to find sudoku with so big minimal required clues,
      // (minimal sudoku is that in which removal of any clue makes it nonunique (invalid)),
      // most likely big number of clues (35-40) will actually contain lesser number of minimal clues plus some excessive clues,
      // so this implementation will not check result for "minimality" when minclues achieved

      MathSrand(seed == -1 ? GetTickCount() : seed);

      int array[][2];
      ArrayResize(array, size);
      for(uint i = 0; i < size; i++)
      {
        if(field[i] == hiddenclue)
        {
          array[i][0] = -1;
          field[i] = 0;
        }
        else
        {
          array[i][0] = rand();
        }
        array[i][1] = (int)i;
      }

      ArraySort(array);
      
      ulong result[];

      ClassicSudoku classic(dimension);
      classic.setMuteMode(true);
      classic.enableMultipleCheck(true);
      
      ulong backtrack;
      bool oncesolved = false;
      uint lastKnownLevel = 0;
      uint removed = hiddenclue == 0 ? 0 : dimension;

      for(uint k = removed; k < size; k++)
      {
        backtrack = field[array[k][1]];
        field[array[k][1]] = 0;
        removed++;

        classic.importField(field);
        if(!classic.solve())
        {
          if(classic.getStatus() == NONUNIQUE)
          {
            // take solution from previous iteration
            field[array[k][1]] = backtrack;
            removed--;
            Print("Not a unique solution");
          }
          else if(classic.getStatus() == ERROR)
          {
            // can't solve anymore, need to backtrack
            #ifdef SUDOKU_DEBUG
            Print("Error at ", k, ", can't solve this:");
            Print(exportAsText('.', array[k][1]));
            #endif
            // restore just removed cell, it eliminates cyclic dependencies in cells
            field[array[k][1]] = backtrack;
            removed--;
          }
          else
          {
            // if we're here, it's unintentional - a bug
            Print("Wrong status: ", EnumToString(classic.getStatus()));
            break;
          }
        }
        else
        {
          oncesolved = true;
          lastKnownLevel = classic.getNestingLevel();
          ArrayCopy(result, field);
          
          if(countFilled() == minclues)
          {
            Print("Exit at requested limit ", minclues);
            break;
          }
          
          // keep removing keys
        }
      }
      
      if(oncesolved)
      {
        ArrayCopy(field, result);
        Print("Ready, number of clues: ", size - removed);
        Print(exportAsText('.'));
      }
      else
      {
        Print("Can't generate a field");
      }
      
      filled = countFilled();
      
      return lastKnownLevel;
    }
    
    class NestingLevel
    {
      public:
        static uint level;
        NestingLevel()
        {
          level++;
        }
        ~NestingLevel()
        {
          level--;
        }
    };
    
    virtual bool speculate() override
    {
      NestingLevel level();
      const uint nestingLevel = level.level;
      
      string padding;
      
      StringInit(padding, 2 * nestingLevel, ' ');

      // push current field (state) to stack
      stack << new State(field, completed);

      uint minvalue = INT_MAX;
      uint minindex = 0;
      
      // choose a cell with minimum guesses
      for(uint i = 0; i < size; i++)
      {
        uint count = getCandidatesCount(i);
        if(count > 0 && count < minvalue)
        {
          minvalue = count;
          minindex = i;
        }
      }
      
      AutoPtr<SudokuVector> can = getCandidates(minindex);
      
      // loop through the guesses
      for(uint i = 0; i < (~can).size(); i++)
      {
        uchar c = (uchar)((~can)[i] + '0');
        #ifdef SUDOKU_DEBUG
        Print(padding, "speculate ", nestingLevel, ": ", i, " of ", (~can).size(), ": ", minindex / dimension, " ", minindex % dimension, " ", CharToString(c));
        #endif
        // place guess
        if(assign(minindex / dimension, minindex % dimension, 0, c))
        {
          if(!solve(nestingLevel))
          {
            #ifdef SUDOKU_DEBUG
            Print(padding, "deadend ", nestingLevel, ": ", i, " of ", (~can).size(), ": ", minindex / dimension, " ", minindex % dimension, " ", CharToString(c));
            #endif
            // take top from the stack
            // restore field for next try
            State *s = stack.top();
            ArrayCopy(field, s.field);
            completed = s.completed;
            backtrackCount++;
          }
          else
          {
            if(solutions.size() > 0)
            {
              State *previous = solutions.top();
              if(ArrayCompare(field, previous.field) != 0)
              {
                solutions << new State(field, completed);
                if(!multipleCheck)
                {
                State *s = stack.pop();
                ArrayCopy(field, s.field);
                completed = s.completed;
                delete s;
                
                return false; // this will stop further solving, 2 solution is already incorrect
                }
              }
            }
            else
            {
              Print("Done at nesting level of speculations/guesses: ", nestingLevel, " / Backtrack count: ", backtrackCount);
              maxNestingLevel = nestingLevel;
              Print("Clues: ", filled, " / Integral difficulty: ", (float)(1.0 * size / 2 / filled * sqrt(nestingLevel + 1)));

              solutions << new State(field, completed);
            }

            // continue running to check multiple solutions,
            // we can stop process here if checkup is not necessary (only needed for generator)
            if(!multipleCheck)
            {
              // if single solution is enough, keep it in the field
              return true;
            }

            State *s = stack.top();
            ArrayCopy(field, s.field);
            completed = s.completed;
          }
        }
        else
        {
          Print("Error: candidate is not assignable: ", minindex / dimension, " ", minindex % dimension, " ", CharToString(c));
        }
      }

      // backtrack on failure
      State *s = stack.pop();
      ArrayCopy(field, s.field);
      completed = s.completed;
      
      delete s;
      
      return false;
    }
    
    virtual bool checkMinimality() override
    {
      // TODO: try to remove some clues and solve given puzzle to identify if it is minimal or not
      return true; // minimal, false - not (can be reduced)
    }
    
    virtual bool solve(const uint nesting = 0) override
    {
      uint start = 0;

      if(nesting == 0)
      {
        iteration = -1;
        completed = 0;
        backtrackCount = 0;
        solutions.clear();
        status = UNKNOWN;
        filled = countFilled();
        if(filled < 17 || (filled == size && !isSolved()))
        {
          if(!mute)
          {
            Print("Incorrect field:");
            Print(exportAsText());
          }
          status = ERROR;
          return false;
        }
        start = GetTickCount();
      }

      do
      {
        do
        {
          iteration++;
          #ifdef SUDOKU_DEBUG
          Print("Iteration: ", iteration);
          Print(exportAsText());
          #endif
        }
        while(move() && completed < size && !IsStopped());
  
        if(completed < size)
        {
          // we can have a logged solution,
          // but current (incomplete) state can be restored from stack,
          // if multiple solutions checkup is enabled
          if(solutions.size() > 0) break;

          #ifdef SUDOKU_DEBUG
          Print("No more moves, speculation required at temporary ", completed);
          #endif

          if(!speculate()) break;
        }
      }
      while(completed < size && !IsStopped());

      if(IsStopped())
      {
        return false;
      }
      
      if(nesting == 0) // initial branch, presenting results to caller or generator
      {
        if(IsStopped())
        {
          Print("Terminated");
        }

        if(solutions.size() > 0 || completed == size)
        {
          if(solutions.size() == 0) // simple case without speculations
          {
            solutions << new State(field, completed);
          }
          const uint n = solutions.size();
          if(n > 1)
          {
            status = NONUNIQUE;
          }
          else
          {
            status = CORRECT;
          }

          if(!mute)
          {
            Print("Iterations: ", iteration);
            Print("Solution: ", ShortToString(0xA71C));
            if(n > 1)
            {
              Print("Many solutions found, only 1 is allowed, puzzle is incorrect!");
            }
          }

          for(uint i = 0; i < n; i++)
          {
            State *s = solutions.pop();
            ArrayCopy(field, s.field); // LIFO
            
            if(!mute)
            {
              if((TerminalInfoInteger(TERMINAL_KEYSTATE_CAPSLOCK) & 0x1) != 0)
              {
                if(i > 0) Print("");
                else Print("[To stop printing solutions to the log, switch CapsLock OFF]");
                Print(exportAsText());
              }
              else
              {
                // for cheating or debugging
                if(i == 0) Print("[To view solutions in the log, switch CapsLock ON]");
              }
            }

            delete s;
          }
        }
        else
        {
          if(!mute)
          {
            Print("Puzzle is unsolvable. Current state:");
            Print(exportAsText());
            findCandidates(true);
            Print(exportCandidates());
          }
          status = ERROR;
        }
        
        if(!mute)
        {
          if(maxNestingLevel == 0 && status != ERROR) Print("Integral difficulty: ", (float)(1.0 * size / 2 / filled));
          Print(GetTickCount() - start, " msec passed");
        }
        
        return status == CORRECT;
      }
      
      return completed == size;
    }

    virtual bool isSolved() const override
    {
      if(countFilled() != size) return false;

      for(uint i = 0; i < dimension; i++)
      {
        AutoPtr<SudokuStructure> r(row(i));
        AutoPtr<SudokuStructure> c(column(i));
        if(!(~r).isSolved())
        {
          if(!mute) Print("Row ", i, " conflict: ", (~r).getVector().toString());
          return false;
        }
        if(!(~c).isSolved())
        {
          if(!mute) Print("Column ", i, " conflict: ", (~c).getVector().toString());
          return false;
        }
      }

      for(uint i = 0; i < blocks; i++)
      {
        AutoPtr<SudokuStructure> b(block(i));
        if(!(~b).isSolved())
        {
          if(!mute) Print("Block ", i, " conflict: ", (~b).getVector().toString());
          return false;
        }
      }
      return true;
    }

    bool findCandidates(const bool finalize = false)
    {
      completed = 0;
      for(uint y = 0; y < dimension; y++)
      {
        for(uint x = 0; x < dimension; x++)
        {
          clearCandidates(y, x, 0);

          if((cell(y, x) & 0xFF) > 0)
          {
            completed++;
            continue;
          }
          
          AutoPtr<SudokuStructure> r(row(y));
          AutoPtr<SudokuStructure> c(column(x));
          AutoPtr<SudokuStructure> b(block(coordinates2block(y, x)));
          
          AutoPtr<SudokuVector> rcandidates = (~r).getEmpties();
          AutoPtr<SudokuVector> ccandidates = (~c).getEmpties();
          AutoPtr<SudokuVector> bcandidates = (~b).getEmpties();

          AutoPtr<SudokuVector> combined((~rcandidates).merge((~ccandidates).merge(~bcandidates)));
          
          if((~combined).size() == 0)
          {
            if(finalize)
            {
              Print("Problem: no candidates for cell ", y, " ", x);
              Print("Row: ", (~rcandidates).toString());
              Print("Col: ", (~ccandidates).toString());
              Print("Blk: ", (~bcandidates).toString());
            }
            else
            {
              #ifdef SUDOKU_DEBUG
              Print("Problem: no candidates for cell ", y, " ", x);
              Print("Row: ", (~rcandidates).toString());
              Print("Col: ", (~ccandidates).toString());
              Print("Blk: ", (~bcandidates).toString());
              #endif
            }
            return false;
          }
          
          for(uint i = 0; i < (~combined).size(); i++)
          {
            setCandidate(y, x, 0, (uchar)((~combined)[i] + '0'));
          }
        }
      }
      
      #ifdef SUDOKU_DEBUG
      Print(exportCandidates());
      #endif
      
      return true;
    }

  protected:    
    bool placeSingleLabel(const uint index, const uchar c, const SudokuStructure *b)
    {
      uint hole = b.firstEmptyIndex(c);
      if(hole == (uint)-1) return false;
      if(index < dimension)
      {
        return assign(index, hole, 0, c);
      }
      else if(index < 2 * dimension)
      {
        return assign(hole, index - dimension, 0, c);
      }
      else
      {
        uint row, column;
        block2coordinates(index - 2 * dimension, hole, row, column);
        return assign(row, column, 0, c);
      }
      return false;
    }
    
    bool move()
    {
      bool result = false;

      if(!findCandidates()) return false;
      
      #ifdef SUDOKU_DEBUG
      Print("Filled cells: ", completed, ", To go: ", size - completed);
      #endif

      uint index = 0;
      AutoPtr<SudokuStructure> r[];
      ArrayResize(r, 2 * dimension + blocks);
      for(uint i = 0; i < dimension; i++)
      {
        r[index] = row(i);
        free[index][0] = dimension - (~r[index]).filledCount();
        free[index][1] = index;
        index++;
      }

      for(uint i = 0; i < dimension; i++)
      {
        r[index] = column(i);
        free[index][0] = dimension - (~r[index]).filledCount();
        free[index][1] = index;
        index++;
      }
      
      for(uint i = 0; i < blocks; i++)
      {
        r[index] = block(i);
        free[index][0] = dimension - (~r[index]).filledCount();
        free[index][1] = index;
        index++;
      }
      
      ArraySort(free);
      
      for(uint i = 0; i < (uint)ArrayRange(free, 0); i++)
      {
        if(free[i][0] == 0) continue; // skip completely filled structures

        index = free[i][1];
        SudokuStructure *current = ~r[index];
        
        AutoPtr<SudokuVector> candidates = current.getEmpties(dimension);
        
        #ifdef SUDOKU_DEBUG
        Print("* ", index);
        Print(current.getVector(true).toString());
        Print((~candidates).toString());
        #endif
        
        if((~candidates).size() == 1)
        {
          uchar c = (uchar)(((~candidates)[0]) + '0');
          result = result || placeSingleLabel(index, c, current);
        }
        else
        {
          uint stats[];
          ArrayResize(stats, dimension);
          ArrayInitialize(stats, 0);
          for(uint j = 0; j < current.size(); j++)
          {
            if((current[j] & 0xFF) == 0) // cell is not completed yet
            {
              for(uint k = 0; k < dimension; k++)
              {
                if(((current[j] >> 8) & (1 << (k + 1))) > 0) // enumerate all possible guesses
                {
                  stats[k]++;
                }
              }
            }
          }
          
          #ifdef SUDOKU_DEBUG
          ArrayPrint(stats);
          #endif
          
          for(uint k = 0; k < dimension; k++)
          {
            if(stats[k] == 1)
            {
              uchar c = (uchar)((k + 1) + '0');
              result = result || placeSingleLabel(index, c, current);
            }
          }
        }
      }
      
      return result;
    }

  public:
    virtual uint coordinates2block(const uint row, const uint column) override
    {
      const uint n = dimension / blockSize;
      return (row / blockSize) * n + column / blockSize;
    }
    
    virtual void block2coordinates(const uint block, const uint index, uint &row, uint &column) override
    {
      const uint n = dimension / blockSize;
      const uint y = block / n;
      const uint x = block % n;
      
      row = y * blockSize + index / blockSize;
      column = x * blockSize + index % blockSize;
    }

    virtual SudokuStructure *row(const uint index) const override
    {
      if(index >= dimension) return NULL;
      return new Row((Sudoku *)&this, index);
    }

    virtual SudokuStructure *column(const uint index) const override
    {
      if(index >= dimension) return NULL;
      return new Column((Sudoku *)&this, index);
    }

    virtual SudokuStructure *block(const uint index) const override
    {
      if(index >= blocks) return NULL;
      return new Block((Sudoku *)&this, index);
    }
    
    virtual string exportAsText(const uchar space = '0', const uint index = -1) const override
    {
      string result = "";
      for(uint i = 0; i < size; i++)
      {
        if(i > 0 && i % dimension == 0) result += "\n";
        if(index == i) result += ShortToString(0x301);
        result += CharToString((uchar)((field[i] & 0xFF) > 0 ? (field[i] & 0xFF) + '0' : space));
      }
      return result;
    }

    virtual string exportCandidates(void) const override
    {
      string result = "";
      for(uint i = 0; i < size; i++)
      {
        if(i > 0 && i % dimension == 0) result += "\n";
        result += StringFormat("%08X ", field[i] >> 8);
      }
      return result;
    }
    
};

static const string Sudoku::SUDOKU_HEADER = "SUDOKU-MQL5-v1.0";
static uint NestingLevel::level = 0;


ulong Row::operator[](uint i) const override
{
  if(i < owner.getDimension())
  {
    return owner.cellByIndex(getIndex(i));
  }
  
  Print("Row index overflow: ", i);
  return (ulong)-1;
}

uint Row::size() const override
{
  return owner.getDimension();
}

uint Row::getIndex(const uint i) const override
{
  return index * owner.getDimension() + i;
}

bool Row::clearCandidate(const uchar c = '0') const override
{
  // if(owner.hasCallback()) Print("Row ", index, " clear ", CharToString(c));
  for(uint i = 0; i < this.size(); i++)
  {
    if(!owner.clearCandidate(index, i, 0, c)) return false;
  }
  return true;
}

ulong Column::operator[](uint i) const override
{
  if(i < owner.getDimension())
  {
    return owner.cellByIndex(getIndex(i));
  }

  Print("Column index overflow: ", i);
  return (ulong)-1;
}

uint Column::size() const override
{
  return owner.getDimension();
}

uint Column::getIndex(const uint i) const override
{
  return i * owner.getDimension() + index;
}

bool Column::clearCandidate(const uchar c = '0') const override
{
  // if(owner.hasCallback()) Print("Col ", index, " clear ", CharToString(c));
  for(uint i = 0; i < this.size(); i++)
  {
    if(!owner.clearCandidate(i, index, 0, c)) return false;
  }
  return true;
}

Block::Block(Sudoku *host, const uint _index): SudokuStructure(host, _index)
{
  const uint n = owner.getDimension() / owner.getBlockSize(); // blocks per row/column
  y = index / n; // base row of bands
  x = index % n; // base column of bands
}

ulong Block::operator[](uint k) const override
{
  return owner.cellByIndex(getIndex(k));
}

uint Block::size() const override
{
  return owner.getBlockSize() * owner.getBlockSize();
}

uint Block::getIndex(const uint k) const override
{
  const uint i = k / owner.getBlockSize();
  const uint j = k % owner.getBlockSize();
  return (y * owner.getBlockSize() + i) * owner.getDimension() + x * owner.getBlockSize() + j;
}

bool Block::clearCandidate(const uchar c = '0') const override
{
  // if(owner.hasCallback()) Print("Block ", y, " ", x, " clear ", CharToString(c));
  for(uint i = 0; i < this.size(); i++)
  {
    if(!owner.clearCandidate(y * owner.getBlockSize() + i / owner.getBlockSize(), x * owner.getBlockSize() + i % owner.getBlockSize(), 0, c)) return false;
  }
  return true;
}
