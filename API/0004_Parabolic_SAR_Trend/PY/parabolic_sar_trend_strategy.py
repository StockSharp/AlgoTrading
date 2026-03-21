import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar
from StockSharp.Algo.Strategies import Strategy

class parabolic_sar_trend_strategy(Strategy):
    """
    Strategy based on Parabolic SAR indicator.
    Enters long when price crosses above SAR, short when price crosses below SAR.
    """

    def __init__(self):
        super(parabolic_sar_trend_strategy, self).__init__()
        self._acceleration_factor = self.Param("AccelerationFactor", 0.003).SetDisplay("Acceleration Factor", "Initial acceleration factor for SAR calculation", "Indicators")
        self._max_acceleration_factor = self.Param("MaxAccelerationFactor", 0.03).SetDisplay("Max Acceleration Factor", "Maximum acceleration factor for SAR calculation", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_sar_value = 0.0
        self._prev_is_price_above_sar = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(parabolic_sar_trend_strategy, self).OnReseted()
        self._prev_sar_value = 0.0
        self._prev_is_price_above_sar = False

    def OnStarted(self, time):
        super(parabolic_sar_trend_strategy, self).OnStarted(time)

        sar = ParabolicSar()
        sar.Acceleration = self._acceleration_factor.Value
        sar.AccelerationMax = self._max_acceleration_factor.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sar, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sar)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sar_val):
        if candle.State != CandleStates.Finished:
            return

        sv = float(sar_val)
        if sv <= 0:
            return

        is_price_above_sar = float(candle.ClosePrice) > sv
        is_entry_signal = self._prev_sar_value > 0 and is_price_above_sar != self._prev_is_price_above_sar

        if is_entry_signal:
            vol = self.Volume + abs(self.Position)
            if is_price_above_sar and self.Position <= 0:
                self.BuyMarket(vol)
            elif not is_price_above_sar and self.Position >= 0:
                self.SellMarket(vol)

        self._prev_sar_value = sv
        self._prev_is_price_above_sar = is_price_above_sar

    def CreateClone(self):
        return parabolic_sar_trend_strategy()
