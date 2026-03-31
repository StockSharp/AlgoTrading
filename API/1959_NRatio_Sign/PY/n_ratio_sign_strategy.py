import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import DecimalIndicatorValue, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


# Mode constants
MODE_IN = 0
MODE_OUT = 1


class n_ratio_sign_strategy(Strategy):

    def __init__(self):
        super(n_ratio_sign_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for indicator calculation", "General")
        self._kf = self.Param("Kf", 1.0) \
            .SetDisplay("Kf", "NRTR coefficient", "Indicator")
        self._length = self.Param("Length", 3) \
            .SetDisplay("Length", "EMA smoothing length", "Indicator")
        self._fast = self.Param("Fast", 2.0) \
            .SetDisplay("Fast", "Fast parameter", "Indicator")
        self._sharp = self.Param("Sharp", 2.0) \
            .SetDisplay("Sharp", "Exponent for oscillator", "Indicator")
        self._up_level = self.Param("UpLevel", 80.0) \
            .SetDisplay("Up Level", "Upper NRatio threshold", "Indicator")
        self._down_level = self.Param("DownLevel", 20.0) \
            .SetDisplay("Down Level", "Lower NRatio threshold", "Indicator")
        self._mode = self.Param("Mode", MODE_IN) \
            .SetDisplay("Mode", "Signal generation mode", "Indicator")
        self._take_profit = self.Param("TakeProfitPercent", 2.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk Management")
        self._stop_loss = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")

        self._nrtr = 0.0
        self._nratio_prev = 50.0
        self._trend = 1
        self._is_initialized = False
        self._ema = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def Kf(self):
        return self._kf.Value

    @Kf.setter
    def Kf(self, value):
        self._kf.Value = value

    @property
    def Length(self):
        return self._length.Value

    @Length.setter
    def Length(self, value):
        self._length.Value = value

    @property
    def Fast(self):
        return self._fast.Value

    @Fast.setter
    def Fast(self, value):
        self._fast.Value = value

    @property
    def Sharp(self):
        return self._sharp.Value

    @Sharp.setter
    def Sharp(self, value):
        self._sharp.Value = value

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

    @property
    def Mode(self):
        return self._mode.Value

    @Mode.setter
    def Mode(self, value):
        self._mode.Value = value

    @property
    def TakeProfitPercent(self):
        return self._take_profit.Value

    @TakeProfitPercent.setter
    def TakeProfitPercent(self, value):
        self._take_profit.Value = value

    @property
    def StopLossPercent(self):
        return self._stop_loss.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss.Value = value

    def OnStarted2(self, time):
        super(n_ratio_sign_strategy, self).OnStarted2(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.Length

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        self.StartProtection(
            takeProfit=Unit(self.TakeProfitPercent, UnitTypes.Percent),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent))

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsOnline:
            return

        price = float(candle.ClosePrice)
        kf = float(self.Kf)

        if not self._is_initialized:
            if float(candle.ClosePrice) >= float(candle.OpenPrice):
                self._trend = 1
            else:
                self._trend = -1
            if self._trend > 0:
                self._nrtr = price * (1.0 - kf / 100.0)
            else:
                self._nrtr = price * (1.0 + kf / 100.0)
            self._nratio_prev = 50.0
            self._is_initialized = True
            return

        nrtr0 = self._nrtr
        trend0 = self._trend

        if self._trend >= 0:
            if price < self._nrtr:
                trend0 = -1
                nrtr0 = price * (1.0 + kf / 100.0)
            else:
                trend0 = 1
                l_price = price * (1.0 - kf / 100.0)
                nrtr0 = max(l_price, self._nrtr)
        else:
            if price > self._nrtr:
                trend0 = 1
                nrtr0 = price * (1.0 - kf / 100.0)
            else:
                trend0 = -1
                h_price = price * (1.0 + kf / 100.0)
                nrtr0 = min(h_price, self._nrtr)

        oscil = (100.0 * abs(price - nrtr0) / price) / kf
        ei = DecimalIndicatorValue(self._ema, oscil, candle.OpenTime)
        ei.IsFinal = True
        x_oscil_value = self._ema.Process(ei)
        x_oscil = oscil if x_oscil_value.IsEmpty else float(x_oscil_value)

        if not self._ema.IsFormed:
            self._nrtr = nrtr0
            self._trend = trend0
            return

        nratio = 100.0 * (x_oscil ** float(self.Sharp))

        buy_signal = False
        sell_signal = False

        if self.Mode == MODE_IN:
            if nratio > float(self.UpLevel) and self._nratio_prev <= float(self.UpLevel):
                buy_signal = True
            if nratio < float(self.DownLevel) and self._nratio_prev >= float(self.DownLevel):
                sell_signal = True
        else:
            if nratio < float(self.UpLevel) and self._nratio_prev >= float(self.UpLevel):
                sell_signal = True
            if nratio > float(self.DownLevel) and self._nratio_prev <= float(self.DownLevel):
                buy_signal = True

        self._nrtr = nrtr0
        self._trend = trend0
        self._nratio_prev = nratio

        if buy_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif sell_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def OnReseted(self):
        super(n_ratio_sign_strategy, self).OnReseted()
        self._is_initialized = False
        self._nrtr = 0.0
        self._nratio_prev = 50.0
        self._trend = 1
        if self._ema is not None:
            self._ema.Length = self.Length
            self._ema.Reset()

    def CreateClone(self):
        return n_ratio_sign_strategy()
