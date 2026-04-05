import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class bull_vs_medved_strategy(Strategy):
    """Enters market orders during intraday windows when bullish or bearish candle sequences appear."""

    def __init__(self):
        super(bull_vs_medved_strategy, self).__init__()

        self._candle_size_pips = self.Param("CandleSizePips", 500.0) \
            .SetDisplay("Candle Size (pips)", "Minimum body size for the latest candle", "Filters") \
            .SetGreaterThanZero()

        self._stop_loss_pips = self.Param("StopLossPips", 60.0) \
            .SetDisplay("Stop Loss (pips)", "Distance from entry for protective stop", "Risk") \
            .SetGreaterThanZero()

        self._take_profit_pips = self.Param("TakeProfitPips", 60.0) \
            .SetDisplay("Take Profit (pips)", "Distance from entry for profit target", "Risk") \
            .SetGreaterThanZero()

        self._entry_window_minutes = self.Param("EntryWindowMinutes", 30) \
            .SetDisplay("Entry Window (min)", "Duration of each trading window", "Timing") \
            .SetGreaterThanZero()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Primary timeframe for pattern detection", "Data")

        self._start_time0 = self.Param("StartTime0", TimeSpan(0, 0, 0)) \
            .SetDisplay("Start Time #1", "First trading window start", "Timing")

        self._start_time1 = self.Param("StartTime1", TimeSpan(4, 0, 0)) \
            .SetDisplay("Start Time #2", "Second trading window start", "Timing")

        self._start_time2 = self.Param("StartTime2", TimeSpan(8, 0, 0)) \
            .SetDisplay("Start Time #3", "Third trading window start", "Timing")

        self._start_time3 = self.Param("StartTime3", TimeSpan(12, 0, 0)) \
            .SetDisplay("Start Time #4", "Fourth trading window start", "Timing")

        self._start_time4 = self.Param("StartTime4", TimeSpan(16, 0, 0)) \
            .SetDisplay("Start Time #5", "Fifth trading window start", "Timing")

        self._start_time5 = self.Param("StartTime5", TimeSpan(20, 0, 0)) \
            .SetDisplay("Start Time #6", "Sixth trading window start", "Timing")

        self._point_value = 0.0
        self._pip_value = 0.0
        self._body_min_size = 0.0
        self._pullback_size = 0.0
        self._candle_size_threshold = 0.0
        self._stop_loss_offset = 0.0
        self._take_profit_offset = 0.0
        self._order_placed_in_window = False
        self._prev_candle1 = None
        self._prev_candle2 = None
        self._entry_times = []

    @property
    def CandleSizePips(self):
        return self._candle_size_pips.Value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def EntryWindowMinutes(self):
        return self._entry_window_minutes.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(bull_vs_medved_strategy, self).OnStarted2(time)

        sec = self.Security
        decimals = sec.Decimals if sec is not None and sec.Decimals is not None else 0
        adjust = 10.0 if decimals == 3 or decimals == 5 else 1.0

        self._point_value = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        self._pip_value = self._point_value * adjust
        self._body_min_size = 10.0 * self._point_value
        self._pullback_size = 20.0 * self._point_value
        self._candle_size_threshold = float(self.CandleSizePips) * self._pip_value
        self._stop_loss_offset = float(self.StopLossPips) * self._pip_value
        self._take_profit_offset = float(self.TakeProfitPips) * self._pip_value

        self._entry_times = [
            self._start_time0.Value,
            self._start_time1.Value,
            self._start_time2.Value,
            self._start_time3.Value,
            self._start_time4.Value,
            self._start_time5.Value,
        ]

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(self.process_candle) \
            .Start()

        sl_unit = Unit(self._stop_loss_offset, UnitTypes.Absolute) if self._stop_loss_offset > 0 else None
        tp_unit = Unit(self._take_profit_offset, UnitTypes.Absolute) if self._take_profit_offset > 0 else None
        self.StartProtection(sl_unit, tp_unit, True)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._shift(candle)
            return

        in_window = self._is_within_window(candle.CloseTime)
        if not in_window:
            self._order_placed_in_window = False
            self._shift(candle)
            return

        if self._order_placed_in_window:
            self._shift(candle)
            return

        if self._prev_candle1 is None or self._prev_candle2 is None:
            self._shift(candle)
            return

        s1 = candle
        s2 = self._prev_candle1
        s3 = self._prev_candle2

        placed = False

        is_bull = self._is_bull(s3, s2, s1)
        is_bad_bull = self._is_bad_bull(s3, s2, s1)
        is_cool_bull = self._is_cool_bull(s2, s1)
        is_bear = self._is_bear(s1)

        if is_bull and not is_bad_bull:
            placed = self._try_buy()
        elif is_cool_bull:
            placed = self._try_buy()
        elif is_bear:
            placed = self._try_sell()

        if placed:
            self._order_placed_in_window = True

        self._shift(candle)

    def _is_within_window(self, time):
        window = TimeSpan.FromMinutes(self.EntryWindowMinutes)
        tod = time.TimeOfDay
        for et in self._entry_times:
            end = et + window
            if tod >= et and tod <= end:
                return True
        return False

    def _try_buy(self):
        if self.Position != 0:
            return False
        self.BuyMarket()
        return True

    def _try_sell(self):
        if self.Position != 0:
            return False
        self.SellMarket()
        return True

    def _is_bull(self, s3, s2, s1):
        return (float(s3.ClosePrice) > float(s2.OpenPrice) and
                float(s2.ClosePrice) - float(s2.OpenPrice) >= self._body_min_size and
                float(s1.ClosePrice) - float(s1.OpenPrice) >= self._candle_size_threshold)

    def _is_bad_bull(self, s3, s2, s1):
        return (float(s3.ClosePrice) - float(s3.OpenPrice) >= self._body_min_size and
                float(s2.ClosePrice) - float(s2.OpenPrice) >= self._body_min_size and
                float(s1.ClosePrice) - float(s1.OpenPrice) >= self._candle_size_threshold)

    def _is_cool_bull(self, s2, s1):
        return (float(s2.OpenPrice) - float(s2.ClosePrice) >= self._pullback_size and
                float(s2.ClosePrice) <= float(s1.OpenPrice) and
                float(s1.ClosePrice) > float(s2.OpenPrice) and
                float(s1.ClosePrice) - float(s1.OpenPrice) >= 0.4 * self._candle_size_threshold)

    def _is_bear(self, s1):
        return float(s1.OpenPrice) - float(s1.ClosePrice) >= self._candle_size_threshold

    def _shift(self, candle):
        self._prev_candle2 = self._prev_candle1
        self._prev_candle1 = candle

    def OnReseted(self):
        super(bull_vs_medved_strategy, self).OnReseted()
        self._point_value = 0.0
        self._pip_value = 0.0
        self._body_min_size = 0.0
        self._pullback_size = 0.0
        self._candle_size_threshold = 0.0
        self._stop_loss_offset = 0.0
        self._take_profit_offset = 0.0
        self._order_placed_in_window = False
        self._prev_candle1 = None
        self._prev_candle2 = None
        self._entry_times = []

    def CreateClone(self):
        return bull_vs_medved_strategy()
