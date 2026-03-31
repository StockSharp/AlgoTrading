import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ma_rsi_trigger_strategy(Strategy):
    def __init__(self):
        super(ma_rsi_trigger_strategy, self).__init__()
        self._fast_rsi_period = self.Param("FastRsiPeriod", 3) \
            .SetDisplay("Fast RSI Period", "Period of the fast RSI", "RSI")
        self._slow_rsi_period = self.Param("SlowRsiPeriod", 13) \
            .SetDisplay("Slow RSI Period", "Period of the slow RSI", "RSI")
        self._fast_ma_period = self.Param("FastMaPeriod", 5) \
            .SetDisplay("Fast EMA Period", "Period of the fast EMA", "MA")
        self._slow_ma_period = self.Param("SlowMaPeriod", 10) \
            .SetDisplay("Slow EMA Period", "Period of the slow EMA", "MA")
        self._allow_buy_entry = self.Param("AllowBuyEntry", True) \
            .SetDisplay("Allow Buy Entry", "Enable entering long positions", "General")
        self._allow_sell_entry = self.Param("AllowSellEntry", True) \
            .SetDisplay("Allow Sell Entry", "Enable entering short positions", "General")
        self._allow_long_exit = self.Param("AllowLongExit", True) \
            .SetDisplay("Allow Long Exit", "Enable exiting long positions", "General")
        self._allow_short_exit = self.Param("AllowShortExit", True) \
            .SetDisplay("Allow Short Exit", "Enable exiting short positions", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._min_rsi_spread = self.Param("MinRsiSpread", 6.0) \
            .SetDisplay("Minimum RSI Spread", "Minimum spread between fast and slow RSI values", "Filters")
        self._min_ma_spread_percent = self.Param("MinMaSpreadPercent", 0.0025) \
            .SetDisplay("Minimum EMA Spread %", "Minimum normalized spread between fast and slow EMA values", "Filters")
        self._cooldown_bars = self.Param("CooldownBars", 6) \
            .SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading")
        self._previous_trend = 0
        self._cooldown_remaining = 0

    @property
    def fast_rsi_period(self):
        return self._fast_rsi_period.Value

    @property
    def slow_rsi_period(self):
        return self._slow_rsi_period.Value

    @property
    def fast_ma_period(self):
        return self._fast_ma_period.Value

    @property
    def slow_ma_period(self):
        return self._slow_ma_period.Value

    @property
    def allow_buy_entry(self):
        return self._allow_buy_entry.Value

    @property
    def allow_sell_entry(self):
        return self._allow_sell_entry.Value

    @property
    def allow_long_exit(self):
        return self._allow_long_exit.Value

    @property
    def allow_short_exit(self):
        return self._allow_short_exit.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def min_rsi_spread(self):
        return self._min_rsi_spread.Value

    @property
    def min_ma_spread_percent(self):
        return self._min_ma_spread_percent.Value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(ma_rsi_trigger_strategy, self).OnReseted()
        self._previous_trend = 0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(ma_rsi_trigger_strategy, self).OnStarted2(time)
        fast_rsi = RelativeStrengthIndex()
        fast_rsi.Length = self.fast_rsi_period
        slow_rsi = RelativeStrengthIndex()
        slow_rsi.Length = self.slow_rsi_period
        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self.fast_ma_period
        slow_ma = ExponentialMovingAverage()
        slow_ma.Length = self.slow_ma_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_rsi, slow_rsi, fast_ma, slow_ma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ma)
            self.DrawIndicator(area, slow_ma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, fast_rsi_val, slow_rsi_val, fast_ma_val, slow_ma_val):
        if candle.State != CandleStates.Finished:
            return
        fast_rsi_val = float(fast_rsi_val)
        slow_rsi_val = float(slow_rsi_val)
        fast_ma_val = float(fast_ma_val)
        slow_ma_val = float(slow_ma_val)
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        normalized_ma_spread = abs(fast_ma_val - slow_ma_val) / slow_ma_val if slow_ma_val != 0 else 0.0
        rsi_spread = abs(fast_rsi_val - slow_rsi_val)
        trend = 0
        min_ma_sp = float(self.min_ma_spread_percent)
        min_rsi_sp = float(self.min_rsi_spread)
        if fast_ma_val > slow_ma_val and normalized_ma_spread >= min_ma_sp:
            trend += 1
        elif fast_ma_val < slow_ma_val and normalized_ma_spread >= min_ma_sp:
            trend -= 1
        if fast_rsi_val > slow_rsi_val and rsi_spread >= min_rsi_sp:
            trend += 1
        elif fast_rsi_val < slow_rsi_val and rsi_spread >= min_rsi_sp:
            trend -= 1
        if self._cooldown_remaining == 0:
            if self._previous_trend < 0 and trend > 0:
                if self.allow_short_exit and self.Position < 0:
                    self.BuyMarket()
                if self.allow_buy_entry and self.Position <= 0:
                    self.BuyMarket()
                    self._cooldown_remaining = self.cooldown_bars
            elif self._previous_trend > 0 and trend < 0:
                if self.allow_long_exit and self.Position > 0:
                    self.SellMarket()
                if self.allow_sell_entry and self.Position >= 0:
                    self.SellMarket()
                    self._cooldown_remaining = self.cooldown_bars
        if trend != 0:
            self._previous_trend = trend

    def CreateClone(self):
        return ma_rsi_trigger_strategy()
