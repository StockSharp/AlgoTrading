import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class intraday_volume_swings_strategy(Strategy):
    """
    Intraday volume swings strategy.
    A swing high forms when three consecutive candles make higher highs on rising volume, a swing low when three make lower lows
    on rising volume. While the run continues the swing region spans the high and low of its candles. Once it ends, the most
    extreme region of the day is kept, and the previous day's regions stay active as well. Price pushing up into a high swing
    region (from the current or previous day) goes long, price pushing down into a low swing region goes short, reversing an
    opposite position. With RegionMustClose the candle has to close inside the region, otherwise touching it is enough.
    """

    def __init__(self):
        super(intraday_volume_swings_strategy, self).__init__()
        self._region_must_close = self.Param("RegionMustClose", True).SetDisplay("Region Must Close", "Candle must close inside the region to trigger", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._current_day = None
        self._bar_count = 0
        self._prev_close = 0.0
        self._high1 = 0.0
        self._high2 = 0.0
        self._low1 = 0.0
        self._low2 = 0.0
        self._volume1 = 0.0
        self._low_bar1 = False
        self._low_bar2 = False
        self._high_bar1 = False
        self._high_bar2 = False
        self._prev_swing_low = False
        self._prev_swing_high = False
        self._run_low_top = None
        self._run_low_bottom = None
        self._run_high_top = None
        self._run_high_bottom = None
        self._day_low_top = None
        self._day_low_bottom = None
        self._day_high_top = None
        self._day_high_bottom = None
        self._prev_day_low_top = None
        self._prev_day_low_bottom = None
        self._prev_day_high_top = None
        self._prev_day_high_bottom = None

    def OnReseted(self):
        super(intraday_volume_swings_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(intraday_volume_swings_strategy, self).OnStarted2(time)

        self._reset_state()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        volume = float(candle.TotalVolume)

        day = candle.OpenTime.Date
        if self._current_day != day:
            if self._current_day is not None:
                self._prev_day_low_top = self._day_low_top
                self._prev_day_low_bottom = self._day_low_bottom
                self._prev_day_high_top = self._day_high_top
                self._prev_day_high_bottom = self._day_high_bottom
            self._current_day = day
            self._day_low_top = None
            self._day_low_bottom = None
            self._day_high_top = None
            self._day_high_bottom = None

        has_history = self._bar_count > 0
        rising_volume = has_history and volume > self._volume1
        low_bar = rising_volume and low < self._low1
        high_bar = rising_volume and high > self._high1

        swing_low = low_bar and self._low_bar1 and self._low_bar2
        swing_high = high_bar and self._high_bar1 and self._high_bar2

        top3 = max(high, self._high1, self._high2)
        bottom3 = min(low, self._low1, self._low2)

        if swing_low:
            self._run_low_top = max(self._run_low_top, high) if self._prev_swing_low and self._run_low_top is not None else top3
            self._run_low_bottom = min(self._run_low_bottom, low) if self._prev_swing_low and self._run_low_bottom is not None else bottom3
        elif self._prev_swing_low and self._run_low_top is not None and self._run_low_bottom is not None:
            # Keep the lowest swing low region of the day.
            if self._day_low_bottom is None or self._run_low_bottom < self._day_low_bottom:
                self._day_low_top = self._run_low_top
                self._day_low_bottom = self._run_low_bottom
            self._run_low_top = None
            self._run_low_bottom = None

        if swing_high:
            self._run_high_top = max(self._run_high_top, high) if self._prev_swing_high and self._run_high_top is not None else top3
            self._run_high_bottom = min(self._run_high_bottom, low) if self._prev_swing_high and self._run_high_bottom is not None else bottom3
        elif self._prev_swing_high and self._run_high_top is not None and self._run_high_bottom is not None:
            # Keep the highest swing high region of the day.
            if self._day_high_top is None or self._run_high_top > self._day_high_top:
                self._day_high_top = self._run_high_top
                self._day_high_bottom = self._run_high_bottom
            self._run_high_top = None
            self._run_high_bottom = None

        if has_history and self.IsFormedAndOnlineAndAllowTrading():
            go_long = self._enters_high_region(high, close, self._day_high_bottom) or self._enters_high_region(high, close, self._prev_day_high_bottom)
            go_short = self._enters_low_region(low, close, self._day_low_top) or self._enters_low_region(low, close, self._prev_day_low_top)

            if go_long and not go_short and self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
            elif go_short and not go_long and self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))

        self._bar_count += 1
        self._prev_close = close
        self._volume1 = volume
        self._high2 = self._high1
        self._high1 = high
        self._low2 = self._low1
        self._low1 = low
        self._low_bar2 = self._low_bar1
        self._low_bar1 = low_bar
        self._high_bar2 = self._high_bar1
        self._high_bar1 = high_bar
        self._prev_swing_low = swing_low
        self._prev_swing_high = swing_high

    # Price comes from below the region bottom and reaches it.
    def _enters_high_region(self, high, close, bottom):
        if bottom is None or self._prev_close >= bottom:
            return False
        return close >= bottom if self._region_must_close.Value else high >= bottom

    # Price comes from above the region top and reaches it.
    def _enters_low_region(self, low, close, top):
        if top is None or self._prev_close <= top:
            return False
        return close <= top if self._region_must_close.Value else low <= top

    def CreateClone(self):
        return intraday_volume_swings_strategy()
