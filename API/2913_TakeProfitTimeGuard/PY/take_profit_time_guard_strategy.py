import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy


class take_profit_time_guard_strategy(Strategy):
    def __init__(self):
        super(take_profit_time_guard_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._cci_length = self.Param("CciLength", 14) \
            .SetDisplay("CCI Length", "CCI period", "Indicators")
        self._upper_level = self.Param("UpperLevel", 100.0) \
            .SetDisplay("Upper Level", "CCI level for sell signal", "Logic")
        self._lower_level = self.Param("LowerLevel", -100.0) \
            .SetDisplay("Lower Level", "CCI level for buy signal", "Logic")

        self._prev_cci = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def CciLength(self):
        return self._cci_length.Value

    @property
    def UpperLevel(self):
        return self._upper_level.Value

    @property
    def LowerLevel(self):
        return self._lower_level.Value

    def OnReseted(self):
        super(take_profit_time_guard_strategy, self).OnReseted()
        self._prev_cci = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(take_profit_time_guard_strategy, self).OnStarted2(time)
        self._prev_cci = 0.0
        self._has_prev = False

        cci = CommodityChannelIndex()
        cci.Length = self.CciLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(cci, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, cci_value):
        if candle.State != CandleStates.Finished:
            return
        cv = float(cci_value)
        if not self._has_prev:
            self._prev_cci = cv
            self._has_prev = True
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_cci = cv
            return
        if self._prev_cci < self.LowerLevel and cv >= self.LowerLevel and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_cci > self.UpperLevel and cv <= self.UpperLevel and self.Position >= 0:
            self.SellMarket()
        self._prev_cci = cv

    def CreateClone(self):
        return take_profit_time_guard_strategy()
