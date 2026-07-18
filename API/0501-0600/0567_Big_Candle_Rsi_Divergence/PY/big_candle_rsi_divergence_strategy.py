import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class big_candle_rsi_divergence_strategy(Strategy):
    def __init__(self):
        super(big_candle_rsi_divergence_strategy, self).__init__()
        self._stop_loss_percent = self.Param("StopLossPercent", 0.3) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Initial stop loss percent", "Risk")
        self._trail_start_percent = self.Param("TrailStartPercent", 0.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Trail Start %", "Profit percent to activate trailing", "Risk")
        self._trail_distance_percent = self.Param("TrailDistancePercent", 0.2) \
            .SetGreaterThanZero() \
            .SetDisplay("Trail Distance %", "Trailing stop distance percent", "Risk")
        self._lookback_bars = self.Param("LookbackBars", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Bars", "Number of bars for big candle comparison", "Strategy")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._bodies = []
        self._entry_price = 0.0
        self._highest_since_entry = 0.0
        self._lowest_since_entry = 0.0
        self._trailing_active = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(big_candle_rsi_divergence_strategy, self).OnReseted()
        self._bodies = []
        self._entry_price = 0.0
        self._highest_since_entry = 0.0
        self._lowest_since_entry = 0.0
        self._trailing_active = False

    def OnStarted2(self, time):
        super(big_candle_rsi_divergence_strategy, self).OnStarted2(time)
        rsi_fast = RelativeStrengthIndex()
        rsi_fast.Length = 5
        rsi_slow = RelativeStrengthIndex()
        rsi_slow.Length = 14
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi_fast, rsi_slow, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, rsi_fast_val, rsi_slow_val):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        body = abs(close - open_p)
        lookback = self._lookback_bars.Value
        self._bodies.append(body)
        if len(self._bodies) > lookback + 1:
            self._bodies = self._bodies[1:]
        if len(self._bodies) <= lookback:
            return
        is_biggest = True
        for i in range(len(self._bodies) - 1):
            if self._bodies[i] >= body:
                is_biggest = False
                break
        is_bullish = close > open_p
        is_bearish = close < open_p
        rsi_divergence = float(rsi_fast_val) - float(rsi_slow_val)
        sl_pct = float(self._stop_loss_percent.Value)
        ts_pct = float(self._trail_start_percent.Value)
        td_pct = float(self._trail_distance_percent.Value)
        if self.Position == 0:
            if is_biggest and is_bullish and rsi_divergence > 0:
                self.BuyMarket()
                self._entry_price = close
                self._highest_since_entry = close
                self._trailing_active = False
            elif is_biggest and is_bearish and rsi_divergence < 0:
                self.SellMarket()
                self._entry_price = close
                self._lowest_since_entry = close
                self._trailing_active = False
        elif self.Position > 0 and self._entry_price > 0:
            if close > self._highest_since_entry:
                self._highest_since_entry = close
            profit_pct = (close - self._entry_price) / self._entry_price * 100.0
            if not self._trailing_active and profit_pct >= ts_pct:
                self._trailing_active = True
            if self._trailing_active:
                stop = self._highest_since_entry * (1.0 - td_pct / 100.0)
                if close <= stop:
                    self.SellMarket()
                    self._entry_price = 0.0
                    self._trailing_active = False
            else:
                stop = self._entry_price * (1.0 - sl_pct / 100.0)
                if close <= stop:
                    self.SellMarket()
                    self._entry_price = 0.0
        elif self.Position < 0 and self._entry_price > 0:
            if close < self._lowest_since_entry:
                self._lowest_since_entry = close
            profit_pct = (self._entry_price - close) / self._entry_price * 100.0
            if not self._trailing_active and profit_pct >= ts_pct:
                self._trailing_active = True
            if self._trailing_active:
                stop = self._lowest_since_entry * (1.0 + td_pct / 100.0)
                if close >= stop:
                    self.BuyMarket()
                    self._entry_price = 0.0
                    self._trailing_active = False
            else:
                stop = self._entry_price * (1.0 + sl_pct / 100.0)
                if close >= stop:
                    self.BuyMarket()
                    self._entry_price = 0.0

    def CreateClone(self):
        return big_candle_rsi_divergence_strategy()
