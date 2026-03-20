import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class martingale_ma_breakout_strategy(Strategy):
    def __init__(self):
        super(martingale_ma_breakout_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Source candles", "General")
        self._ma_period = self.Param("MaPeriod", 12) \
            .SetDisplay("MA Period", "Moving average period", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period for volatility filter", "Indicators")

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    def OnStarted(self, time):
        super(martingale_ma_breakout_strategy, self).OnStarted(time)

        ema = ExponentialMovingAverage()
        ema.Length = self.MaPeriod

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(ema, atr, self._on_process) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, ema_value, atr_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)
        ev = float(ema_value)
        av = float(atr_value)
        distance = abs(close - ev)

        if close > ev and distance > av and self.Position <= 0:
            self.BuyMarket()
        elif close < ev and distance > av and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return martingale_ma_breakout_strategy()
