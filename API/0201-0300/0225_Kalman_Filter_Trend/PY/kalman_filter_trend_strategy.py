import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import KalmanFilter, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class kalman_filter_trend_strategy(Strategy):
    """
    Kalman Filter trend: trades based on price position relative to Kalman filter.
    """

    def __init__(self):
        super(kalman_filter_trend_strategy, self).__init__()
        self._process_noise = self.Param("ProcessNoise", 0.01).SetDisplay("Process Noise", "Kalman process noise", "Parameters")
        self._measurement_noise = self.Param("MeasurementNoise", 0.1).SetDisplay("Measurement Noise", "Kalman measurement noise", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(kalman_filter_trend_strategy, self).OnReseted()

    def OnStarted2(self, time):
        super(kalman_filter_trend_strategy, self).OnStarted2(time)
        kf = KalmanFilter()
        kf.ProcessNoise = self._process_noise.Value
        kf.MeasurementNoise = self._measurement_noise.Value
        atr = AverageTrueRange()
        atr.Length = 14
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(kf, atr, self._process_candle).Start()
        self.StartProtection(None, Unit(2, UnitTypes.Absolute))
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, kf)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, kf_val, atr_val):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        kf = float(kf_val)
        if close > kf and self.Position <= 0:
            self.BuyMarket()
        elif close < kf and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return kalman_filter_trend_strategy()
