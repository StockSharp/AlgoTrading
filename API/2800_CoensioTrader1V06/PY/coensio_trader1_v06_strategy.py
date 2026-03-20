import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from StockSharp.Algo.Indicators import BollingerBands, DoubleExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import DataType, CandleStates
from System import TimeSpan


class coensio_trader1_v06_strategy(Strategy):
    def __init__(self):
        super(coensio_trader1_v06_strategy, self).__init__()

        self._bollinger_period = self.Param("BollingerPeriod", 30)
        self._bollinger_deviation = self.Param("BollingerDeviation", 1.5)
        self._dema_period = self.Param("DemaPeriod", 20)
        self._close_on_signal = self.Param("CloseOnSignal", False)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._prev_open = None
        self._prev_high = None
        self._prev_low = None
        self._prev_close = None
        self._prev_upper_band = None
        self._prev_lower_band = None
        self._prev_dema = None
        self._bollinger = None
        self._dema = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(coensio_trader1_v06_strategy, self).OnStarted(time)

        self._bollinger = BollingerBands()
        self._bollinger.Length = self._bollinger_period.Value
        self._bollinger.Width = self._bollinger_deviation.Value

        self._dema = DoubleExponentialMovingAverage()
        self._dema.Length = self._dema_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        bb_result = self._bollinger.Process(candle)
        dema_result = self._dema.Process(candle)

        if bb_result.IsEmpty or dema_result.IsEmpty or not self._bollinger.IsFormed or not self._dema.IsFormed:
            self._prev_open = float(candle.OpenPrice)
            self._prev_close = float(candle.ClosePrice)
            self._prev_low = float(candle.LowPrice)
            self._prev_high = float(candle.HighPrice)
            return

        upper = float(bb_result.UpBand) if bb_result.UpBand is not None else 0.0
        lower = float(bb_result.LowBand) if bb_result.LowBand is not None else 0.0
        dema_value = float(dema_result)

        if (self._prev_open is not None and self._prev_close is not None and
                self._prev_low is not None and self._prev_high is not None and
                self._prev_lower_band is not None and self._prev_upper_band is not None and
                self._prev_dema is not None):

            crossed_lower = self._prev_low <= self._prev_lower_band and self._prev_close > self._prev_lower_band
            bullish_trend = dema_value > self._prev_dema

            if crossed_lower and bullish_trend:
                if self._close_on_signal.Value and self.Position < 0:
                    self.BuyMarket(abs(self.Position))
                if self.Position <= 0:
                    self.BuyMarket()

            crossed_upper = self._prev_high >= self._prev_upper_band and self._prev_close < self._prev_upper_band
            bearish_trend = dema_value < self._prev_dema

            if crossed_upper and bearish_trend:
                if self._close_on_signal.Value and self.Position > 0:
                    self.SellMarket(self.Position)
                if self.Position >= 0:
                    self.SellMarket()

        self._prev_lower_band = lower
        self._prev_upper_band = upper
        self._prev_dema = dema_value
        self._prev_open = float(candle.OpenPrice)
        self._prev_close = float(candle.ClosePrice)
        self._prev_low = float(candle.LowPrice)
        self._prev_high = float(candle.HighPrice)

    def OnReseted(self):
        super(coensio_trader1_v06_strategy, self).OnReseted()
        self._prev_open = None
        self._prev_high = None
        self._prev_low = None
        self._prev_close = None
        self._prev_upper_band = None
        self._prev_lower_band = None
        self._prev_dema = None

    def CreateClone(self):
        return coensio_trader1_v06_strategy()
