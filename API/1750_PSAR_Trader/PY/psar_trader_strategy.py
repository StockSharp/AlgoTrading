import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar
from StockSharp.Algo.Strategies import Strategy


class psar_trader_strategy(Strategy):
    def __init__(self):
        super(psar_trader_strategy, self).__init__()
        self._sar_step = self.Param("SarStep", 0.001) \
            .SetDisplay("SAR Step", "Acceleration factor for Parabolic SAR", "Parabolic SAR")
        self._sar_max_step = self.Param("SarMaxStep", 0.2) \
            .SetDisplay("SAR Max Step", "Maximum acceleration factor", "Parabolic SAR")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_price_above_sar = False
        self._has_prev = False

    @property
    def sar_step(self):
        return self._sar_step.Value

    @property
    def sar_max_step(self):
        return self._sar_max_step.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(psar_trader_strategy, self).OnReseted()
        self._prev_price_above_sar = False
        self._has_prev = False

    def OnStarted(self, time):
        super(psar_trader_strategy, self).OnStarted(time)
        psar = ParabolicSar()
        psar.AccelerationStep = self.sar_step
        psar.AccelerationMax = self.sar_max_step
        self.SubscribeCandles(self.candle_type).Bind(psar, self.process_candle).Start()

    def process_candle(self, candle, sar_value):
        if candle.State != CandleStates.Finished:
            return

        is_price_above_sar = float(candle.ClosePrice) > float(sar_value)

        if not self._has_prev:
            self._prev_price_above_sar = is_price_above_sar
            self._has_prev = True
            return

        if self._prev_price_above_sar != is_price_above_sar:
            if is_price_above_sar and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif not is_price_above_sar and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

        self._prev_price_above_sar = is_price_above_sar

    def CreateClone(self):
        return psar_trader_strategy()
