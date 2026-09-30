import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal, Int32, MidpointRounding, InvalidOperationException
from StockSharp.Messages import DataType, CandleStates, Sides, OrderStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy, StrategyTradingModes
from StockSharp.BusinessEntities import Security
from indicator_extensions import *

_ENTRY_COMMENT = "Spread entry"
_AVERAGE_COMMENT = "Spread at average"
_STOP_COMMENT = "Spread stop"
_ROLL_COMMENT = "Contract roll"


def _lot_size(security):
    multiplier = security.Multiplier
    return multiplier if multiplier is not None and multiplier > Decimal.Zero else Decimal.One


def _is_pending(order):
    return order is not None and order.State != OrderStates.Done and order.State != OrderStates.Failed


class wti_brent_spread_strategy(Strategy):
    """
    Mean reversion of the WTI-Brent price differential. Security is the front-month WTI contract and
    BrentSecurity the front-month Brent contract; the spread is the WTI close minus the Brent close of the
    bar both legs finished. When the spread's z-score over Lookback bars moves beyond EntryZScore, the grade
    that is cheap against the spread's average is bought and the expensive one sold for the same dollar
    amount. The pair is closed when the spread returns to its average, when it widens StopWidening standard
    deviations past its level at entry, or RollDays days before the earlier expiry of the two contracts.
    """

    def __init__(self):
        super(wti_brent_spread_strategy, self).__init__()

        self._brent_security = self.Param[Security]("BrentSecurity", None) \
            .SetDisplay("Brent Security", "Front-month Brent contract traded against the WTI security", "Instruments") \
            .SetRequired()

        self._lookback = self.Param("Lookback", 20) \
            .SetRange(2, Int32.MaxValue) \
            .SetDisplay("Lookback", "Bars that define the spread's average and standard deviation", "Spread") \
            .SetOptimize(10, 60, 10)

        self._entry_z_score = self.Param("EntryZScore", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Entry Z-Score", "Standard deviations from the average beyond which the pair is opened", "Spread") \
            .SetOptimize(1.5, 3.0, 0.5)

        self._stop_widening = self.Param("StopWidening", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Widening", "Standard deviations the spread may widen past its level at entry before the pair is closed", "Risk") \
            .SetOptimize(0.5, 3.0, 0.5)

        self._roll_days = self.Param("RollDays", 5) \
            .SetNotNegative() \
            .SetDisplay("Roll Days", "Days before the earlier front-month expiry when the pair is closed", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Bars on which the two legs are compared", "General")

        self._spread_average = None
        self._spread_deviation = None
        self._clear_state()

    @property
    def BrentSecurity(self):
        return self._brent_security.Value

    @BrentSecurity.setter
    def BrentSecurity(self, value):
        self._brent_security.Value = value

    @property
    def Lookback(self):
        return self._lookback.Value

    @Lookback.setter
    def Lookback(self, value):
        self._lookback.Value = value

    @property
    def EntryZScore(self):
        return self._entry_z_score.Value

    @EntryZScore.setter
    def EntryZScore(self, value):
        self._entry_z_score.Value = value

    @property
    def StopWidening(self):
        return self._stop_widening.Value

    @StopWidening.setter
    def StopWidening(self, value):
        self._stop_widening.Value = value

    @property
    def RollDays(self):
        return self._roll_days.Value

    @RollDays.setter
    def RollDays(self, value):
        self._roll_days.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        securities = []
        if self.Security is not None:
            securities.append((self.Security, self.CandleType))
        if self.BrentSecurity is not None:
            securities.append((self.BrentSecurity, self.CandleType))
        return securities

    def OnReseted(self):
        super(wti_brent_spread_strategy, self).OnReseted()
        self._spread_average = None
        self._spread_deviation = None
        self._clear_state()

    def OnStarted2(self, time):
        # Checked before any subscription: a missing Brent leg would otherwise fall back to the WTI security.
        if self.Security is None:
            raise InvalidOperationException("Security must be the front-month WTI contract.")
        if self.BrentSecurity is None or str(self.BrentSecurity.Id).lower() == str(self.Security.Id).lower():
            raise InvalidOperationException("BrentSecurity must be a different instrument than the WTI security.")

        super(wti_brent_spread_strategy, self).OnStarted2(time)

        self._clear_state()
        self._spread_average = SimpleMovingAverage()
        self._spread_average.Length = self.Lookback
        self._spread_deviation = StandardDeviation()
        self._spread_deviation.Length = self.Lookback

        wti_subscription = self.SubscribeCandles(self.CandleType)
        wti_subscription.Bind(self._process_wti_candle).Start()

        # The second positional argument is isFinishedOnly, not the instrument.
        self.SubscribeCandles(self.CandleType, security=self.BrentSecurity) \
            .Bind(self._process_brent_candle) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, wti_subscription)
            self.DrawOwnTrades(area)

    def _clear_state(self):
        self._wti_bar_time = None
        self._wti_close = Decimal.Zero
        self._brent_bar_time = None
        self._brent_close = Decimal.Zero
        self._pair_time = None
        self._stop_spread = None
        self._blocked_sign = 0
        self._wti_order = None
        self._brent_order = None

    def _process_wti_candle(self, candle):
        if candle.State != CandleStates.Finished or (self._wti_bar_time is not None and candle.OpenTime <= self._wti_bar_time):
            return
        self._wti_bar_time = candle.OpenTime
        self._wti_close = candle.ClosePrice
        self._process_pair()

    def _process_brent_candle(self, candle):
        if candle.State != CandleStates.Finished or (self._brent_bar_time is not None and candle.OpenTime <= self._brent_bar_time):
            return
        self._brent_bar_time = candle.OpenTime
        self._brent_close = candle.ClosePrice
        self._process_pair()

    def _process_pair(self):
        # Only bars both legs finished at the same time form a spread; an unmatched bar is never reused.
        bar_time = self._wti_bar_time
        if bar_time is None or self._brent_bar_time is None or self._brent_bar_time != bar_time:
            return
        if self._pair_time is not None and self._pair_time == bar_time:
            return
        self._pair_time = bar_time

        spread = self._wti_close - self._brent_close
        average_value = process_value(self._spread_average, spread, bar_time, True)
        deviation_value = process_value(self._spread_deviation, spread, bar_time, True)

        if not self._spread_average.IsFormed or not self._spread_deviation.IsFormed:
            return

        average = average_value.GetValue[Decimal](None)
        deviation = deviation_value.GetValue[Decimal](None)
        # A window of equal spreads has no deviation: the spread sits at its average.
        z_score = Decimal.Zero if deviation == Decimal.Zero else (spread - average) / deviation
        entry = Decimal(self.EntryZScore)

        # After a stop the same side waits until the spread is back inside the entry threshold.
        if self._blocked_sign != 0 and Decimal(self._blocked_sign) * z_score <= entry:
            self._blocked_sign = 0

        if _is_pending(self._wti_order) or _is_pending(self._brent_order):
            return

        wti_position = self.Position
        brent_value = self.GetPositionValue(self.BrentSecurity, self.Portfolio)
        brent_position = Decimal.Zero if brent_value is None else brent_value
        now = self.CurrentTime

        if wti_position != Decimal.Zero or brent_position != Decimal.Zero:
            if not self.IsFormedAndOnlineAndAllowTrading(StrategyTradingModes.ReducePositionOnly):
                return

            # 1 holds long WTI and short Brent, -1 the opposite.
            side = Math.Sign(wti_position) if wti_position != Decimal.Zero else -Math.Sign(brent_position)
            reason = self._get_exit_reason(side, spread, z_score, now)

            if reason is None:
                return

            if reason == _STOP_COMMENT:
                self._blocked_sign = -side

            self._close_pair(wti_position, brent_position, reason)
            return

        if not self.IsFormedAndOnlineAndAllowTrading() or self._is_roll_due(now):
            return

        # 1 buys WTI while it is cheap against Brent, -1 sells it while it is expensive.
        if z_score > entry:
            direction = -1
        elif z_score < -entry:
            direction = 1
        else:
            return

        if self._blocked_sign == -direction:
            return

        brent_volume = self._get_brent_volume(self._wti_close, self._brent_close)

        if brent_volume <= Decimal.Zero:
            self.LogWarning("No Brent volume within the instrument limits matches the value of {0} WTI at {1} against Brent at {2}.".format(
                str(self.Volume), str(self._wti_close), str(self._brent_close)))
            return

        # Fixed at entry, past the entry spread on the side it would keep widening to.
        self._stop_spread = spread - Decimal(direction) * Decimal(self.StopWidening) * deviation
        self._wti_order = self._register_leg(self.Security, Sides.Buy if direction > 0 else Sides.Sell, self.Volume, _ENTRY_COMMENT)
        self._brent_order = self._register_leg(self.BrentSecurity, Sides.Sell if direction > 0 else Sides.Buy, brent_volume, _ENTRY_COMMENT)

    def _get_exit_reason(self, side, spread, z_score, time):
        if self._is_roll_due(time):
            return _ROLL_COMMENT

        if self._stop_spread is not None and (spread <= self._stop_spread if side > 0 else spread >= self._stop_spread):
            return _STOP_COMMENT

        at_average = z_score >= Decimal.Zero if side > 0 else z_score <= Decimal.Zero
        return _AVERAGE_COMMENT if at_average else None

    def _is_roll_due(self, time):
        expiry = self.Security.ExpiryDate
        brent_expiry = self.BrentSecurity.ExpiryDate

        if brent_expiry is not None and (expiry is None or brent_expiry < expiry):
            expiry = brent_expiry

        return expiry is not None and time >= expiry.AddDays(-self.RollDays)

    def _get_brent_volume(self, wti_price, brent_price):
        # The Brent quantity worth as many dollars as Volume WTI contracts, to the nearest volume step.
        wti_value = self.Volume * wti_price * _lot_size(self.Security)
        brent_lot_value = brent_price * _lot_size(self.BrentSecurity)

        if wti_value <= Decimal.Zero or brent_lot_value <= Decimal.Zero:
            return Decimal.Zero

        step = self.BrentSecurity.VolumeStep
        if step is None or step <= Decimal.Zero:
            step = Decimal.One

        volume = Math.Round(wti_value / brent_lot_value / step, MidpointRounding.AwayFromZero) * step

        min_volume = self.BrentSecurity.MinVolume
        if min_volume is not None and volume < min_volume:
            return Decimal.Zero

        max_volume = self.BrentSecurity.MaxVolume
        if max_volume is not None and max_volume > Decimal.Zero and volume > max_volume:
            return Decimal.Zero

        return volume

    def _close_pair(self, wti_position, brent_position, reason):
        if wti_position != Decimal.Zero:
            self._wti_order = self._register_leg(self.Security, Sides.Sell if wti_position > Decimal.Zero else Sides.Buy, Math.Abs(wti_position), reason)

        if brent_position != Decimal.Zero:
            self._brent_order = self._register_leg(self.BrentSecurity, Sides.Sell if brent_position > Decimal.Zero else Sides.Buy, Math.Abs(brent_position), reason)

        self._stop_spread = None

    def _register_leg(self, security, side, volume, comment):
        order = self.CreateOrder(side, Decimal.Zero, volume, security)
        order.Comment = comment
        self.RegisterOrder(order)
        return order

    def CreateClone(self):
        return wti_brent_spread_strategy()
