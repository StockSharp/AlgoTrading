import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import SmoothedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class alligator_simple_strategy(Strategy):
    """
    Simplified Bill Williams Alligator breakout strategy.
    Buys when Lips > Teeth > Jaw (upward expansion).
    Sells when Lips < Teeth < Jaw (downward expansion).
    Uses stop-loss and take-profit for risk management.
    """

    def __init__(self):
        super(alligator_simple_strategy, self).__init__()

        self._jaw_period = self.Param("JawPeriod", 13) \
            .SetGreaterThanZero() \
            .SetDisplay("Jaw Period", "Alligator jaw period", "Alligator")

        self._teeth_period = self.Param("TeethPeriod", 8) \
            .SetGreaterThanZero() \
            .SetDisplay("Teeth Period", "Alligator teeth period", "Alligator")

        self._lips_period = self.Param("LipsPeriod", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Lips Period", "Alligator lips period", "Alligator")

        self._stop_loss_points = self.Param("StopLossPoints", 200) \
            .SetDisplay("Stop Loss", "Stop-loss distance in price steps", "Risk")

        self._take_profit_points = self.Param("TakeProfitPoints", 400) \
            .SetDisplay("Take Profit", "Take-profit distance in price steps", "Risk")

        self._entry_price = 0.0
        self._cooldown = 0

    @property
    def JawPeriod(self):
        return self._jaw_period.Value

    @JawPeriod.setter
    def JawPeriod(self, value):
        self._jaw_period.Value = value

    @property
    def TeethPeriod(self):
        return self._teeth_period.Value

    @TeethPeriod.setter
    def TeethPeriod(self, value):
        self._teeth_period.Value = value

    @property
    def LipsPeriod(self):
        return self._lips_period.Value

    @LipsPeriod.setter
    def LipsPeriod(self, value):
        self._lips_period.Value = value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @StopLossPoints.setter
    def StopLossPoints(self, value):
        self._stop_loss_points.Value = value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @TakeProfitPoints.setter
    def TakeProfitPoints(self, value):
        self._take_profit_points.Value = value

    def OnReseted(self):
        super(alligator_simple_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(alligator_simple_strategy, self).OnStarted2(time)

        jaw = SmoothedMovingAverage()
        jaw.Length = self.JawPeriod
        teeth = SmoothedMovingAverage()
        teeth.Length = self.TeethPeriod
        lips = SmoothedMovingAverage()
        lips.Length = self.LipsPeriod

        subscription = self.SubscribeCandles(tf(5))
        subscription.Bind(jaw, teeth, lips, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, jaw_value, teeth_value, lips_value):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        close = float(candle.ClosePrice)
        step = 1.0

        # Check SL/TP for existing positions
        if self.Position > 0 and self._entry_price > 0:
            if self.StopLossPoints > 0 and close <= self._entry_price - self.StopLossPoints * step:
                self.SellMarket()
                self._entry_price = 0.0
                self._cooldown = 110
                return
            if self.TakeProfitPoints > 0 and close >= self._entry_price + self.TakeProfitPoints * step:
                self.SellMarket()
                self._entry_price = 0.0
                self._cooldown = 110
                return
        elif self.Position < 0 and self._entry_price > 0:
            if self.StopLossPoints > 0 and close >= self._entry_price + self.StopLossPoints * step:
                self.BuyMarket()
                self._entry_price = 0.0
                self._cooldown = 110
                return
            if self.TakeProfitPoints > 0 and close <= self._entry_price - self.TakeProfitPoints * step:
                self.BuyMarket()
                self._entry_price = 0.0
                self._cooldown = 110
                return

        # Buy when lips > teeth > jaw (Alligator opening upward)
        if lips_value > teeth_value and teeth_value > jaw_value and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
            self._cooldown = 110
        # Sell when lips < teeth < jaw (Alligator opening downward)
        elif lips_value < teeth_value and teeth_value < jaw_value and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
            self._cooldown = 110

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return alligator_simple_strategy()
