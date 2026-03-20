import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class full_dump_bb_rsi_strategy(Strategy):
    def __init__(self):
        super(full_dump_bb_rsi_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI averaging period", "Indicators")
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "Trend filter EMA", "Indicators")

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    def OnStarted(self, time):
        super(full_dump_bb_rsi_strategy, self).OnStarted(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod
        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, ema, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, rsi_value, ema_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        close = float(candle.ClosePrice)
        rv = float(rsi_value)
        ev = float(ema_value)
        if rv < 25 and self.Position <= 0:
            self.BuyMarket()
        elif rv > 75 and self.Position >= 0:
            self.SellMarket()
        elif close > ev and rv > 60 and rv < 70 and self.Position <= 0:
            self.BuyMarket()
        elif close < ev and rv < 40 and rv > 30 and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return full_dump_bb_rsi_strategy()
