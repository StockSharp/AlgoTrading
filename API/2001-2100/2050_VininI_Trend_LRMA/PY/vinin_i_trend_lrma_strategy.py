import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import LinearReg
from StockSharp.Algo.Strategies import Strategy


class vinin_i_trend_lrma_strategy(Strategy):
    """LRMA percent-deviation breakdown or LRMA-slope twist, with no stop."""

    BREAKDOWN = 0
    TWIST = 1

    def __init__(self):
        super(vinin_i_trend_lrma_strategy, self).__init__()
        self._period = self.Param("Period", 13).SetDisplay("LRMA Period", "Linear regression lookback", "Indicators")
        self._up_level = self.Param("UpLevel", 10.0).SetDisplay("Upper Level", "Percent deviation trigger", "Signals")
        self._dn_level = self.Param("DnLevel", -10.0).SetDisplay("Lower Level", "Percent deviation trigger", "Signals")
        self._mode = self.Param("Mode", self.BREAKDOWN).SetDisplay("Signal Mode", "0=Breakdown, 1=Twist", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Type of candles", "General")
        self._reset_signals()

    @property
    def Period(self):
        return self._period.Value

    @Period.setter
    def Period(self, value):
        self._period.Value = value

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
    def Mode(self):
        return self._mode.Value

    @Mode.setter
    def Mode(self, value):
        self._mode.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _reset_signals(self):
        self._previous_oscillator = None
        self._previous_lrma = None
        self._previous_slope = None

    def OnReseted(self):
        super(vinin_i_trend_lrma_strategy, self).OnReseted()
        self._reset_signals()

    def OnStarted2(self, time):
        super(vinin_i_trend_lrma_strategy, self).OnStarted2(time)
        self._reset_signals()
        lrma = LinearReg()
        lrma.Length = self.Period
        self.SubscribeCandles(self.CandleType).Bind(lrma, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, lrma_value):
        if candle.State != CandleStates.Finished:
            return
        lrma = float(lrma_value)
        if lrma == 0:
            return

        oscillator = (float(candle.ClosePrice) - lrma) / lrma * 100.0
        slope = lrma - self._previous_lrma if self._previous_lrma is not None else None
        buy = sell = False
        mode = int(self.Mode)
        if mode == self.BREAKDOWN:
            if self._previous_oscillator is not None:
                buy = self._previous_oscillator <= float(self.UpLevel) < oscillator
                sell = self._previous_oscillator >= float(self.DnLevel) > oscillator
        elif mode == self.TWIST:
            if self._previous_slope is not None and slope is not None:
                buy = self._previous_slope < 0 < slope
                sell = self._previous_slope > 0 > slope
        else:
            raise ValueError("Unsupported LRMA signal mode: %s" % mode)

        self._previous_oscillator = oscillator
        self._previous_lrma = lrma
        self._previous_slope = slope

        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        position = self.Position
        if buy and position <= 0:
            if position < 0:
                self.BuyMarket(-position)
            self.BuyMarket(self.Volume)
        elif sell and position >= 0:
            if position > 0:
                self.SellMarket(position)
            self.SellMarket(self.Volume)

    def CreateClone(self):
        return vinin_i_trend_lrma_strategy()
