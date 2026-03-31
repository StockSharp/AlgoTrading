import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import LinearRegression, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class forecast_oscillator_strategy(Strategy):

    def __init__(self):
        super(forecast_oscillator_strategy, self).__init__()

        self._length = self.Param("Length", 15) \
            .SetDisplay("Length", "Regression length", "Indicators")
        self._t3_period = self.Param("T3Period", 3) \
            .SetDisplay("T3 Period", "T3 smoothing period", "Indicators")
        self._b_factor = self.Param("BFactor", 0.7) \
            .SetDisplay("T3 Factor", "T3 smoothing factor", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._lin_reg = None
        self._b2 = 0.0
        self._b3 = 0.0
        self._c1 = 0.0
        self._c2 = 0.0
        self._c3 = 0.0
        self._c4 = 0.0
        self._w1 = 0.0
        self._w2 = 0.0
        self._e1 = 0.0
        self._e2 = 0.0
        self._e3 = 0.0
        self._e4 = 0.0
        self._e5 = 0.0
        self._e6 = 0.0
        self._forecast_prev1 = None
        self._forecast_prev2 = None
        self._sig_prev1 = None
        self._sig_prev2 = None
        self._sig_prev3 = None

    @property
    def Length(self):
        return self._length.Value

    @Length.setter
    def Length(self, value):
        self._length.Value = value

    @property
    def T3Period(self):
        return self._t3_period.Value

    @T3Period.setter
    def T3Period(self, value):
        self._t3_period.Value = value

    @property
    def BFactor(self):
        return self._b_factor.Value

    @BFactor.setter
    def BFactor(self, value):
        self._b_factor.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(forecast_oscillator_strategy, self).OnStarted2(time)

        self._lin_reg = LinearRegression()
        self._lin_reg.Length = self.Length

        b = float(self.BFactor)
        self._b2 = b * b
        self._b3 = self._b2 * b
        self._c1 = -self._b3
        self._c2 = 3.0 * (self._b2 + self._b3)
        self._c3 = -3.0 * (2.0 * self._b2 + b + self._b3)
        self._c4 = 1.0 + 3.0 * b + self._b3 + 3.0 * self._b2

        n = 1.0 + 0.5 * (float(self.T3Period) - 1.0)
        self._w1 = 2.0 / (n + 1.0)
        self._w2 = 1.0 - self._w1

        self._e1 = self._e2 = self._e3 = self._e4 = self._e5 = self._e6 = 0.0
        self._forecast_prev1 = None
        self._forecast_prev2 = None
        self._sig_prev1 = None
        self._sig_prev2 = None
        self._sig_prev3 = None

        self.SubscribeCandles(self.CandleType) \
            .Bind(self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)
        t = candle.OpenTime

        lr_input = DecimalIndicatorValue(self._lin_reg, price, t)
        lr_input.IsFinal = True
        lr_result = self._lin_reg.Process(lr_input)
        if not lr_result.IsFormed:
            return

        lr_val = lr_result.LinearReg
        if lr_val is None:
            return

        reg_value = float(lr_val)
        if reg_value == 0:
            return

        forecast = (price - reg_value) / reg_value * 100.0

        self._e1 = self._w1 * forecast + self._w2 * self._e1
        self._e2 = self._w1 * self._e1 + self._w2 * self._e2
        self._e3 = self._w1 * self._e2 + self._w2 * self._e3
        self._e4 = self._w1 * self._e3 + self._w2 * self._e4
        self._e5 = self._w1 * self._e4 + self._w2 * self._e5
        self._e6 = self._w1 * self._e5 + self._w2 * self._e6
        t3 = self._c1 * self._e6 + self._c2 * self._e5 + self._c3 * self._e4 + self._c4 * self._e3

        if self._forecast_prev1 is not None and self._forecast_prev2 is not None and \
           self._sig_prev1 is not None and self._sig_prev2 is not None and self._sig_prev3 is not None:

            buy_signal = self._forecast_prev1 > self._sig_prev2 and self._forecast_prev2 <= self._sig_prev3 and self._sig_prev1 < 0
            sell_signal = self._forecast_prev1 < self._sig_prev2 and self._forecast_prev2 >= self._sig_prev3 and self._sig_prev1 > 0

            if buy_signal and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif sell_signal and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

        self._forecast_prev2 = self._forecast_prev1
        self._forecast_prev1 = forecast
        self._sig_prev3 = self._sig_prev2
        self._sig_prev2 = self._sig_prev1
        self._sig_prev1 = t3

    def OnReseted(self):
        super(forecast_oscillator_strategy, self).OnReseted()
        self._lin_reg = None
        self._b2 = 0.0
        self._b3 = 0.0
        self._c1 = 0.0
        self._c2 = 0.0
        self._c3 = 0.0
        self._c4 = 0.0
        self._w1 = 0.0
        self._w2 = 0.0
        self._e1 = 0.0
        self._e2 = 0.0
        self._e3 = 0.0
        self._e4 = 0.0
        self._e5 = 0.0
        self._e6 = 0.0
        self._forecast_prev1 = None
        self._forecast_prev2 = None
        self._sig_prev1 = None
        self._sig_prev2 = None
        self._sig_prev3 = None

    def CreateClone(self):
        return forecast_oscillator_strategy()
