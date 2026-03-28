import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal

class macd_pattern_trader_trigger_strategy(Strategy):
    def __init__(self):
        super(macd_pattern_trader_trigger_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 0.1) \
            .SetDisplay("Trade Volume", "Order volume for entries", "Trading")
        self._fast_period = self.Param("FastPeriod", 13) \
            .SetDisplay("Fast EMA", "Fast EMA length for MACD", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 5) \
            .SetDisplay("Slow EMA", "Slow EMA length for MACD", "Indicators")
        self._signal_period = self.Param("SignalPeriod", 1) \
            .SetDisplay("Signal EMA", "Signal EMA length for MACD", "Indicators")
        self._bullish_trigger = self.Param("BullishTrigger", 50.0) \
            .SetDisplay("Bullish Trigger", "MACD level that arms the bullish pattern", "Logic")
        self._bullish_reset = self.Param("BullishReset", 20.0) \
            .SetDisplay("Bullish Reset", "MACD pullback threshold for bullish setup", "Logic")
        self._bearish_trigger = self.Param("BearishTrigger", 50.0) \
            .SetDisplay("Bearish Trigger", "Absolute MACD level that arms the bearish pattern", "Logic")
        self._bearish_reset = self.Param("BearishReset", 20.0) \
            .SetDisplay("Bearish Reset", "MACD pullback threshold for bearish setup", "Logic")
        self._stop_loss_points = self.Param("StopLossPoints", 100.0) \
            .SetDisplay("Stop Loss", "Stop-loss distance in points", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 300.0) \
            .SetDisplay("Take Profit", "Take-profit distance in points", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Time frame for indicator calculations", "Data")

        self._macd_prev1 = None
        self._macd_prev2 = None
        self._macd_prev3 = None

        self._bullish_armed = False
        self._bullish_window = False
        self._bullish_ready = False
        self._bearish_armed = False
        self._bearish_window = False
        self._bearish_ready = False

        self._price_step = 1.0

    @property
    def TradeVolume(self):
        return self._trade_volume.Value

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @property
    def SignalPeriod(self):
        return self._signal_period.Value

    @property
    def BullishTrigger(self):
        return self._bullish_trigger.Value

    @property
    def BullishReset(self):
        return self._bullish_reset.Value

    @property
    def BearishTrigger(self):
        return self._bearish_trigger.Value

    @property
    def BearishReset(self):
        return self._bearish_reset.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(macd_pattern_trader_trigger_strategy, self).OnStarted(time)

        ps = self.Security.PriceStep if self.Security is not None else None
        self._price_step = float(ps) if ps is not None else 1.0
        if self._price_step <= 0:
            self._price_step = 1.0

        tp = float(self.TakeProfitPoints)
        sl = float(self.StopLossPoints)
        take_profit = Unit(tp * self._price_step, UnitTypes.Absolute) if tp > 0 else None
        stop_loss = Unit(sl * self._price_step, UnitTypes.Absolute) if sl > 0 else None
        self.StartProtection(take_profit, stop_loss)

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.FastPeriod
        macd.Macd.LongMa.Length = self.SlowPeriod
        macd.SignalMa.Length = self.SignalPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(macd, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        if not macd_value.IsFinal:
            return

        macd_line = macd_value.Macd
        if macd_line is None:
            return
        current_macd = float(macd_line)

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._shift_history(current_macd)
            return

        if self._macd_prev1 is None or self._macd_prev2 is None or self._macd_prev3 is None:
            self._shift_history(current_macd)
            return

        macd_curr = self._macd_prev1
        macd_last = self._macd_prev2
        macd_last3 = self._macd_prev3

        self._evaluate_bearish_pattern(macd_curr, macd_last, macd_last3)
        self._evaluate_bullish_pattern(macd_curr, macd_last, macd_last3)

        self._shift_history(current_macd)

    def _evaluate_bullish_pattern(self, macd_curr, macd_last, macd_last3):
        bull_trigger = float(self.BullishTrigger)
        bull_reset = float(self.BullishReset)

        if macd_curr < 0:
            self._bullish_armed = False
            self._bullish_window = False
            self._bullish_ready = False
        else:
            if not self._bullish_armed and macd_curr > bull_trigger:
                self._bullish_armed = True
            if self._bullish_armed and macd_curr < bull_reset:
                self._bullish_armed = False
                self._bullish_window = True

        if (self._bullish_window and macd_curr > macd_last and macd_last < macd_last3
                and macd_curr > bull_reset and macd_last < bull_reset):
            self._bullish_ready = True
            self._bullish_window = False

        if not self._bullish_ready:
            return

        tv = float(self.TradeVolume)
        volume_to_buy = tv + max(0.0, -float(self.Position))
        if volume_to_buy > 0:
            self.BuyMarket(volume_to_buy)

        self._bullish_ready = False
        self._bullish_armed = False
        self._bullish_window = False

    def _evaluate_bearish_pattern(self, macd_curr, macd_last, macd_last3):
        bear_trigger = float(self.BearishTrigger)
        bear_reset = float(self.BearishReset)

        if macd_curr > 0:
            self._bearish_armed = False
            self._bearish_window = False
            self._bearish_ready = False
        else:
            if not self._bearish_armed and macd_curr < -bear_trigger:
                self._bearish_armed = True
            if self._bearish_armed and macd_curr > -bear_reset:
                self._bearish_armed = False
                self._bearish_window = True

        if (self._bearish_window and macd_curr < macd_last and macd_last > macd_last3
                and macd_curr < -bear_reset and macd_last > -bear_reset):
            self._bearish_ready = True
            self._bearish_window = False

        if not self._bearish_ready:
            return

        tv = float(self.TradeVolume)
        volume_to_sell = tv + max(0.0, float(self.Position))
        if volume_to_sell > 0:
            self.SellMarket(volume_to_sell)

        self._bearish_ready = False
        self._bearish_armed = False
        self._bearish_window = False

    def _shift_history(self, current):
        self._macd_prev3 = self._macd_prev2
        self._macd_prev2 = self._macd_prev1
        self._macd_prev1 = current

    def OnReseted(self):
        super(macd_pattern_trader_trigger_strategy, self).OnReseted()
        self._macd_prev1 = None
        self._macd_prev2 = None
        self._macd_prev3 = None
        self._bullish_armed = False
        self._bullish_window = False
        self._bullish_ready = False
        self._bearish_armed = False
        self._bearish_window = False
        self._bearish_ready = False

    def CreateClone(self):
        return macd_pattern_trader_trigger_strategy()
