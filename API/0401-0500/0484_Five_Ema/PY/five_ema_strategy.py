import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class five_ema_strategy(Strategy):
    """5 EMA Strategy."""

    def __init__(self):
        super(five_ema_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ema_length = self.Param("EmaLength", 5) \
            .SetDisplay("EMA Length", "Length of EMA", "EMA")
        self._target_rr = self.Param("TargetRR", 3.0) \
            .SetDisplay("Target R:R", "Reward to risk ratio", "Risk Management")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._ema = None
        self._signal_high = None
        self._signal_low = None
        self._signal_index = None
        self._is_buy_signal = False
        self._is_sell_signal = False
        self._bar_index = 0
        self._long_stop = None
        self._long_target = None
        self._short_stop = None
        self._short_target = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(five_ema_strategy, self).OnReseted()
        self._ema = None
        self._signal_high = None
        self._signal_low = None
        self._signal_index = None
        self._is_buy_signal = False
        self._is_sell_signal = False
        self._bar_index = 0
        self._long_stop = None
        self._long_target = None
        self._short_stop = None
        self._short_target = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(five_ema_strategy, self).OnStarted2(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = int(self._ema_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._ema.IsFormed:
            return

        self._bar_index += 1

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        ema_val = float(ema_value)
        cooldown = int(self._cooldown_bars.Value)
        rr = float(self._target_rr.Value)

        # Check stop/target exits first (always)
        if self.Position > 0 and self._long_stop is not None and self._long_target is not None:
            if low <= self._long_stop or high >= self._long_target:
                self.SellMarket(Math.Abs(self.Position))
                self._long_stop = None
                self._long_target = None
                self._cooldown_remaining = cooldown
        elif self.Position < 0 and self._short_stop is not None and self._short_target is not None:
            if high >= self._short_stop or low <= self._short_target:
                self.BuyMarket(Math.Abs(self.Position))
                self._short_stop = None
                self._short_target = None
                self._cooldown_remaining = cooldown

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Signal detection
        if high < ema_val:
            self._signal_high = high
            self._signal_low = low
            self._signal_index = self._bar_index
            self._is_buy_signal = True
            self._is_sell_signal = False
        elif low > ema_val:
            self._signal_high = high
            self._signal_low = low
            self._signal_index = self._bar_index
            self._is_buy_signal = False
            self._is_sell_signal = True

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        within_window = (self._signal_index is not None and
                         self._bar_index > self._signal_index and
                         self._bar_index <= self._signal_index + 3)

        # Buy entry
        if (self._is_buy_signal and within_window and self._signal_high is not None
                and high > self._signal_high and self.Position <= 0):
            sl = self._signal_low if self._signal_low is not None else low
            risk = self._signal_high - sl
            if risk > 0:
                if self.Position < 0:
                    self.BuyMarket(Math.Abs(self.Position))
                self._long_stop = sl
                self._long_target = self._signal_high + risk * rr
                self.BuyMarket(self.Volume)
                self._is_buy_signal = False
                self._signal_high = None
                self._signal_low = None
                self._signal_index = None
                self._cooldown_remaining = cooldown
        # Sell entry
        elif (self._is_sell_signal and within_window and self._signal_low is not None
              and low < self._signal_low and self.Position >= 0):
            sl = self._signal_high if self._signal_high is not None else high
            risk = sl - self._signal_low
            if risk > 0:
                if self.Position > 0:
                    self.SellMarket(Math.Abs(self.Position))
                self._short_stop = sl
                self._short_target = self._signal_low - risk * rr
                self.SellMarket(self.Volume)
                self._is_sell_signal = False
                self._signal_high = None
                self._signal_low = None
                self._signal_index = None
                self._cooldown_remaining = cooldown

    def CreateClone(self):
        return five_ema_strategy()
