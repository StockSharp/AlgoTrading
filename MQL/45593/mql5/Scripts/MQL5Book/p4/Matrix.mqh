//+------------------------------------------------------------------+
//|                                                       Matrix.mqh |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

enum ENUM_ERR_USER_MATRIX
{
   ERR_USER_MATRIX_OK = 0,
   ERR_USER_MATRIX_EMPTY =  1,
   ERR_USER_MATRIX_SINGULAR = 2,
   ERR_USER_MATRIX_NOT_SQUARE = 3
};

//+------------------------------------------------------------------+
//| Matrix class with operator overloads                             |
//+------------------------------------------------------------------+
class Matrix
{
protected:
   double m[];
   int rows;
   int columns;
   
   // check matrix compatibility for summation
   bool isCompatible(const Matrix &other)
   {
      const bool check = rows == other.rows && columns == other.columns;
      if(!check) Print("Check failed: matrices must be of the same sizes");
      return check;
   }
   
   // check matrix compatibility for multiplication
   bool isMCompatible(const Matrix &other)
   {
      const bool check = columns == other.rows;
      if(!check) Print("Check failed: this.columns should be equal to other.rows");
      return check;
   }
   
   void assign(const int r, const int c, const double v)
   {
      m[r * columns + c] = v;
   }
   
public:
   Matrix(const Matrix &other) : rows(other.rows), columns(other.columns)
   {
      ArrayCopy(m, other.m);
      if(rows <= 0 || columns <= 0)
      {
         SetUserError(ERR_USER_MATRIX_EMPTY);
      }
   }

   Matrix(const int r, const int c) : rows(r), columns(c)
   {
      if(rows <= 0 || columns <= 0)
      {
         SetUserError(ERR_USER_MATRIX_EMPTY);
      }
      else
      {
         ArrayResize(m, rows * columns);
         ArrayInitialize(m, 0);
      }
   }
   
   class MatrixRow
   {
   protected:
      const Matrix *owner;
      const int row;
      
   public:
      class MatrixElement
      {
      protected:
         const MatrixRow *row;
         const int column;
         
      public:
         MatrixElement(const MatrixRow &mr, const int c) : row(&mr), column(c) { }
         MatrixElement(const MatrixElement &other) : row(other.row), column(other.column) { }
         
         // return value of this element as double
         double operator~() const
         {
            return row.owner.m[row.row * row.owner.columns + column];
         }
         
         // assign double value to this element
         double operator=(const double v)
         {
            row.owner.m[row.row * row.owner.columns + column] = v;
            return v;
         }
      };
   
      MatrixRow(const Matrix &m, const int r) : owner(&m), row(r) { }
      MatrixRow(const MatrixRow &other) : owner(other.owner), row(other.row) { }
      
      // access matrix element as object to allow subsequent edition/assignment
      // note the type of index is int
      MatrixElement operator[](int c)
      {
         return MatrixElement(this, c);
      }

      // access matrix element as double value for immediate reading
      // note the type of index is uint
      double operator[](uint c) const
      {
         return owner.m[row * owner.columns + c];
      }
   };
   

   Matrix *operator=(const double &a[])
   {
      if(ArraySize(a) == ArraySize(m))
      {
         ArrayCopy(m, a);
      }
      return &this;
   }

   Matrix *operator=(const Matrix &other) // optional, required for builds pre-2715
   {
      ArrayCopy(m, other.m);
      ArrayResize(m, ArraySize(other.m));
      rows = other.rows;
      columns = other.columns;
      return &this;
   }

   MatrixRow operator[](int r) const
   {
      return MatrixRow(this, r);
   }

   Matrix *operator+=(const Matrix &other)
   {
      if(!isCompatible(other)) return &this;
      for(int i = 0; i < rows * columns; ++i)
      {
         m[i] += other.m[i];
      }
      return &this;
   }

   Matrix operator+(const Matrix &other) const
   {
      Matrix temp(this);
      return temp += other;
   }

   Matrix operator-(const Matrix &other) const
   {
      Matrix temp(this);
      return temp += other * -1.0;
   }

   Matrix *operator*=(const Matrix &other)
   {
      // check multiplication prerequisite: this.columns == other.rows
      if(!isMCompatible(other)) return &this;
      
      // declare temporary matrix for calculations
      // with resulting size this.rows * other.columns
      Matrix temp(rows, other.columns);
      
      for(int r = 0; r < temp.rows; ++r)
      {
         for(int c = 0; c < temp.columns; ++c)
         {
            double t = 0;
            // sum up products of i-th elements
            // from r-th row of this and c-th column of other matrices
            for(int i = 0; i < columns; ++i)
            {
               t += m[r * columns + i] * other.m[i * other.columns + c];
            }
            temp.assign(r, c, t);
         }
      }
      // copy result into current object
      this = temp; // call overloaded assignment for current object
      return &this;
   }

   Matrix operator*(const Matrix &other) const
   {
      Matrix temp(this);
      return temp *= other;
   }

   Matrix *operator*=(const double v)
   {
      for(int i = 0; i < ArraySize(m); ++i)
      {
         m[i] *= v;
      }
      return &this;
   }

   Matrix operator*(const double v) const
   {
      Matrix temp(this);
      return temp *= v;
   }
   
   bool operator==(const Matrix &other) const
   {
      return ArrayCompare(m, other.m) == 0;
   }

   bool operator!=(const Matrix &other) const
   {
      return !(this == other);
   }

   void print() const
   {
      for(int i = 0; i < rows; ++i)
      {
         ArrayPrint(m, _Digits, NULL, i * columns, columns, 0);
      }
   }
   
   template<typename T>
   T transpose() const
   {
      T result(columns, rows);
      for(int i = 0; i < rows; ++i)
      {
         for(int j = 0; j < columns; ++j)
         {
            result[j][i] = this[i][(uint)j];
         }
      }
      return result;
   }
   
   static ENUM_ERR_USER_MATRIX lastError()
   {
      if(_LastError >= ERR_USER_ERROR_FIRST)
      {
         return (ENUM_ERR_USER_MATRIX)(_LastError - ERR_USER_ERROR_FIRST);
      }
      return (ENUM_ERR_USER_MATRIX)_LastError;
   }
};

//+------------------------------------------------------------------+
//| Helper square matrix class                                       |
//+------------------------------------------------------------------+
class MatrixSquare : public Matrix
{
public:
   MatrixSquare(const int n, const int _ = -1) : Matrix(n, n)
   {
      if(_ != -1 && _ != n)
      {
         SetUserError(ERR_USER_MATRIX_NOT_SQUARE);
      }
   }
   MatrixSquare(const MatrixSquare &other) : Matrix(other.rows, other.rows)
   {
      ArrayCopy(m, other.m);
   }
   MatrixSquare(const double &array[]) : Matrix((int)sqrt(ArraySize(array)), (int)sqrt(ArraySize(array)))
   {
      if(rows * columns == ArraySize(array))
      {
         this = array;
      }
      else
      {
         SetUserError(ERR_USER_MATRIX_NOT_SQUARE);
      }
   }
   
   // Recursive function for determinant calculation
   double determinant() const
   {
      double result = 0.0;
      if(rows == 0)
      {
         SetUserError(ERR_USER_MATRIX_EMPTY);
         return 0;
      }
      else if(rows == 1)
      {
         return m[0];
      }
      else if(rows == 2)
      {
         // for matrix 2x2 do it crosswise,
         result = m[0] * m[3] - m[2] * m[1];
      }
      else
      {
         // otherwise extract corresponding minors and
         // find their determinats by the same method
         for(int i = 0; i < columns; ++i)
         {
            // since decomposition is always performed by the 1-st row with index 0
            // we can check oddity of the column index only
            result += (1 - 2 * (i % 2)) * m[i] * getMinor(0, i).determinant(); 
         }
      }
        
      return result;
   }

   MatrixSquare complement() const
   {
      MatrixSquare result(rows);
      for(int i = 0; i < rows; ++i)
      {
         for(int j = 0; j < columns; ++j)
         {
            result[i][j] = (1 - 2 * ((i + j) % 2)) * getMinor(i, j).determinant();
         }
      }
      
      return result;
   }
   
   MatrixSquare inverse() const
   {
      MatrixSquare result(rows);
      const double d = determinant();
      if(fabs(d) > DBL_EPSILON)
      {
         result = complement().transpose<MatrixSquare>() * (1 / d);
      }
      else
      {
         SetUserError(ERR_USER_MATRIX_SINGULAR);
      }
      return result;
   }
   
   MatrixSquare operator!() const
   {
      return inverse();
   }
   
   // Get required minor for the current matrix,
   // and given column/row indices to exclude
   MatrixSquare getMinor(const int row, const int column) const
   {
      const int size = rows - 1;
      MatrixSquare minor(size);
      int di = 0; // variables to skip the column/row
      int dj = 0;
      for(int i = 0; i <= size; ++i)
      {
         if(i == row)
         {
            di = 1;
            continue;
         }
         dj = 0;
         for(int j = 0; j <= size; ++j)
         {
            if(j == column)
            {
               dj = 1;
            }
            else
            {
               minor.m[(i - di) * size + j - dj] = m[i * columns + j];
            }
         }
      }
        
      return minor;
   }
};

//+------------------------------------------------------------------+
//| Helper identity matrix class                                     |
//+------------------------------------------------------------------+
class MatrixIdentity : public MatrixSquare
{
public:
   MatrixIdentity(const int n) : MatrixSquare(n)
   {
      for(int i = 0; i < n; ++i)
      {
         m[i * rows + i] = 1;
      }
   }
};

//+------------------------------------------------------------------+
