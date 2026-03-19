import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

import math
from collections import deque
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class firebird_channel_averaging_strategy(Strategy):
    """
    Firebird Channel Averaging: grid strategy trading price deviations
    from a moving average channel. Averages into positions at configurable
    pip intervals with SL/TP management.
    """

    def __init__(self):
        super(firebird_channel_averaging_strategy, self).__init__()
        self._stop_loss_pips = self.Param("StopLossPips", 50) \
            .SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 150) \
            .SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk")
        self._ma_period = self.Param("MaPeriod", 10) \
            .SetDisplay("MA Period", "Moving average length", "Indicator")
        self._ma_shift = self.Param("MaShift", 0) \
            .SetDisplay("MA Shift", "Forward shift for moving average", "Indicator")
        self._price_percent = self.Param("PricePercent", 0.3) \
            .SetDisplay("Channel %", "Channel width percentage", "Indicator")
        self._step_pips = self.Param("StepPips", 30) \
            .SetDisplay("Step (pips)", "Distance between averaged entries", "Grid")
        self._step_exponent = self.Param("StepExponent", 0.0) \
            .SetDisplay("Step Exponent", "Power growth for step size", "Grid")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Working timeframe", "Data")

        self._entries = []
        self._ma_history = deque()
        self._is_long = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(firebird_channel_averaging_strategy, self).OnReseted()
        self._entries = []
        self._ma_history = deque()
        self._is_long = None

    def OnStarted(self, time):
        super(firebird_channel_averaging_strategy, self).OnStarted(time)

        ma = ExponentialMovingAverage()
        ma.Length = self._ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ma_val = float(ma_value)

        shifted = self._apply_shift(ma_val)
        if shifted is None:
            return

        price = float(candle.ClosePrice)
        ma = shifted

        lower_band = ma * (1.0 - self._price_percent.Value / 100.0)
        upper_band = ma * (1.0 + self._price_percent.Value / 100.0)

        pip_size = self._get_pip_size()
        base_step = self._step_pips.Value * pip_size
        if base_step <= 0:
            base_step = pip_size

        entries_count = len(self._entries)
        exp = self._step_exponent.Value
        if exp <= 0:
            step_mult = 1.0
        else:
            step_mult = math.pow(max(entries_count, 1), exp)
        current_step = base_step * step_mult
        if current_step <= 0:
            current_step = base_step

        # Try open long
        if price < lower_band:
            if entries_count == 0 or self._is_long == True:
                if entries_count == 0 or price <= self._entries[-1][0] - current_step:
                    self.BuyMarket()
                    self._entries.append((price, candle.CloseTime))
                    self._is_long = True

        # Try open short
        if price > upper_band:
            if entries_count == 0 or self._is_long == False:
                if entries_count == 0 or price >= self._entries[-1][0] + current_step:
                    self.SellMarket()
                    self._entries.append((price, candle.CloseTime))
                    self._is_long = False

        # Manage open positions
        self._manage_positions(price, pip_size)

    def _manage_positions(self, price, pip_size):
        entries_count = len(self._entries)
        if entries_count == 0:
            return

        if pip_size <= 0:
            pip_size = 0.0001

        stop_distance = self._stop_loss_pips.Value * pip_size
        take_distance = self._take_profit_pips.Value * pip_size

        avg_price = sum(e[0] for e in self._entries) / entries_count

        if self._is_long == True:
            stop_price = avg_price - (stop_distance / entries_count if entries_count > 1 else stop_distance) if stop_distance > 0 else avg_price
            take_price = avg_price + take_distance if take_distance > 0 else float('inf')

            if price <= stop_price:
                self.SellMarket()
                self._reset_entries()
                return
            if price >= take_price:
                self.SellMarket()
                self._reset_entries()
        elif self._is_long == False:
            stop_price = avg_price + (stop_distance / entries_count if entries_count > 1 else stop_distance) if stop_distance > 0 else avg_price
            take_price = avg_price - take_distance if take_distance > 0 else float('-inf')

            if price >= stop_price:
                self.BuyMarket()
                self._reset_entries()
                return
            if price <= take_price:
                self.BuyMarket()
                self._reset_entries()

    def _reset_entries(self):
        self._entries = []
        self._is_long = None

    def _apply_shift(self, ma_value):
        shift = self._ma_shift.Value
        if shift <= 0:
            return ma_value

        self._ma_history.append(ma_value)
        if len(self._ma_history) <= shift:
            return None

        while len(self._ma_history) > shift + 1:
            self._ma_history.popleft()

        return self._ma_history[0]

    def _get_pip_size(self):
        if self.Security is not None and self.Security.PriceStep is not None:
            ps = float(self.Security.PriceStep)
            if ps > 0:
                return ps
        return 0.0001

    def CreateClone(self):
        return firebird_channel_averaging_strategy()
