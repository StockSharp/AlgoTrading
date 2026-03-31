import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class cryptos_strategy(Strategy):
    def __init__(self):
        super(cryptos_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._wma_period = self.Param("WmaPeriod", 55)
        self._bollinger_period = self.Param("BollingerPeriod", 20)
        self._bollinger_width = self.Param("BollingerWidth", 2.0)

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def WmaPeriod(self):
        return self._wma_period.Value

    @WmaPeriod.setter
    def WmaPeriod(self, value):
        self._wma_period.Value = value

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

    def OnReseted(self):
        super(cryptos_strategy, self).OnReseted()

    def OnStarted2(self, time):
        super(cryptos_strategy, self).OnStarted2(time)

        band_ema = ExponentialMovingAverage()
        band_ema.Length = self.BollingerPeriod
        trend_ema = ExponentialMovingAverage()
        trend_ema.Length = self.WmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(band_ema, trend_ema, self._process_candle).Start()

    def _process_candle(self, candle, band_value, trend_value):
        if candle.State != CandleStates.Finished:
            return

        band_val = float(band_value)
        trend_val = float(trend_value)
        close = float(candle.ClosePrice)
        band_offset = band_val * (float(self.BollingerWidth) / 100.0)
        upper_band = band_val + band_offset
        lower_band = band_val - band_offset

        # Buy: price below WMA, touches lower band
        if close < trend_val and close <= lower_band:
            if self.Position <= 0:
                self.BuyMarket()
        # Sell: price above WMA, touches upper band
        elif close > trend_val and close >= upper_band:
            if self.Position >= 0:
                self.SellMarket()

        # Exit at opposite band
        if self.Position > 0 and close >= upper_band:
            self.SellMarket()
        elif self.Position < 0 and close <= lower_band:
            self.BuyMarket()

    def CreateClone(self):
        return cryptos_strategy()
