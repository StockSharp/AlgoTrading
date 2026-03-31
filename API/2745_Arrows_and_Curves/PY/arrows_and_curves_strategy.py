import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class arrows_and_curves_strategy(Strategy):
    """
    Port of Arrows and Curves EA. Uses custom channel computation for signals
    with SL/TP and trailing stop.
    """

    def __init__(self):
        super(arrows_and_curves_strategy, self).__init__()

        self._stop_loss_pips = self.Param("StopLossPips", 50) \
            .SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 50) \
            .SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 0) \
            .SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk")
        self._trailing_step_pips = self.Param("TrailingStepPips", 5) \
            .SetDisplay("Trailing Step (pips)", "Minimum movement before trailing updates", "Risk")
        self._ssp_period = self.Param("SspPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("SSP", "Lookback period of the custom channel", "Indicator")
        self._channel_percent = self.Param("ChannelPercent", 0) \
            .SetDisplay("Channel %", "Outer channel percentage", "Indicator")
        self._channel_stop_percent = self.Param("ChannelStopPercent", 30) \
            .SetDisplay("Channel Stop %", "Inner channel percentage", "Indicator")
        self._relay_shift = self.Param("RelayShift", 10) \
            .SetDisplay("Relay", "Shift used by the indicator", "Indicator")
        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Candles used for processing", "General")

        self._high_series = []
        self._low_series = []
        self._close_series = []
        self._uptrend = False
        self._uptrend2 = False
        self._previous_sell_arrow = False
        self._previous_buy_arrow = False
        self._entry_price = None
        self._stop_price = None
        self._take_price = None

    @property
    def StopLossPips(self): return self._stop_loss_pips.Value
    @StopLossPips.setter
    def StopLossPips(self, v): self._stop_loss_pips.Value = v
    @property
    def TakeProfitPips(self): return self._take_profit_pips.Value
    @TakeProfitPips.setter
    def TakeProfitPips(self, v): self._take_profit_pips.Value = v
    @property
    def TrailingStopPips(self): return self._trailing_stop_pips.Value
    @TrailingStopPips.setter
    def TrailingStopPips(self, v): self._trailing_stop_pips.Value = v
    @property
    def TrailingStepPips(self): return self._trailing_step_pips.Value
    @TrailingStepPips.setter
    def TrailingStepPips(self, v): self._trailing_step_pips.Value = v
    @property
    def SspPeriod(self): return self._ssp_period.Value
    @SspPeriod.setter
    def SspPeriod(self, v): self._ssp_period.Value = v
    @property
    def ChannelPercent(self): return self._channel_percent.Value
    @ChannelPercent.setter
    def ChannelPercent(self, v): self._channel_percent.Value = v
    @property
    def ChannelStopPercent(self): return self._channel_stop_percent.Value
    @ChannelStopPercent.setter
    def ChannelStopPercent(self, v): self._channel_stop_percent.Value = v
    @property
    def RelayShift(self): return self._relay_shift.Value
    @RelayShift.setter
    def RelayShift(self, v): self._relay_shift.Value = v
    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v

    def OnReseted(self):
        super(arrows_and_curves_strategy, self).OnReseted()
        self._high_series = []
        self._low_series = []
        self._close_series = []
        self._uptrend = False
        self._uptrend2 = False
        self._previous_sell_arrow = False
        self._previous_buy_arrow = False
        self._entry_price = None
        self._stop_price = None
        self._take_price = None

    def OnStarted2(self, time):
        super(arrows_and_curves_strategy, self).OnStarted2(time)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._add_candle(candle)

        should_open_buy = self._previous_sell_arrow
        should_open_sell = self._previous_buy_arrow

        if self.Position == 0:
            if should_open_buy:
                self._open_long(candle)
            elif should_open_sell:
                self._open_short(candle)
        else:
            if self.Position > 0 and should_open_sell:
                self._close_and_reset()
            elif self.Position < 0 and should_open_buy:
                self._close_and_reset()
            self._update_trailing(candle)
            self._check_risk_exits(candle)

        result = self._try_compute_signals()
        if result is None:
            self._previous_buy_arrow = False
            self._previous_sell_arrow = False
        else:
            self._previous_buy_arrow = result[0]
            self._previous_sell_arrow = result[1]

    def _open_long(self, candle):
        close = float(candle.ClosePrice)
        step = float(self.Security.PriceStep or 0.01)
        self.BuyMarket(self.Volume)
        self._entry_price = close
        self._stop_price = close - self.StopLossPips * step if self.StopLossPips > 0 else None
        self._take_price = close + self.TakeProfitPips * step if self.TakeProfitPips > 0 else None

    def _open_short(self, candle):
        close = float(candle.ClosePrice)
        step = float(self.Security.PriceStep or 0.01)
        self.SellMarket(self.Volume)
        self._entry_price = close
        self._stop_price = close + self.StopLossPips * step if self.StopLossPips > 0 else None
        self._take_price = close - self.TakeProfitPips * step if self.TakeProfitPips > 0 else None

    def _update_trailing(self, candle):
        if self.TrailingStopPips <= 0 or self._entry_price is None:
            return
        step = float(self.Security.PriceStep or 0.01)
        distance = self.TrailingStopPips * step
        if distance <= 0:
            return
        trail_step = self.TrailingStepPips * step
        close = float(candle.ClosePrice)

        if self.Position > 0:
            gain = close - self._entry_price
            if gain > distance + trail_step:
                new_stop = close - distance
                if self._stop_price is None or self._stop_price < new_stop - trail_step:
                    self._stop_price = new_stop
        elif self.Position < 0:
            gain = self._entry_price - close
            if gain > distance + trail_step:
                new_stop = close + distance
                if self._stop_price is None or self._stop_price > new_stop + trail_step:
                    self._stop_price = new_stop

    def _check_risk_exits(self, candle):
        if self.Position > 0:
            stop_hit = self._stop_price is not None and float(candle.LowPrice) <= self._stop_price
            take_hit = self._take_price is not None and float(candle.HighPrice) >= self._take_price
            if stop_hit or take_hit:
                self._close_and_reset()
        elif self.Position < 0:
            stop_hit = self._stop_price is not None and float(candle.HighPrice) >= self._stop_price
            take_hit = self._take_price is not None and float(candle.LowPrice) <= self._take_price
            if stop_hit or take_hit:
                self._close_and_reset()

    def _close_and_reset(self):
        if self.Position > 0:
            self.SellMarket(self.Position)
        elif self.Position < 0:
            self.BuyMarket(Math.Abs(self.Position))
        self._entry_price = None
        self._stop_price = None
        self._take_price = None

    def _add_candle(self, candle):
        self._high_series.append(float(candle.HighPrice))
        self._low_series.append(float(candle.LowPrice))
        self._close_series.append(float(candle.ClosePrice))
        max_count = self.RelayShift + self.SspPeriod + 5
        if len(self._high_series) > max_count:
            excess = len(self._high_series) - max_count
            self._high_series = self._high_series[excess:]
            self._low_series = self._low_series[excess:]
            self._close_series = self._close_series[excess:]

    def _get_series_value(self, series, index):
        target = len(series) - 1 - index
        return series[target] if target >= 0 else 0.0

    def _try_compute_signals(self):
        if len(self._close_series) <= 1:
            return None

        start = self.RelayShift + 1
        end = start + self.SspPeriod

        if end > len(self._high_series) or end > len(self._low_series):
            return None

        close = self._get_series_value(self._close_series, 1)

        high = float('-inf')
        low = float('inf')
        for i in range(start, end):
            h = self._get_series_value(self._high_series, i)
            l = self._get_series_value(self._low_series, i)
            if h > high:
                high = h
            if l < low:
                low = l

        rng = high - low
        smax = high - (low - high) * self.ChannelPercent / 100.0
        smin = low + rng * self.ChannelPercent / 100.0
        inner_percent = self.ChannelPercent + self.ChannelStopPercent
        smax2 = high - rng * inner_percent / 100.0
        smin2 = low + rng * inner_percent / 100.0

        uptrend = self._uptrend
        uptrend2 = self._uptrend2
        old = uptrend
        old2 = uptrend2

        buy_signal = False
        sell_signal = False

        if close < smin and close < smax and uptrend2:
            uptrend = False
        if close > smax and close > smin and not uptrend2:
            uptrend = True
        if (close > smax2 or close > smin2) and not uptrend:
            uptrend2 = False
        if (close < smin2 or close < smax2) and uptrend:
            uptrend2 = True
        if close < smin and close < smax and not uptrend2:
            sell_signal = True
            uptrend2 = True
        if close > smax and close > smin and uptrend2:
            buy_signal = True
            uptrend2 = False
        if uptrend != old and not uptrend:
            sell_signal = True
        if uptrend != old and uptrend:
            buy_signal = True

        self._uptrend = uptrend
        self._uptrend2 = uptrend2

        return (buy_signal, sell_signal)

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return arrows_and_curves_strategy()
