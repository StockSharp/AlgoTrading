import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SuperTrend, Momentum
from StockSharp.Algo.Strategies import Strategy


class supertrend_momentum_filter_strategy(Strategy):
    """
    Trend-following strategy that trades SuperTrend direction only when momentum confirms acceleration.
    """

    def __init__(self):
        super(supertrend_momentum_filter_strategy, self).__init__()

        self._supertrend_period = self.Param("SupertrendPeriod", 10) \
            .SetRange(2, 50) \
            .SetDisplay("Supertrend Period", "Period of the SuperTrend indicator", "Indicators")

        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 3.0) \
            .SetRange(0.5, 10.0) \
            .SetDisplay("Supertrend Multiplier", "Multiplier of the SuperTrend indicator", "Indicators")

        self._momentum_period = self.Param("MomentumPeriod", 14) \
            .SetRange(2, 100) \
            .SetDisplay("Momentum Period", "Period of the Momentum indicator", "Indicators")

        self._cooldown_bars = self.Param("CooldownBars", 84) \
            .SetRange(1, 500) \
            .SetDisplay("Cooldown Bars", "Bars to wait between orders", "Risk")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetRange(0.5, 10.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_momentum = 0.0
        self._is_initialized = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(supertrend_momentum_filter_strategy, self).OnReseted()
        self._prev_momentum = 0.0
        self._is_initialized = False
        self._cooldown = 0

    def OnStarted2(self, time):
        super(supertrend_momentum_filter_strategy, self).OnStarted2(time)

        supertrend = SuperTrend()
        supertrend.Length = int(self._supertrend_period.Value)
        supertrend.Multiplier = Decimal(self._supertrend_multiplier.Value)

        momentum = Momentum()
        momentum.Length = int(self._momentum_period.Value)

        self._cooldown = 0
        self._is_initialized = False

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(supertrend, momentum, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, supertrend)
            self.DrawIndicator(area, momentum)
            self.DrawOwnTrades(area)

        self.StartProtection(
            Unit(0, UnitTypes.Absolute),
            Unit(self._stop_loss_percent.Value, UnitTypes.Percent),
            False
        )

    def _process_candle(self, candle, supertrend_value, momentum_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if not self._is_initialized:
            self._prev_momentum = float(momentum_value)
            self._is_initialized = True
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_momentum = float(momentum_value)
            return

        price = float(candle.ClosePrice)
        st_val = float(supertrend_value)
        mom_val = float(momentum_value)
        is_above_supertrend = price > st_val
        is_below_supertrend = price < st_val
        is_momentum_rising = mom_val > self._prev_momentum
        is_momentum_falling = mom_val < self._prev_momentum

        cd = int(self._cooldown_bars.Value)

        if self.Position == 0:
            if is_above_supertrend and is_momentum_rising and mom_val >= 100.0:
                self.BuyMarket()
                self._cooldown = cd
            elif is_below_supertrend and is_momentum_falling and mom_val <= 100.0:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position > 0:
            if is_below_supertrend or is_momentum_falling:
                self.SellMarket(Math.Abs(self.Position))
                self._cooldown = cd
        elif self.Position < 0:
            if is_above_supertrend or is_momentum_rising:
                self.BuyMarket(Math.Abs(self.Position))
                self._cooldown = cd

        self._prev_momentum = mom_val

    def CreateClone(self):
        return supertrend_momentum_filter_strategy()
