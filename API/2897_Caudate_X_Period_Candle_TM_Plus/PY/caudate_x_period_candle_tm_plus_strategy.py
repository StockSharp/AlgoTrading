import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class caudate_x_period_candle_tm_plus_strategy(Strategy):
    def __init__(self):
        super(caudate_x_period_candle_tm_plus_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period", "Indicators")
        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("EMA Length", "Trend EMA period", "Indicators")

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def AtrLength(self):
        return self._atr_length.Value

    @property
    def EmaLength(self):
        return self._ema_length.Value

    def OnStarted(self, time):
        super(caudate_x_period_candle_tm_plus_strategy, self).OnStarted(time)

        atr = AverageTrueRange()
        atr.Length = self.AtrLength
        ema = ExponentialMovingAverage()
        ema.Length = self.EmaLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(atr, ema, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, atr_value, ema_value):
        if candle.State != CandleStates.Finished:
            return
        av = float(atr_value)
        if av <= 0:
            return
        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        ev = float(ema_value)
        body = abs(close - open_p)
        if body > av * 0.75:
            if close > open_p and close > ev and self.Position <= 0:
                self.BuyMarket()
            elif close < open_p and close < ev and self.Position >= 0:
                self.SellMarket()

    def CreateClone(self):
        return caudate_x_period_candle_tm_plus_strategy()
