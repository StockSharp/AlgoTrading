import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class rsi_expert_breakout_strategy(Strategy):
    def __init__(self):
        super(rsi_expert_breakout_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI lookback period", "Indicators")
        self._rsi_upper = self.Param("RsiUpper", 70.0) \
            .SetDisplay("RSI Upper", "Overbought threshold", "Indicators")
        self._rsi_lower = self.Param("RsiLower", 30.0) \
            .SetDisplay("RSI Lower", "Oversold threshold", "Indicators")

        self._prev_rsi = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @property
    def RsiUpper(self):
        return self._rsi_upper.Value

    @property
    def RsiLower(self):
        return self._rsi_lower.Value

    def OnReseted(self):
        super(rsi_expert_breakout_strategy, self).OnReseted()
        self._prev_rsi = None

    def OnStarted2(self, time):
        super(rsi_expert_breakout_strategy, self).OnStarted2(time)
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

        rl = float(self.RsiLower)
        ru = float(self.RsiUpper)

        # RSI crosses above lower level from below
        if self._prev_rsi < rl and rv >= rl and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # RSI crosses below upper level from above
        elif self._prev_rsi > ru and rv <= ru and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_rsi = rv

    def CreateClone(self):
        return rsi_expert_breakout_strategy()
