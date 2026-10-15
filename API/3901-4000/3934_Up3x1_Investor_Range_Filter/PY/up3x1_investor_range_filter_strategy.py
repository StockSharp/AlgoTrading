import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class up3x1_investor_range_filter_strategy(Strategy):
    def __init__(self):
        super(up3x1_investor_range_filter_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 14).SetDisplay("EMA Period", "EMA lookback", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_close = 0.0; self._prev_ema = 0.0; self._has_prev = False; self._cooldown = 0

    @property
    def ema_period(self): return self._ema_period.Value
    @property
    def candle_type(self): return self._candle_type.Value

    def OnReseted(self):
        super(up3x1_investor_range_filter_strategy, self).OnReseted()
        self._prev_close = 0.0; self._prev_ema = 0.0; self._has_prev = False; self._cooldown = 0

    def OnStarted2(self, time):
        super(up3x1_investor_range_filter_strategy, self).OnStarted2(time)
        self._has_prev = False; self._cooldown = 0
        ema = ExponentialMovingAverage(); ema.Length = self.ema_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self.process_candle).Start()

    def process_candle(self, candle, ema):
        if candle.State != CandleStates.Finished: return
        if not self.IsFormedAndOnlineAndAllowTrading(): return
        close = float(candle.ClosePrice); ema_val = float(ema)
        if not self._has_prev:
            self._prev_close = close; self._prev_ema = ema_val; self._has_prev = True; return
        if self._cooldown > 0:
            self._cooldown -= 1; self._prev_close = close; self._prev_ema = ema_val; return
        if self._prev_close <= self._prev_ema and close > ema_val and self.Position <= 0:
            volume = self.Volume + abs(self.Position)
            self.BuyMarket(volume); self._cooldown = 6
        elif self._prev_close >= self._prev_ema and close < ema_val and self.Position >= 0:
            volume = self.Volume + abs(self.Position)
            self.SellMarket(volume); self._cooldown = 6
        self._prev_close = close; self._prev_ema = ema_val

    def CreateClone(self): return up3x1_investor_range_filter_strategy()
