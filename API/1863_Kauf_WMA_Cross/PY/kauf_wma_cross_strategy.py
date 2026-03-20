import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import KaufmanAdaptiveMovingAverage, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class kauf_wma_cross_strategy(Strategy):
    def __init__(self):
        super(kauf_wma_cross_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle type", "Type of candles", "General")
        self._ama_period = self.Param("AmaPeriod", 9) \
            .SetDisplay("AMA length", "Kaufman AMA period", "Indicators")
        self._fast_period = self.Param("FastPeriod", 2) \
            .SetDisplay("Fast EMA", "Fast smoothing period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 30) \
            .SetDisplay("Slow EMA", "Slow smoothing period", "Indicators")
        self._wma_period = self.Param("WmaPeriod", 13) \
            .SetDisplay("WMA length", "Weighted MA period", "Indicators")
        self._buy_open = self.Param("BuyOpen", True) \
            .SetDisplay("Open long", "Allow opening long position", "Signals")
        self._sell_open = self.Param("SellOpen", True) \
            .SetDisplay("Open short", "Allow opening short position", "Signals")
        self._buy_close = self.Param("BuyClose", True) \
            .SetDisplay("Close long", "Allow closing long on sell signal", "Signals")
        self._sell_close = self.Param("SellClose", True) \
            .SetDisplay("Close short", "Allow closing short on buy signal", "Signals")
        self._min_spread_percent = self.Param("MinSpreadPercent", 0.0008) \
            .SetDisplay("Minimum Spread %", "Minimum normalized spread between AMA and WMA", "Filters")
        self._cooldown_bars = self.Param("CooldownBars", 4) \
            .SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading")
        self._prev_kama = 0.0
        self._prev_wma = 0.0
        self._is_first = True
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def ama_period(self):
        return self._ama_period.Value
    @property
    def fast_period(self):
        return self._fast_period.Value
    @property
    def slow_period(self):
        return self._slow_period.Value
    @property
    def wma_period(self):
        return self._wma_period.Value
    @property
    def buy_open(self):
        return self._buy_open.Value
    @property
    def sell_open(self):
        return self._sell_open.Value
    @property
    def buy_close(self):
        return self._buy_close.Value
    @property
    def sell_close(self):
        return self._sell_close.Value
    @property
    def min_spread_percent(self):
        return self._min_spread_percent.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(kauf_wma_cross_strategy, self).OnReseted()
        self._prev_kama = 0.0
        self._prev_wma = 0.0
        self._is_first = True
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(kauf_wma_cross_strategy, self).OnStarted(time)
        kama = KaufmanAdaptiveMovingAverage()
        kama.Length = self.ama_period
        kama.FastSCPeriod = self.fast_period
        kama.SlowSCPeriod = self.slow_period
        wma = WeightedMovingAverage()
        wma.Length = self.wma_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(kama, wma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, kama)
            self.DrawIndicator(area, wma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, kama_value, wma_value):
        if candle.State != CandleStates.Finished:
            return
        kama_value = float(kama_value)
        wma_value = float(wma_value)
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        if self._is_first:
            self._prev_kama = kama_value
            self._prev_wma = wma_value
            self._is_first = False
            return
        close = float(candle.ClosePrice)
        spread_percent = abs(kama_value - wma_value) / close if close != 0 else 0.0
        min_sp = float(self.min_spread_percent)
        cross_up = self._prev_kama <= self._prev_wma and kama_value > wma_value and spread_percent >= min_sp
        cross_down = self._prev_kama >= self._prev_wma and kama_value < wma_value and spread_percent >= min_sp
        if self._cooldown_remaining == 0:
            if cross_up:
                if self.sell_close and self.Position < 0:
                    self.BuyMarket()
                if self.buy_open and self.Position <= 0:
                    self.BuyMarket()
                    self._cooldown_remaining = self.cooldown_bars
            elif cross_down:
                if self.buy_close and self.Position > 0:
                    self.SellMarket()
                if self.sell_open and self.Position >= 0:
                    self.SellMarket()
                    self._cooldown_remaining = self.cooldown_bars
        self._prev_kama = kama_value
        self._prev_wma = wma_value

    def CreateClone(self):
        return kauf_wma_cross_strategy()
