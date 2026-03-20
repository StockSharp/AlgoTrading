import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class silver_trend_duplex_strategy(Strategy):
    def __init__(self):
        super(silver_trend_duplex_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._atr_length = self.Param("AtrLength", 10) \
            .SetDisplay("ATR Length", "ATR period", "Indicators")
        self._ema_length = self.Param("EmaLength", 21) \
            .SetDisplay("EMA Length", "EMA trend", "Indicators")

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
        super(silver_trend_duplex_strategy, self).OnStarted(time)

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
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        close = float(candle.ClosePrice)
        ev = float(ema_value)
        rng = float(candle.HighPrice) - float(candle.LowPrice)
        if rng <= 0:
            return
        if close > ev + rng * 1.5 and self.Position <= 0:
            self.BuyMarket()
        elif close < ev - rng * 1.5 and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return silver_trend_duplex_strategy()
