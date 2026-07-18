import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class basic_rsi_ea_template_strategy(Strategy):
    def __init__(self):
        super(basic_rsi_ea_template_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI calculation period", "Indicators")
        self._overbought_level = self.Param("OverboughtLevel", 70.0) \
            .SetDisplay("Overbought Level", "RSI overbought threshold", "Indicators")
        self._oversold_level = self.Param("OversoldLevel", 30.0) \
            .SetDisplay("Oversold Level", "RSI oversold threshold", "Indicators")

        self._rsi = None
        self._prev_rsi = None

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def overbought_level(self):
        return self._overbought_level.Value

    @property
    def oversold_level(self):
        return self._oversold_level.Value

    def OnReseted(self):
        super(basic_rsi_ea_template_strategy, self).OnReseted()
        self._rsi = None
        self._prev_rsi = None

    def OnStarted2(self, time):
        super(basic_rsi_ea_template_strategy, self).OnStarted2(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        subscription.Bind(self._rsi, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._rsi.IsFormed:
            return

        rsi = float(rsi_value)

        if self._prev_rsi is not None:
            cross_below_oversold = self._prev_rsi >= self.oversold_level and rsi < self.oversold_level
            cross_above_overbought = self._prev_rsi <= self.overbought_level and rsi > self.overbought_level

            if cross_below_oversold and self.Position <= 0:
                self.BuyMarket()
            elif cross_above_overbought and self.Position >= 0:
                self.SellMarket()

        self._prev_rsi = rsi

    def CreateClone(self):
        return basic_rsi_ea_template_strategy()
