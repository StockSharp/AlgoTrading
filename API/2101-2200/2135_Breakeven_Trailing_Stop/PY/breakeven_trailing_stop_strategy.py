import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class breakeven_trailing_stop_strategy(Strategy):
    def __init__(self):
        super(breakeven_trailing_stop_strategy, self).__init__()
        self._breakeven_percent = self.Param("BreakevenPercent", 0.5) \
            .SetDisplay("Breakeven %", "Profit percent before breakeven", "Trading")
        self._breakeven_offset = self.Param("BreakevenOffset", 0.1) \
            .SetDisplay("Breakeven Offset", "Stop offset percent after breakeven", "Trading")
        self._trailing_activation = self.Param("TrailingActivation", 0.3) \
            .SetDisplay("Trailing Activation", "Profit percent above stop before trailing", "Trading")
        self._trailing_distance = self.Param("TrailingDistance", 0.3) \
            .SetDisplay("Trailing Distance", "Percent from price to trailing stop", "Trading")
        self._fast_ema_period = self.Param("FastEmaPeriod", 8) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 21) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for updates", "General")
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._breakeven_reached = False

    @property
    def breakeven_percent(self):
        return self._breakeven_percent.Value
    @property
    def breakeven_offset(self):
        return self._breakeven_offset.Value
    @property
    def trailing_activation(self):
        return self._trailing_activation.Value
    @property
    def trailing_distance(self):
        return self._trailing_distance.Value
    @property
    def fast_ema_period(self):
        return self._fast_ema_period.Value
    @property
    def slow_ema_period(self):
        return self._slow_ema_period.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(breakeven_trailing_stop_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._breakeven_reached = False

    def OnStarted2(self, time):
        super(breakeven_trailing_stop_strategy, self).OnStarted2(time)
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.fast_ema_period
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.slow_ema_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, self.OnProcess).Start()

    def OnProcess(self, candle, fast_ema, slow_ema):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        fast_val = float(fast_ema)
        slow_val = float(slow_ema)

        if self.Position == 0:
            self._breakeven_reached = False
            if fast_val > slow_val:
                self._entry_price = close
                self._stop_price = close * (1.0 - 1.0 / 100.0)
                self.BuyMarket()
            elif fast_val < slow_val:
                self._entry_price = close
                self._stop_price = close * (1.0 + 1.0 / 100.0)
                self.SellMarket()
            return

        if self.Position > 0:
            if not self._breakeven_reached:
                if close >= self._entry_price * (1.0 + float(self.breakeven_percent) / 100.0):
                    self._stop_price = self._entry_price * (1.0 + float(self.breakeven_offset) / 100.0)
                    self._breakeven_reached = True
            else:
                trailing_stop = close * (1.0 - float(self.trailing_distance) / 100.0)
                if close >= self._stop_price * (1.0 + float(self.trailing_activation) / 100.0) and trailing_stop > self._stop_price:
                    self._stop_price = trailing_stop
            if float(candle.LowPrice) <= self._stop_price:
                self.SellMarket()
            elif fast_val < slow_val:
                self.SellMarket()
        elif self.Position < 0:
            if not self._breakeven_reached:
                if close <= self._entry_price * (1.0 - float(self.breakeven_percent) / 100.0):
                    self._stop_price = self._entry_price * (1.0 - float(self.breakeven_offset) / 100.0)
                    self._breakeven_reached = True
            else:
                trailing_stop = close * (1.0 + float(self.trailing_distance) / 100.0)
                if close <= self._stop_price * (1.0 - float(self.trailing_activation) / 100.0) and trailing_stop < self._stop_price:
                    self._stop_price = trailing_stop
            if float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket()
            elif fast_val > slow_val:
                self.BuyMarket()

    def CreateClone(self):
        return breakeven_trailing_stop_strategy()
