import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class macd_volume_strategy(Strategy):
    """
    Strategy combining MACD crossover with trend confirmation.
    Enters on MACD line crossing Signal line.
    """

    def __init__(self):
        super(macd_volume_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._cooldown_bars = self.Param("CooldownBars", 100) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General") \
            .SetRange(5, 500)

        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnStarted(self, time):
        super(macd_volume_strategy, self).OnStarted(time)

        self._cooldown = 0

        macd = MovingAverageConvergenceDivergenceSignal()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

            macd_area = self.CreateChartArea()
            if macd_area is not None:
                self.DrawIndicator(macd_area, macd)

    def ProcessCandle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        if macd_value.Macd is None or macd_value.Signal is None:
            return

        macd_line = float(macd_value.Macd)
        signal_line = float(macd_value.Signal)

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        # Entry: MACD bullish
        if macd_line > signal_line and self.Position == 0:
            self.BuyMarket()
            self._cooldown = self.cooldown_bars
        # Entry: MACD bearish
        elif macd_line < signal_line and self.Position == 0:
            self.SellMarket()
            self._cooldown = self.cooldown_bars

        # Exit on MACD crossover against position
        if self.Position > 0 and macd_line < signal_line:
            self.SellMarket()
            self._cooldown = self.cooldown_bars
        elif self.Position < 0 and macd_line > signal_line:
            self.BuyMarket()
            self._cooldown = self.cooldown_bars

    def OnReseted(self):
        super(macd_volume_strategy, self).OnReseted()
        self._cooldown = 0

    def CreateClone(self):
        return macd_volume_strategy()
