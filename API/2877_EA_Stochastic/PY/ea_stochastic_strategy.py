import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class ea_stochastic_strategy(Strategy):
    def __init__(self):
        super(ea_stochastic_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period", "Indicators")
        self._upper_level = self.Param("UpperLevel", 70.0) \
            .SetDisplay("Upper Level", "Overbought level", "Levels")
        self._lower_level = self.Param("LowerLevel", 30.0) \
            .SetDisplay("Lower Level", "Oversold level", "Levels")

        self._prev_rsi = 50.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def RsiLength(self):
        return self._rsi_length.Value

    @property
    def UpperLevel(self):
        return self._upper_level.Value

    @property
    def LowerLevel(self):
        return self._lower_level.Value

    def OnReseted(self):
        super(ea_stochastic_strategy, self).OnReseted()
        self._prev_rsi = 50.0
        self._has_prev = False

    def OnStarted(self, time):
        super(ea_stochastic_strategy, self).OnStarted(time)

        self._prev_rsi = 50.0
        self._has_prev = False

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiLength

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
        if not self._has_prev:
            self._prev_rsi = rv
            self._has_prev = True
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_rsi = rv
            return
        ll = float(self.LowerLevel)
        ul = float(self.UpperLevel)
        if self._prev_rsi < ll and rv >= ll and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_rsi > ul and rv <= ul and self.Position >= 0:
            self.SellMarket()
        self._prev_rsi = rv

    def CreateClone(self):
        return ea_stochastic_strategy()
