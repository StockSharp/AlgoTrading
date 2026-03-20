import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class color_fisher_m11_strategy(Strategy):
    """Color Fisher Transform strategy with configurable entries/exits and SL/TP protection."""

    def __init__(self):
        super(color_fisher_m11_strategy, self).__init__()

        self._range_periods = self.Param("RangePeriods", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Range Periods", "Lookback window for highs and lows", "Indicator")
        self._price_smoothing = self.Param("PriceSmoothing", 0.3) \
            .SetDisplay("Price Smoothing", "Smoothing factor before Fisher transform", "Indicator")
        self._index_smoothing = self.Param("IndexSmoothing", 0.3) \
            .SetDisplay("Index Smoothing", "Smoothing factor after Fisher transform", "Indicator")
        self._high_level = self.Param("HighLevel", 0.05) \
            .SetDisplay("High Level", "Upper level for bullish color", "Indicator")
        self._low_level = self.Param("LowLevel", -0.05) \
            .SetDisplay("Low Level", "Lower level for bearish color", "Indicator")
        self._signal_bar = self.Param("SignalBar", 0) \
            .SetDisplay("Signal Bar", "Bars to delay signal execution", "Trading")
        self._enable_buy_entry = self.Param("EnableBuyEntry", True) \
            .SetDisplay("Enable Buy Entry", "Allow opening long positions", "Trading")
        self._enable_sell_entry = self.Param("EnableSellEntry", True) \
            .SetDisplay("Enable Sell Entry", "Allow opening short positions", "Trading")
        self._enable_buy_exit = self.Param("EnableBuyExit", True) \
            .SetDisplay("Enable Buy Exit", "Allow closing long positions", "Trading")
        self._enable_sell_exit = self.Param("EnableSellExit", True) \
            .SetDisplay("Enable Sell Exit", "Allow closing short positions", "Trading")
        self._stop_loss_points = self.Param("StopLossPoints", 1000) \
            .SetDisplay("Stop Loss (pts)", "Protective stop distance in price steps", "Protection")
        self._take_profit_points = self.Param("TakeProfitPoints", 2000) \
            .SetDisplay("Take Profit (pts)", "Target distance in price steps", "Protection")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for indicator calculation", "General")

        # Fisher indicator state
        self._highs = []
        self._lows = []
        self._prev_fish = 0.0
        self._prev_index = 0.0
        self._has_prev = False
        self._fisher_count = 0
        self._last_color = 2

        # Color history (most recent first)
        self._color_history = []

    @property
    def RangePeriods(self):
        return int(self._range_periods.Value)
    @property
    def PriceSmoothing(self):
        return float(self._price_smoothing.Value)
    @property
    def IndexSmoothing(self):
        return float(self._index_smoothing.Value)
    @property
    def HighLevel(self):
        return float(self._high_level.Value)
    @property
    def LowLevel(self):
        return float(self._low_level.Value)
    @property
    def SignalBar(self):
        return int(self._signal_bar.Value)
    @property
    def EnableBuyEntry(self):
        return self._enable_buy_entry.Value
    @property
    def EnableSellEntry(self):
        return self._enable_sell_entry.Value
    @property
    def EnableBuyExit(self):
        return self._enable_buy_exit.Value
    @property
    def EnableSellExit(self):
        return self._enable_sell_exit.Value
    @property
    def StopLossPoints(self):
        return int(self._stop_loss_points.Value)
    @property
    def TakeProfitPoints(self):
        return int(self._take_profit_points.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(color_fisher_m11_strategy, self).OnStarted(time)

        self._highs = []
        self._lows = []
        self._prev_fish = 0.0
        self._prev_index = 0.0
        self._has_prev = False
        self._fisher_count = 0
        self._last_color = 2
        self._color_history = []

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 1.0
        sl = self.StopLossPoints * step if self.StopLossPoints > 0 else 0.0
        tp = self.TakeProfitPoints * step if self.TakeProfitPoints > 0 else 0.0
        if sl > 0 or tp > 0:
            self.StartProtection(
                stopLoss=Unit(sl, UnitTypes.Absolute) if sl > 0 else None,
                takeProfit=Unit(tp, UnitTypes.Absolute) if tp > 0 else None
            )

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _calc_fisher(self, candle):
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        self._highs.append(h)
        self._lows.append(lo)
        self._fisher_count += 1

        length = max(1, self.RangePeriods)
        while len(self._highs) > length:
            self._highs.pop(0)
            self._lows.pop(0)

        highest = max(self._highs)
        lowest = min(self._lows)

        sec = self.Security
        min_range = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 0.0001
        rng = highest - lowest
        if rng < min_range:
            rng = min_range

        mid = (h + lo) / 2.0
        price_loc = (mid - lowest) / rng if rng != 0 else 0.99
        price_loc = 2.0 * price_loc - 1.0

        prev_fish = self._prev_fish if self._has_prev else price_loc
        fish = self.PriceSmoothing * prev_fish + (1.0 - self.PriceSmoothing) * price_loc
        smoothed = min(max(fish, -0.99), 0.99)

        diff = 1.0 - smoothed
        if diff == 0:
            fisher_raw = 0.0
        else:
            ratio = (1.0 + smoothed) / diff
            fisher_raw = math.log(ratio)

        prev_idx = self._prev_index if self._has_prev else fisher_raw
        value = self.IndexSmoothing * prev_idx + (1.0 - self.IndexSmoothing) * fisher_raw

        self._prev_fish = fish
        self._prev_index = value
        self._has_prev = True

        is_formed = self._fisher_count >= length

        color = 2
        if value > 0:
            color = 0 if value > self.HighLevel else 1
        elif value < 0:
            color = 4 if value < self.LowLevel else 3

        self._last_color = color
        return color, is_formed

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        color, is_formed = self._calc_fisher(candle)
        self._color_history.insert(0, color)
        mx = max(self.SignalBar + 2, 5)
        while len(self._color_history) > mx:
            self._color_history.pop()

        if not is_formed:
            return

        sig_bar = self.SignalBar
        signal_color = self._get_color(sig_bar)
        prev_color = self._get_color(sig_bar + 1)

        if signal_color is None or prev_color is None:
            return

        if self.EnableSellExit and signal_color < 2 and self.Position < 0:
            self.BuyMarket()

        if self.EnableBuyExit and signal_color > 2 and self.Position > 0:
            self.SellMarket()

        if self.EnableBuyEntry and signal_color <= 1 and prev_color > 1 and self.Position <= 0:
            self.BuyMarket()
        elif self.EnableSellEntry and signal_color >= 3 and prev_color < 3 and self.Position >= 0:
            self.SellMarket()

    def _get_color(self, index):
        if index < 0 or index >= len(self._color_history):
            return None
        return self._color_history[index]

    def OnReseted(self):
        super(color_fisher_m11_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._prev_fish = 0.0
        self._prev_index = 0.0
        self._has_prev = False
        self._fisher_count = 0
        self._last_color = 2
        self._color_history = []

    def CreateClone(self):
        return color_fisher_m11_strategy()
