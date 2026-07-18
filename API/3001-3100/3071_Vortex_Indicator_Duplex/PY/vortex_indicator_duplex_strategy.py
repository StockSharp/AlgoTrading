import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import VortexIndicator, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class vortex_indicator_duplex_strategy(Strategy):
    def __init__(self):
        super(vortex_indicator_duplex_strategy, self).__init__()

        self._long_candle_type = self.Param("LongCandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Long Candle Type", "Timeframe used for long-side Vortex calculations", "General")
        self._short_candle_type = self.Param("ShortCandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Short Candle Type", "Timeframe used for short-side Vortex calculations", "General")
        self._long_length = self.Param("LongLength", 14) \
            .SetDisplay("Long Vortex Length", "VI period applied to the long signal stream", "Indicator")
        self._short_length = self.Param("ShortLength", 14) \
            .SetDisplay("Short Vortex Length", "VI period applied to the short signal stream", "Indicator")
        self._long_signal_bar = self.Param("LongSignalBar", 1) \
            .SetDisplay("Long Signal Bar", "Closed bar shift used for long evaluations", "Signals")
        self._short_signal_bar = self.Param("ShortSignalBar", 1) \
            .SetDisplay("Short Signal Bar", "Closed bar shift used for short evaluations", "Signals")
        self._long_stop_loss_steps = self.Param("LongStopLossSteps", 1000.0) \
            .SetDisplay("Long Stop Loss Steps", "Protective distance below long entry in price steps", "Risk")
        self._long_take_profit_steps = self.Param("LongTakeProfitSteps", 2000.0) \
            .SetDisplay("Long Take Profit Steps", "Target distance above long entry in price steps", "Risk")
        self._short_stop_loss_steps = self.Param("ShortStopLossSteps", 1000.0) \
            .SetDisplay("Short Stop Loss Steps", "Protective distance above short entry in price steps", "Risk")
        self._short_take_profit_steps = self.Param("ShortTakeProfitSteps", 2000.0) \
            .SetDisplay("Short Take Profit Steps", "Target distance below short entry in price steps", "Risk")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 4) \
            .SetDisplay("Signal Cooldown", "Bars to wait between trading actions", "Trading")

        self._long_vortex = None
        self._short_vortex = None
        self._long_history = []
        self._short_history = []
        self._price_step = 1.0
        self._long_entry_price = None
        self._short_entry_price = None
        self._long_stop_price = None
        self._short_stop_price = None
        self._long_take_profit_price = None
        self._short_take_profit_price = None
        self._cooldown_remaining = 0

    @property
    def LongCandleType(self):
        return self._long_candle_type.Value
    @property
    def ShortCandleType(self):
        return self._short_candle_type.Value
    @property
    def LongLength(self):
        return self._long_length.Value
    @property
    def ShortLength(self):
        return self._short_length.Value
    @property
    def LongSignalBar(self):
        return self._long_signal_bar.Value
    @property
    def ShortSignalBar(self):
        return self._short_signal_bar.Value
    @property
    def LongStopLossSteps(self):
        return self._long_stop_loss_steps.Value
    @property
    def LongTakeProfitSteps(self):
        return self._long_take_profit_steps.Value
    @property
    def ShortStopLossSteps(self):
        return self._short_stop_loss_steps.Value
    @property
    def ShortTakeProfitSteps(self):
        return self._short_take_profit_steps.Value
    @property
    def SignalCooldownBars(self):
        return self._signal_cooldown_bars.Value

    def OnReseted(self):
        super(vortex_indicator_duplex_strategy, self).OnReseted()
        self._long_history = []
        self._short_history = []
        self._reset_long_state()
        self._reset_short_state()
        self._price_step = 1.0
        self._cooldown_remaining = 0
        self._long_vortex = None
        self._short_vortex = None

    def OnStarted2(self, time):
        super(vortex_indicator_duplex_strategy, self).OnStarted2(time)
        sec = self.Security
        self._price_step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        if self._price_step <= 0.0:
            self._price_step = 1.0

        self._long_history = []
        self._short_history = []
        self._reset_long_state()
        self._reset_short_state()
        self._cooldown_remaining = 0

        self._long_vortex = VortexIndicator()
        self._long_vortex.Length = self.LongLength
        long_subscription = self.SubscribeCandles(self.LongCandleType)
        long_subscription.Bind(self._on_long_candle).Start()

        self._short_vortex = VortexIndicator()
        self._short_vortex.Length = self.ShortLength
        short_subscription = self.SubscribeCandles(self.ShortCandleType)
        short_subscription.Bind(self._on_short_candle).Start()

    def _on_long_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        if self._check_risk_management(float(candle.ClosePrice)):
            return

        value = self._long_vortex.Process(CandleIndicatorValue(self._long_vortex, candle))

        try:
            vi_plus = float(value.PlusVi)
            vi_minus = float(value.MinusVi)
        except:
            return

        self._long_history.append((vi_plus, vi_minus))
        max_history = 512
        if len(self._long_history) > max_history:
            self._long_history.pop(0)

        if not self._long_vortex.IsFormed:
            return

        pair = self._try_get_history_pair(self._long_history, self.LongSignalBar)
        if pair is None:
            return

        previous, current = pair
        cross_up = previous[0] <= previous[1] and current[0] > current[1]
        long_exit = current[1] > current[0]

        if long_exit and self.Position > 0:
            self.SellMarket()
            self._reset_long_state()
            self._cooldown_remaining = self.SignalCooldownBars

        if self._cooldown_remaining == 0 and cross_up:
            self._try_open_long(float(candle.ClosePrice))

    def _on_short_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        if self._check_risk_management(float(candle.ClosePrice)):
            return

        value = self._short_vortex.Process(CandleIndicatorValue(self._short_vortex, candle))

        try:
            vi_plus = float(value.PlusVi)
            vi_minus = float(value.MinusVi)
        except:
            return

        self._short_history.append((vi_plus, vi_minus))
        max_history = 512
        if len(self._short_history) > max_history:
            self._short_history.pop(0)

        if not self._short_vortex.IsFormed:
            return

        pair = self._try_get_history_pair(self._short_history, self.ShortSignalBar)
        if pair is None:
            return

        previous, current = pair
        cross_down = previous[0] >= previous[1] and current[0] < current[1]
        short_exit = current[0] > current[1]

        if short_exit and self.Position < 0:
            self.BuyMarket()
            self._reset_short_state()
            self._cooldown_remaining = self.SignalCooldownBars

        if self._cooldown_remaining == 0 and cross_down:
            self._try_open_short(float(candle.ClosePrice))

    def _try_open_long(self, price):
        if self.Position > 0:
            return

        if self.Position < 0:
            self.BuyMarket()
        self.BuyMarket()

        self._long_entry_price = price
        sl = float(self.LongStopLossSteps)
        tp = float(self.LongTakeProfitSteps)
        self._long_stop_price = price - sl * self._price_step if sl > 0.0 else None
        self._long_take_profit_price = price + tp * self._price_step if tp > 0.0 else None
        self._cooldown_remaining = self.SignalCooldownBars
        self._reset_short_state()

    def _try_open_short(self, price):
        if self.Position < 0:
            return

        if self.Position > 0:
            self.SellMarket()
        self.SellMarket()

        self._short_entry_price = price
        sl = float(self.ShortStopLossSteps)
        tp = float(self.ShortTakeProfitSteps)
        self._short_stop_price = price + sl * self._price_step if sl > 0.0 else None
        self._short_take_profit_price = price - tp * self._price_step if tp > 0.0 else None
        self._cooldown_remaining = self.SignalCooldownBars
        self._reset_long_state()

    def _check_risk_management(self, price):
        if self.Position > 0:
            if self._long_stop_price is not None and price <= self._long_stop_price:
                self.SellMarket()
                self._reset_long_state()
                self._cooldown_remaining = self.SignalCooldownBars
                return True
            if self._long_take_profit_price is not None and price >= self._long_take_profit_price:
                self.SellMarket()
                self._reset_long_state()
                self._cooldown_remaining = self.SignalCooldownBars
                return True
        elif self.Position < 0:
            if self._short_stop_price is not None and price >= self._short_stop_price:
                self.BuyMarket()
                self._reset_short_state()
                self._cooldown_remaining = self.SignalCooldownBars
                return True
            if self._short_take_profit_price is not None and price <= self._short_take_profit_price:
                self.BuyMarket()
                self._reset_short_state()
                self._cooldown_remaining = self.SignalCooldownBars
                return True
        else:
            self._reset_long_state()
            self._reset_short_state()
        return False

    def _try_get_history_pair(self, history, signal_bar):
        current_index = len(history) - 1 - signal_bar
        previous_index = current_index - 1
        if current_index < 0 or previous_index < 0:
            return None
        return (history[previous_index], history[current_index])

    def _reset_long_state(self):
        self._long_entry_price = None
        self._long_stop_price = None
        self._long_take_profit_price = None

    def _reset_short_state(self):
        self._short_entry_price = None
        self._short_stop_price = None
        self._short_take_profit_price = None

    def CreateClone(self):
        return vortex_indicator_duplex_strategy()
