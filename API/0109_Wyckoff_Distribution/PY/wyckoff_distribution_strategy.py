import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy

class wyckoff_distribution_strategy(Strategy):
    """
    Strategy based on Wyckoff Distribution pattern.
    Detects narrowing ranges near extremes (distribution/accumulation),
    then enters on upthrust/spring confirmation with MA filter.
    Uses bar-based cooldown to control trade frequency.
    """

    def __init__(self):
        super(wyckoff_distribution_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._ma_period = self.Param("MaPeriod", 20).SetDisplay("MA Period", "SMA period", "Indicators")
        self._range_period = self.Param("RangePeriod", 20).SetDisplay("Range Period", "Highest/Lowest period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 800).SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._prev_ma = 0.0
        self._prev_close = 0.0
        self._narrow_count = 0
        self._bars_since_entry = 0
        self._entry_price = 0.0
        self._hold_bars = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(wyckoff_distribution_strategy, self).OnReseted()
        self._prev_ma = 0.0
        self._prev_close = 0.0
        self._narrow_count = 0
        self._bars_since_entry = 0
        self._entry_price = 0.0
        self._hold_bars = 0

    def OnStarted(self, time):
        super(wyckoff_distribution_strategy, self).OnStarted(time)

        self._bars_since_entry = self._cooldown_bars.Value  # allow immediate first trade
        self._prev_ma = 0.0
        self._prev_close = 0.0
        self._narrow_count = 0
        self._entry_price = 0.0
        self._hold_bars = 0

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value
        highest = Highest()
        highest.Length = self._range_period.Value
        lowest = Lowest()
        lowest.Length = self._range_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, highest, lowest, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ma_val, highest_val, lowest_val):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)
        ma = float(ma_val)
        highest = float(highest_val)
        lowest = float(lowest_val)
        rng = highest - lowest

        if rng <= 0 or self._prev_ma == 0:
            self._prev_ma = ma
            self._prev_close = close
            return

        self._bars_since_entry += 1

        candle_range = float(candle.HighPrice) - float(candle.LowPrice)
        is_narrow = candle_range < rng * 0.35

        # Track consecutive narrow-range candles
        if is_narrow:
            self._narrow_count += 1
        else:
            self._narrow_count = 0

        cd = self._cooldown_bars.Value

        # Exit logic: hold for minimum bars, then exit on MA cross
        if self.Position != 0 and self._hold_bars > 0:
            self._hold_bars -= 1

        if self.Position > 0 and self._hold_bars == 0:
            if close < ma:
                self.SellMarket()
                self._bars_since_entry = 0
        elif self.Position < 0 and self._hold_bars == 0:
            if close > ma:
                self.BuyMarket()
                self._bars_since_entry = 0

        # Entry logic: only when no position and sufficient cooldown
        if self.Position == 0 and self._bars_since_entry >= cd and self._narrow_count >= 2:
            near_top = close > lowest + rng * 0.55
            near_bottom = close < highest - rng * 0.55

            # Upthrust (short): price near top after consolidation, bearish candle below MA
            if near_top and candle.ClosePrice < candle.OpenPrice and close < ma:
                self.SellMarket()
                self._entry_price = close
                self._bars_since_entry = 0
                self._narrow_count = 0
                self._hold_bars = 20
            # Spring (long): price near bottom after consolidation, bullish candle above MA
            elif near_bottom and candle.ClosePrice > candle.OpenPrice and close > ma:
                self.BuyMarket()
                self._entry_price = close
                self._bars_since_entry = 0
                self._narrow_count = 0
                self._hold_bars = 20

        self._prev_ma = ma
        self._prev_close = close

    def CreateClone(self):
        return wyckoff_distribution_strategy()
