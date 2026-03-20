import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
class equal_volume_range_bars_strategy(Strategy):
    def __init__(self):
        super(equal_volume_range_bars_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 20).SetDisplay("EMA Period", "EMA lookback", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14).SetDisplay("ATR Period", "ATR lookback", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetDisplay("ATR Multiplier", "ATR band multiplier", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Candle timeframe", "General")
    @property
    def ema_period(self): return self._ema_period.Value
    @property
    def atr_period(self): return self._atr_period.Value
    @property
    def atr_multiplier(self): return self._atr_multiplier.Value
    @property
    def candle_type(self): return self._candle_type.Value
    def OnReseted(self): super(equal_volume_range_bars_strategy, self).OnReseted()
    def OnStarted(self, time):
        super(equal_volume_range_bars_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage(); ema.Length = self.ema_period
        atr = AverageTrueRange(); atr.Length = self.atr_period
        sub = self.SubscribeCandles(self.candle_type)
        sub.Bind(ema, atr, self.process_candle).Start()
    def process_candle(self, candle, ema, atr):
        if candle.State != CandleStates.Finished: return
        close = float(candle.ClosePrice); e = float(ema); a = float(atr)
        upper = e + a * self.atr_multiplier; lower = e - a * self.atr_multiplier
        if close > upper and self.Position <= 0:
            if self.Position < 0: self.BuyMarket()
            self.BuyMarket()
        elif close < lower and self.Position >= 0:
            if self.Position > 0: self.SellMarket()
            self.SellMarket()
    def CreateClone(self): return equal_volume_range_bars_strategy()
