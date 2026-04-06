import clr
import math
import threading

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from collections import deque

from StockSharp.Messages import DataType, CandleStates, OrderTypes, Sides
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security
from datatype_extensions import *


class spreader2_strategy(Strategy):
    """
    Pair trading strategy inspired by the 'Spreader 2' MetaTrader expert.
    Looks for short term mean-reverting moves between two correlated symbols
    and trades the spread once correlation and volatility filters align.
    """

    def __init__(self):
        super(spreader2_strategy, self).__init__()

        self._second_security_param = self.Param[Security]("SecondSecurity", None) \
            .SetDisplay("Second Symbol", "Secondary instrument for the spread trade", "General") \
            .SetRequired()

        self._primary_volume_param = self.Param("PrimaryVolume", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Primary Volume", "Order volume for the primary symbol", "Trading") \
            .SetOptimize(0.5, 3.0, 0.5)

        self._target_profit_param = self.Param("TargetProfit", 100.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Target Profit", "Total profit target for the pair position", "Risk") \
            .SetOptimize(20.0, 200.0, 20.0)

        self._shift_param = self.Param("ShiftLength", 6) \
            .SetGreaterThanZero() \
            .SetDisplay("Shift Length", "Number of bars between comparison points", "Logic") \
            .SetOptimize(10, 60, 10)

        self._candle_type_param = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Timeframe for pair analysis", "General")

        self._day_bars_param = self.Param("DayBars", 288) \
            .SetGreaterThanZero() \
            .SetDisplay("Day Bars", "Number of intraday bars used for rolling statistics", "Data")

        # Internal state
        self._first_pending = deque()
        self._second_pending = deque()
        self._first_closes = []
        self._second_closes = []
        self._lock = threading.Lock()

        self._last_first_close = 0.0
        self._last_second_close = 0.0

        self._first_entry_price = 0.0
        self._second_entry_price = 0.0
        self._second_position = 0.0

        self._second_portfolio = None
        self._contracts_match = True

    @property
    def SecondSecurity(self):
        return self._second_security_param.Value

    @SecondSecurity.setter
    def SecondSecurity(self, value):
        self._second_security_param.Value = value

    @property
    def PrimaryVolume(self):
        return self._primary_volume_param.Value

    @PrimaryVolume.setter
    def PrimaryVolume(self, value):
        self._primary_volume_param.Value = value

    @property
    def TargetProfit(self):
        return self._target_profit_param.Value

    @TargetProfit.setter
    def TargetProfit(self, value):
        self._target_profit_param.Value = value

    @property
    def ShiftLength(self):
        return self._shift_param.Value

    @ShiftLength.setter
    def ShiftLength(self, value):
        self._shift_param.Value = value

    @property
    def CandleType(self):
        return self._candle_type_param.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type_param.Value = value

    @property
    def DayBars(self):
        return self._day_bars_param.Value

    @DayBars.setter
    def DayBars(self, value):
        self._day_bars_param.Value = value

    def GetWorkingSecurities(self):
        return [
            (self.Security, self.CandleType),
            (self.SecondSecurity, self.CandleType)
        ]

    def OnReseted(self):
        super(spreader2_strategy, self).OnReseted()

        self._first_pending.clear()
        self._second_pending.clear()
        self._first_closes.clear()
        self._second_closes.clear()

        self._last_first_close = 0.0
        self._last_second_close = 0.0

        self._first_entry_price = 0.0
        self._second_entry_price = 0.0
        self._second_position = 0.0

        self._second_portfolio = None
        self._contracts_match = True

    def OnStarted2(self, time):
        super(spreader2_strategy, self).OnStarted2(time)

        if self.SecondSecurity is None:
            raise Exception("Second security is not specified.")

        self._second_portfolio = self.Portfolio
        if self._second_portfolio is None:
            raise Exception("Portfolio is not specified.")

        sec = self.Security
        sec2 = self.SecondSecurity
        if sec is not None and sec2 is not None \
                and sec.Multiplier is not None and sec2.Multiplier is not None \
                and sec.Multiplier != sec2.Multiplier:
            self.LogWarning("Contract size mismatch between {0} and {1}. Trading disabled.".format(
                sec.Code, sec2.Code))
            self._contracts_match = False

        primary_subscription = self.SubscribeCandles(self.CandleType)
        primary_subscription.Bind(self._process_primary_candle).Start()

        secondary_subscription = self.SubscribeCandles(self.CandleType, security=self.SecondSecurity)
        secondary_subscription.Bind(self._process_secondary_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, primary_subscription)
            self.DrawOwnTrades(area)

    def _process_primary_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._last_first_close = float(candle.ClosePrice)
        with self._lock:
            self._first_pending.append(candle)
            self._process_pending_candles()

    def _process_secondary_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._last_second_close = float(candle.ClosePrice)
        with self._lock:
            self._second_pending.append(candle)
            self._process_pending_candles()

    def _process_pending_candles(self):
        while len(self._first_pending) > 0 and len(self._second_pending) > 0:
            first = self._first_pending[0]
            second = self._second_pending[0]

            if first is None:
                self._first_pending.popleft()
                continue

            if second is None:
                self._second_pending.popleft()
                continue

            if first.CloseTime < second.CloseTime:
                self._first_pending.popleft()
                continue

            if second.CloseTime < first.CloseTime:
                self._second_pending.popleft()
                continue

            self._first_pending.popleft()
            self._second_pending.popleft()

            self._handle_paired_candles(first, second)

    def _handle_paired_candles(self, first_candle, second_candle):
        max_history = max(self.DayBars, self.ShiftLength * 2) + 10
        self._append_history(self._first_closes, float(first_candle.ClosePrice), max_history)
        self._append_history(self._second_closes, float(second_candle.ClosePrice), max_history)

        if not self._update_profit_check(float(first_candle.ClosePrice), float(second_candle.ClosePrice)):
            return

        if not self._contracts_match:
            return

        if self.PrimaryVolume <= 0:
            return

        shift = self.ShiftLength

        if len(self._first_closes) <= shift * 2 or len(self._second_closes) <= shift * 2:
            return

        if len(self._first_closes) <= self.DayBars or len(self._second_closes) <= self.DayBars:
            return

        current_index = len(self._first_closes) - 1
        second_index = len(self._second_closes) - 1
        shift_index = current_index - shift
        shift_index2 = current_index - (shift * 2)
        day_index = current_index - self.DayBars
        second_shift_index = second_index - shift
        second_shift_index2 = second_index - (shift * 2)
        second_day_index = second_index - self.DayBars

        if shift_index < 0 or shift_index2 < 0 or day_index < 0:
            return
        if second_shift_index < 0 or second_shift_index2 < 0 or second_day_index < 0:
            return

        close_cur0 = self._first_closes[current_index]
        close_cur_shift = self._first_closes[shift_index]
        close_cur_shift2 = self._first_closes[shift_index2]
        close_cur_day = self._first_closes[day_index]

        close_sec0 = self._second_closes[second_index]
        close_sec_shift = self._second_closes[second_shift_index]
        close_sec_shift2 = self._second_closes[second_shift_index2]
        close_sec_day = self._second_closes[second_day_index]

        # Use relative (percentage) moves so the ratio comparison works
        # for instruments with different price scales.
        x1 = 0.0 if close_cur_shift == 0 else (close_cur0 - close_cur_shift) / close_cur_shift
        x2 = 0.0 if close_cur_shift2 == 0 else (close_cur_shift - close_cur_shift2) / close_cur_shift2
        y1 = 0.0 if close_sec_shift == 0 else (close_sec0 - close_sec_shift) / close_sec_shift
        y2 = 0.0 if close_sec_shift2 == 0 else (close_sec_shift - close_sec_shift2) / close_sec_shift2

        if (x1 * x2) > 0:
            sec = self.Security
            self.LogInfo("Trend detected on {0}, skipping correlation check.".format(
                sec.Code if sec is not None else "?"))
            return

        if (y1 * y2) > 0:
            sec2 = self.SecondSecurity
            self.LogInfo("Trend detected on {0}, skipping correlation check.".format(
                sec2.Code if sec2 is not None else "?"))
            return

        if (x1 * y1) <= 0:
            self.LogInfo("Negative correlation detected. Waiting for better alignment.")
            return

        a = abs(x1) + abs(x2)
        b = abs(y1) + abs(y2)

        if b == 0:
            return

        ratio = a / b

        if ratio > 3.0:
            return

        if ratio < 0.3:
            return

        second_volume = self._adjust_secondary_volume(ratio * self.PrimaryVolume)

        if second_volume <= 0:
            self.LogInfo("Secondary volume too small after adjustment. Skipping trade.")
            return

        x3 = 0.0 if close_cur_day == 0 else (close_cur0 - close_cur_day) / close_cur_day
        y3 = 0.0 if close_sec_day == 0 else (close_sec0 - close_sec_day) / close_sec_day

        primary_side = Sides.Buy if x1 * b > y1 * a else Sides.Sell
        secondary_side = Sides.Sell if primary_side == Sides.Buy else Sides.Buy

        if primary_side == Sides.Buy and (x3 * b) < (y3 * a):
            self.LogInfo("Buy signal rejected by daily confirmation check.")
            return

        if primary_side == Sides.Sell and (x3 * b) > (y3 * a):
            self.LogInfo("Sell signal rejected by daily confirmation check.")
            return

        self._open_pair(primary_side, secondary_side, second_volume)

    def _update_profit_check(self, first_close, second_close):
        primary_position = float(self.Position)
        has_secondary = self._second_position != 0

        if primary_position == 0 and not has_secondary:
            return True

        if primary_position != 0 and not has_secondary:
            self.LogInfo("Secondary position missing. Closing primary exposure.")
            self._close_primary_position()
            return False

        if primary_position == 0 and has_secondary:
            required_side = Sides.Sell if self._second_position > 0 else Sides.Buy
            self.LogInfo("Primary position missing. Opening trade to balance spread.")
            self._open_primary(required_side, self.PrimaryVolume)
            return False

        if self._first_entry_price == 0 or self._second_entry_price == 0:
            return False

        primary_volume = abs(primary_position)
        secondary_volume = abs(self._second_position)

        if primary_position > 0:
            primary_profit = (first_close - self._first_entry_price) * primary_volume
        else:
            primary_profit = (self._first_entry_price - first_close) * primary_volume

        if self._second_position > 0:
            secondary_profit = (second_close - self._second_entry_price) * secondary_volume
        else:
            secondary_profit = (self._second_entry_price - second_close) * secondary_volume

        total_profit = primary_profit + secondary_profit

        if total_profit >= self.TargetProfit:
            self.LogInfo("Target profit reached ({0:.2f}). Closing both legs.".format(total_profit))
            self._close_pair()

        return False

    def _open_pair(self, primary_side, secondary_side, secondary_volume):
        self._open_secondary(secondary_side, secondary_volume)
        self._open_primary(primary_side, self.PrimaryVolume)

        sec = self.Security
        sec2 = self.SecondSecurity
        self.LogInfo("Opened spread: {0} {1} {2}, {3} {4} {5}.".format(
            primary_side, self.PrimaryVolume,
            sec.Code if sec is not None else "?",
            secondary_side, secondary_volume,
            sec2.Code if sec2 is not None else "?"))

    def _open_primary(self, side, volume):
        if volume <= 0:
            return

        if side == Sides.Buy:
            self.BuyMarket(volume)
        else:
            self.SellMarket(volume)

        self._first_entry_price = self._last_first_close

    def _open_secondary(self, side, volume):
        if volume <= 0 or self.SecondSecurity is None or self._second_portfolio is None:
            return

        order = self.CreateOrder(side, self._last_second_close, volume)
        order.Type = OrderTypes.Market
        order.Security = self.SecondSecurity
        order.Portfolio = self._second_portfolio

        self.RegisterOrder(order)

        self._second_position = volume if side == Sides.Buy else -volume
        self._second_entry_price = self._last_second_close

    def _close_pair(self):
        self._close_primary_position()
        self._close_secondary_position()

    def _close_primary_position(self):
        primary_position = float(self.Position)

        if primary_position > 0:
            self.SellMarket(primary_position)
        elif primary_position < 0:
            self.BuyMarket(abs(primary_position))

        self._first_entry_price = 0.0

    def _close_secondary_position(self):
        if self._second_position == 0 or self.SecondSecurity is None or self._second_portfolio is None:
            return

        side = Sides.Sell if self._second_position > 0 else Sides.Buy
        volume = abs(self._second_position)

        order = self.CreateOrder(side, self._last_second_close, volume)
        order.Type = OrderTypes.Market
        order.Security = self.SecondSecurity
        order.Portfolio = self._second_portfolio

        self.RegisterOrder(order)

        self._second_position = 0.0
        self._second_entry_price = 0.0

    def _adjust_secondary_volume(self, requested_volume):
        if self.SecondSecurity is None:
            return 0.0

        volume = abs(requested_volume)
        step = self.SecondSecurity.VolumeStep
        if step is not None:
            step = float(step)
            if step > 0:
                volume = math.floor(volume / step) * step

        min_vol = self.SecondSecurity.MinVolume
        if min_vol is not None:
            min_vol = float(min_vol)
            if min_vol > 0 and volume < min_vol:
                return 0.0

        max_vol = self.SecondSecurity.MaxVolume
        if max_vol is not None:
            max_vol = float(max_vol)
            if volume > max_vol:
                volume = max_vol

        return volume

    @staticmethod
    def _append_history(storage, value, max_history):
        storage.append(value)
        if len(storage) > max_history:
            storage.pop(0)

    def CreateClone(self):
        return spreader2_strategy()
