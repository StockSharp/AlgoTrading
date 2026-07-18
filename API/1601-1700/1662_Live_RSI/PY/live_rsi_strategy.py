import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class live_rsi_strategy(Strategy):
    def __init__(self):
        super(live_rsi_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Period for RSI", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_rsi = 0.0
        self._has_prev = False

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(live_rsi_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(live_rsi_strategy, self).OnStarted2(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        sar = ParabolicSar()
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, sar, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, rsi, sar):
        if candle.State != CandleStates.Finished:
            return
        if not self._has_prev:
            self._prev_rsi = rsi
            self._has_prev = True
            return
        close = candle.ClosePrice
        # RSI cross above 50 + SAR below price -> buy
        if self._prev_rsi <= 50 and rsi > 50 and sar < close:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        # RSI cross below 50 + SAR above price -> sell
        elif self._prev_rsi >= 50 and rsi < 50 and sar > close:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()
        self._prev_rsi = rsi

    def CreateClone(self):
        return live_rsi_strategy()
