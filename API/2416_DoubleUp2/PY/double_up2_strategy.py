import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (
    RelativeStrengthIndex,
    MovingAverageConvergenceDivergence,
    ExponentialMovingAverage,
)
from StockSharp.Algo.Strategies import Strategy


class double_up2_strategy(Strategy):
    def __init__(self):
        super(double_up2_strategy, self).__init__()

        self._cci_period = self.Param("CciPeriod", 8)
        self._macd_fast_period = self.Param("MacdFastPeriod", 13)
        self._macd_slow_period = self.Param("MacdSlowPeriod", 33)
        self._threshold = self.Param("Threshold", 70.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._entry_price = 0.0
        self._martingale_step = 0

    @property
    def CciPeriod(self):
        return self._cci_period.Value

    @CciPeriod.setter
    def CciPeriod(self, value):
        self._cci_period.Value = value

    @property
    def MacdFastPeriod(self):
        return self._macd_fast_period.Value

    @MacdFastPeriod.setter
    def MacdFastPeriod(self, value):
        self._macd_fast_period.Value = value

    @property
    def MacdSlowPeriod(self):
        return self._macd_slow_period.Value

    @MacdSlowPeriod.setter
    def MacdSlowPeriod(self, value):
        self._macd_slow_period.Value = value

    @property
    def Threshold(self):
        return self._threshold.Value

    @Threshold.setter
    def Threshold(self, value):
        self._threshold.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(double_up2_strategy, self).OnStarted(time)

        self._entry_price = 0.0
        self._martingale_step = 0

        rsi = RelativeStrengthIndex()
        rsi.Length = self.CciPeriod

        macd = MovingAverageConvergenceDivergence(
            ExponentialMovingAverage.Create(self.MacdSlowPeriod),
            ExponentialMovingAverage.Create(self.MacdFastPeriod))

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, macd, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, rsi_value, macd_value):
        if candle.State != CandleStates.Finished:
            return

        rsi_val = float(rsi_value)
        macd_val = float(macd_value)
        close = float(candle.ClosePrice)
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        threshold = float(self.Threshold)
        low_threshold = 100.0 - threshold

        if rsi_val > threshold and macd_val > 0.0:
            if self.Position > 0:
                profit = close - self._entry_price
                if profit > 0.0:
                    self._martingale_step = 0
                else:
                    self._martingale_step += 1
            self.SellMarket()
            self._entry_price = close
            return

        if rsi_val < low_threshold and macd_val < 0.0:
            if self.Position < 0:
                profit = self._entry_price - close
                if profit > 0.0:
                    self._martingale_step = 0
                else:
                    self._martingale_step += 1
            self.BuyMarket()
            self._entry_price = close
            return

        if self.Position > 0 and close - self._entry_price > 120.0 * step:
            self.SellMarket()
            self._martingale_step += 2
            return

        if self.Position < 0 and self._entry_price - close > 120.0 * step:
            self.BuyMarket()
            self._martingale_step += 2

    def OnReseted(self):
        super(double_up2_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._martingale_step = 0

    def CreateClone(self):
        return double_up2_strategy()
