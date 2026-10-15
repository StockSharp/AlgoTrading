import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageDirectionalIndex, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

CYCLE_CHOP = 0
CYCLE_LONG = 1
CYCLE_SHORT = 2


class j_lines_ribbon_4_cycle_engine_strategy(Strategy):
    """
    J-Lines Ribbon 4-Cycle Engine strategy.
    The market is CHOP while ADX is below AdxFloor, LONG when EMA72 is above EMA89 and price closes above EMA126, and SHORT in the mirror case.
    A long opens on a new LONG cycle or when price dips to EMA72/EMA126 and closes back above it while EMA72 is above EMA89; shorts mirror this,
    reversing an opposite position. The stop is the last swing low (high) and a position also closes when EMA72 crosses EMA89 against it.
    """

    def __init__(self):
        super(j_lines_ribbon_4_cycle_engine_strategy, self).__init__()
        self._dmi_length = self.Param("DmiLength", 8).SetGreaterThanZero().SetDisplay("DMI Length", "Period of the DMI/ADX", "Indicators")
        self._adx_floor = self.Param("AdxFloor", 12.0).SetNotNegative().SetDisplay("ADX Floor", "ADX level below which the market is CHOP", "Indicators")
        self._swing_length = self.Param("SwingLength", 10).SetGreaterThanZero().SetDisplay("Swing Length", "Candles the swing high/low spans", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._swing_high = None
        self._swing_low = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_cycle = CYCLE_CHOP
        self._prev_ema72 = None
        self._prev_ema89 = None
        self._prev_swing_high = None
        self._prev_swing_low = None
        self._stop_price = None

    def OnReseted(self):
        super(j_lines_ribbon_4_cycle_engine_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(j_lines_ribbon_4_cycle_engine_strategy, self).OnStarted2(time)

        self._reset_state()

        ema72 = ExponentialMovingAverage()
        ema72.Length = 72
        ema89 = ExponentialMovingAverage()
        ema89.Length = 89
        ema126 = ExponentialMovingAverage()
        ema126.Length = 126
        adx = AverageDirectionalIndex()
        adx.Length = self._dmi_length.Value
        self._swing_high = Highest()
        self._swing_high.Length = self._swing_length.Value
        self._swing_low = Lowest()
        self._swing_low.Length = self._swing_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ema72, ema89, ema126, adx, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema72)
            self.DrawIndicator(area, ema89)
            self.DrawIndicator(area, ema126)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, adx)

    def _process_candle(self, candle, ema72_value, ema89_value, ema126_value, adx_value):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        time = candle.OpenTime

        # The swing is measured on the candles before this one.
        swing_high = self._prev_swing_high
        swing_low = self._prev_swing_low

        high_value = float(process_float(self._swing_high, high, time, True))
        low_value = float(process_float(self._swing_low, low, time, True))
        self._prev_swing_high = high_value if self._swing_high.IsFormed else None
        self._prev_swing_low = low_value if self._swing_low.IsFormed else None

        if not ema72_value.IsFormed or not ema89_value.IsFormed or not ema126_value.IsFormed or not adx_value.IsFormed:
            return
        if adx_value.MovingAverage is None:
            return

        adx = float(adx_value.MovingAverage)
        ema72 = float(ema72_value.GetValue[Decimal](None))
        ema89 = float(ema89_value.GetValue[Decimal](None))
        ema126 = float(ema126_value.GetValue[Decimal](None))

        if adx < float(self._adx_floor.Value):
            cycle = CYCLE_CHOP
        elif ema72 > ema89 and close > ema126:
            cycle = CYCLE_LONG
        elif ema72 < ema89 and close < ema126:
            cycle = CYCLE_SHORT
        else:
            cycle = CYCLE_CHOP

        prev_cycle = self._prev_cycle
        prev_ema72 = self._prev_ema72
        prev_ema89 = self._prev_ema89

        self._prev_cycle = cycle
        self._prev_ema72 = ema72
        self._prev_ema89 = ema89

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        has_prev = prev_ema72 is not None and prev_ema89 is not None
        cross_down = has_prev and prev_ema72 >= prev_ema89 and ema72 < ema89
        cross_up = has_prev and prev_ema72 <= prev_ema89 and ema72 > ema89

        if self.Position > 0 and (cross_down or (self._stop_price is not None and low <= self._stop_price)):
            self.SellMarket(self.Position)
            self._stop_price = None
            return

        if self.Position < 0 and (cross_up or (self._stop_price is not None and high >= self._stop_price)):
            self.BuyMarket(-self.Position)
            self._stop_price = None
            return

        rebound_up = (low <= ema72 and close > ema72) or (low <= ema126 and close > ema126)
        rebound_down = (high >= ema72 and close < ema72) or (high >= ema126 and close < ema126)

        long_signal = (cycle == CYCLE_LONG and prev_cycle != CYCLE_LONG) or (ema72 > ema89 and rebound_up)
        short_signal = (cycle == CYCLE_SHORT and prev_cycle != CYCLE_SHORT) or (ema72 < ema89 and rebound_down)

        if long_signal and self.Position <= 0 and swing_low is not None and swing_low < close:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = swing_low
        elif short_signal and self.Position >= 0 and swing_high is not None and swing_high > close:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = swing_high

    def CreateClone(self):
        return j_lines_ribbon_4_cycle_engine_strategy()
