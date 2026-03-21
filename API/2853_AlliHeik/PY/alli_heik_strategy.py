import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class alli_heik_strategy(Strategy):
    def __init__(self):
        super(alli_heik_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Source candles for the strategy", "General")
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "Trend filter EMA period", "Indicators")

        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0
        self._initialized = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    def OnReseted(self):
        super(alli_heik_strategy, self).OnReseted()
        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0
        self._initialized = False

    def OnStarted(self, time):
        super(alli_heik_strategy, self).OnStarted(time)

        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0
        self._initialized = False

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(ema, self._on_process) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        ev = float(ema_value)
        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        c = float(candle.ClosePrice)

        ha_close = (o + h + l + c) / 4.0

        if not self._initialized:
            ha_open = (o + c) / 2.0
            self._initialized = True
        else:
            ha_open = (self._prev_ha_open + self._prev_ha_close) / 2.0

        ha_bullish = ha_close > ha_open
        ha_bearish = ha_close < ha_open
        prev_ha_bullish = self._prev_ha_close > self._prev_ha_open
        prev_ha_bearish = self._prev_ha_close < self._prev_ha_open

        if ha_bullish and prev_ha_bearish and c > ev and self.Position <= 0:
            self.BuyMarket()
        elif ha_bearish and prev_ha_bullish and c < ev and self.Position >= 0:
            self.SellMarket()

        self._prev_ha_open = ha_open
        self._prev_ha_close = ha_close

    def CreateClone(self):
        return alli_heik_strategy()
