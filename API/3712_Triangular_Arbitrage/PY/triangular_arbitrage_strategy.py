import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class triangular_arbitrage_strategy(Strategy):
    """Triangular arbitrage strategy - uses SMA crossover as simplified single-instrument proxy."""

    def __init__(self):
        super(triangular_arbitrage_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._fast_period = self.Param("FastPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast MA", "Fast MA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow MA", "Slow MA period", "Indicators")

        self._prev_fast = None
        self._prev_slow = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    def OnReseted(self):
        super(triangular_arbitrage_strategy, self).OnReseted()
        self._prev_fast = None
        self._prev_slow = None

    def OnStarted2(self, time):
        super(triangular_arbitrage_strategy, self).OnStarted2(time)

        fast_ma = SimpleMovingAverage()
        fast_ma.Length = self.FastPeriod
        slow_ma = SimpleMovingAverage()
        slow_ma.Length = self.SlowPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ma, slow_ma, self._process_candle).Start()

    def _process_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        fv = float(fast_val)
        sv = float(slow_val)

        if self._prev_fast is not None and self._prev_slow is not None:
            cross_up = self._prev_fast <= self._prev_slow and fv > sv
            cross_down = self._prev_fast >= self._prev_slow and fv < sv

            if cross_up and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif cross_down and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

        self._prev_fast = fv
        self._prev_slow = sv

    def CreateClone(self):
        return triangular_arbitrage_strategy()
