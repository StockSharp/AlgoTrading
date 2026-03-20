import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class atr_normalize_histogram_strategy(Strategy):
    def __init__(self):
        super(atr_normalize_histogram_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period", "Indicators")
        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("EMA Length", "EMA period for trend", "Indicators")

        self._prev_atr = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def AtrLength(self):
        return self._atr_length.Value

    @property
    def EmaLength(self):
        return self._ema_length.Value

    def OnReseted(self):
        super(atr_normalize_histogram_strategy, self).OnReseted()
        self._prev_atr = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(atr_normalize_histogram_strategy, self).OnStarted(time)

        self._prev_atr = 0.0
        self._has_prev = False

        atr = AverageTrueRange()
        atr.Length = self.AtrLength

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(atr, ema, self._on_process) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, atr_value, ema_value):
        if candle.State != CandleStates.Finished:
            return

        av = float(atr_value)
        ev = float(ema_value)

        if not self._has_prev:
            self._prev_atr = av
            self._has_prev = True
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_atr = av
            return

        close = float(candle.ClosePrice)

        if av > self._prev_atr * 1.1 and close > ev and self.Position <= 0:
            self.BuyMarket()
        elif av > self._prev_atr * 1.1 and close < ev and self.Position >= 0:
            self.SellMarket()

        self._prev_atr = av

    def CreateClone(self):
        return atr_normalize_histogram_strategy()
