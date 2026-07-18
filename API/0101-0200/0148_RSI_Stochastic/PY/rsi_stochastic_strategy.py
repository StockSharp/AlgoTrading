import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class rsi_stochastic_strategy(Strategy):
    """
    Strategy combining RSI with EMA trend filter for oversold/overbought trading.
    """

    def __init__(self):
        super(rsi_stochastic_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetRange(7, 21) \
            .SetDisplay("RSI Period", "Period of the RSI indicator", "Indicators")

        self._rsi_oversold = self.Param("RsiOversold", 30.0) \
            .SetDisplay("RSI Oversold", "RSI oversold level", "Indicators")

        self._rsi_overbought = self.Param("RsiOverbought", 70.0) \
            .SetDisplay("RSI Overbought", "RSI overbought level", "Indicators")

        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetRange(10, 50) \
            .SetDisplay("EMA Period", "EMA period for trend filter", "Indicators")

        self._cooldown_bars = self.Param("CooldownBars", 100) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General") \
            .SetRange(5, 500)

        self._ema_value = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def rsi_oversold(self):
        return self._rsi_oversold.Value

    @property
    def rsi_overbought(self):
        return self._rsi_overbought.Value

    @property
    def ema_period(self):
        return self._ema_period.Value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnStarted2(self, time):
        super(rsi_stochastic_strategy, self).OnStarted2(time)

        self._ema_value = 0.0
        self._cooldown = 0

        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period

        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period

        subscription = self.SubscribeCandles(self.candle_type)

        # Bind EMA to capture value
        subscription.Bind(ema, self.OnEma)

        # Bind RSI for main logic
        subscription.Bind(rsi, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

            rsi_area = self.CreateChartArea()
            if rsi_area is not None:
                self.DrawIndicator(rsi_area, rsi)

    def OnEma(self, candle, ema_val):
        self._ema_value = float(ema_val)

    def ProcessCandle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if self._ema_value == 0:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        rv = float(rsi_value)

        # Long: RSI oversold
        if rv < self.rsi_oversold and self.Position == 0:
            self.BuyMarket()
            self._cooldown = self.cooldown_bars
        # Short: RSI overbought
        elif rv > self.rsi_overbought and self.Position == 0:
            self.SellMarket()
            self._cooldown = self.cooldown_bars

        # Exit long: RSI returns to neutral
        if self.Position > 0 and rv > 50:
            self.SellMarket()
            self._cooldown = self.cooldown_bars
        # Exit short: RSI returns to neutral
        elif self.Position < 0 and rv < 50:
            self.BuyMarket()
            self._cooldown = self.cooldown_bars

    def OnReseted(self):
        super(rsi_stochastic_strategy, self).OnReseted()
        self._ema_value = 0.0
        self._cooldown = 0

    def CreateClone(self):
        return rsi_stochastic_strategy()
