import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
class ag_macd_dual_strategy(Strategy):
    def __init__(self):
        super(ag_macd_dual_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 50).SetDisplay("EMA Period", "EMA trend filter", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_macd = 0.0; self._prev_signal = 0.0; self._has_prev = False; self._current_ema = 0.0
    @property
    def ema_period(self): return self._ema_period.Value
    @property
    def candle_type(self): return self._candle_type.Value
    def OnReseted(self):
        super(ag_macd_dual_strategy, self).OnReseted()
        self._prev_macd = 0.0; self._prev_signal = 0.0; self._has_prev = False; self._current_ema = 0.0
    def OnStarted2(self, time):
        super(ag_macd_dual_strategy, self).OnStarted2(time)
        self._has_prev = False
        macd = MovingAverageConvergenceDivergenceSignal()
        ema = ExponentialMovingAverage(); ema.Length = self.ema_period
        sub = self.SubscribeCandles(self.candle_type)
        sub.BindEx(macd, self.process_macd).Bind(ema, self.process_ema).Start()
    def process_ema(self, candle, ema):
        if candle.State != CandleStates.Finished: return
        self._current_ema = float(ema)
    def process_macd(self, candle, value):
        if candle.State != CandleStates.Finished: return
        if not value.IsFinal or value.IsEmpty: return
        if value.Macd is None or value.Signal is None: return
        ml = float(value.Macd); sl = float(value.Signal); hist = ml - sl
        if not self._has_prev: self._prev_macd = ml; self._prev_signal = sl; self._has_prev = True; return
        prev_hist = self._prev_macd - self._prev_signal; close = float(candle.ClosePrice)
        if prev_hist <= 0 and hist > 0 and close > self._current_ema and self.Position <= 0:
            if self.Position < 0: self.BuyMarket()
            self.BuyMarket()
        elif prev_hist >= 0 and hist < 0 and close < self._current_ema and self.Position >= 0:
            if self.Position > 0: self.SellMarket()
            self.SellMarket()
        self._prev_macd = ml; self._prev_signal = sl
    def CreateClone(self): return ag_macd_dual_strategy()
