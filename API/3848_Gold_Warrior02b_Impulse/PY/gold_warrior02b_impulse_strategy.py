import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class gold_warrior02b_impulse_strategy(Strategy):
    def __init__(self):
        super(gold_warrior02b_impulse_strategy, self).__init__()
        self._cci_period = self.Param("CciPeriod", 14).SetDisplay("CCI Period", "CCI lookback", "Indicators")
        self._ema_period = self.Param("EmaPeriod", 21).SetDisplay("EMA Period", "EMA trend filter", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_cci = 0.0
        self._has_prev = False
    @property
    def cci_period(self): return self._cci_period.Value
    @property
    def ema_period(self): return self._ema_period.Value
    @property
    def candle_type(self): return self._candle_type.Value
    def OnReseted(self):
        super(gold_warrior02b_impulse_strategy, self).OnReseted()
        self._prev_cci = 0.0
        self._has_prev = False
    def OnStarted2(self, time):
        super(gold_warrior02b_impulse_strategy, self).OnStarted2(time)
        self._has_prev = False
        cci = CommodityChannelIndex()
        cci.Length = self.cci_period
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(cci, ema, self.process_candle).Start()
    def process_candle(self, candle, cci, ema):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        cci_val = float(cci)
        ema_val = float(ema)
        if not self._has_prev:
            self._prev_cci = cci_val
            self._has_prev = True
            return
        if self._prev_cci <= 0 and cci_val > 0 and close > ema_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_cci >= 0 and cci_val < 0 and close < ema_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_cci = cci_val
    def CreateClone(self):
        return gold_warrior02b_impulse_strategy()
