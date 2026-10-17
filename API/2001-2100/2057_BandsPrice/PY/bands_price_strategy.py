import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, OrderStates
from StockSharp.Algo.Indicators import BollingerBands, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class bands_price_strategy(Strategy):

    def __init__(self):
        super(bands_price_strategy, self).__init__()

        self._buy_open = self.Param("BuyOpen", True)
        self._sell_open = self.Param("SellOpen", True)
        self._buy_close = self.Param("BuyClose", True)
        self._sell_close = self.Param("SellClose", True)
        self._pending = None
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

    def OnStarted2(self, time):
        super(bands_price_strategy, self).OnStarted2(time)

        self._prev_color = -1
        self._prev_prev_color = -1
        self._pending = None
        self._smoother = SimpleMovingAverage()
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
        smooth_result = process_float(self._smoother, res, t, True)
        if not smooth_result.IsFormed:
            return

        jres = float(smooth_result)
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

        if self._prev_color != -1 and self.IsFormedAndOnlineAndAllowTrading() and (self._pending is None or self._pending.State in (OrderStates.Done, OrderStates.Failed)):
            long_entry = self._prev_color == 4 and color != 4
            short_entry = self._prev_color == 0 and color > 0
            if self.Position > 0 and self._buy_close.Value and color <= 2:
                self._pending = self.SellMarket(self.Position)
            elif self.Position < 0 and self._sell_close.Value and color >= 2:
                self._pending = self.BuyMarket(-self.Position)
            elif long_entry and self.Position <= 0:
                if self.Position < 0 and self._sell_close.Value:
                    self._pending = self.BuyMarket(-self.Position + (self.Volume if self._buy_open.Value else 0))
                elif self.Position == 0 and self._buy_open.Value:
                    self._pending = self.BuyMarket(self.Volume)
            elif short_entry and self.Position >= 0:
                if self.Position > 0 and self._buy_close.Value:
                    self._pending = self.SellMarket(self.Position + (self.Volume if self._sell_open.Value else 0))
                elif self.Position == 0 and self._sell_open.Value:
                    self._pending = self.SellMarket(self.Volume)

        self._prev_prev_color = self._prev_color
        self._prev_color = color

    def OnReseted(self):
        super(bands_price_strategy, self).OnReseted()
        self._smoother = None
        self._prev_color = -1
        self._prev_prev_color = -1

    def CreateClone(self):
        return bands_price_strategy()
