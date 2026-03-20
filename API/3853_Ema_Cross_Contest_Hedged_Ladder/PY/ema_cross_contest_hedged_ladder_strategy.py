import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ema_cross_contest_hedged_ladder_strategy(Strategy):
    def __init__(self):
        super(ema_cross_contest_hedged_ladder_strategy, self).__init__()
        self._short_period = self.Param("ShortPeriod", 9).SetDisplay("Short EMA", "Short EMA period", "Indicators")
        self._long_period = self.Param("LongPeriod", 21).SetDisplay("Long EMA", "Long EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_short = 0.0
        self._prev_long = 0.0
        self._has_prev = False
    @property
    def short_period(self): return self._short_period.Value
    @property
    def long_period(self): return self._long_period.Value
    @property
    def candle_type(self): return self._candle_type.Value
    def OnReseted(self):
        super(ema_cross_contest_hedged_ladder_strategy, self).OnReseted()
        self._prev_short = 0.0
        self._prev_long = 0.0
        self._has_prev = False
    def OnStarted(self, time):
        super(ema_cross_contest_hedged_ladder_strategy, self).OnStarted(time)
        self._has_prev = False
        short_ema = ExponentialMovingAverage()
        short_ema.Length = self.short_period
        long_ema = ExponentialMovingAverage()
        long_ema.Length = self.long_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(short_ema, long_ema, self.process_candle).Start()
    def process_candle(self, candle, short_ema, long_ema):
        if candle.State != CandleStates.Finished:
            return
        short_val = float(short_ema)
        long_val = float(long_ema)
        if not self._has_prev:
            self._prev_short = short_val
            self._prev_long = long_val
            self._has_prev = True
            return
        if self._prev_short <= self._prev_long and short_val > long_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_short >= self._prev_long and short_val < long_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_short = short_val
        self._prev_long = long_val
    def CreateClone(self):
        return ema_cross_contest_hedged_ladder_strategy()
