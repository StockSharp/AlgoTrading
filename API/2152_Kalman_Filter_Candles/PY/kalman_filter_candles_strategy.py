import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import KalmanFilter, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class kalman_filter_candles_strategy(Strategy):
    def __init__(self):
        super(kalman_filter_candles_strategy, self).__init__()
        self._process_noise = self.Param("ProcessNoise", 1.0) \
            .SetDisplay("Process Noise", "Kalman filter smoothing factor", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for candles", "Common")
        self._open_filter = None
        self._close_filter = None
        self._prev_color = 1
        self._has_prev = False

    @property
    def process_noise(self):
        return self._process_noise.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(kalman_filter_candles_strategy, self).OnReseted()
        self._prev_color = 1
        self._has_prev = False
        self._open_filter = None
        self._close_filter = None

    def OnStarted(self, time):
        super(kalman_filter_candles_strategy, self).OnStarted(time)
        self._open_filter = KalmanFilter()
        self._open_filter.ProcessNoise = self.process_noise
        self._open_filter.MeasurementNoise = self.process_noise
        self._close_filter = KalmanFilter()
        self._close_filter.ProcessNoise = self.process_noise
        self._close_filter.MeasurementNoise = self.process_noise
        self.Indicators.Add(self._open_filter)
        self.Indicators.Add(self._close_filter)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        open_input = DecimalIndicatorValue(self._open_filter, candle.OpenPrice, candle.OpenTime)
        open_input.IsFinal = True
        close_input = DecimalIndicatorValue(self._close_filter, candle.ClosePrice, candle.OpenTime)
        close_input.IsFinal = True
        open_res = self._open_filter.Process(open_input)
        close_res = self._close_filter.Process(close_input)
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        open_val = float(open_res)
        close_val = float(close_res)
        if open_val < close_val:
            color = 2
        elif open_val > close_val:
            color = 0
        else:
            color = 1
        if self._has_prev:
            if color == 2 and self._prev_color != 2:
                if self.Position < 0:
                    self.BuyMarket()
                if self.Position <= 0:
                    self.BuyMarket()
            elif color == 0 and self._prev_color != 0:
                if self.Position > 0:
                    self.SellMarket()
                if self.Position >= 0:
                    self.SellMarket()
        self._prev_color = color
        self._has_prev = True

    def CreateClone(self):
        return kalman_filter_candles_strategy()
