import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class ohlc_stochastic_strategy(Strategy):
    def __init__(self):
        super(ohlc_stochastic_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI lookback", "Indicators")

        self._prev_rsi = None

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    def OnReseted(self):
        super(ohlc_stochastic_strategy, self).OnReseted()
        self._prev_rsi = None

    def OnStarted2(self, time):
        super(ohlc_stochastic_strategy, self).OnStarted2(time)
        self._prev_rsi = None
        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self._on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        rv = float(rsi_value)
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_rsi = rv
            return
        if self._prev_rsi is None:
            self._prev_rsi = rv
            return
        if self._prev_rsi < 30.0 and rv >= 30.0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_rsi > 70.0 and rv <= 70.0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_rsi = rv

    def CreateClone(self):
        return ohlc_stochastic_strategy()
