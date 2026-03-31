import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class keltner_channel_golden_cross_strategy(Strategy):
    def __init__(self):
        super(keltner_channel_golden_cross_strategy, self).__init__()
        self._ma_length = self.Param("MaLength", 21) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Length", "Length for basis moving average", "General")
        self._entry_atr_mult = self.Param("EntryAtrMultiplier", 1.0) \
            .SetDisplay("Entry ATR Mult", "ATR multiplier for entry channel", "Risk")
        self._profit_atr_mult = self.Param("ProfitAtrMultiplier", 4.0) \
            .SetDisplay("Profit Mult", "ATR multiplier for take profit", "Risk")
        self._exit_atr_mult = self.Param("ExitAtrMultiplier", -1.0) \
            .SetDisplay("Exit Mult", "ATR multiplier for stop", "Risk")
        self._short_ma_length = self.Param("ShortMaLength", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Short MA", "Short moving average length", "Trend")
        self._long_ma_length = self.Param("LongMaLength", 200) \
            .SetGreaterThanZero() \
            .SetDisplay("Long MA", "Long moving average length", "Trend")
        self._max_entries = self.Param("MaxEntries", 45) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Entries", "Maximum entries per run", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 12000) \
            .SetGreaterThanZero() \
            .SetDisplay("Cooldown Bars", "Minimum bars between entries", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._entries_executed = 0
        self._bars_since_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(keltner_channel_golden_cross_strategy, self).OnReseted()
        self._entries_executed = 0
        self._bars_since_signal = 0

    def OnStarted2(self, time):
        super(keltner_channel_golden_cross_strategy, self).OnStarted2(time)
        self._entries_executed = 0
        self._bars_since_signal = self._cooldown_bars.Value
        basis = SimpleMovingAverage()
        basis.Length = self._ma_length.Value
        entry_atr = AverageTrueRange()
        entry_atr.Length = 10
        atr = AverageTrueRange()
        atr.Length = self._ma_length.Value
        short_ma = ExponentialMovingAverage()
        short_ma.Length = self._short_ma_length.Value
        long_ma = ExponentialMovingAverage()
        long_ma.Length = self._long_ma_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(basis, entry_atr, atr, short_ma, long_ma, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, basis)
            self.DrawIndicator(area, short_ma)
            self.DrawIndicator(area, long_ma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, basis_val, entry_atr_val, atr_val, short_ma_val, long_ma_val):
        if candle.State != CandleStates.Finished:
            return
        self._bars_since_signal += 1
        bv = float(basis_val)
        ea = float(entry_atr_val)
        av = float(atr_val)
        sm = float(short_ma_val)
        lm = float(long_ma_val)
        price = float(candle.ClosePrice)
        entry_mult = float(self._entry_atr_mult.Value)
        profit_mult = float(self._profit_atr_mult.Value)
        exit_mult = float(self._exit_atr_mult.Value)
        upper_entry = bv + entry_mult * ea
        lower_entry = bv - entry_mult * ea
        take_profit = bv + profit_mult * av
        take_profit_short = bv - profit_mult * av
        stop_long = bv + exit_mult * av
        stop_short = bv - exit_mult * av
        long_trend = sm > lm
        short_trend = sm < lm
        if self._bars_since_signal < self._cooldown_bars.Value:
            return
        if self.Position > 0:
            if price >= take_profit or price <= stop_long:
                self.SellMarket()
                self._bars_since_signal = 0
            return
        if self.Position < 0:
            if price <= take_profit_short or price >= stop_short:
                self.BuyMarket()
                self._bars_since_signal = 0
            return
        if self._entries_executed >= self._max_entries.Value:
            return
        if long_trend and price > upper_entry:
            self.BuyMarket()
            self._entries_executed += 1
            self._bars_since_signal = 0
        elif short_trend and price < lower_entry:
            self.SellMarket()
            self._entries_executed += 1
            self._bars_since_signal = 0

    def CreateClone(self):
        return keltner_channel_golden_cross_strategy()
