import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class mean_reversion_with_incremental_entry_strategy(Strategy):
    def __init__(self):
        super(mean_reversion_with_incremental_entry_strategy, self).__init__()
        self._ma_length = self.Param("MaLength", 40) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Length", "Moving average period", "Parameters")
        self._initial_percent = self.Param("InitialPercent", 3.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Initial Percent", "Percent from MA for first entry", "Parameters")
        self._percent_step = self.Param("PercentStep", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Percent Step", "Additional order percent step", "Parameters")
        self._max_entries_per_side = self.Param("MaxEntriesPerSide", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Entries Per Side", "Maximum incremental entries for each direction", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._last_buy_price = None
        self._last_sell_price = None
        self._buy_entries = 0
        self._sell_entries = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(mean_reversion_with_incremental_entry_strategy, self).OnReseted()
        self._last_buy_price = None
        self._last_sell_price = None
        self._buy_entries = 0
        self._sell_entries = 0

    def OnStarted(self, time):
        super(mean_reversion_with_incremental_entry_strategy, self).OnStarted(time)
        self._last_buy_price = None
        self._last_sell_price = None
        self._buy_entries = 0
        self._sell_entries = 0
        self._sma = SimpleMovingAverage()
        self._sma.Length = self._ma_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sma, self.OnProcess).Start()

    def _price_pct_diff(self, p1, p2):
        return abs(p1 - p2) / p2 * 100.0 if p2 != 0.0 else 0.0

    def OnProcess(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._sma.IsFormed:
            return
        mv = float(ma_value)
        low = float(candle.LowPrice)
        high = float(candle.HighPrice)
        close = float(candle.ClosePrice)
        init_pct = float(self._initial_percent.Value)
        step_pct = float(self._percent_step.Value)
        max_e = self._max_entries_per_side.Value
        if low < mv and self.Position <= 0:
            if self._last_buy_price is None:
                if self._buy_entries < max_e and self._price_pct_diff(low, mv) >= init_pct:
                    self.BuyMarket()
                    self._last_buy_price = low
                    self._buy_entries += 1
            elif self._buy_entries < max_e and low < self._last_buy_price and self._price_pct_diff(low, self._last_buy_price) >= step_pct:
                self.BuyMarket()
                self._last_buy_price = low
                self._buy_entries += 1
        if high > mv and self.Position >= 0:
            if self._last_sell_price is None:
                if self._sell_entries < max_e and self._price_pct_diff(high, mv) >= init_pct:
                    self.SellMarket()
                    self._last_sell_price = high
                    self._sell_entries += 1
            elif self._sell_entries < max_e and high > self._last_sell_price and self._price_pct_diff(high, self._last_sell_price) >= step_pct:
                self.SellMarket()
                self._last_sell_price = high
                self._sell_entries += 1
        if close >= mv and self.Position > 0:
            self.SellMarket()
            self._last_buy_price = None
            self._buy_entries = 0
        elif close <= mv and self.Position < 0:
            self.BuyMarket()
            self._last_sell_price = None
            self._sell_entries = 0
        if self.Position == 0:
            self._last_buy_price = None
            self._last_sell_price = None
            self._buy_entries = 0
            self._sell_entries = 0

    def CreateClone(self):
        return mean_reversion_with_incremental_entry_strategy()
