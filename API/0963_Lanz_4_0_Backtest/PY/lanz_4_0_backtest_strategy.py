import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class lanz_4_0_backtest_strategy(Strategy):
    def __init__(self):
        super(lanz_4_0_backtest_strategy, self).__init__()
        self._swing_length = self.Param("SwingLength", 180) \
            .SetGreaterThanZero() \
            .SetDisplay("Swing Length", "Pivot swing length", "General")
        self._sl_buffer_points = self.Param("SlBufferPoints", 50.0) \
            .SetGreaterThanZero() \
            .SetDisplay("SL Buffer", "Stop loss buffer points", "Risk")
        self._risk_reward = self.Param("RiskReward", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("TP RR", "Take profit risk-reward", "Risk")
        self._max_entries = self.Param("MaxEntries", 45) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Entries", "Maximum entries per run", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._last_top = None
        self._last_bottom = None
        self._prev_high = None
        self._prev_low = None
        self._trend_dir = 0
        self._top_crossed = False
        self._bottom_crossed = False
        self._signal_buy = False
        self._signal_sell = False
        self._stop_price = 0.0
        self._take_profit_price = 0.0
        self._entries_executed = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(lanz_4_0_backtest_strategy, self).OnReseted()
        self._last_top = None
        self._last_bottom = None
        self._prev_high = None
        self._prev_low = None
        self._trend_dir = 0
        self._top_crossed = False
        self._bottom_crossed = False
        self._signal_buy = False
        self._signal_sell = False
        self._stop_price = 0.0
        self._take_profit_price = 0.0
        self._entries_executed = 0

    def OnStarted2(self, time):
        super(lanz_4_0_backtest_strategy, self).OnStarted2(time)
        highest = Highest()
        highest.Length = self._swing_length.Value
        lowest = Lowest()
        lowest.Length = self._swing_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, highest)
            self.DrawIndicator(area, lowest)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, high_val, low_val):
        if candle.State != CandleStates.Finished:
            return
        hv = float(high_val)
        lv = float(low_val)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        if self._last_top is None or hv != self._last_top:
            self._prev_high = self._last_top
            self._last_top = hv
            if self._prev_high is not None and hv < self._prev_high:
                self._trend_dir = -1
            self._top_crossed = False
        if self._last_bottom is None or lv != self._last_bottom:
            self._prev_low = self._last_bottom
            self._last_bottom = lv
            if self._prev_low is not None and lv > self._prev_low:
                self._trend_dir = 1
            self._bottom_crossed = False
        buy_signal = not self._top_crossed and self._prev_high is not None and close > self._prev_high
        sell_signal = not self._bottom_crossed and self._prev_low is not None and close < self._prev_low
        if self.Position == 0:
            self._signal_buy = False
            self._signal_sell = False
            self._stop_price = 0.0
            self._take_profit_price = 0.0
        if buy_signal and not self._signal_buy and self.Position == 0:
            self._signal_buy = True
        if sell_signal and not self._signal_sell and self.Position == 0:
            self._signal_sell = True
        buf = float(self._sl_buffer_points.Value) * 0.0001
        rr = float(self._risk_reward.Value)
        if self._signal_buy and self.Position == 0 and self._entries_executed < self._max_entries.Value:
            sl = low - buf
            tp = close + (close - sl) * rr
            self.BuyMarket()
            self._stop_price = sl
            self._take_profit_price = tp
            self._top_crossed = True
            self._entries_executed += 1
        elif self._signal_sell and self.Position == 0 and self._entries_executed < self._max_entries.Value:
            sl = high + buf
            tp = close - (sl - close) * rr
            self.SellMarket()
            self._stop_price = sl
            self._take_profit_price = tp
            self._bottom_crossed = True
            self._entries_executed += 1
        if self.Position > 0 and (self._stop_price > 0.0 or self._take_profit_price > 0.0):
            if low <= self._stop_price or high >= self._take_profit_price:
                self.SellMarket()
        elif self.Position < 0 and (self._stop_price > 0.0 or self._take_profit_price > 0.0):
            if high >= self._stop_price or low <= self._take_profit_price:
                self.BuyMarket()

    def CreateClone(self):
        return lanz_4_0_backtest_strategy()
