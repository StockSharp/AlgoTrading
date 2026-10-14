import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import KalmanFilter, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class kalman_filter_trend_strategy(Strategy):
    """
    Kalman Filter Trend strategy.
    A close above the Kalman filter estimate goes long and a close below it goes short, reversing an opposite position, so the position flips
    whenever price crosses the line. The stop lies AtrMultiplier times the AtrPeriod ATR from the entry close and is checked on candle closes.
    """

    def __init__(self):
        super(kalman_filter_trend_strategy, self).__init__()
        self._process_noise = self.Param("ProcessNoise", 0.01).SetGreaterThanZero().SetDisplay("Process Noise", "Process noise of the Kalman filter", "Kalman Filter")
        self._measurement_noise = self.Param("MeasurementNoise", 0.1).SetGreaterThanZero().SetDisplay("Measurement Noise", "Measurement noise of the Kalman filter", "Kalman Filter")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "Period of the stop ATR", "Risk")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetNotNegative().SetDisplay("ATR Multiplier", "Stop distance from the entry in ATRs", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._stop_price = Decimal(0)

    def OnReseted(self):
        super(kalman_filter_trend_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(kalman_filter_trend_strategy, self).OnStarted2(time)

        self._reset_state()

        kalman = KalmanFilter()
        kalman.ProcessNoise = Decimal(self._process_noise.Value)
        kalman.MeasurementNoise = Decimal(self._measurement_noise.Value)
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(kalman, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, kalman)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, atr)

    def _process_candle(self, candle, kalman_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not kalman_value.IsFormed or not atr_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        line = kalman_value.GetValue[Decimal](None)
        atr = atr_value.GetValue[Decimal](None)
        close = candle.ClosePrice

        stop_atr = Decimal(self._atr_multiplier.Value)
        if close > line and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - stop_atr * atr
        elif close < line and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + stop_atr * atr
        elif self.Position > 0 and stop_atr > 0 and close <= self._stop_price:
            self.SellMarket(self.Position)
        elif self.Position < 0 and stop_atr > 0 and close >= self._stop_price:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return kalman_filter_trend_strategy()
