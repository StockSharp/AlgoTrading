import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class e_friday_session_strategy(Strategy):
    def __init__(self):
        super(e_friday_session_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 20).SetDisplay("EMA Period", "EMA trend filter", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_open = 0.0
        self._prev_close = 0.0
        self._has_prev = False
    @property
    def ema_period(self): return self._ema_period.Value
    @property
    def candle_type(self): return self._candle_type.Value
    def OnReseted(self):
        super(e_friday_session_strategy, self).OnReseted()
        self._prev_open = 0.0; self._prev_close = 0.0; self._has_prev = False
    def OnStarted2(self, time):
        super(e_friday_session_strategy, self).OnStarted2(time)
        self._has_prev = False
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self.process_candle).Start()
    def process_candle(self, candle, ema):
        if candle.State != CandleStates.Finished: return
        close = float(candle.ClosePrice)
        ema_val = float(ema)
        if not self._has_prev:
            self._prev_open = float(candle.OpenPrice); self._prev_close = close; self._has_prev = True; return
        prev_bearish = self._prev_close < self._prev_open
        prev_bullish = self._prev_close > self._prev_open
        if prev_bearish and close > ema_val and self.Position <= 0:
            if self.Position < 0: self.BuyMarket()
            self.BuyMarket()
        elif prev_bullish and close < ema_val and self.Position >= 0:
            if self.Position > 0: self.SellMarket()
            self.SellMarket()
        self._prev_open = float(candle.OpenPrice); self._prev_close = close
    def CreateClone(self): return e_friday_session_strategy()
