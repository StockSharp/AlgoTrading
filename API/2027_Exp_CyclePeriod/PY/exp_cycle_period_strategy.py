import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class exp_cycle_period_strategy(Strategy):

    def __init__(self):
        super(exp_cycle_period_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(6))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._fast_period = self.Param("FastPeriod", 7) \
            .SetDisplay("Fast Period", "Fast EMA period", "Indicator")
        self._slow_period = self.Param("SlowPeriod", 21) \
            .SetDisplay("Slow Period", "Slow EMA period", "Indicator")
        self._take_profit = self.Param("TakeProfit", 2000.0) \
            .SetDisplay("Take Profit", "Take profit in price", "Risk")
        self._stop_loss = self.Param("StopLoss", 1000.0) \
            .SetDisplay("Stop Loss", "Stop loss in price", "Risk")
        self._buy_pos_open = self.Param("BuyPosOpen", True) \
            .SetDisplay("Buy Open", "Allow long entries", "Logic")
        self._sell_pos_open = self.Param("SellPosOpen", True) \
            .SetDisplay("Sell Open", "Allow short entries", "Logic")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_ready = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @FastPeriod.setter
    def FastPeriod(self, value):
        self._fast_period.Value = value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @SlowPeriod.setter
    def SlowPeriod(self, value):
        self._slow_period.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def BuyPosOpen(self):
        return self._buy_pos_open.Value

    @BuyPosOpen.setter
    def BuyPosOpen(self, value):
        self._buy_pos_open.Value = value

    @property
    def SellPosOpen(self):
        return self._sell_pos_open.Value

    @SellPosOpen.setter
    def SellPosOpen(self, value):
        self._sell_pos_open.Value = value

    def OnStarted(self, time):
        super(exp_cycle_period_strategy, self).OnStarted(time)

        self.StartProtection(
            stopLoss=Unit(self.StopLoss, UnitTypes.Absolute),
            takeProfit=Unit(self.TakeProfit, UnitTypes.Absolute)
        )

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.FastPeriod
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.SlowPeriod

        self.SubscribeCandles(self.CandleType) \
            .Bind(fast_ema, slow_ema, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast_value)
        slow_val = float(slow_value)

        if not self._prev_ready:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            self._prev_ready = True
            return

        prev_diff = self._prev_fast - self._prev_slow
        curr_diff = fast_val - slow_val

        if prev_diff <= 0 and curr_diff > 0:
            if self.BuyPosOpen and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
        elif prev_diff >= 0 and curr_diff < 0:
            if self.SellPosOpen and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def OnReseted(self):
        super(exp_cycle_period_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_ready = False

    def CreateClone(self):
        return exp_cycle_period_strategy()
