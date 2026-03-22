import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import KaufmanAdaptiveMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class adaptive_ema_breakout_strategy(Strategy):
    """
    Breakout strategy that trades in the direction of a rising or falling adaptive
    moving average when price extends beyond an ATR buffer.
    """

    def __init__(self):
        super(adaptive_ema_breakout_strategy, self).__init__()

        self._fast = self.Param("Fast", 2) \
            .SetDisplay("Fast Period", "Fast period for KAMA smoothing", "KAMA")

        self._slow = self.Param("Slow", 30) \
            .SetDisplay("Slow Period", "Slow period for KAMA smoothing", "KAMA")

        self._lookback = self.Param("Lookback", 10) \
            .SetDisplay("Lookback", "Main lookback period for KAMA", "KAMA")

        self._breakout_atr_multiplier = self.Param("BreakoutAtrMultiplier", 0.75) \
            .SetDisplay("Breakout ATR", "ATR multiple required for entry", "Signals")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._cooldown_bars = self.Param("CooldownBars", 72) \
            .SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles for the strategy", "General")

        self._adaptive_ema = None
        self._atr = None
        self._previous_adaptive_ema_value = 0.0
        self._is_initialized = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(adaptive_ema_breakout_strategy, self).OnReseted()
        self._adaptive_ema = None
        self._atr = None
        self._previous_adaptive_ema_value = 0.0
        self._is_initialized = False
        self._cooldown = 0

    def OnStarted(self, time):
        super(adaptive_ema_breakout_strategy, self).OnStarted(time)

        self._adaptive_ema = KaufmanAdaptiveMovingAverage()
        self._adaptive_ema.Length = int(self._lookback.Value)
        self._adaptive_ema.FastSCPeriod = int(self._fast.Value)
        self._adaptive_ema.SlowSCPeriod = int(self._slow.Value)

        self._atr = AverageTrueRange()
        self._atr.Length = 14
        self._cooldown = 0
        self._is_initialized = False

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._adaptive_ema, self._atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._adaptive_ema)
            self.DrawOwnTrades(area)

        self.StartProtection(Unit(0, UnitTypes.Absolute), Unit(self._stop_loss_percent.Value, UnitTypes.Percent), False)

    def _process_candle(self, candle, adaptive_ema_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._adaptive_ema.IsFormed or not self._atr.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ae = float(adaptive_ema_value)
        av = float(atr_value)

        if not self._is_initialized:
            self._previous_adaptive_ema_value = ae
            self._is_initialized = True
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._previous_adaptive_ema_value = ae
            return

        is_trend_up = ae > self._previous_adaptive_ema_value
        is_trend_down = ae < self._previous_adaptive_ema_value
        close_price = float(candle.ClosePrice)
        breakout_distance = close_price - ae
        bam = float(self._breakout_atr_multiplier.Value)
        required_distance = av * bam
        cd = int(self._cooldown_bars.Value)

        if self.Position == 0:
            if is_trend_up and breakout_distance >= required_distance:
                self.BuyMarket()
                self._cooldown = cd
            elif is_trend_down and breakout_distance <= -required_distance:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position > 0:
            if close_price <= ae or is_trend_down:
                self.SellMarket(Math.Abs(self.Position))
                self._cooldown = cd
        elif self.Position < 0:
            if close_price >= ae or is_trend_up:
                self.BuyMarket(Math.Abs(self.Position))
                self._cooldown = cd

        self._previous_adaptive_ema_value = ae

    def CreateClone(self):
        return adaptive_ema_breakout_strategy()
