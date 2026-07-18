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

class acb1_strategy(Strategy):
    """
    Breakout strategy converted from the "ACB1" MetaTrader expert advisor.
    Enters on breakouts above previous candle high / below previous candle low,
    with trailing stop based on ATR.
    """

    def __init__(self):
        super(acb1_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Timeframe for breakout detection.", "General")

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for ATR indicator used in trailing.", "Indicators")

        self._take_factor = self.Param("TakeFactor", 2.0) \
            .SetDisplay("Take Factor", "ATR multiplier for take profit distance.", "Execution")

        self._trail_factor = self.Param("TrailFactor", 1.5) \
            .SetDisplay("Trail Factor", "ATR multiplier for trailing stop distance.", "Execution")

        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_close = 0.0
        self._prev_mid = 0.0
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._has_prev = False

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
        super(acb1_strategy, self).OnReseted()
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_close = 0.0
        self._prev_mid = 0.0
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(acb1_strategy, self).OnStarted2(time)

        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_close = 0.0
        self._prev_mid = 0.0
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._has_prev = False

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
            return

        # Manage open position
        if self.Position != 0:
            if self.Position > 0:
                # Trailing stop for long
                new_stop = float(candle.ClosePrice) - atr_value * self.TrailFactor
                if new_stop > self._stop_price:
                    self._stop_price = new_stop

                # Check stop hit
                if float(candle.LowPrice) <= self._stop_price:
                    self.SellMarket()
                    self._entry_price = 0.0
                    self._stop_price = 0.0
                # Check take profit
                elif self._entry_price > 0 and float(candle.HighPrice) >= self._entry_price + atr_value * self.TakeFactor:
                    self.SellMarket()
                    self._entry_price = 0.0
                    self._stop_price = 0.0
            else:
                # Trailing stop for short
                new_stop = float(candle.ClosePrice) + atr_value * self.TrailFactor
                if self._stop_price == 0 or new_stop < self._stop_price:
                    self._stop_price = new_stop

                # Check stop hit
                if float(candle.HighPrice) >= self._stop_price:
                    self.BuyMarket()
                    self._entry_price = 0.0
                    self._stop_price = 0.0
                # Check take profit
                elif self._entry_price > 0 and float(candle.LowPrice) <= self._entry_price - atr_value * self.TakeFactor:
                    self.BuyMarket()
                    self._entry_price = 0.0
                    self._stop_price = 0.0

        # Entry logic after managing position
        if self._has_prev and self.Position == 0:
            if self._prev_close > self._prev_mid and float(candle.ClosePrice) > self._prev_high:
                # Bullish breakout
                self.BuyMarket()
                self._entry_price = float(candle.ClosePrice)
                self._stop_price = self._prev_low
            elif self._prev_close < self._prev_mid and float(candle.ClosePrice) < self._prev_low:
                # Bearish breakout
                self.SellMarket()
                self._entry_price = float(candle.ClosePrice)
                self._stop_price = self._prev_high

        # Store for next candle
        self._prev_high = float(candle.HighPrice)
        self._prev_low = float(candle.LowPrice)
        self._prev_close = float(candle.ClosePrice)
        self._prev_mid = (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0
        self._has_prev = True

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return acb1_strategy()
