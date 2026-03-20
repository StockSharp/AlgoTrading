import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class brain_trend2_v2_duplex_strategy(Strategy):
    def __init__(self):
        super(brain_trend2_v2_duplex_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._atr_period = self.Param("AtrPeriod", 7) \
            .SetDisplay("ATR Period", "ATR length", "Indicators")
        self._ema_period = self.Param("EmaPeriod", 14) \
            .SetDisplay("EMA Period", "EMA length for trend", "Indicators")
        self._channel_mult = self.Param("ChannelMult", 2.5) \
            .SetDisplay("Channel Mult", "ATR multiplier for channel width", "Indicators")

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    @property
    def ChannelMult(self):
        return self._channel_mult.Value

    def OnStarted(self, time):
        super(brain_trend2_v2_duplex_strategy, self).OnStarted(time)

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod

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
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        av = float(atr_value)
        ev = float(ema_value)
        close = float(candle.ClosePrice)
        mult = float(self.ChannelMult)
        upper = ev + mult * av
        lower = ev - mult * av

        if close > upper and self.Position <= 0:
            self.BuyMarket()
        elif close < lower and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return brain_trend2_v2_duplex_strategy()
