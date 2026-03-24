import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, ParabolicSar
from StockSharp.Algo.Strategies import Strategy


class parabolic_sar_multi_timeframe_strategy(Strategy):
    def __init__(self):
        super(parabolic_sar_multi_timeframe_strategy, self).__init__()
        self._sar_acceleration = self.Param("SarAcceleration", 0.02) \
            .SetDisplay("SAR Accel", "SAR acceleration factor", "Indicators")
        self._sar_max_acceleration = self.Param("SarMaxAcceleration", 0.2) \
            .SetDisplay("SAR Max", "SAR max acceleration", "Indicators")
        self._ema_length = self.Param("EmaLength", 50) \
            .SetDisplay("EMA Length", "EMA trend filter period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle Type", "General")

    @property
    def sar_acceleration(self):
        return self._sar_acceleration.Value

    @property
    def sar_max_acceleration(self):
        return self._sar_max_acceleration.Value

    @property
    def ema_length(self):
        return self._ema_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(parabolic_sar_multi_timeframe_strategy, self).OnStarted(time)
        sar = ParabolicSar()
        sar.Acceleration = self.sar_acceleration
        sar.AccelerationMax = self.sar_max_acceleration
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sar, ema, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sar)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def on_process(self, candle, sar_value, ema_value):
        if candle.State != CandleStates.Finished:
            return
        price = candle.ClosePrice
        # Buy when price is above both SAR and EMA
        if price > sar_value and price > ema_value and self.Position <= 0:
            self.BuyMarket()
        # Sell when price is below both SAR and EMA
        elif price < sar_value and price < ema_value and self.Position >= 0:
            self.SellMarket()
        # Exit on SAR flip
        if self.Position > 0 and price < sar_value:
            self.SellMarket()
        elif self.Position < 0 and price > sar_value:
            self.BuyMarket()

    def CreateClone(self):
        return parabolic_sar_multi_timeframe_strategy()
