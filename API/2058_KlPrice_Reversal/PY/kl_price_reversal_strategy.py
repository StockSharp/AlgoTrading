import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class kl_price_reversal_strategy(Strategy):

    def __init__(self):
        super(kl_price_reversal_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Time frame for calculations", "General")
        self._price_ma_length = self.Param("PriceMaLength", 100) \
            .SetDisplay("Price MA Length", "EMA period for price smoothing", "Parameters")
        self._atr_length = self.Param("AtrLength", 20) \
            .SetDisplay("ATR Length", "ATR period for range estimation", "Parameters")
        self._up_level = self.Param("UpLevel", 50.0) \
            .SetDisplay("Upper Level", "Upper threshold for signals", "Parameters")
        self._down_level = self.Param("DownLevel", -50.0) \
            .SetDisplay("Lower Level", "Lower threshold for signals", "Parameters")

        self._prev_color = 2.0
        self._is_first = True

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def PriceMaLength(self):
        return self._price_ma_length.Value

    @PriceMaLength.setter
    def PriceMaLength(self, value):
        self._price_ma_length.Value = value

    @property
    def AtrLength(self):
        return self._atr_length.Value

    @AtrLength.setter
    def AtrLength(self, value):
        self._atr_length.Value = value

    @property
    def UpLevel(self):
        return self._up_level.Value

    @UpLevel.setter
    def UpLevel(self, value):
        self._up_level.Value = value

    @property
    def DownLevel(self):
        return self._down_level.Value

    @DownLevel.setter
    def DownLevel(self, value):
        self._down_level.Value = value

    def OnStarted(self, time):
        super(kl_price_reversal_strategy, self).OnStarted(time)

        self._is_first = True
        self._prev_color = 2.0

        price_ma = ExponentialMovingAverage()
        price_ma.Length = self.PriceMaLength
        atr = AverageTrueRange()
        atr.Length = self.AtrLength

        self.SubscribeCandles(self.CandleType) \
            .BindEx(price_ma, atr, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, ma_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not ma_value.IsFormed or not atr_value.IsFormed:
            return

        ma = float(ma_value)
        tr = float(atr_value)
        if tr == 0:
            return

        close = float(candle.ClosePrice)
        dwband = ma - tr
        jres = 100.0 * (close - dwband) / (2.0 * tr) - 50.0

        up = float(self.UpLevel)
        dn = float(self.DownLevel)

        color = 2.0
        if jres > up:
            color = 4.0
        elif jres > 0:
            color = 3.0

        if jres < dn:
            color = 0.0
        elif jres < 0:
            color = 1.0

        if not self._is_first:
            if self._prev_color == 4.0 and color < 4.0 and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif self._prev_color == 0.0 and color > 0.0 and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

        self._prev_color = color
        self._is_first = False

    def OnReseted(self):
        super(kl_price_reversal_strategy, self).OnReseted()
        self._prev_color = 2.0
        self._is_first = True

    def CreateClone(self):
        return kl_price_reversal_strategy()
