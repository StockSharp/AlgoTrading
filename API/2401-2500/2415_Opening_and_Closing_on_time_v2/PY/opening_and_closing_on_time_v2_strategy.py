import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class opening_and_closing_on_time_v2_strategy(Strategy):
    BUY = 0
    SELL = 1
    BUY_AND_SELL = 2

    def __init__(self):
        super(opening_and_closing_on_time_v2_strategy, self).__init__()

        self._open_hour = self.Param("OpenHour", 2)
        self._close_hour = self.Param("CloseHour", 14)
        self._trade_mode = self.Param("TradeMode", 2)
        self._slow_period = self.Param("SlowPeriod", 20)
        self._fast_period = self.Param("FastPeriod", 5)
        self._stop_loss = self.Param("StopLoss", 1000.0)
        self._take_profit = self.Param("TakeProfit", 2000.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._prev_slow = 0.0
        self._prev_fast = 0.0
        self._opened = False

    @property
    def OpenHour(self):
        return self._open_hour.Value

    @OpenHour.setter
    def OpenHour(self, value):
        self._open_hour.Value = value

    @property
    def CloseHour(self):
        return self._close_hour.Value

    @CloseHour.setter
    def CloseHour(self, value):
        self._close_hour.Value = value

    @property
    def TradeMode(self):
        return self._trade_mode.Value

    @TradeMode.setter
    def TradeMode(self, value):
        self._trade_mode.Value = value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @SlowPeriod.setter
    def SlowPeriod(self, value):
        self._slow_period.Value = value

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @FastPeriod.setter
    def FastPeriod(self, value):
        self._fast_period.Value = value

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

    def OnStarted2(self, time):
        super(opening_and_closing_on_time_v2_strategy, self).OnStarted2(time)

        self._prev_slow = 0.0
        self._prev_fast = 0.0
        self._opened = False

        slow_ma = ExponentialMovingAverage()
        slow_ma.Length = self.SlowPeriod
        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self.FastPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(slow_ma, fast_ma, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(self.TakeProfit, UnitTypes.Absolute),
            Unit(self.StopLoss, UnitTypes.Absolute))

    def ProcessCandle(self, candle, slow, fast):
        if candle.State != CandleStates.Finished:
            return

        hour = candle.OpenTime.Hour
        slow_val = float(slow)
        fast_val = float(fast)

        if not self._opened and hour >= int(self.OpenHour) and hour < int(self.CloseHour):
            if self._prev_slow != 0.0 and self._prev_fast != 0.0:
                buy_signal = self._prev_fast <= self._prev_slow and fast_val > slow_val
                sell_signal = self._prev_fast >= self._prev_slow and fast_val < slow_val

                if buy_signal and (self.TradeMode == self.BUY or self.TradeMode == self.BUY_AND_SELL) and self.Position <= 0:
                    self.BuyMarket()
                    self._opened = True
                elif sell_signal and (self.TradeMode == self.SELL or self.TradeMode == self.BUY_AND_SELL) and self.Position >= 0:
                    self.SellMarket()
                    self._opened = True
        elif self._opened and hour >= int(self.CloseHour):
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()
            self._opened = False

        self._prev_slow = slow_val
        self._prev_fast = fast_val

    def OnReseted(self):
        super(opening_and_closing_on_time_v2_strategy, self).OnReseted()
        self._prev_slow = 0.0
        self._prev_fast = 0.0
        self._opened = False

    def CreateClone(self):
        return opening_and_closing_on_time_v2_strategy()
