import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class aussie_surfer_ltd_strategy(Strategy):
    def __init__(self):
        super(aussie_surfer_ltd_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(120)))
        self._bollinger_period = self.Param("BollingerPeriod", 20)
        self._bollinger_width = self.Param("BollingerWidth", 2.5)
        self._sma_period = self.Param("SmaPeriod", 21)

        self._prev_sma = None
        self._prev_close = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def BollingerPeriod(self):
        return self._bollinger_period.Value

    @BollingerPeriod.setter
    def BollingerPeriod(self, value):
        self._bollinger_period.Value = value

    @property
    def BollingerWidth(self):
        return self._bollinger_width.Value

    @BollingerWidth.setter
    def BollingerWidth(self, value):
        self._bollinger_width.Value = value

    @property
    def SmaPeriod(self):
        return self._sma_period.Value

    @SmaPeriod.setter
    def SmaPeriod(self, value):
        self._sma_period.Value = value

    def OnReseted(self):
        super(aussie_surfer_ltd_strategy, self).OnReseted()
        self._prev_sma = None
        self._prev_close = None

    def OnStarted2(self, time):
        super(aussie_surfer_ltd_strategy, self).OnStarted2(time)
        self._prev_sma = None
        self._prev_close = None

        band_ema = ExponentialMovingAverage()
        band_ema.Length = self.BollingerPeriod
        slope_ema = ExponentialMovingAverage()
        slope_ema.Length = self.SmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(band_ema, slope_ema, self._process_candle).Start()

    def _process_candle(self, candle, band_value, sma_value):
        if candle.State != CandleStates.Finished:
            return

        band_val = float(band_value)
        sma_val = float(sma_value)
        close = float(candle.ClosePrice)
        band_offset = band_val * (float(self.BollingerWidth) / 100.0)
        upper_band = band_val + band_offset
        lower_band = band_val - band_offset

        if self._prev_sma is None or self._prev_close is None:
            self._prev_sma = sma_val
            self._prev_close = close
            return

        sma_rising = sma_val > self._prev_sma
        sma_falling = sma_val < self._prev_sma

        # Long: price was below lower band and crosses back above, SMA falling (reversal)
        long_signal = self._prev_close < lower_band and close >= lower_band and sma_falling
        # Short: price was above upper band and crosses back below, SMA rising (reversal)
        short_signal = self._prev_close > upper_band and close <= upper_band and sma_rising

        if long_signal:
            if self.Position <= 0:
                self.BuyMarket()
        elif short_signal:
            if self.Position >= 0:
                self.SellMarket()

        # Exit at opposite band or SMA reversal
        if self.Position > 0 and (close >= upper_band or (sma_falling and self._prev_sma > sma_val)):
            self.SellMarket()
        elif self.Position < 0 and (close <= lower_band or (sma_rising and self._prev_sma < sma_val)):
            self.BuyMarket()

        self._prev_sma = sma_val
        self._prev_close = close

    def CreateClone(self):
        return aussie_surfer_ltd_strategy()
