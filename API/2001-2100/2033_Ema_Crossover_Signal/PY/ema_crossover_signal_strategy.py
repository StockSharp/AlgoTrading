import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ema_crossover_signal_strategy(Strategy):

    def __init__(self):
        super(ema_crossover_signal_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 5) \
            .SetDisplay("Fast Period", "Length of the fast EMA", "EMA")
        self._slow_period = self.Param("SlowPeriod", 13) \
            .SetDisplay("Slow Period", "Length of the slow EMA", "EMA")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")

        self._allow_buy_open = self.Param("AllowBuyOpen", True)
        self._allow_sell_open = self.Param("AllowSellOpen", True)
        self._allow_buy_close = self.Param("AllowBuyClose", True)
        self._allow_sell_close = self.Param("AllowSellClose", True)
        self._stop_loss_ticks = self.Param("StopLossTicks", 1000.0)
        self._take_profit_ticks = self.Param("TakeProfitTicks", 2000.0)
        self._pending = None
        self._is_initialized = False
        self._was_fast_above_slow = False

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
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(ema_crossover_signal_strategy, self).OnStarted2(time)
        self._pending = None
        step = self.Security.PriceStep if self.Security.PriceStep and self.Security.PriceStep > 0 else 1
        self.StartProtection(takeProfit=Unit(self._take_profit_ticks.Value * step, UnitTypes.Absolute), stopLoss=Unit(self._stop_loss_ticks.Value * step, UnitTypes.Absolute), useMarketOrders=True)

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

        fast = float(fast_value)
        slow = float(slow_value)

        if not self._is_initialized:
            self._was_fast_above_slow = fast > slow
            self._is_initialized = True
            return

        is_fast_above_slow = fast > slow

        crossing = self._was_fast_above_slow != is_fast_above_slow
        self._was_fast_above_slow = is_fast_above_slow
        if not crossing or not self.IsFormedAndOnlineAndAllowTrading() or (self._pending is not None and self._pending.State not in (OrderStates.Done, OrderStates.Failed)):
            return
        if is_fast_above_slow:
            if self.Position < 0 and self._allow_sell_close.Value:
                self._pending = self.BuyMarket(-self.Position + (self.Volume if self._allow_buy_open.Value else 0))
            elif self.Position == 0 and self._allow_buy_open.Value:
                self._pending = self.BuyMarket(self.Volume)
        else:
            if self.Position > 0 and self._allow_buy_close.Value:
                self._pending = self.SellMarket(self.Position + (self.Volume if self._allow_sell_open.Value else 0))
            elif self.Position == 0 and self._allow_sell_open.Value:
                self._pending = self.SellMarket(self.Volume)

    def OnReseted(self):
        super(ema_crossover_signal_strategy, self).OnReseted()
        self._is_initialized = False
        self._was_fast_above_slow = False

    def CreateClone(self):
        return ema_crossover_signal_strategy()
