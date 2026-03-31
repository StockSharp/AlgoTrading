import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy

# Wyckoff phases
PHASE_NONE = 0
PHASE_ACCUMULATION = 1
PHASE_SPRING = 2
PHASE_MARKUP = 3

class wyckoff_accumulation_strategy(Strategy):
    """
    Strategy based on Wyckoff Accumulation pattern.
    Detects a selling climax followed by sideways accumulation and a spring,
    then enters long on the markup phase.
    """

    def __init__(self):
        super(wyckoff_accumulation_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._ma_period = self.Param("MaPeriod", 20).SetDisplay("MA Period", "SMA period", "Indicators")
        self._range_period = self.Param("RangePeriod", 20).SetDisplay("Range Period", "Highest/Lowest period", "Indicators")
        self._sideways_threshold = self.Param("SidewaysThreshold", 3).SetDisplay("Sideways Threshold", "Narrow candles to confirm accumulation", "Logic")
        self._cooldown_bars = self.Param("CooldownBars", 65).SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._phase = PHASE_NONE
        self._range_high = 0.0
        self._range_low = 0.0
        self._narrow_count = 0
        self._entry_price = 0.0
        self._prev_ma = 0.0
        self._prev_close = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(wyckoff_accumulation_strategy, self).OnReseted()
        self._phase = PHASE_NONE
        self._range_high = 0.0
        self._range_low = 0.0
        self._narrow_count = 0
        self._entry_price = 0.0
        self._prev_ma = 0.0
        self._prev_close = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(wyckoff_accumulation_strategy, self).OnStarted2(time)

        self._phase = PHASE_NONE
        self._range_high = 0.0
        self._range_low = 0.0
        self._narrow_count = 0
        self._entry_price = 0.0
        self._prev_ma = 0.0
        self._prev_close = 0.0
        self._cooldown = 0

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

        close = float(candle.ClosePrice)
        ma = float(ma_val)
        highest = float(highest_val)
        lowest = float(lowest_val)
        rng = highest - lowest

        if rng <= 0:
            self._prev_ma = ma
            self._prev_close = close
            return

        cd = self._cooldown_bars.Value
        sw_thresh = self._sideways_threshold.Value

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_ma = ma
            self._prev_close = close
            # Still check exit logic during cooldown
            if self.Position > 0:
                if close < ma and self._prev_close >= self._prev_ma:
                    self.SellMarket()
                    self._phase = PHASE_NONE
                    self._narrow_count = 0
                    self._cooldown = cd
            elif self.Position < 0:
                if close > ma and self._prev_close <= self._prev_ma:
                    self.BuyMarket()
                    self._phase = PHASE_NONE
                    self._narrow_count = 0
                    self._cooldown = cd
            self._prev_ma = ma
            self._prev_close = close
            return

        candle_range = float(candle.HighPrice) - float(candle.LowPrice)
        is_narrow = candle_range < rng * 0.4
        is_bullish = candle.ClosePrice > candle.OpenPrice
        is_bearish = candle.ClosePrice < candle.OpenPrice

        # Wyckoff accumulation detection (simplified)
        if self._phase == PHASE_NONE:
            # Look for price near or below the range low (selling climax area)
            if close <= lowest + rng * 0.2 and is_bearish:
                self._phase = PHASE_ACCUMULATION
                self._range_low = lowest
                self._range_high = highest
                self._narrow_count = 0
            # Also detect distribution (price near range high for shorts)
            elif close >= highest - rng * 0.2 and is_bullish:
                self._phase = PHASE_ACCUMULATION
                self._range_low = lowest
                self._range_high = highest
                self._narrow_count = -1  # negative to track distribution

        elif self._phase == PHASE_ACCUMULATION:
            if self._narrow_count >= 0:
                # Accumulation (long setup): count narrow-range candles
                if is_narrow:
                    self._narrow_count += 1
                if self._narrow_count >= sw_thresh:
                    self._phase = PHASE_SPRING
                elif close > self._range_high + rng * 0.1:
                    self._phase = PHASE_NONE
                    self._narrow_count = 0
            else:
                # Distribution (short setup): count narrow-range candles
                if is_narrow:
                    self._narrow_count -= 1
                if self._narrow_count <= -sw_thresh:
                    self._phase = PHASE_SPRING
                elif close < self._range_low - rng * 0.1:
                    self._phase = PHASE_NONE
                    self._narrow_count = 0

        elif self._phase == PHASE_SPRING:
            if self._narrow_count > 0:
                # Long spring: price dips below range low then closes back above
                if float(candle.LowPrice) < self._range_low and close > self._range_low:
                    self._phase = PHASE_MARKUP
                # Or: bullish candle near support with close above MA
                elif is_bullish and close > ma and close > self._range_low:
                    self._phase = PHASE_MARKUP
            else:
                # Short spring (upthrust): price spikes above range high then closes back below
                if float(candle.HighPrice) > self._range_high and close < self._range_high:
                    self._phase = PHASE_MARKUP
                # Or: bearish candle near resistance with close below MA
                elif is_bearish and close < ma and close < self._range_high:
                    self._phase = PHASE_MARKUP

        elif self._phase == PHASE_MARKUP:
            if self.Position == 0:
                if self._narrow_count > 0:
                    # Enter long on markup
                    if is_bullish and close > ma:
                        self.BuyMarket()
                        self._entry_price = close
                        self._cooldown = cd
                        self._phase = PHASE_NONE
                        self._narrow_count = 0
                else:
                    # Enter short on markdown
                    if is_bearish and close < ma:
                        self.SellMarket()
                        self._entry_price = close
                        self._cooldown = cd
                        self._phase = PHASE_NONE
                        self._narrow_count = 0

        # Exit logic for open positions
        if self.Position > 0:
            # Exit long: price crosses below MA
            if close < ma and self._prev_close >= self._prev_ma:
                self.SellMarket()
                self._phase = PHASE_NONE
                self._narrow_count = 0
                self._cooldown = cd
        elif self.Position < 0:
            # Exit short: price crosses above MA
            if close > ma and self._prev_close <= self._prev_ma:
                self.BuyMarket()
                self._phase = PHASE_NONE
                self._narrow_count = 0
                self._cooldown = cd

        self._prev_ma = ma
        self._prev_close = close

    def CreateClone(self):
        return wyckoff_accumulation_strategy()
