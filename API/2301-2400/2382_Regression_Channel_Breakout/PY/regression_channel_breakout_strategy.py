import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import LinearReg, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class regression_channel_breakout_strategy(Strategy):
    def __init__(self):
        super(regression_channel_breakout_strategy, self).__init__()

        self._length = self.Param("Length", 250)
        self._deviation = self.Param("Deviation", 2.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._use_trailing = self.Param("UseTrailing", False)
        self._trailing_start = self.Param("TrailingStart", 30.0)
        self._trailing_step = self.Param("TrailingStep", 30.0)

        self._entry_price = 0.0
        self._long_stop = 0.0
        self._short_stop = 0.0

    @property
    def Length(self):
        return self._length.Value

    @Length.setter
    def Length(self, value):
        self._length.Value = value

    @property
    def Deviation(self):
        return self._deviation.Value

    @Deviation.setter
    def Deviation(self, value):
        self._deviation.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def UseTrailing(self):
        return self._use_trailing.Value

    @UseTrailing.setter
    def UseTrailing(self, value):
        self._use_trailing.Value = value

    @property
    def TrailingStart(self):
        return self._trailing_start.Value

    @TrailingStart.setter
    def TrailingStart(self, value):
        self._trailing_start.Value = value

    @property
    def TrailingStep(self):
        return self._trailing_step.Value

    @TrailingStep.setter
    def TrailingStep(self, value):
        self._trailing_step.Value = value

    def OnStarted2(self, time):
        super(regression_channel_breakout_strategy, self).OnStarted2(time)

        self._regression = LinearReg()
        self._regression.Length = self.Length
        self._stdev = StandardDeviation()
        self._stdev.Length = self.Length
        self._reset_trailing()

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._regression, self._stdev, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, reg, dev):
        if candle.State != CandleStates.Finished:
            return

        middle = float(reg)
        upper = float(reg) + float(self.Deviation) * float(dev)
        lower = float(reg) - float(self.Deviation) * float(dev)
        price = float(candle.ClosePrice)

        if price >= middle and self.Position > 0:
            self.SellMarket()
            self._reset_trailing()
        elif price <= middle and self.Position < 0:
            self.BuyMarket()
            self._reset_trailing()
        else:
            if float(candle.LowPrice) <= lower and self.Position <= 0:
                self.BuyMarket()
                self._entry_price = price
                self._reset_trailing()
            elif float(candle.HighPrice) >= upper and self.Position >= 0:
                self.SellMarket()
                self._entry_price = price
                self._reset_trailing()

        if self.UseTrailing:
            self._apply_trailing(price)

    def _apply_trailing(self, price):
        if self.Position > 0:
            if self._entry_price == 0.0:
                self._entry_price = price

            profit = price - self._entry_price

            if profit >= float(self.TrailingStart):
                stop = price - float(self.TrailingStep)
                if self._long_stop == 0.0 or stop > self._long_stop:
                    self._long_stop = stop

            if self._long_stop != 0.0 and price <= self._long_stop:
                self.SellMarket()
                self._reset_trailing()

        elif self.Position < 0:
            if self._entry_price == 0.0:
                self._entry_price = price

            profit = self._entry_price - price

            if profit >= float(self.TrailingStart):
                stop = price + float(self.TrailingStep)
                if self._short_stop == 0.0 or stop < self._short_stop:
                    self._short_stop = stop

            if self._short_stop != 0.0 and price >= self._short_stop:
                self.BuyMarket()
                self._reset_trailing()
        else:
            self._reset_trailing()

    def _reset_trailing(self):
        self._entry_price = 0.0
        self._long_stop = 0.0
        self._short_stop = 0.0

    def OnReseted(self):
        super(regression_channel_breakout_strategy, self).OnReseted()
        self._reset_trailing()

    def CreateClone(self):
        return regression_channel_breakout_strategy()
