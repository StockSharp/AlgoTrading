import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class a_system_championship_strategy(Strategy):
    """
    Multi-timeframe breakout strategy converted from the original MetaTrader "A System" expert advisor.
    Enters on momentum breakouts when close is above/below the midpoint of the previous candle range.
    Uses ATR-based trailing stop for position management.
    """

    def __init__(self):
        super(a_system_championship_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Timeframe for breakout detection.", "General")

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for ATR used in trailing stop.", "Indicators")

        self._take_factor = self.Param("TakeFactor", 2.5) \
            .SetDisplay("Take Factor", "ATR multiplier for take profit.", "Risk")

        self._trail_factor = self.Param("TrailFactor", 1.5) \
            .SetDisplay("Trail Factor", "ATR multiplier for trailing stop.", "Risk")

        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_close = 0.0
        self._has_prev = False
        self._entry_price = 0.0
        self._stop_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def TakeFactor(self):
        return self._take_factor.Value

    @TakeFactor.setter
    def TakeFactor(self, value):
        self._take_factor.Value = value

    @property
    def TrailFactor(self):
        return self._trail_factor.Value

    @TrailFactor.setter
    def TrailFactor(self, value):
        self._trail_factor.Value = value

    def OnReseted(self):
        super(a_system_championship_strategy, self).OnReseted()
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_close = 0.0
        self._has_prev = False
        self._entry_price = 0.0
        self._stop_price = 0.0

    def OnStarted2(self, time):
        super(a_system_championship_strategy, self).OnStarted2(time)

        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_close = 0.0
        self._has_prev = False
        self._entry_price = 0.0
        self._stop_price = 0.0

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(atr, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if atr_value <= 0:
            self._save_candle(candle)
            return

        # Manage open position
        if self.Position > 0:
            # Trail stop up
            new_stop = float(candle.ClosePrice) - atr_value * self.TrailFactor
            if new_stop > self._stop_price:
                self._stop_price = new_stop

            if float(candle.LowPrice) <= self._stop_price:
                self.SellMarket()
                self._reset_position()
            elif self._entry_price > 0 and float(candle.HighPrice) >= self._entry_price + atr_value * self.TakeFactor:
                self.SellMarket()
                self._reset_position()

        elif self.Position < 0:
            # Trail stop down
            new_stop = float(candle.ClosePrice) + atr_value * self.TrailFactor
            if self._stop_price == 0 or new_stop < self._stop_price:
                self._stop_price = new_stop

            if float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket()
                self._reset_position()
            elif self._entry_price > 0 and float(candle.LowPrice) <= self._entry_price - atr_value * self.TakeFactor:
                self.BuyMarket()
                self._reset_position()

        # Entry logic
        if self._has_prev and self.Position == 0:
            mid = (self._prev_high + self._prev_low) / 2.0

            if self._prev_close > mid and float(candle.ClosePrice) > self._prev_high:
                # Bullish breakout
                self.BuyMarket()
                self._entry_price = float(candle.ClosePrice)
                self._stop_price = float(candle.ClosePrice) - atr_value * self.TrailFactor
            elif self._prev_close < mid and float(candle.ClosePrice) < self._prev_low:
                # Bearish breakout
                self.SellMarket()
                self._entry_price = float(candle.ClosePrice)
                self._stop_price = float(candle.ClosePrice) + atr_value * self.TrailFactor

        self._save_candle(candle)

    def _save_candle(self, candle):
        self._prev_high = float(candle.HighPrice)
        self._prev_low = float(candle.LowPrice)
        self._prev_close = float(candle.ClosePrice)
        self._has_prev = True

    def _reset_position(self):
        self._entry_price = 0.0
        self._stop_price = 0.0

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return a_system_championship_strategy()
