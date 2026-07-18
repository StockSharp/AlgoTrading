import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class improved_ema_cdc_trailing_stop_strategy(Strategy):
    def __init__(self):
        super(improved_ema_cdc_trailing_stop_strategy, self).__init__()
        self._ema60_period = self.Param("Ema60Period", 60) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA 60 Period", "Length of the fast EMA", "Parameters")
        self._ema90_period = self.Param("Ema90Period", 90) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA 90 Period", "Length of the slow EMA", "Parameters")
        self._atr_period = self.Param("AtrPeriod", 24) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period for ATR calculation", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(improved_ema_cdc_trailing_stop_strategy, self).OnStarted2(time)
        ema60 = ExponentialMovingAverage()
        ema60.Length = self._ema60_period.Value
        ema90 = ExponentialMovingAverage()
        ema90.Length = self._ema90_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._macd.Macd.ShortMa.Length = 12
        self._macd.Macd.LongMa.Length = 26
        self._macd.SignalMa.Length = 9
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._macd, ema60, ema90, atr, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema60)
            self.DrawIndicator(area, ema90)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, macd_val, ema60_val, ema90_val, atr_val):
        if candle.State != CandleStates.Finished:
            return
        if ema60_val.IsEmpty or ema90_val.IsEmpty or atr_val.IsEmpty:
            return
        macd_v = macd_val.Macd
        signal_v = macd_val.Signal
        if macd_v is None or signal_v is None:
            return
        macd_d = float(macd_v)
        signal_d = float(signal_v)
        ema60_v = float(ema60_val)
        ema90_v = float(ema90_val)
        atr_v = float(atr_val)
        if atr_v <= 0:
            return
        close = float(candle.ClosePrice)
        long_cond = close > ema60_v and ema60_v > ema90_v and macd_d > signal_d
        short_cond = close < ema60_v and ema60_v < ema90_v and macd_d < signal_d
        if long_cond and self.Position <= 0:
            self.BuyMarket()
        elif short_cond and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return improved_ema_cdc_trailing_stop_strategy()
