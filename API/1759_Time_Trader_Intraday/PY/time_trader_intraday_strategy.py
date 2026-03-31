import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class time_trader_intraday_strategy(Strategy):
    def __init__(self):
        super(time_trader_intraday_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 10) \
            .SetDisplay("Fast Period", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 21) \
            .SetDisplay("Slow Period", "Slow EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_initialized = False

    @property
    def fast_period(self):
        return self._fast_period.Value

    @property
    def slow_period(self):
        return self._slow_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(time_trader_intraday_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_initialized = False

    def OnStarted2(self, time):
        super(time_trader_intraday_strategy, self).OnStarted2(time)
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.fast_period
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.slow_period
        self.SubscribeCandles(self.candle_type).Bind(fast_ema, slow_ema, self.process_candle).Start()

    def process_candle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return

        fv = float(fast)
        sv = float(slow)

        if not self._is_initialized:
            self._prev_fast = fv
            self._prev_slow = sv
            self._is_initialized = True
            return

        cross_up = self._prev_fast <= self._prev_slow and fv > sv
        cross_down = self._prev_fast >= self._prev_slow and fv < sv

        self._prev_fast = fv
        self._prev_slow = sv

        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return time_trader_intraday_strategy()
