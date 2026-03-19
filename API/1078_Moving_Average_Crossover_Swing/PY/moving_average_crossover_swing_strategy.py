import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class moving_average_crossover_swing_strategy(Strategy):
    """
    Moving average crossover swing: fast/medium EMA cross with ATR-based stops.
    """

    def __init__(self):
        super(moving_average_crossover_swing_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 10).SetDisplay("Fast", "Fast EMA", "Indicators")
        self._medium_period = self.Param("MediumPeriod", 30).SetDisplay("Medium", "Medium EMA", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 20).SetDisplay("ATR", "ATR period", "Indicators")
        self._atr_stop_mult = self.Param("AtrStopMult", 5.0).SetDisplay("ATR Stop", "ATR stop mult", "Risk")
        self._atr_take_mult = self.Param("AtrTakeMult", 10.0).SetDisplay("ATR Take", "ATR take mult", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 30).SetDisplay("Cooldown", "Min bars between entries", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_fast = 0.0
        self._prev_medium = 0.0
        self._entry_price = 0.0
        self._entry_atr = 0.0
        self._has_prev = False
        self._bar_index = 0
        self._last_signal_bar = -1000000

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(moving_average_crossover_swing_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_medium = 0.0
        self._entry_price = 0.0
        self._entry_atr = 0.0
        self._has_prev = False
        self._bar_index = 0
        self._last_signal_bar = -1000000

    def OnStarted(self, time):
        super(moving_average_crossover_swing_strategy, self).OnStarted(time)
        fast = ExponentialMovingAverage()
        fast.Length = self._fast_period.Value
        medium = ExponentialMovingAverage()
        medium.Length = self._medium_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, medium, atr, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, medium)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, medium_val, atr_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        fast = float(fast_val)
        medium = float(medium_val)
        atr = float(atr_val)
        self._bar_index += 1
        if not self._has_prev:
            self._prev_fast = fast
            self._prev_medium = medium
            self._has_prev = True
            return
        can_signal = self._bar_index - self._last_signal_bar >= self._cooldown_bars.Value
        long_cross = self._prev_fast <= self._prev_medium and fast > medium
        short_cross = self._prev_fast >= self._prev_medium and fast < medium
        close = float(candle.ClosePrice)
        if can_signal and long_cross and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = close
            self._entry_atr = atr
            self._last_signal_bar = self._bar_index
        elif can_signal and short_cross and self.Position >= 0:
            self.SellMarket()
            self._entry_price = close
            self._entry_atr = atr
            self._last_signal_bar = self._bar_index
        stop_mult = float(self._atr_stop_mult.Value)
        take_mult = float(self._atr_take_mult.Value)
        if can_signal and self.Position > 0 and self._entry_atr > 0:
            stop = self._entry_price - self._entry_atr * stop_mult
            take = self._entry_price + self._entry_atr * take_mult
            if float(candle.LowPrice) <= stop or float(candle.HighPrice) >= take:
                self.SellMarket()
                self._last_signal_bar = self._bar_index
        elif can_signal and self.Position < 0 and self._entry_atr > 0:
            stop = self._entry_price + self._entry_atr * stop_mult
            take = self._entry_price - self._entry_atr * take_mult
            if float(candle.HighPrice) >= stop or float(candle.LowPrice) <= take:
                self.BuyMarket()
                self._last_signal_bar = self._bar_index
        self._prev_fast = fast
        self._prev_medium = medium

    def CreateClone(self):
        return moving_average_crossover_swing_strategy()
