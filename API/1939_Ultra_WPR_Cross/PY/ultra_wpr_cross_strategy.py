import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates, DecimalIndicatorValue
from StockSharp.Algo.Indicators import SimpleMovingAverage, WilliamsR
from StockSharp.Algo.Strategies import Strategy


class ultra_wpr_cross_strategy(Strategy):

    def __init__(self):
        super(ultra_wpr_cross_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._wpr_period = self.Param("WprPeriod", 13) \
            .SetDisplay("WPR Period", "Williams %R period", "Indicators")
        self._fast_length = self.Param("FastLength", 3) \
            .SetDisplay("Fast Length", "Fast smoothing length", "Indicators")
        self._slow_length = self.Param("SlowLength", 53) \
            .SetDisplay("Slow Length", "Slow smoothing length", "Indicators")
        self._take_profit = self.Param("TakeProfit", 900.0) \
            .SetDisplay("Take Profit", "Take profit in price", "Risk")
        self._stop_loss = self.Param("StopLoss", 450.0) \
            .SetDisplay("Stop Loss", "Stop loss in price", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 1) \
            .SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "Risk")

        self._fast_ma = SimpleMovingAverage()
        self._slow_ma = SimpleMovingAverage()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_initialized = False
        self._bars_since_trade = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def WprPeriod(self):
        return self._wpr_period.Value

    @WprPeriod.setter
    def WprPeriod(self, value):
        self._wpr_period.Value = value

    @property
    def FastLength(self):
        return self._fast_length.Value

    @FastLength.setter
    def FastLength(self, value):
        self._fast_length.Value = value

    @property
    def SlowLength(self):
        return self._slow_length.Value

    @SlowLength.setter
    def SlowLength(self, value):
        self._slow_length.Value = value

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
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    def OnStarted(self, time):
        super(ultra_wpr_cross_strategy, self).OnStarted(time)

        self._fast_ma.Length = self.FastLength
        self._slow_ma.Length = self.SlowLength

        wpr = WilliamsR()
        wpr.Length = self.WprPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(wpr, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._fast_ma)
            self.DrawIndicator(area, self._slow_ma)
            self.DrawOwnTrades(area)

        self.StartProtection(
            Unit(self.TakeProfit, UnitTypes.Absolute),
            Unit(self.StopLoss, UnitTypes.Absolute))

    def ProcessCandle(self, candle, wpr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        wpr_val = float(wpr_value)

        fast = float(self._fast_ma.Process(
            DecimalIndicatorValue(self._fast_ma, wpr_val, candle.OpenTime, True)))
        slow = float(self._slow_ma.Process(
            DecimalIndicatorValue(self._slow_ma, wpr_val, candle.OpenTime, True)))

        if not self._fast_ma.IsFormed or not self._slow_ma.IsFormed:
            return

        if self._bars_since_trade < self.CooldownBars:
            self._bars_since_trade += 1

        if not self._is_initialized:
            self._prev_fast = fast
            self._prev_slow = slow
            self._is_initialized = True
            return

        cross_up = self._prev_fast <= self._prev_slow and fast > slow and fast < -55.0
        cross_down = self._prev_fast >= self._prev_slow and fast < slow and fast > -45.0

        if self._bars_since_trade >= self.CooldownBars:
            pos = self.Position
            if cross_up and pos <= 0:
                self.BuyMarket(self.Volume + abs(pos))
                self._bars_since_trade = 0
            elif cross_down and pos >= 0:
                self.SellMarket(self.Volume + abs(pos))
                self._bars_since_trade = 0

        self._prev_fast = fast
        self._prev_slow = slow

    def OnReseted(self):
        super(ultra_wpr_cross_strategy, self).OnReseted()
        self._fast_ma.Length = self.FastLength
        self._slow_ma.Length = self.SlowLength
        self._fast_ma.Reset()
        self._slow_ma.Reset()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_initialized = False
        self._bars_since_trade = self.CooldownBars

    def CreateClone(self):
        return ultra_wpr_cross_strategy()
