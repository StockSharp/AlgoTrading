import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, KalmanFilter, CandleIndicatorValue, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class bollinger_kalman_filter_strategy(Strategy):
    """
    Bollinger Bands with Kalman Filter Strategy.
    Enters positions when price is at Bollinger extremes and confirmed by Kalman Filter trend direction.
    """

    def __init__(self):
        super(bollinger_kalman_filter_strategy, self).__init__()

        self._bollinger_length = self.Param("BollingerLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Length", "Length of the Bollinger Bands", "Bollinger Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Bollinger Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(1.5, 2.5, 0.5)

        self._kalman_q = self.Param("KalmanQ", 0.01) \
            .SetGreaterThanZero() \
            .SetDisplay("Kalman Q", "Process noise for Kalman Filter", "Kalman Filter Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(0.001, 0.1, 0.01)

        self._kalman_r = self.Param("KalmanR", 0.1) \
            .SetGreaterThanZero() \
            .SetDisplay("Kalman R", "Measurement noise for Kalman Filter", "Kalman Filter Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(0.01, 1.0, 0.1)

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(2))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 3) \
            .SetNotNegative() \
            .SetDisplay("Signal Cooldown", "Closed candles to wait before the next entry", "General")

        self._upper_band = 0.0
        self._lower_band = 0.0
        self._mid_band = 0.0
        self._kalman_value = 0.0
        self._previous_kalman_value = None
        self._cooldown_remaining = 0
        self._bollinger = None
        self._kalman_filter = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(bollinger_kalman_filter_strategy, self).OnReseted()
        self._upper_band = 0.0
        self._lower_band = 0.0
        self._mid_band = 0.0
        self._kalman_value = 0.0
        self._previous_kalman_value = None
        self._cooldown_remaining = 0
        self._bollinger = None
        self._kalman_filter = None

    def OnStarted(self, time):
        super(bollinger_kalman_filter_strategy, self).OnStarted(time)

        self._bollinger = BollingerBands()
        self._bollinger.Length = int(self._bollinger_length.Value)
        self._bollinger.Width = Decimal(self._bollinger_deviation.Value)

        self._kalman_filter = KalmanFilter()
        self._kalman_filter.ProcessNoise = Decimal(self._kalman_q.Value)
        self._kalman_filter.MeasurementNoise = Decimal(self._kalman_r.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.ProcessCandle).Start()

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._bollinger)
            self.DrawIndicator(area, self._kalman_filter)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        biv = CandleIndicatorValue(self._bollinger, candle)
        biv.IsFinal = True
        bollinger_val = self._bollinger.Process(biv)

        kiv = DecimalIndicatorValue(self._kalman_filter, candle.ClosePrice, candle.OpenTime)
        kiv.IsFinal = True
        kalman_val = self._kalman_filter.Process(kiv)

        if not bollinger_val.IsFinal or not kalman_val.IsFinal or not self._bollinger.IsFormed or not self._kalman_filter.IsFormed:
            return

        if bollinger_val.UpBand is None or bollinger_val.LowBand is None or bollinger_val.MovingAverage is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        upper_band = float(bollinger_val.UpBand)
        lower_band = float(bollinger_val.LowBand)
        mid_band = float(bollinger_val.MovingAverage)
        kalman_filter_value = float(kalman_val)

        kalman_trend_up = self._previous_kalman_value is not None and kalman_filter_value > self._previous_kalman_value
        kalman_trend_down = self._previous_kalman_value is not None and kalman_filter_value < self._previous_kalman_value

        self._upper_band = upper_band
        self._lower_band = lower_band
        self._mid_band = mid_band
        self._kalman_value = kalman_filter_value

        cooldown_bars = int(self._signal_cooldown_bars.Value)

        if self._cooldown_remaining == 0 and float(candle.LowPrice) <= lower_band and kalman_trend_up and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
            self._cooldown_remaining = cooldown_bars
        elif self._cooldown_remaining == 0 and float(candle.HighPrice) >= upper_band and kalman_trend_down and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self._cooldown_remaining = cooldown_bars
        elif self.Position > 0 and float(candle.ClosePrice) >= mid_band:
            self.SellMarket(self.Position)
        elif self.Position < 0 and float(candle.ClosePrice) <= mid_band:
            self.BuyMarket(Math.Abs(self.Position))

        self._previous_kalman_value = kalman_filter_value

    def CreateClone(self):
        return bollinger_kalman_filter_strategy()
