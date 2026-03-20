import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class bad_orders_strategy(Strategy):
    def __init__(self):
        super(bad_orders_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 20).SetDisplay("EMA Period", "EMA lookback", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14).SetDisplay("ATR Period", "ATR lookback", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 1.5).SetDisplay("ATR Multiplier", "ATR breakout multiplier", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Candle timeframe", "General")
    @property
    def ema_period(self): return self._ema_period.Value
    @property
    def atr_period(self): return self._atr_period.Value
    @property
    def atr_multiplier(self): return self._atr_multiplier.Value
    @property
    def candle_type(self): return self._candle_type.Value
    def OnReseted(self):
        super(bad_orders_strategy, self).OnReseted()
    def OnStarted(self, time):
        super(bad_orders_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        atr = AverageTrueRange()
        atr.Length = self.atr_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, atr, self.process_candle).Start()
    def process_candle(self, candle, ema, atr):
        if candle.State != CandleStates.Finished: return
        close = float(candle.ClosePrice); ema_val = float(ema); atr_val = float(atr)
        upper = ema_val + atr_val * self.atr_multiplier
        lower = ema_val - atr_val * self.atr_multiplier
        if close > upper and self.Position <= 0:
            if self.Position < 0: self.BuyMarket()
            self.BuyMarket()
        elif close < lower and self.Position >= 0:
            if self.Position > 0: self.SellMarket()
            self.SellMarket()
    def CreateClone(self): return bad_orders_strategy()
