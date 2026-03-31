import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class mutanabby_ai_algo_pro_strategy(Strategy):
    def __init__(self):
        super(mutanabby_ai_algo_pro_strategy, self).__init__()
        self._candle_stability_index = self.Param("CandleStabilityIndex", 0.5) \
            .SetDisplay("Candle Stability Index", "Minimum body/true range ratio", "Technical")
        self._rsi_index = self.Param("RsiIndex", 50) \
            .SetDisplay("RSI Index", "RSI threshold for entries", "Technical")
        self._candle_delta_length = self.Param("CandleDeltaLength", 5) \
            .SetDisplay("Candle Delta Length", "Bars for price comparison", "Technical")
        self._disable_repeating_signals = self.Param("DisableRepeatingSignals", False) \
            .SetDisplay("Disable Repeating Signals", "Avoid consecutive identical signals", "Technical")
        self._enable_stop_loss = self.Param("EnableStopLoss", True) \
            .SetDisplay("Enable Stop Loss", "Activate stop loss", "Risk Management")
        self._entry_stop_loss_percent = self.Param("EntryStopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Entry Stop Loss %", "Stop loss percent from entry", "Risk Management")
        self._lookback_period = self.Param("LookbackPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Bars for lowest low stop", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_open = 0.0
        self._prev_close = 0.0
        self._close_queue = []
        self._low_queue = []
        self._lowest_low = 999999999.0
        self._last_signal = ""
        self._stop_loss_price = 0.0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(mutanabby_ai_algo_pro_strategy, self).OnReseted()
        self._prev_open = 0.0
        self._prev_close = 0.0
        self._close_queue = []
        self._low_queue = []
        self._lowest_low = 999999999.0
        self._last_signal = ""
        self._stop_loss_price = 0.0
        self._entry_price = 0.0

    def OnStarted2(self, time):
        super(mutanabby_ai_algo_pro_strategy, self).OnStarted2(time)
        self._prev_open = 0.0
        self._prev_close = 0.0
        self._close_queue = []
        self._low_queue = []
        self._lowest_low = 999999999.0
        self._last_signal = ""
        self._stop_loss_price = 0.0
        self._entry_price = 0.0
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = 14
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        opn = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        rsi_input = DecimalIndicatorValue(self._rsi, candle.ClosePrice, candle.OpenTime)
        rsi_input.IsFinal = True
        rsi_result = self._rsi.Process(rsi_input)
        if not self._rsi.IsFormed:
            self._prev_open = opn
            self._prev_close = close
            self._close_queue.append(close)
            cdl = self._candle_delta_length.Value
            if len(self._close_queue) > cdl:
                self._close_queue.pop(0)
            self._low_queue.append(low)
            lp = self._lookback_period.Value
            if len(self._low_queue) > lp:
                self._low_queue.pop(0)
            self._lowest_low = min(self._low_queue) if self._low_queue else 999999999.0
            return
        rsi_value = float(rsi_result)
        lp = self._lookback_period.Value
        if len(self._low_queue) >= lp:
            self._low_queue.pop(0)
        self._low_queue.append(low)
        self._lowest_low = min(self._low_queue) if self._low_queue else 999999999.0
        cdl = self._candle_delta_length.Value
        price_n = self._close_queue[0] if len(self._close_queue) == cdl else None
        true_range = high - low
        csi = float(self._candle_stability_index.Value)
        stable_candle = true_range > 0.0 and abs(close - opn) / true_range > csi
        bullish_engulfing = self._prev_close < self._prev_open and close > opn and close > self._prev_open
        rsi_idx = self._rsi_index.Value
        rsi_below = rsi_value < rsi_idx
        decrease_over = price_n is not None and close < price_n
        entry_signal = bullish_engulfing and stable_candle and rsi_below and decrease_over
        bearish_engulfing = self._prev_close > self._prev_open and close < opn and close < self._prev_open
        rsi_above = rsi_value > (100 - rsi_idx)
        increase_over = price_n is not None and close > price_n
        exit_signal = bearish_engulfing and stable_candle and rsi_above and increase_over
        disable_rep = self._disable_repeating_signals.Value
        if entry_signal and self.Position <= 0 and (not disable_rep or self._last_signal != "buy"):
            self.BuyMarket()
            self._entry_price = close
            if self._enable_stop_loss.Value:
                sl_pct = float(self._entry_stop_loss_percent.Value)
                self._stop_loss_price = self._entry_price * (1.0 - sl_pct / 100.0)
            self._last_signal = "buy"
        if exit_signal and self.Position > 0 and (not disable_rep or self._last_signal != "sell"):
            self.SellMarket()
            self._last_signal = "sell"
        if self._enable_stop_loss.Value and self.Position > 0 and self._stop_loss_price > 0.0 and close <= self._stop_loss_price:
            self.SellMarket()
            self._last_signal = "sell"
        self._close_queue.append(close)
        if len(self._close_queue) > cdl:
            self._close_queue.pop(0)
        self._prev_open = opn
        self._prev_close = close

    def CreateClone(self):
        return mutanabby_ai_algo_pro_strategy()
