import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ema_prediction_strategy(Strategy):

    def __init__(self):
        super(ema_prediction_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._fast_period = self.Param("FastPeriod", 5) \
            .SetDisplay("Fast EMA Period", "Period of fast EMA", "Indicator")
        self._slow_period = self.Param("SlowPeriod", 20) \
            .SetDisplay("Slow EMA Period", "Period of slow EMA", "Indicator")
        self._take_profit_ticks = self.Param("TakeProfitTicks", 2000.0) \
            .SetDisplay("Take Profit Ticks", "Take profit in ticks", "Risk Management")
        self._stop_loss_ticks = self.Param("StopLossTicks", 1000.0) \
            .SetDisplay("Stop Loss Ticks", "Stop loss in ticks", "Risk Management")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False

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
    def TakeProfitTicks(self):
        return self._take_profit_ticks.Value

    @TakeProfitTicks.setter
    def TakeProfitTicks(self, value):
        self._take_profit_ticks.Value = value

    @property
    def StopLossTicks(self):
        return self._stop_loss_ticks.Value

    @StopLossTicks.setter
    def StopLossTicks(self, value):
        self._stop_loss_ticks.Value = value

    def OnStarted2(self, time):
        super(ema_prediction_strategy, self).OnStarted2(time)

        fast = ExponentialMovingAverage()
        fast.Length = self.FastPeriod
        slow = ExponentialMovingAverage()
        slow.Length = self.SlowPeriod

        self.SubscribeCandles(self.CandleType) \
            .Bind(fast, slow, self.ProcessCandle) \
            .Start()

        ps = self.Security.PriceStep
        step = float(ps) if ps is not None else 1.0

        self.StartProtection(
            stopLoss=Unit(self.StopLossTicks * step, UnitTypes.Absolute),
            takeProfit=Unit(self.TakeProfitTicks * step, UnitTypes.Absolute)
        )

    def ProcessCandle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        fast = float(fast_value)
        slow = float(slow_value)

        if not self._initialized:
            self._prev_fast = fast
            self._prev_slow = slow
            self._initialized = True
            return

        bullish = self._prev_fast < self._prev_slow and fast > slow and float(candle.OpenPrice) < float(candle.ClosePrice)
        bearish = self._prev_fast > self._prev_slow and fast < slow and float(candle.OpenPrice) > float(candle.ClosePrice)

        if bullish and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif bearish and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_fast = fast
        self._prev_slow = slow

    def OnReseted(self):
        super(ema_prediction_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False

    def CreateClone(self):
        return ema_prediction_strategy()
