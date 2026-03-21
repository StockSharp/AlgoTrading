import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class candle_shadows_v1_strategy(Strategy):
    def __init__(self):
        super(candle_shadows_v1_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._ema_length = self.Param("EmaLength", 30) \
            .SetDisplay("EMA Length", "Trend EMA period", "Indicators")
        self._shadow_ratio = self.Param("ShadowRatio", 3.0) \
            .SetDisplay("Shadow Ratio", "Min shadow/body ratio", "Logic")

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def EmaLength(self):
        return self._ema_length.Value

    @property
    def ShadowRatio(self):
        return self._shadow_ratio.Value

    def OnStarted(self, time):
        super(candle_shadows_v1_strategy, self).OnStarted(time)

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        ev = float(ema_value)

        body = abs(close - open_p)
        if body <= 0:
            return

        upper_shadow = high - max(close, open_p)
        lower_shadow = min(close, open_p) - low

        if lower_shadow > body * self.ShadowRatio and close > ev and self.Position <= 0:
            self.BuyMarket()
        elif upper_shadow > body * self.ShadowRatio and close < ev and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return candle_shadows_v1_strategy()
