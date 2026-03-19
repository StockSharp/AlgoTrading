import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class ais_trade_machine_strategy(Strategy):
    """
    AIS Trade Machine: EMA crossover strategy with ATR-based risk management.
    Entry on EMA cross confirmed by RSI, exit on reversal or ATR stop.
    """

    def __init__(self):
        super(ais_trade_machine_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Timeframe.", "General")

        self._fast_length = self.Param("FastLength", 10) \
            .SetDisplay("Fast EMA", "Fast EMA period.", "Indicators")

        self._slow_length = self.Param("SlowLength", 30) \
            .SetDisplay("Slow EMA", "Slow EMA period.", "Indicators")

        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period.", "Indicators")

        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period.", "Indicators")

        self._stop_multiplier = self.Param("StopMultiplier", 2.0) \
            .SetDisplay("Stop Multiplier", "ATR multiplier for stop.", "Risk")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._stop_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FastLength(self):
        return self._fast_length.Value

    @FastLength.setter
    def FastLength(self, value):
        self._fast_length.Value = value

    @property
    def SlowLength(self):
        return self._slow_length.Value

    @SlowLength.setter
    def SlowLength(self, value):
        self._slow_length.Value = value

    @property
    def RsiLength(self):
        return self._rsi_length.Value

    @RsiLength.setter
    def RsiLength(self, value):
        self._rsi_length.Value = value

    @property
    def AtrLength(self):
        return self._atr_length.Value

    @AtrLength.setter
    def AtrLength(self, value):
        self._atr_length.Value = value

    @property
    def StopMultiplier(self):
        return self._stop_multiplier.Value

    @StopMultiplier.setter
    def StopMultiplier(self, value):
        self._stop_multiplier.Value = value

    def OnReseted(self):
        super(ais_trade_machine_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._stop_price = 0.0

    def OnStarted(self, time):
        super(ais_trade_machine_strategy, self).OnStarted(time)

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._stop_price = 0.0

        fast = ExponentialMovingAverage()
        fast.Length = self.FastLength
        slow = ExponentialMovingAverage()
        slow.Length = self.SlowLength
        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiLength
        atr = AverageTrueRange()
        atr.Length = self.AtrLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast, slow, rsi, atr, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, fast_val, slow_val, rsi_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        if self._prev_fast == 0 or self._prev_slow == 0 or atr_val <= 0:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            return

        close = float(candle.ClosePrice)
        stop_dist = atr_val * self.StopMultiplier

        bullish_cross = self._prev_fast <= self._prev_slow and fast_val > slow_val
        bearish_cross = self._prev_fast >= self._prev_slow and fast_val < slow_val

        # Stop management
        if self.Position > 0 and self._stop_price > 0 and close <= self._stop_price:
            self.SellMarket()
            self._entry_price = 0.0
            self._stop_price = 0.0
        elif self.Position < 0 and self._stop_price > 0 and close >= self._stop_price:
            self.BuyMarket()
            self._entry_price = 0.0
            self._stop_price = 0.0

        # Exit on opposite cross
        if self.Position > 0 and bearish_cross:
            self.SellMarket()
            self._entry_price = 0.0
            self._stop_price = 0.0
        elif self.Position < 0 and bullish_cross:
            self.BuyMarket()
            self._entry_price = 0.0
            self._stop_price = 0.0

        # Trail stop
        if self.Position > 0:
            trail = close - stop_dist
            if trail > self._stop_price:
                self._stop_price = trail
        elif self.Position < 0 and self._stop_price > 0:
            trail = close + stop_dist
            if trail < self._stop_price:
                self._stop_price = trail

        # Entry on cross + RSI confirmation
        if self.Position == 0:
            if bullish_cross and rsi_val > 50:
                self._entry_price = close
                self._stop_price = close - stop_dist
                self.BuyMarket()
            elif bearish_cross and rsi_val < 50:
                self._entry_price = close
                self._stop_price = close + stop_dist
                self.SellMarket()

        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return ais_trade_machine_strategy()
