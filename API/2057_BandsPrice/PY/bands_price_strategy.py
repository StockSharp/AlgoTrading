import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, ExponentialMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class bands_price_strategy(Strategy):

    def __init__(self):
        super(bands_price_strategy, self).__init__()

        self._bands_period = self.Param("BandsPeriod", 100) \
            .SetDisplay("Bands Period", "Bollinger Bands period", "Indicator")
        self._bands_deviation = self.Param("BandsDeviation", 2.0) \
            .SetDisplay("Bands Deviation", "Width of Bollinger Bands", "Indicator")
        self._smooth = self.Param("Smooth", 5) \
            .SetDisplay("Smoothing", "Length of smoothing EMA", "Indicator")
        self._up_level = self.Param("UpLevel", 25) \
            .SetDisplay("Upper Level", "Threshold for overbought zone", "Indicator")
        self._dn_level = self.Param("DnLevel", -25) \
            .SetDisplay("Lower Level", "Threshold for oversold zone", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe for analysis", "General")

        self._smoother = None
        self._prev_color = -1
        self._prev_prev_color = -1

    @property
    def BandsPeriod(self):
        return self._bands_period.Value

    @BandsPeriod.setter
    def BandsPeriod(self, value):
        self._bands_period.Value = value

    @property
    def BandsDeviation(self):
        return self._bands_deviation.Value

    @BandsDeviation.setter
    def BandsDeviation(self, value):
        self._bands_deviation.Value = value

    @property
    def Smooth(self):
        return self._smooth.Value

    @Smooth.setter
    def Smooth(self, value):
        self._smooth.Value = value

    @property
    def UpLevel(self):
        return self._up_level.Value

    @UpLevel.setter
    def UpLevel(self, value):
        self._up_level.Value = value

    @property
    def DnLevel(self):
        return self._dn_level.Value

    @DnLevel.setter
    def DnLevel(self, value):
        self._dn_level.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(bands_price_strategy, self).OnStarted(time)

        self._prev_color = -1
        self._prev_prev_color = -1
        self._smoother = ExponentialMovingAverage()
        self._smoother.Length = self.Smooth

        bands = BollingerBands()
        bands.Length = self.BandsPeriod
        bands.Width = self.BandsDeviation

        self.SubscribeCandles(self.CandleType) \
            .BindEx(bands, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return

        if not bb_value.IsFormed:
            return

        upper = bb_value.UpBand
        lower = bb_value.LowBand

        if upper is None or lower is None:
            return

        upper_f = float(upper)
        lower_f = float(lower)
        width = upper_f - lower_f

        if width == 0:
            return

        close = float(candle.ClosePrice)
        res = 100.0 * (close - lower_f) / width - 50.0

        t = candle.OpenTime
        smooth_result = self._smoother.Process(DecimalIndicatorValue(self._smoother, res, t, True))
        if not smooth_result.IsFormed:
            return

        jres = float(smooth_result.ToDecimal())
        up = float(self.UpLevel)
        dn = float(self.DnLevel)

        if jres > up:
            color = 4
        elif jres > 0:
            color = 3
        elif jres < dn:
            color = 0
        elif jres < 0:
            color = 1
        else:
            color = 2

        if self._prev_prev_color != -1 and self._prev_color != -1:
            if self._prev_prev_color == 4 and self._prev_color < 4 and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif self._prev_prev_color == 0 and self._prev_color > 0 and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

        self._prev_prev_color = self._prev_color
        self._prev_color = color

    def OnReseted(self):
        super(bands_price_strategy, self).OnReseted()
        self._smoother = None
        self._prev_color = -1
        self._prev_prev_color = -1

    def CreateClone(self):
        return bands_price_strategy()
