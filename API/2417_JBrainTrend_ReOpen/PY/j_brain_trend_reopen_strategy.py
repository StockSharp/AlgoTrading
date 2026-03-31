import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class j_brain_trend_reopen_strategy(Strategy):
    def __init__(self):
        super(j_brain_trend_reopen_strategy, self).__init__()

        self._stoch_period = self.Param("StochPeriod", 9)
        self._k_smoothing = self.Param("KSmoothing", 3)
        self._d_period = self.Param("DPeriod", 3)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._stop_loss = self.Param("StopLoss", 1000.0)
        self._take_profit = self.Param("TakeProfit", 2000.0)
        self._price_step = self.Param("PriceStep", 300.0)
        self._max_positions = self.Param("MaxPositions", 1)
        self._buy_enabled = self.Param("BuyEnabled", True)
        self._sell_enabled = self.Param("SellEnabled", True)

        self._last_entry_price = 0.0
        self._entries_count = 0
        self._is_long = False

    @property
    def StochPeriod(self):
        return self._stoch_period.Value

    @StochPeriod.setter
    def StochPeriod(self, value):
        self._stoch_period.Value = value

    @property
    def KSmoothing(self):
        return self._k_smoothing.Value

    @KSmoothing.setter
    def KSmoothing(self, value):
        self._k_smoothing.Value = value

    @property
    def DPeriod(self):
        return self._d_period.Value

    @DPeriod.setter
    def DPeriod(self, value):
        self._d_period.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

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
    def PriceStep(self):
        return self._price_step.Value

    @PriceStep.setter
    def PriceStep(self, value):
        self._price_step.Value = value

    @property
    def MaxPositions(self):
        return self._max_positions.Value

    @MaxPositions.setter
    def MaxPositions(self, value):
        self._max_positions.Value = value

    @property
    def BuyEnabled(self):
        return self._buy_enabled.Value

    @BuyEnabled.setter
    def BuyEnabled(self, value):
        self._buy_enabled.Value = value

    @property
    def SellEnabled(self):
        return self._sell_enabled.Value

    @SellEnabled.setter
    def SellEnabled(self, value):
        self._sell_enabled.Value = value

    def OnStarted2(self, time):
        super(j_brain_trend_reopen_strategy, self).OnStarted2(time)

        self._last_entry_price = 0.0
        self._entries_count = 0
        self._is_long = False

        rsi = RelativeStrengthIndex()
        rsi.Length = self.StochPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(self.TakeProfit, UnitTypes.Absolute),
            Unit(self.StopLoss, UnitTypes.Absolute))

    def ProcessCandle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        rsi_val = float(rsi_value)
        price = float(candle.ClosePrice)

        if self.Position == 0:
            self._entries_count = 0

        if rsi_val < 30.0 and self.Position <= 0 and self.BuyEnabled:
            self.BuyMarket()
            self._is_long = True
            self._last_entry_price = price
            self._entries_count = 1
            return

        if rsi_val > 70.0 and self.Position >= 0 and self.SellEnabled:
            self.SellMarket()
            self._is_long = False
            self._last_entry_price = price
            self._entries_count = 1
            return

        if self.Position > 0 and rsi_val > 70.0:
            self.SellMarket()
            return

        if self.Position < 0 and rsi_val < 30.0:
            self.BuyMarket()
            return

        if self._entries_count > 0 and self._entries_count < int(self.MaxPositions):
            if self._is_long and self.Position > 0 and price - self._last_entry_price >= float(self.PriceStep):
                self.BuyMarket()
                self._last_entry_price = price
                self._entries_count += 1
            elif not self._is_long and self.Position < 0 and self._last_entry_price - price >= float(self.PriceStep):
                self.SellMarket()
                self._last_entry_price = price
                self._entries_count += 1

    def OnReseted(self):
        super(j_brain_trend_reopen_strategy, self).OnReseted()
        self._last_entry_price = 0.0
        self._entries_count = 0
        self._is_long = False

    def CreateClone(self):
        return j_brain_trend_reopen_strategy()
