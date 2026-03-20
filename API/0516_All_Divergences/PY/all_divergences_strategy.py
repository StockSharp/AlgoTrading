import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class all_divergences_strategy(Strategy):
    def __init__(self):
        super(all_divergences_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._ma_length = self.Param("MaLength", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Length", "Length of moving average", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI period", "Indicators")
        self._lookback_bars = self.Param("LookbackBars", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Bars", "Bars to look back for divergence", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._prev_low_price = 0.0
        self._prev_low_rsi = 0.0
        self._prev_high_price = 0.0
        self._prev_high_rsi = 0.0
        self._cur_low_price = 1e18
        self._cur_low_rsi = 100.0
        self._cur_high_price = 0.0
        self._cur_high_rsi = 0.0
        self._bars_since_extreme = 0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    @cooldown_bars.setter
    def cooldown_bars(self, value):
        self._cooldown_bars.Value = value

    def OnReseted(self):
        super(all_divergences_strategy, self).OnReseted()
        self._prev_low_price = 0.0
        self._prev_low_rsi = 0.0
        self._prev_high_price = 0.0
        self._prev_high_rsi = 0.0
        self._cur_low_price = 1e18
        self._cur_low_rsi = 100.0
        self._cur_high_price = 0.0
        self._cur_high_rsi = 0.0
        self._bars_since_extreme = 0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(all_divergences_strategy, self).OnStarted(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        ma = SimpleMovingAverage()
        ma.Length = self._ma_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, ma, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, rsi_val, ma_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        rsi_v = float(rsi_val)
        ma_v = float(ma_val)
        low = float(candle.LowPrice)
        high = float(candle.HighPrice)
        close = float(candle.ClosePrice)
        if low < self._cur_low_price:
            self._cur_low_price = low
            self._cur_low_rsi = rsi_v
        if high > self._cur_high_price:
            self._cur_high_price = high
            self._cur_high_rsi = rsi_v
        self._bars_since_extreme += 1
        lookback = self._lookback_bars.Value
        if self._bars_since_extreme >= lookback:
            self._prev_low_price = self._cur_low_price
            self._prev_low_rsi = self._cur_low_rsi
            self._prev_high_price = self._cur_high_price
            self._prev_high_rsi = self._cur_high_rsi
            self._cur_low_price = 1e18
            self._cur_low_rsi = 100.0
            self._cur_high_price = 0.0
            self._cur_high_rsi = 0.0
            self._bars_since_extreme = 0
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return
        if self._prev_low_price == 0 or self._prev_high_price == 0:
            return
        bullish_div = low < self._prev_low_price and rsi_v > self._prev_low_rsi and close > ma_v
        bearish_div = high > self._prev_high_price and rsi_v < self._prev_high_rsi and close < ma_v
        if bullish_div and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif bearish_div and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars

    def CreateClone(self):
        return all_divergences_strategy()
