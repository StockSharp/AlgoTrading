import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    WeightedMovingAverage,
    ExponentialMovingAverage,
    MovingAverageConvergenceDivergence,
    DecimalIndicatorValue,
)


class mam_acd_strategy(Strategy):
    """MAMACD: two LWMA filters on lows, EMA trigger on close, MACD confirmation."""

    def __init__(self):
        super(mam_acd_strategy, self).__init__()

        self._first_low_ma_length = self.Param("FirstLowMaLength", 85) \
            .SetGreaterThanZero() \
            .SetDisplay("LWMA #1", "Length of the first LWMA on lows", "Indicators")
        self._second_low_ma_length = self.Param("SecondLowMaLength", 75) \
            .SetGreaterThanZero() \
            .SetDisplay("LWMA #2", "Length of the second LWMA on lows", "Indicators")
        self._trigger_ema_length = self.Param("TriggerEmaLength", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Trigger EMA", "Length of the EMA on closes", "Indicators")
        self._macd_fast_length = self.Param("MacdFastLength", 15) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Fast", "Fast EMA length of MACD", "Indicators")
        self._macd_slow_length = self.Param("MacdSlowLength", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Slow", "Slow EMA length of MACD", "Indicators")
        self._stop_loss_pips = self.Param("StopLossPips", 500) \
            .SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk management")
        self._take_profit_pips = self.Param("TakeProfitPips", 500) \
            .SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles used for calculations", "General")

        self._previous_macd = None
        self._ready_for_long = False
        self._ready_for_short = False
        self._pip_size = 0.0

    @property
    def FirstLowMaLength(self):
        return int(self._first_low_ma_length.Value)
    @property
    def SecondLowMaLength(self):
        return int(self._second_low_ma_length.Value)
    @property
    def TriggerEmaLength(self):
        return int(self._trigger_ema_length.Value)
    @property
    def MacdFastLength(self):
        return int(self._macd_fast_length.Value)
    @property
    def MacdSlowLength(self):
        return int(self._macd_slow_length.Value)
    @property
    def StopLossPips(self):
        return int(self._stop_loss_pips.Value)
    @property
    def TakeProfitPips(self):
        return int(self._take_profit_pips.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def _calc_pip_size(self):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 1.0
        if step <= 0:
            return 1.0
        # Count decimal places
        v = abs(step)
        count = 0
        while v != int(v) and count < 10:
            v *= 10
            count += 1
        return step * 10.0 if (count == 3 or count == 5) else step

    def OnStarted(self, time):
        super(mam_acd_strategy, self).OnStarted(time)

        self._previous_macd = None
        self._ready_for_long = False
        self._ready_for_short = False

        self._first_low_ma = WeightedMovingAverage()
        self._first_low_ma.Length = self.FirstLowMaLength
        self._second_low_ma = WeightedMovingAverage()
        self._second_low_ma.Length = self.SecondLowMaLength
        self._trigger_ema = ExponentialMovingAverage()
        self._trigger_ema.Length = self.TriggerEmaLength
        self._macd_ind = MovingAverageConvergenceDivergence()
        self._macd_ind.ShortMa.Length = self.MacdFastLength
        self._macd_ind.LongMa.Length = self.MacdSlowLength

        self._pip_size = self._calc_pip_size()

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

        tp = Unit(self.TakeProfitPips * self._pip_size, UnitTypes.Absolute) if self.TakeProfitPips > 0 else None
        sl = Unit(self.StopLossPips * self._pip_size, UnitTypes.Absolute) if self.StopLossPips > 0 else None
        if tp is not None and sl is not None:
            self.StartProtection(takeProfit=tp, stopLoss=sl)
        elif tp is not None:
            self.StartProtection(takeProfit=tp)
        elif sl is not None:
            self.StartProtection(stopLoss=sl)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._first_low_ma)
            self.DrawIndicator(area, self._second_low_ma)
            self.DrawIndicator(area, self._trigger_ema)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        t = candle.OpenTime
        lo = candle.LowPrice
        close = candle.ClosePrice

        first_low_val = self._first_low_ma.Process(DecimalIndicatorValue(self._first_low_ma, lo, t))
        second_low_val = self._second_low_ma.Process(DecimalIndicatorValue(self._second_low_ma, lo, t))
        trigger_val = self._trigger_ema.Process(DecimalIndicatorValue(self._trigger_ema, close, t))
        macd_val = self._macd_ind.Process(DecimalIndicatorValue(self._macd_ind, close, t))

        if (not self._first_low_ma.IsFormed or not self._second_low_ma.IsFormed
                or not self._trigger_ema.IsFormed or not self._macd_ind.IsFormed):
            if self._macd_ind.IsFormed:
                self._previous_macd = float(macd_val.GetValue[float]())
            return

        ma1 = float(first_low_val.GetValue[float]())
        ma2 = float(second_low_val.GetValue[float]())
        ma3 = float(trigger_val.GetValue[float]())
        macd = float(macd_val.GetValue[float]())

        if self._previous_macd is None:
            self._previous_macd = macd
            return

        if macd == 0 or self._previous_macd == 0:
            self._previous_macd = macd
            return

        # Track readiness flags
        if ma3 < ma1 and ma3 < ma2:
            self._ready_for_long = True
        if ma3 > ma1 and ma3 > ma2:
            self._ready_for_short = True

        macd_improving = macd > self._previous_macd
        long_signal = ma3 > ma1 and ma3 > ma2 and self._ready_for_long and (macd > 0 or macd_improving)
        short_signal = ma3 < ma1 and ma3 < ma2 and self._ready_for_short and (macd < 0 or not macd_improving)

        if long_signal and self.Position <= 0:
            self.BuyMarket()
            self._ready_for_long = False
        elif short_signal and self.Position >= 0:
            self.SellMarket()
            self._ready_for_short = False

        self._previous_macd = macd

    def OnReseted(self):
        super(mam_acd_strategy, self).OnReseted()
        self._previous_macd = None
        self._ready_for_long = False
        self._ready_for_short = False
        self._pip_size = 0.0

    def CreateClone(self):
        return mam_acd_strategy()
