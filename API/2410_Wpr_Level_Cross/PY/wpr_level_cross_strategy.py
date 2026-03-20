import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy


class wpr_level_cross_strategy(Strategy):
    DIRECT = 0
    AGAINST = 1

    def __init__(self):
        super(wpr_level_cross_strategy, self).__init__()

        self._wpr_period = self.Param("WprPeriod", 14)
        self._high_level = self.Param("HighLevel", -20.0)
        self._low_level = self.Param("LowLevel", -80.0)
        self._trend = self.Param("Trend", 0)
        self._enable_buy_entry = self.Param("EnableBuyEntry", True)
        self._enable_sell_entry = self.Param("EnableSellEntry", True)
        self._enable_buy_exit = self.Param("EnableBuyExit", True)
        self._enable_sell_exit = self.Param("EnableSellExit", True)
        self._stop_loss = self.Param("StopLoss", 1000.0)
        self._take_profit = self.Param("TakeProfit", 2000.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._prev_wr = 0.0

    @property
    def WprPeriod(self):
        return self._wpr_period.Value

    @WprPeriod.setter
    def WprPeriod(self, value):
        self._wpr_period.Value = value

    @property
    def HighLevel(self):
        return self._high_level.Value

    @HighLevel.setter
    def HighLevel(self, value):
        self._high_level.Value = value

    @property
    def LowLevel(self):
        return self._low_level.Value

    @LowLevel.setter
    def LowLevel(self, value):
        self._low_level.Value = value

    @property
    def Trend(self):
        return self._trend.Value

    @Trend.setter
    def Trend(self, value):
        self._trend.Value = value

    @property
    def EnableBuyEntry(self):
        return self._enable_buy_entry.Value

    @EnableBuyEntry.setter
    def EnableBuyEntry(self, value):
        self._enable_buy_entry.Value = value

    @property
    def EnableSellEntry(self):
        return self._enable_sell_entry.Value

    @EnableSellEntry.setter
    def EnableSellEntry(self, value):
        self._enable_sell_entry.Value = value

    @property
    def EnableBuyExit(self):
        return self._enable_buy_exit.Value

    @EnableBuyExit.setter
    def EnableBuyExit(self, value):
        self._enable_buy_exit.Value = value

    @property
    def EnableSellExit(self):
        return self._enable_sell_exit.Value

    @EnableSellExit.setter
    def EnableSellExit(self, value):
        self._enable_sell_exit.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(wpr_level_cross_strategy, self).OnStarted(time)

        self._prev_wr = 0.0

        wpr = WilliamsR()
        wpr.Length = self.WprPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(wpr, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(self.TakeProfit, UnitTypes.Absolute),
            Unit(self.StopLoss, UnitTypes.Absolute))

    def ProcessCandle(self, candle, wr_value):
        if candle.State != CandleStates.Finished:
            return

        wr = float(wr_value)

        if self._prev_wr == 0.0:
            self._prev_wr = wr
            return

        crossed_below_low = self._prev_wr > float(self.LowLevel) and wr <= float(self.LowLevel)
        crossed_above_high = self._prev_wr < float(self.HighLevel) and wr >= float(self.HighLevel)

        if self.Trend == self.DIRECT:
            if crossed_below_low and self.EnableBuyEntry and self.Position <= 0:
                self.BuyMarket()
            if crossed_above_high and self.EnableSellEntry and self.Position >= 0:
                self.SellMarket()
        else:
            if crossed_below_low and self.EnableSellEntry and self.Position >= 0:
                self.SellMarket()
            if crossed_above_high and self.EnableBuyEntry and self.Position <= 0:
                self.BuyMarket()

        self._prev_wr = wr

    def OnReseted(self):
        super(wpr_level_cross_strategy, self).OnReseted()
        self._prev_wr = 0.0

    def CreateClone(self):
        return wpr_level_cross_strategy()
