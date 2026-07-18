import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class reverse_strategy(Strategy):
    def __init__(self):
        super(reverse_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        self._bollinger_period = self.Param("BollingerPeriod", 20)
        self._bollinger_width = self.Param("BollingerWidth", 1.0)
        self._rsi_period = self.Param("RsiPeriod", 14)
        self._rsi_overbought = self.Param("RsiOverbought", 70.0)
        self._rsi_oversold = self.Param("RsiOversold", 30.0)

        self._prev_close = 0.0
        self._prev_rsi = 0.0
        self._prev_lower = 0.0
        self._prev_upper = 0.0
        self._has_prev = False

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
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def RsiOverbought(self):
        return self._rsi_overbought.Value

    @RsiOverbought.setter
    def RsiOverbought(self, value):
        self._rsi_overbought.Value = value

    @property
    def RsiOversold(self):
        return self._rsi_oversold.Value

    @RsiOversold.setter
    def RsiOversold(self, value):
        self._rsi_oversold.Value = value

    def OnReseted(self):
        super(reverse_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_rsi = 0.0
        self._prev_lower = 0.0
        self._prev_upper = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(reverse_strategy, self).OnStarted2(time)
        self._prev_close = 0.0
        self._prev_rsi = 0.0
        self._prev_lower = 0.0
        self._prev_upper = 0.0
        self._has_prev = False

        ema = ExponentialMovingAverage()
        ema.Length = self.BollingerPeriod
        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, rsi, self._process_candle).Start()

    def _process_candle(self, candle, ema_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        ema_val = float(ema_value)
        rsi_val = float(rsi_value)
        close = float(candle.ClosePrice)
        band_offset = ema_val * (float(self.BollingerWidth) / 100.0)
        upper_band = ema_val + band_offset
        lower_band = ema_val - band_offset

        if not self._has_prev:
            self._prev_close = close
            self._prev_rsi = rsi_val
            self._prev_lower = lower_band
            self._prev_upper = upper_band
            self._has_prev = True
            return

        # Long: price crosses up from below lower band + RSI was oversold
        long_signal = (self._prev_close < self._prev_lower and close >= lower_band and
                       self._prev_rsi < float(self.RsiOversold))
        # Short: price crosses down from above upper band + RSI was overbought
        short_signal = (self._prev_close > self._prev_upper and close <= upper_band and
                        self._prev_rsi > float(self.RsiOverbought))

        if long_signal:
            if self.Position <= 0:
                self.BuyMarket()
        elif short_signal:
            if self.Position >= 0:
                self.SellMarket()

        # Exit long at upper band
        if self.Position > 0 and close >= upper_band:
            self.SellMarket()

        # Exit short at lower band
        if self.Position < 0 and close <= lower_band:
            self.BuyMarket()

        self._prev_close = close
        self._prev_rsi = rsi_val
        self._prev_lower = lower_band
        self._prev_upper = upper_band

    def CreateClone(self):
        return reverse_strategy()
