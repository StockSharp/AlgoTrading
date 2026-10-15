import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

PERIODS = (8, 13, 21, 34, 55)
WEIGHTS = (5.0, 4.0, 3.0, 2.0, 1.0)
ATR_LENGTH = 14

class fibonacci_atr_fusion_strategy(Strategy):
    """
    Fibonacci ATR Fusion strategy.
    For each Fibonacci period 8, 13, 21, 34 and 55 the buying pressure (close minus the true low) summed over the period is divided
    by the true range summed over the same period. The ratios are averaged with weights 5, 4, 3, 2, 1 and scaled to 0..100. A cross
    above LongEntryThreshold goes long and a cross below ShortEntryThreshold goes short, reversing an opposite position; a long
    closes when the average crosses below LongExitThreshold and a short when it crosses above ShortExitThreshold. Three take-profit
    layers at Tp1Atr, Tp2Atr and Tp3Atr ATRs from the entry each close their percentage of the entry volume.
    """

    def __init__(self):
        super(fibonacci_atr_fusion_strategy, self).__init__()
        self._long_entry_threshold = self.Param("LongEntryThreshold", 58.0).SetDisplay("Long Entry", "Level the average crosses upward to go long", "Signals")
        self._short_entry_threshold = self.Param("ShortEntryThreshold", 42.0).SetDisplay("Short Entry", "Level the average crosses downward to go short", "Signals")
        self._long_exit_threshold = self.Param("LongExitThreshold", 42.0).SetDisplay("Long Exit", "Level the average crosses downward to close a long", "Signals")
        self._short_exit_threshold = self.Param("ShortExitThreshold", 58.0).SetDisplay("Short Exit", "Level the average crosses upward to close a short", "Signals")
        self._tp1_atr = self.Param("Tp1Atr", 3.0).SetNotNegative().SetDisplay("TP1 ATR", "First take profit distance in ATRs", "Take Profit")
        self._tp2_atr = self.Param("Tp2Atr", 8.0).SetNotNegative().SetDisplay("TP2 ATR", "Second take profit distance in ATRs", "Take Profit")
        self._tp3_atr = self.Param("Tp3Atr", 14.0).SetNotNegative().SetDisplay("TP3 ATR", "Third take profit distance in ATRs", "Take Profit")
        self._tp1_percent = self.Param("Tp1Percent", 12.0).SetNotNegative().SetDisplay("TP1 %", "Percent of the entry volume closed at the first take profit", "Take Profit")
        self._tp2_percent = self.Param("Tp2Percent", 12.0).SetNotNegative().SetDisplay("TP2 %", "Percent of the entry volume closed at the second take profit", "Take Profit")
        self._tp3_percent = self.Param("Tp3Percent", 12.0).SetNotNegative().SetDisplay("TP3 %", "Percent of the entry volume closed at the third take profit", "Take Profit")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._history = []
        self._prev_close = None
        self._prev_average = None
        self._reset_entry(0.0, 0.0, 0.0)

    def _reset_entry(self, price, atr, volume):
        self._entry_price = price
        self._entry_atr = atr
        self._entry_volume = volume
        self._tp_done = [False, False, False]

    def OnReseted(self):
        super(fibonacci_atr_fusion_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(fibonacci_atr_fusion_strategy, self).OnStarted2(time)

        self._reset_state()

        atr = AverageTrueRange()
        atr.Length = ATR_LENGTH

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        current = self._update_average(candle)
        if current is None:
            return

        last = self._prev_average
        self._prev_average = current

        if last is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        long_entry_level = float(self._long_entry_threshold.Value)
        short_entry_level = float(self._short_entry_threshold.Value)
        long_exit_level = float(self._long_exit_threshold.Value)
        short_exit_level = float(self._short_exit_threshold.Value)

        long_entry = last <= long_entry_level and current > long_entry_level
        short_entry = last >= short_entry_level and current < short_entry_level
        long_exit = last >= long_exit_level and current < long_exit_level
        short_exit = last <= short_exit_level and current > short_exit_level

        atr = float(atr_value)

        if long_entry and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._reset_entry(float(candle.ClosePrice), atr, float(self.Volume))
            return

        if short_entry and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._reset_entry(float(candle.ClosePrice), atr, float(self.Volume))
            return

        if self.Position > 0 and long_exit:
            self.SellMarket(self.Position)
            return

        if self.Position < 0 and short_exit:
            self.BuyMarket(-self.Position)
            return

        if self.Position != 0:
            self._check_take_profits(candle)

    def _update_average(self, candle):
        close = float(candle.ClosePrice)
        last_close = self._prev_close
        self._prev_close = close

        if last_close is None:
            return None

        true_low = min(float(candle.LowPrice), last_close)
        true_high = max(float(candle.HighPrice), last_close)

        self._history.append((close - true_low, true_high - true_low))

        max_period = PERIODS[-1]
        if len(self._history) > max_period:
            del self._history[0]

        if len(self._history) < max_period:
            return None

        weighted = 0.0
        total_weight = 0.0

        for period, weight in zip(PERIODS, WEIGHTS):
            window = self._history[-period:]
            bp_sum = sum(item[0] for item in window)
            tr_sum = sum(item[1] for item in window)
            if tr_sum <= 0:
                return None
            weighted += weight * bp_sum / tr_sum
            total_weight += weight

        return weighted / total_weight * 100.0

    def _check_take_profits(self, candle):
        if self._entry_atr <= 0:
            return

        distances = (float(self._tp1_atr.Value), float(self._tp2_atr.Value), float(self._tp3_atr.Value))
        percents = (float(self._tp1_percent.Value), float(self._tp2_percent.Value), float(self._tp3_percent.Value))

        for i in range(3):
            if self._tp_done[i] or distances[i] <= 0 or percents[i] <= 0:
                continue

            is_long = self.Position > 0
            target = self._entry_price + distances[i] * self._entry_atr if is_long else self._entry_price - distances[i] * self._entry_atr
            hit = float(candle.HighPrice) >= target if is_long else float(candle.LowPrice) <= target

            if not hit:
                continue

            self._tp_done[i] = True

            volume = min(abs(float(self.Position)), self._round_volume(self._entry_volume * percents[i] / 100.0))
            if volume <= 0:
                continue

            if is_long:
                self.SellMarket(Decimal(volume))
            else:
                self.BuyMarket(Decimal(volume))
            return

    def _round_volume(self, volume):
        step = float(self.Security.VolumeStep) if self.Security is not None and self.Security.VolumeStep is not None else 0.0
        return math.floor(volume / step) * step if step > 0 else volume

    def CreateClone(self):
        return fibonacci_atr_fusion_strategy()
