import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import MoneyFlowIndex
from StockSharp.Algo.Strategies import Strategy


class mfi_level_cross_strategy(Strategy):
    DIRECT = 0
    AGAINST = 1

    def __init__(self):
        super(mfi_level_cross_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._mfi_period = self.Param("MfiPeriod", 14)
        self._low_level = self.Param("LowLevel", 30.0)
        self._high_level = self.Param("HighLevel", 70.0)
        self._trend = self.Param("Trend", 0)
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0)
        self._take_profit_percent = self.Param("TakeProfitPercent", 2.0)

        self._prev_mfi = 0.0
        self._is_first = True

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def MfiPeriod(self):
        return self._mfi_period.Value

    @MfiPeriod.setter
    def MfiPeriod(self, value):
        self._mfi_period.Value = value

    @property
    def LowLevel(self):
        return self._low_level.Value

    @LowLevel.setter
    def LowLevel(self, value):
        self._low_level.Value = value

    @property
    def HighLevel(self):
        return self._high_level.Value

    @HighLevel.setter
    def HighLevel(self, value):
        self._high_level.Value = value

    @property
    def Trend(self):
        return self._trend.Value

    @Trend.setter
    def Trend(self, value):
        self._trend.Value = value

    @property
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def TakeProfitPercent(self):
        return self._take_profit_percent.Value

    @TakeProfitPercent.setter
    def TakeProfitPercent(self, value):
        self._take_profit_percent.Value = value

    def OnStarted2(self, time):
        super(mfi_level_cross_strategy, self).OnStarted2(time)

        self._prev_mfi = 0.0
        self._is_first = True

        self.StartProtection(
            Unit(self.TakeProfitPercent, UnitTypes.Percent),
            Unit(self.StopLossPercent, UnitTypes.Percent))

        mfi = MoneyFlowIndex()
        mfi.Length = self.MfiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(mfi, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, mfi_value):
        if candle.State != CandleStates.Finished:
            return

        mfi_val = float(mfi_value)

        if self._is_first:
            self._prev_mfi = mfi_val
            self._is_first = False
            return

        cross_below_low = self._prev_mfi > float(self.LowLevel) and mfi_val <= float(self.LowLevel)
        cross_above_high = self._prev_mfi < float(self.HighLevel) and mfi_val >= float(self.HighLevel)

        if self.Trend == self.DIRECT:
            if cross_below_low and self.Position <= 0:
                self.BuyMarket()
            elif cross_above_high and self.Position >= 0:
                self.SellMarket()
        else:
            if cross_below_low and self.Position >= 0:
                self.SellMarket()
            elif cross_above_high and self.Position <= 0:
                self.BuyMarket()

        self._prev_mfi = mfi_val

    def OnReseted(self):
        super(mfi_level_cross_strategy, self).OnReseted()
        self._prev_mfi = 0.0
        self._is_first = True

    def CreateClone(self):
        return mfi_level_cross_strategy()
