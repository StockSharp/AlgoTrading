import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy

SIDE_BUY = 0
SIDE_SELL = 1

PATTERN_VALUES = [
    "0", "1",
    "00", "01", "10", "11",
    "000", "001", "010", "011", "100", "101", "110", "111",
    "0000", "0001", "0010", "0011", "0100", "0101", "0110", "0111",
    "1000", "1001", "1010", "1011", "1100", "1101", "1110", "1111",
    "00000", "00000", "00010", "00011", "00100", "00101", "00111", "00111",
    "01000", "01001", "01010", "01011", "01100", "01101", "01110", "01111",
    "10000", "10001", "10010", "10011", "10100", "10101", "10110", "10111",
    "11000", "11001", "11010", "11011", "11100", "11101", "11110", "11111"
]


class morse_code_strategy(Strategy):
    def __init__(self):
        super(morse_code_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._pattern_mask = self.Param("Pattern", 14)
        self._direction = self.Param("Direction", SIDE_BUY)
        self._take_profit_pips = self.Param("TakeProfitPips", 50.0)
        self._stop_loss_pips = self.Param("StopLossPips", 50.0)

        self._pattern_text = ""
        self._pattern_length = 0
        self._mask_limit = 0
        self._bull_mask = 0
        self._bear_mask = 0
        self._processed_bars = 0
        self._pip_size = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def Pattern(self):
        return self._pattern_mask.Value

    @Pattern.setter
    def Pattern(self, value):
        self._pattern_mask.Value = value

    @property
    def Direction(self):
        return self._direction.Value

    @Direction.setter
    def Direction(self, value):
        self._direction.Value = value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @TakeProfitPips.setter
    def TakeProfitPips(self, value):
        self._take_profit_pips.Value = value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @StopLossPips.setter
    def StopLossPips(self, value):
        self._stop_loss_pips.Value = value

    def _calculate_pip_size(self):
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if step <= 0.0:
            return 1.0

        value = step
        digits = 0
        while value < 1.0 and digits < 10:
            value *= 10.0
            digits += 1

        if digits == 3 or digits == 5:
            step *= 10.0

        return step

    def OnStarted2(self, time):
        super(morse_code_strategy, self).OnStarted2(time)

        idx = int(self.Pattern)
        if idx < 0 or idx >= len(PATTERN_VALUES):
            idx = 0
        self._pattern_text = PATTERN_VALUES[idx]
        self._pattern_length = len(self._pattern_text)
        self._mask_limit = (1 << self._pattern_length) - 1
        self._bull_mask = 0
        self._bear_mask = 0
        self._processed_bars = 0
        self._pip_size = self._calculate_pip_size()

        tp_distance = float(self.TakeProfitPips) * self._pip_size
        sl_distance = float(self.StopLossPips) * self._pip_size

        self.StartProtection(
            Unit(tp_distance, UnitTypes.Absolute),
            Unit(sl_distance, UnitTypes.Absolute),
            False, None, None, True)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._update_pattern_masks(candle)

        if self._processed_bars < self._pattern_length:
            return

        if not self._is_pattern_matched():
            return

        direction = int(self.Direction)

        if direction == SIDE_BUY:
            if self.Position > 0:
                return
            self.BuyMarket()
        else:
            if self.Position < 0:
                return
            self.SellMarket()

    def _update_pattern_masks(self, candle):
        if self._pattern_length == 0:
            return

        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)

        strict_bull = 1 if close > open_price else 0
        strict_bear = 1 if close < open_price else 0

        self._bull_mask = ((self._bull_mask << 1) | strict_bull) & self._mask_limit
        self._bear_mask = ((self._bear_mask << 1) | strict_bear) & self._mask_limit

        if self._processed_bars < self._pattern_length:
            self._processed_bars += 1

    def _is_pattern_matched(self):
        for i in range(self._pattern_length):
            expected = self._pattern_text[i]
            is_strict_bull = ((self._bull_mask >> i) & 1) == 1
            is_strict_bear = ((self._bear_mask >> i) & 1) == 1

            if expected == '1':
                if is_strict_bear:
                    return False
            else:
                if is_strict_bull:
                    return False

        return True

    def OnReseted(self):
        super(morse_code_strategy, self).OnReseted()
        self._pattern_text = ""
        self._pattern_length = 0
        self._mask_limit = 0
        self._bull_mask = 0
        self._bear_mask = 0
        self._processed_bars = 0
        self._pip_size = 0.0

    def CreateClone(self):
        return morse_code_strategy()
