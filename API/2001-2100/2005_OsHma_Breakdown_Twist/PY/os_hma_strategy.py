import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import HullMovingAverage
from StockSharp.Algo.Strategies import Strategy

# Mode constants
MODE_BREAKDOWN = 0
MODE_TWIST = 1


class os_hma_strategy(Strategy):

    def __init__(self):
        super(os_hma_strategy, self).__init__()

        self._fast_hma = self.Param("FastHma", 13) \
            .SetDisplay("Fast HMA", "Length of fast Hull Moving Average", "Indicators")
        self._slow_hma = self.Param("SlowHma", 26) \
            .SetDisplay("Slow HMA", "Length of slow Hull Moving Average", "Indicators")
        self._mode = self.Param("Mode", MODE_TWIST) \
            .SetDisplay("Mode", "0=Breakdown, 1=Twist", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "General")
        self._take_profit = self.Param("TakeProfit", 2000.0) \
            .SetDisplay("Take Profit", "Target profit in points", "Risk")
        self._stop_loss = self.Param("StopLoss", 1000.0) \
            .SetDisplay("Stop Loss", "Loss limit in points", "Risk")

        self._prev_value = 0.0
        self._prev_prev_value = 0.0
        self._count = 0

    @property
    def FastHma(self):
        return self._fast_hma.Value

    @FastHma.setter
    def FastHma(self, value):
        self._fast_hma.Value = value

    @property
    def SlowHma(self):
        return self._slow_hma.Value

    @SlowHma.setter
    def SlowHma(self, value):
        self._slow_hma.Value = value

    @property
    def Mode(self):
        return self._mode.Value

    @Mode.setter
    def Mode(self, value):
        self._mode.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

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

    def OnStarted2(self, time):
        super(os_hma_strategy, self).OnStarted2(time)

        fast_hma = HullMovingAverage()
        fast_hma.Length = self.FastHma
        slow_hma = HullMovingAverage()
        slow_hma.Length = self.SlowHma

        self.SubscribeCandles(self.CandleType) \
            .Bind(fast_hma, slow_hma, self.ProcessCandle) \
            .Start()

        self.StartProtection(
            takeProfit=Unit(self.TakeProfit, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLoss, UnitTypes.Absolute)
        )

    def ProcessCandle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        current = float(fast_value) - float(slow_value)
        self._count += 1

        if self._count < 3:
            self._prev_prev_value = self._prev_value
            self._prev_value = current
            return

        buy_signal = False
        sell_signal = False

        mode = self.Mode

        if mode == MODE_BREAKDOWN:
            buy_signal = self._prev_value <= 0 and current > 0
            sell_signal = self._prev_value >= 0 and current < 0
        elif mode == MODE_TWIST:
            buy_signal = self._prev_value < self._prev_prev_value and current > self._prev_value
            sell_signal = self._prev_value > self._prev_prev_value and current < self._prev_value

        if buy_signal and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif sell_signal and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_prev_value = self._prev_value
        self._prev_value = current

    def OnReseted(self):
        super(os_hma_strategy, self).OnReseted()
        self._prev_value = 0.0
        self._prev_prev_value = 0.0
        self._count = 0

    def CreateClone(self):
        return os_hma_strategy()
