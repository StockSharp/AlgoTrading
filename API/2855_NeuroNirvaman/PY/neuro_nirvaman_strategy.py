import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class neuro_nirvaman_strategy(Strategy):
    def __init__(self):
        super(neuro_nirvaman_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Source candles", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI indicator period", "Indicators")
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "EMA trend filter period", "Indicators")
        self._rsi_oversold = self.Param("RsiOversold", 45.0) \
            .SetDisplay("RSI Oversold", "RSI oversold level", "Signals")
        self._rsi_overbought = self.Param("RsiOverbought", 55.0) \
            .SetDisplay("RSI Overbought", "RSI overbought level", "Signals")

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    @property
    def RsiOversold(self):
        return self._rsi_oversold.Value

    @property
    def RsiOverbought(self):
        return self._rsi_overbought.Value

    def OnStarted(self, time):
        super(neuro_nirvaman_strategy, self).OnStarted(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(rsi, ema, self._on_process) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, rsi_value, ema_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        rv = float(rsi_value)

        if rv < float(self.RsiOversold) and self.Position <= 0:
            self.BuyMarket()
        elif rv > float(self.RsiOverbought) and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return neuro_nirvaman_strategy()
