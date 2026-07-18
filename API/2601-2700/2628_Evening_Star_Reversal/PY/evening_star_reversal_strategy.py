import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class evening_star_reversal_strategy(Strategy):
    """Evening Star candlestick pattern strategy with SL/TP."""

    def __init__(self):
        super(evening_star_reversal_strategy, self).__init__()

        self._direction_long = self.Param("DirectionLong", False) \
            .SetDisplay("Long Direction", "True=trade long on pattern, False=short", "General")
        self._take_profit_pips = self.Param("TakeProfitPips", 150) \
            .SetGreaterThanZero() \
            .SetDisplay("Take Profit (pips)", "TP distance in pips", "Risk")
        self._stop_loss_pips = self.Param("StopLossPips", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss (pips)", "SL distance in pips", "Risk")
        self._shift = self.Param("Shift", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Shift", "Offset for the bar sequence", "Pattern")
        self._consider_gap = self.Param("ConsiderGap", True) \
            .SetDisplay("Consider Gap", "Require price gaps between candles", "Pattern")
        self._candle2_bullish = self.Param("Candle2Bullish", True) \
            .SetDisplay("Middle Candle Bullish", "Should second candle close above open", "Pattern")
        self._check_sizes = self.Param("CheckCandleSizes", True) \
            .SetDisplay("Check Candle Sizes", "Ensure middle candle smallest body", "Pattern")
        self._close_opposite = self.Param("CloseOpposite", True) \
            .SetDisplay("Close Opposite", "Close opposite position before entry", "Execution")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle series to process", "General")

        self._history = []
        self._pip_size = 0.0
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_profit_price = 0.0

    @property
    def DirectionLong(self):
        return self._direction_long.Value
    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value
    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value
    @property
    def Shift(self):
        return self._shift.Value
    @property
    def ConsiderGap(self):
        return self._consider_gap.Value
    @property
    def Candle2Bullish(self):
        return self._candle2_bullish.Value
    @property
    def CheckCandleSizes(self):
        return self._check_sizes.Value
    @property
    def CloseOpposite(self):
        return self._close_opposite.Value
    @property
    def CandleType(self):
        return self._candle_type.Value

    def _calc_pip_size(self):
        sec = self.Security
        if sec is None or sec.PriceStep is None or float(sec.PriceStep) <= 0:
            return 0.0
        step = float(sec.PriceStep)
        decimals = sec.Decimals if sec.Decimals is not None else 0
        if decimals == 3 or decimals == 5:
            return step * 10.0
        return step

    def OnStarted2(self, time):
        super(evening_star_reversal_strategy, self).OnStarted2(time)
        self._pip_size = self._calc_pip_size()

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        o = float(candle.OpenPrice)
        c = float(candle.ClosePrice)
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        self._history.append((o, c, h, lo))
        max_count = max(self.Shift + 5, 10)
        while len(self._history) > max_count:
            self._history.pop(0)

        # Manage existing position
        self._handle_active_position(candle)

        required = self.Shift + 2
        if len(self._history) < required:
            return

        last_idx = len(self._history) - self.Shift
        if last_idx < 2 or last_idx >= len(self._history):
            return

        recent = self._history[last_idx]
        middle = self._history[last_idx - 1]
        first = self._history[last_idx - 2]

        if not self._is_pattern_valid(first, middle, recent):
            return

        is_long = self.DirectionLong
        entry = recent[1]  # close
        stop = self._calc_stop(entry, is_long)
        take = self._calc_take(entry, is_long)

        if is_long:
            if self.Position < 0 and not self.CloseOpposite:
                return
            if self.Position < 0 and self.CloseOpposite:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = entry
            self._stop_price = stop
            self._take_profit_price = take
        else:
            if self.Position > 0 and not self.CloseOpposite:
                return
            if self.Position > 0 and self.CloseOpposite:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = entry
            self._stop_price = stop
            self._take_profit_price = take

    def _handle_active_position(self, candle):
        if self.Position == 0:
            self._reset_targets()
            return

        if self.Position > 0:
            stop_hit = self._stop_price > 0 and float(candle.LowPrice) <= self._stop_price
            take_hit = self._take_profit_price > 0 and float(candle.HighPrice) >= self._take_profit_price
            if stop_hit or take_hit:
                self.SellMarket()
                self._reset_targets()
        elif self.Position < 0:
            stop_hit = self._stop_price > 0 and float(candle.HighPrice) >= self._stop_price
            take_hit = self._take_profit_price > 0 and float(candle.LowPrice) <= self._take_profit_price
            if stop_hit or take_hit:
                self.BuyMarket()
                self._reset_targets()

    def _is_pattern_valid(self, first, middle, recent):
        # Evening Star: bullish candle, small-body candle, bearish candle
        # recent: (o,c,h,lo)
        if not (recent[0] > recent[1] and first[0] < first[1]):
            return False

        if self.CheckCandleSizes:
            last_body = abs(recent[0] - recent[1])
            mid_body = abs(middle[0] - middle[1])
            first_body = abs(first[0] - first[1])
            if last_body < mid_body or first_body < mid_body:
                return False

        if self.Candle2Bullish:
            if middle[0] > middle[1]:
                return False
        else:
            if middle[1] > middle[0]:
                return False

        if self.ConsiderGap and self._pip_size > 0:
            gap = self._pip_size
            if recent[0] >= middle[1] - gap or middle[0] <= first[1] + gap:
                return False

        return True

    def _calc_stop(self, entry, is_long):
        dist = self.StopLossPips * self._pip_size
        if dist <= 0:
            return 0.0
        return entry - dist if is_long else entry + dist

    def _calc_take(self, entry, is_long):
        dist = self.TakeProfitPips * self._pip_size
        if dist <= 0:
            return 0.0
        return entry + dist if is_long else entry - dist

    def _reset_targets(self):
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_profit_price = 0.0

    def OnReseted(self):
        super(evening_star_reversal_strategy, self).OnReseted()
        self._history = []
        self._pip_size = 0.0
        self._reset_targets()

    def CreateClone(self):
        return evening_star_reversal_strategy()
