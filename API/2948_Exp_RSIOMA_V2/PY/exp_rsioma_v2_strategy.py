import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class exp_rsioma_v2_strategy(Strategy):
    def __init__(self):
        super(exp_rsioma_v2_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI calculation period", "Indicators")
        self._overbought = self.Param("Overbought", 65.0) \
            .SetDisplay("Overbought", "Overbought RSI level", "Levels")
        self._oversold = self.Param("Oversold", 35.0) \
            .SetDisplay("Oversold", "Oversold RSI level", "Levels")

        self._prev_rsi = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @property
    def Overbought(self):
        return self._overbought.Value

    @property
    def Oversold(self):
        return self._oversold.Value

    def OnReseted(self):
        super(exp_rsioma_v2_strategy, self).OnReseted()
        self._prev_rsi = None

    def OnStarted(self, time):
        super(exp_rsioma_v2_strategy, self).OnStarted(time)
        self._prev_rsi = None

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

        ind_area = self.CreateChartArea()
        if ind_area is not None:
            self.DrawIndicator(ind_area, rsi)

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

        ob = float(self.Overbought)
        os_level = float(self.Oversold)

        # RSI crosses above oversold
        if self._prev_rsi <= os_level and rv > os_level and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # RSI crosses below overbought
        elif self._prev_rsi >= ob and rv < ob and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_rsi = rv

    def CreateClone(self):
        return exp_rsioma_v2_strategy()
