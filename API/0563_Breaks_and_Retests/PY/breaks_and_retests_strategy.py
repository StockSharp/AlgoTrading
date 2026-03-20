import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class breaks_and_retests_strategy(Strategy):
    def __init__(self):
        super(breaks_and_retests_strategy, self).__init__()
        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Number of bars for support/resistance", "Levels")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Initial stop loss", "Risk")
        self._profit_threshold_percent = self.Param("ProfitThresholdPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Profit Threshold %", "Activate trailing after profit", "Risk")
        self._trailing_stop_gap_percent = self.Param("TrailingStopGapPercent", 0.8) \
            .SetGreaterThanZero() \
            .SetDisplay("Trailing Gap %", "Gap for trailing stop", "Risk")
        self._max_hold_bars = self.Param("MaxHoldBars", 25) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Hold Bars", "Max bars to hold position", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Cooldown Bars", "Bars to wait after exit", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Candles for calculations", "General")
        self._highs = []
        self._lows = []
        self._prev_highest = 0.0
        self._prev_lowest = 0.0
        self._entry_price = 0.0
        self._trailing_stop_active = False
        self._highest_since_trailing = 0.0
        self._lowest_since_trailing = 0.0
        self._bars_in_position = 0
        self._bars_since_exit = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(breaks_and_retests_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._prev_highest = 0.0
        self._prev_lowest = 0.0
        self._entry_price = 0.0
        self._trailing_stop_active = False
        self._highest_since_trailing = 0.0
        self._lowest_since_trailing = 0.0
        self._bars_in_position = 0
        self._bars_since_exit = 0

    def OnStarted(self, time):
        super(breaks_and_retests_strategy, self).OnStarted(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _close_position(self):
        if self.Position > 0:
            self.SellMarket()
        elif self.Position < 0:
            self.BuyMarket()
        self._entry_price = 0.0
        self._trailing_stop_active = False
        self._bars_in_position = 0
        self._bars_since_exit = 0

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        lookback = self._lookback_period.Value
        self._highs.append(float(candle.HighPrice))
        self._lows.append(float(candle.LowPrice))
        if len(self._highs) > lookback + 1:
            self._highs = self._highs[1:]
        if len(self._lows) > lookback + 1:
            self._lows = self._lows[1:]
        if len(self._highs) <= lookback:
            return
        highest = max(self._highs[:-1])
        lowest = min(self._lows[:-1])
        close = float(candle.ClosePrice)
        if self.Position != 0:
            self._bars_in_position += 1
            self._handle_stop(candle)
            if self.Position != 0 and self._bars_in_position >= self._max_hold_bars.Value:
                self._close_position()
        else:
            self._bars_since_exit += 1
            if self._bars_since_exit >= self._cooldown_bars.Value and self._prev_highest > 0 and self._prev_lowest > 0:
                if close > self._prev_highest:
                    self.BuyMarket()
                    self._entry_price = close
                    self._trailing_stop_active = False
                    self._bars_in_position = 0
                elif close < self._prev_lowest:
                    self.SellMarket()
                    self._entry_price = close
                    self._trailing_stop_active = False
                    self._bars_in_position = 0
        self._prev_highest = highest
        self._prev_lowest = lowest

    def _handle_stop(self, candle):
        close = float(candle.ClosePrice)
        sl_pct = float(self._stop_loss_percent.Value)
        pt_pct = float(self._profit_threshold_percent.Value)
        tg_pct = float(self._trailing_stop_gap_percent.Value)
        if self.Position > 0 and self._entry_price > 0:
            profit_pct = (close - self._entry_price) / self._entry_price * 100.0
            if not self._trailing_stop_active and profit_pct >= pt_pct:
                self._trailing_stop_active = True
                self._highest_since_trailing = close
            if self._trailing_stop_active:
                if close > self._highest_since_trailing:
                    self._highest_since_trailing = close
                stop = self._highest_since_trailing * (1.0 - tg_pct / 100.0)
                if close <= stop:
                    self._close_position()
            else:
                stop = self._entry_price * (1.0 - sl_pct / 100.0)
                if close <= stop:
                    self._close_position()
        elif self.Position < 0 and self._entry_price > 0:
            profit_pct = (self._entry_price - close) / self._entry_price * 100.0
            if not self._trailing_stop_active and profit_pct >= pt_pct:
                self._trailing_stop_active = True
                self._lowest_since_trailing = close
            if self._trailing_stop_active:
                if close < self._lowest_since_trailing:
                    self._lowest_since_trailing = close
                stop = self._lowest_since_trailing * (1.0 + tg_pct / 100.0)
                if close >= stop:
                    self._close_position()
            else:
                stop = self._entry_price * (1.0 + sl_pct / 100.0)
                if close >= stop:
                    self._close_position()

    def CreateClone(self):
        return breaks_and_retests_strategy()
