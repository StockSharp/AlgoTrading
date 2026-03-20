import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class xrsid_de_marker_histogram_strategy(Strategy):
    def __init__(self):
        super(xrsid_de_marker_histogram_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Source candles", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI indicator period", "Indicators")
        self._sma_period = self.Param("SmaPeriod", 5) \
            .SetDisplay("SMA Period", "Smoothing period", "Indicators")

        self._prev_rsi = 0.0
        self._prev_prev_rsi = 0.0
        self._initialized = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @property
    def SmaPeriod(self):
        return self._sma_period.Value

    def OnReseted(self):
        super(xrsid_de_marker_histogram_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._prev_prev_rsi = 0.0
        self._initialized = False

    def OnStarted(self, time):
        super(xrsid_de_marker_histogram_strategy, self).OnStarted(time)

        self._prev_rsi = 0.0
        self._prev_prev_rsi = 0.0
        self._initialized = False

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        sma = SimpleMovingAverage()
        sma.Length = self.SmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(rsi, sma, self._on_process) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, rsi_value, sma_value):
        if candle.State != CandleStates.Finished:
            return

        rv = float(rsi_value)

        if not self._initialized:
            self._prev_prev_rsi = rv
            self._prev_rsi = rv
            self._initialized = True
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_prev_rsi = self._prev_rsi
            self._prev_rsi = rv
            return

        buy_signal = self._prev_prev_rsi > self._prev_rsi and rv >= self._prev_rsi and self._prev_rsi < 35.0
        sell_signal = self._prev_prev_rsi < self._prev_rsi and rv <= self._prev_rsi and self._prev_rsi > 65.0

        if buy_signal and self.Position <= 0:
            self.BuyMarket()
        elif sell_signal and self.Position >= 0:
            self.SellMarket()

        self._prev_prev_rsi = self._prev_rsi
        self._prev_rsi = rv

    def CreateClone(self):
        return xrsid_de_marker_histogram_strategy()
