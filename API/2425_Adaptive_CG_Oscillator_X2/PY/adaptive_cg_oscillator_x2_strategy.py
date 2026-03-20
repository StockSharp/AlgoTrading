import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class adaptive_cg_oscillator_x2_strategy(Strategy):
    def __init__(self):
        super(adaptive_cg_oscillator_x2_strategy, self).__init__()

        self._period = self.Param("Period", 20)
        self._stop_loss = self.Param("StopLoss", 1000.0)
        self._take_profit = self.Param("TakeProfit", 2000.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))

        self._prices = []
        self._prev_cg = 0.0
        self._prev_prev_cg = 0.0
        self._count = 0
        self._bars_since_signal = 999999

    @property
    def Period(self):
        return self._period.Value

    @Period.setter
    def Period(self, value):
        self._period.Value = value

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
        super(adaptive_cg_oscillator_x2_strategy, self).OnStarted(time)

        self._prices = []
        self._prev_cg = 0.0
        self._prev_prev_cg = 0.0
        self._count = 0
        self._bars_since_signal = 999999

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        self.StartProtection(
            Unit(self.TakeProfit, UnitTypes.Absolute),
            Unit(self.StopLoss, UnitTypes.Absolute))

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)
        period = int(self.Period)

        self._bars_since_signal += 1
        self._prices.append(price)

        if len(self._prices) > period:
            self._prices.pop(0)

        if len(self._prices) < period:
            return

        num = 0.0
        denom = 0.0
        for i in range(len(self._prices)):
            num += (1 + i) * self._prices[i]
            denom += self._prices[i]

        if denom != 0.0:
            cg = -num / denom + (period + 1.0) / 2.0
        else:
            cg = 0.0

        self._count += 1
        if self._count < 3:
            self._prev_prev_cg = self._prev_cg
            self._prev_cg = cg
            return

        long_signal = cg > 0.0 and cg > self._prev_cg and self._prev_cg <= self._prev_prev_cg
        short_signal = cg < 0.0 and cg < self._prev_cg and self._prev_cg >= self._prev_prev_cg

        if long_signal and self._bars_since_signal >= 12 and self.Position <= 0:
            self.BuyMarket()
            self._bars_since_signal = 0
        elif short_signal and self._bars_since_signal >= 12 and self.Position >= 0:
            self.SellMarket()
            self._bars_since_signal = 0

        self._prev_prev_cg = self._prev_cg
        self._prev_cg = cg

    def OnReseted(self):
        super(adaptive_cg_oscillator_x2_strategy, self).OnReseted()
        self._prices = []
        self._prev_cg = 0.0
        self._prev_prev_cg = 0.0
        self._count = 0
        self._bars_since_signal = 999999

    def CreateClone(self):
        return adaptive_cg_oscillator_x2_strategy()
