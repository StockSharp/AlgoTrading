import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar
from StockSharp.Algo.Strategies import Strategy


class psar_trader_v2_strategy(Strategy):
    def __init__(self):
        super(psar_trader_v2_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Trading timeframe", "General")
        self._step = self.Param("Step", 0.001) \
            .SetDisplay("PSAR Step", "Acceleration step for PSAR", "Indicators")
        self._maximum = self.Param("Maximum", 0.2) \
            .SetDisplay("PSAR Maximum", "Maximum acceleration for PSAR", "Indicators")
        self._prev_sar = 0.0
        self._prev_price_above_sar = False
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def step(self):
        return self._step.Value

    @property
    def maximum(self):
        return self._maximum.Value

    def OnReseted(self):
        super(psar_trader_v2_strategy, self).OnReseted()
        self._prev_sar = 0.0
        self._prev_price_above_sar = False
        self._has_prev = False

    def OnStarted(self, time):
        super(psar_trader_v2_strategy, self).OnStarted(time)
        psar = ParabolicSar()
        psar.AccelerationStep = self.step
        psar.AccelerationMax = self.maximum
        self.SubscribeCandles(self.candle_type).Bind(psar, self.process_candle).Start()

    def process_candle(self, candle, sar):
        if candle.State != CandleStates.Finished:
            return

        sv = float(sar)
        price_above_sar = float(candle.ClosePrice) > sv

        if not self._has_prev:
            self._prev_sar = sv
            self._prev_price_above_sar = price_above_sar
            self._has_prev = True
            return

        if price_above_sar != self._prev_price_above_sar:
            if price_above_sar and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif not price_above_sar and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

        self._prev_sar = sv
        self._prev_price_above_sar = price_above_sar

    def CreateClone(self):
        return psar_trader_v2_strategy()
