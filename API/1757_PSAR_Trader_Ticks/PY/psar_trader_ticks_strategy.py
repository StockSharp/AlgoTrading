import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar
from StockSharp.Algo.Strategies import Strategy


class psar_trader_ticks_strategy(Strategy):
    def __init__(self):
        super(psar_trader_ticks_strategy, self).__init__()
        self._step = self.Param("Step", 0.001) \
            .SetDisplay("SAR Step", "Acceleration factor step", "Indicators")
        self._maximum = self.Param("Maximum", 0.2) \
            .SetDisplay("SAR Maximum", "Maximum acceleration factor", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_sar = 0.0
        self._prev_price = 0.0
        self._has_prev = False

    @property
    def step(self):
        return self._step.Value

    @property
    def maximum(self):
        return self._maximum.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(psar_trader_ticks_strategy, self).OnReseted()
        self._prev_sar = 0.0
        self._prev_price = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(psar_trader_ticks_strategy, self).OnStarted(time)
        psar = ParabolicSar()
        psar.AccelerationStep = self.step
        psar.AccelerationMax = self.maximum
        self.SubscribeCandles(self.candle_type).Bind(psar, self.process_candle).Start()

    def process_candle(self, candle, sar_value):
        if candle.State != CandleStates.Finished:
            return

        sv = float(sar_value)
        price = float(candle.ClosePrice)

        if not self._has_prev:
            self._prev_sar = sv
            self._prev_price = price
            self._has_prev = True
            return

        prev_above = self._prev_price > self._prev_sar
        curr_above = price > sv

        if curr_above and not prev_above and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif not curr_above and prev_above and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_sar = sv
        self._prev_price = price

    def CreateClone(self):
        return psar_trader_ticks_strategy()
