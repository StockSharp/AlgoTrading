import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class quadratic_regression_strategy(Strategy):
    def __init__(self):
        super(quadratic_regression_strategy, self).__init__()

        self._length = self.Param("Length", 54).SetGreaterThanZero()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1)))
        self._closes = []
        self._prev_price = None
        self._prev_regression = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(quadratic_regression_strategy, self).OnReseted()
        self._closes = []
        self._prev_price = None
        self._prev_regression = None

    def OnStarted2(self, time):
        super(quadratic_regression_strategy, self).OnStarted2(time)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        length = int(self._length.Value)
        price = float(candle.ClosePrice)
        self._closes.append(price)

        if len(self._closes) > length:
            del self._closes[0:len(self._closes) - length]

        if len(self._closes) < length:
            return

        regression = self._fit_last(self._closes)

        if self._prev_price is not None:
            cross_up = self._prev_price <= self._prev_regression and price > regression
            cross_down = self._prev_price >= self._prev_regression and price < regression

            if cross_up and self.Position <= 0:
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
            elif cross_down and self.Position >= 0:
                self.SellMarket(self.Volume + Math.Abs(self.Position))

        self._prev_price = price
        self._prev_regression = regression

    @staticmethod
    def _fit_last(values):
        n = len(values)
        if n < 3:
            return values[-1] if n else 0.0

        s0 = float(n)
        s1 = s2 = s3 = s4 = 0.0
        t0 = t1 = t2 = 0.0

        for i, y in enumerate(values):
            x = float(i)
            x2 = x * x
            s1 += x
            s2 += x2
            s3 += x2 * x
            s4 += x2 * x2
            t0 += y
            t1 += x * y
            t2 += x2 * y

        a = [
            [s0, s1, s2, t0],
            [s1, s2, s3, t1],
            [s2, s3, s4, t2],
        ]

        for col in range(3):
            pivot = max(range(col, 3), key=lambda row: abs(a[row][col]))
            if abs(a[pivot][col]) < 1e-12:
                return values[-1]

            if pivot != col:
                a[col], a[pivot] = a[pivot], a[col]

            divisor = a[col][col]
            for k in range(col, 4):
                a[col][k] /= divisor

            for row in range(3):
                if row == col:
                    continue
                factor = a[row][col]
                for k in range(col, 4):
                    a[row][k] -= factor * a[col][k]

        x = float(n - 1)
        return a[0][3] + a[1][3] * x + a[2][3] * x * x

    def CreateClone(self):
        return quadratic_regression_strategy()
