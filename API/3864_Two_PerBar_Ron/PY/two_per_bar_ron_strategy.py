import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, Momentum
from StockSharp.Algo.Strategies import Strategy
class two_per_bar_ron_strategy(Strategy):
    def __init__(self):
        super(two_per_bar_ron_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 20).SetDisplay("EMA Period", "EMA trend filter", "Indicators")
        self._momentum_period = self.Param("MomentumPeriod", 10).SetDisplay("Momentum Period", "Momentum lookback", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_mom = 0.0; self._has_prev = False
    @property
    def ema_period(self): return self._ema_period.Value
    @property
    def momentum_period(self): return self._momentum_period.Value
    @property
    def candle_type(self): return self._candle_type.Value
    def OnReseted(self):
        super(two_per_bar_ron_strategy, self).OnReseted()
        self._prev_mom = 0.0; self._has_prev = False
    def OnStarted(self, time):
        super(two_per_bar_ron_strategy, self).OnStarted(time)
        self._has_prev = False
        ema = ExponentialMovingAverage(); ema.Length = self.ema_period
        mom = Momentum(); mom.Length = self.momentum_period
        sub = self.SubscribeCandles(self.candle_type)
        sub.Bind(ema, mom, self.process_candle).Start()
    def process_candle(self, candle, ema, mom):
        if candle.State != CandleStates.Finished: return
        close = float(candle.ClosePrice); ema_val = float(ema); mom_val = float(mom)
        if not self._has_prev: self._prev_mom = mom_val; self._has_prev = True; return
        if self._prev_mom <= 0 and mom_val > 0 and close > ema_val and self.Position <= 0:
            if self.Position < 0: self.BuyMarket()
            self.BuyMarket()
        elif self._prev_mom >= 0 and mom_val < 0 and close < ema_val and self.Position >= 0:
            if self.Position > 0: self.SellMarket()
            self.SellMarket()
        self._prev_mom = mom_val
    def CreateClone(self): return two_per_bar_ron_strategy()
