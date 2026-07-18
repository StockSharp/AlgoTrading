import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, Lowest
from StockSharp.Algo.Strategies import Strategy


class innocent_heikin_ashi_ethereum_strategy(Strategy):
    def __init__(self):
        super(innocent_heikin_ashi_ethereum_strategy, self).__init__()
        self._risk_reward = self.Param("RiskReward", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Risk/Reward", "Take profit to stop ratio", "Risk")
        self._confirmation_level = self.Param("ConfirmationLevel", 1) \
            .SetDisplay("Confirmation Level", "Number of red candles below EMA50 required before entry", "General")
        self._enable_moon_mode = self.Param("EnableMoonMode", True) \
            .SetDisplay("Enable Moon Mode", "Allow entries above EMA200", "General")
        self._show_sell_signals = self.Param("ShowSellSignals", True) \
            .SetDisplay("Show Sell Signals", "Close on sell signals", "General")
        self._show_bull_traps = self.Param("ShowBullTraps", True) \
            .SetDisplay("Show Bull Traps", "Close if next candle after buy is red", "General")
        self._show_bear_traps = self.Param("ShowBearTraps", True) \
            .SetDisplay("Show Bear Traps", "Close if sell signal fails", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._last_red_vector_below_ema50 = None
        self._last_buy_signal_index = None
        self._last_sell_signal_index = None
        self._red_count_under_ema50 = 0
        self._green_count_above_ema200 = 0
        self._bar_index = 0
        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(innocent_heikin_ashi_ethereum_strategy, self).OnReseted()
        self._last_red_vector_below_ema50 = None
        self._last_buy_signal_index = None
        self._last_sell_signal_index = None
        self._red_count_under_ema50 = 0
        self._green_count_above_ema200 = 0
        self._bar_index = 0
        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0

    def OnStarted2(self, time):
        super(innocent_heikin_ashi_ethereum_strategy, self).OnStarted2(time)
        ema50 = ExponentialMovingAverage()
        ema50.Length = 50
        ema200 = ExponentialMovingAverage()
        ema200.Length = 200
        lowest = Lowest()
        lowest.Length = 28
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema50, ema200, lowest, self.OnProcess).Start()

    def OnProcess(self, candle, ema50_val, ema200_val, lowest_val):
        if candle.State != CandleStates.Finished:
            return
        open_p = float(candle.OpenPrice)
        high_p = float(candle.HighPrice)
        low_p = float(candle.LowPrice)
        close_p = float(candle.ClosePrice)
        ema50 = float(ema50_val)
        ema200 = float(ema200_val)
        lowest_v = float(lowest_val)
        rr = float(self._risk_reward.Value)
        conf = self._confirmation_level.Value
        if self._bar_index == 0:
            ha_open = (open_p + close_p) / 2.0
            ha_close = (open_p + high_p + low_p + close_p) / 4.0
        else:
            ha_open = (self._prev_ha_open + self._prev_ha_close) / 2.0
            ha_close = (open_p + high_p + low_p + close_p) / 4.0
        is_green = ha_close > ha_open
        is_red = not is_green
        if is_red and close_p < ema50:
            self._red_count_under_ema50 += 1
        if is_red and open_p < ema50 and close_p < ema50:
            self._last_red_vector_below_ema50 = self._bar_index
        if is_green and open_p > ema200 and open_p > ema50:
            self._last_sell_signal_index = self._bar_index
        if is_green and close_p > ema200:
            self._green_count_above_ema200 += 1
        if self._last_red_vector_below_ema50 is not None and is_green:
            self._stop_price = lowest_v
            self._take_price = close_p + (close_p - self._stop_price) * rr
        can_buy = (self._last_red_vector_below_ema50 is not None and
                   is_green and open_p > ema50 and
                   (self._last_buy_signal_index is None or self._bar_index > self._last_buy_signal_index))
        if can_buy:
            if close_p < ema200 and self._red_count_under_ema50 >= conf:
                self.BuyMarket()
                self._last_buy_signal_index = self._bar_index
                self._last_red_vector_below_ema50 = None
                self._red_count_under_ema50 = 0
            elif self._enable_moon_mode.Value and close_p > ema200 and self._red_count_under_ema50 >= conf:
                self.BuyMarket()
                self._last_buy_signal_index = self._bar_index
                self._last_red_vector_below_ema50 = None
                self._red_count_under_ema50 = 0
        if self.Position > 0:
            if low_p <= self._stop_price or high_p >= self._take_price:
                self.SellMarket()
        if self._show_sell_signals.Value and self._last_sell_signal_index is not None and is_red:
            if open_p > ema200 and close_p > ema200 and self._bar_index == self._last_sell_signal_index + 1:
                if self._green_count_above_ema200 >= conf:
                    self.SellMarket()
                    self._last_sell_signal_index = None
                    self._green_count_above_ema200 = 0
        if self._show_bull_traps.Value and self._last_buy_signal_index is not None:
            if self._bar_index == self._last_buy_signal_index + 1 and is_red:
                self.SellMarket()
        if self._show_bear_traps.Value and self._last_sell_signal_index is not None:
            if self._bar_index == self._last_sell_signal_index + 1 and is_green:
                self.SellMarket()
                self._last_sell_signal_index = None
                self._green_count_above_ema200 = 0
        self._prev_ha_open = ha_open
        self._prev_ha_close = ha_close
        self._bar_index += 1

    def CreateClone(self):
        return innocent_heikin_ashi_ethereum_strategy()
