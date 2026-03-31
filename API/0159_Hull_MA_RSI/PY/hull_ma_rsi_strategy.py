import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class hull_ma_rsi_strategy(Strategy):
    """
    Hull Moving Average + RSI strategy.
    Buy when HMA is rising and RSI is oversold.
    Sell when HMA is falling and RSI is overbought.
    """

    def __init__(self):
        super(hull_ma_rsi_strategy, self).__init__()

        self._hma_period = self.Param("HmaPeriod", 9) \
            .SetDisplay("HMA Period", "Period for Hull Moving Average", "HMA Parameters")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Period for Relative Strength Index", "RSI Parameters")
        self._rsi_oversold = self.Param("RsiOversold", 30.0) \
            .SetRange(1, 100) \
            .SetDisplay("RSI Oversold", "RSI level to consider market oversold", "RSI Parameters")
        self._rsi_overbought = self.Param("RsiOverbought", 70.0) \
            .SetRange(1, 100) \
            .SetDisplay("RSI Overbought", "RSI level to consider market overbought", "RSI Parameters")
        self._cooldown_bars = self.Param("CooldownBars", 130) \
            .SetRange(5, 500) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General")
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")

        self._hma_value = 0.0
        self._prev_hma_value = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(hull_ma_rsi_strategy, self).OnStarted2(time)
        self._hma_value = 0.0
        self._prev_hma_value = 0.0
        self._cooldown = 0

        hma = HullMovingAverage()
        hma.Length = self._hma_period.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(hma, self.OnHma)
        subscription.Bind(rsi, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hma)
            rsi_area = self.CreateChartArea()
            if rsi_area is not None:
                self.DrawIndicator(rsi_area, rsi)
            self.DrawOwnTrades(area)

    def OnHma(self, candle, hma_value):
        self._hma_value = float(hma_value)

    def ProcessCandle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        if self._hma_value == 0:
            return

        if self._prev_hma_value == 0:
            self._prev_hma_value = self._hma_value
            return

        is_hma_rising = self._hma_value > self._prev_hma_value
        is_hma_falling = self._hma_value < self._prev_hma_value
        rv = float(rsi_value)

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_hma_value = self._hma_value
            return

        cd = self._cooldown_bars.Value
        os_level = self._rsi_oversold.Value
        ob_level = self._rsi_overbought.Value

        if is_hma_rising and rv < os_level and self.Position == 0:
            self.BuyMarket()
            self._cooldown = cd
        elif is_hma_falling and rv > ob_level and self.Position == 0:
            self.SellMarket()
            self._cooldown = cd
        elif is_hma_falling and self.Position > 0:
            self.SellMarket()
            self._cooldown = cd
        elif is_hma_rising and self.Position < 0:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_hma_value = self._hma_value

    def OnReseted(self):
        super(hull_ma_rsi_strategy, self).OnReseted()
        self._hma_value = 0.0
        self._prev_hma_value = 0.0
        self._cooldown = 0

    def CreateClone(self):
        return hull_ma_rsi_strategy()
