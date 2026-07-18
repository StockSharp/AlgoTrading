import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class rsi_donchian_strategy(Strategy):
    """
    Strategy based on RSI and Donchian Channel indicators.
    """

    def __init__(self):
        super(rsi_donchian_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "Period for RSI calculation", "Indicators")

        self._donchian_period = self.Param("DonchianPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Donchian Period", "Period for Donchian Channel calculation", "Indicators")

        self._cooldown_bars = self.Param("CooldownBars", 80) \
            .SetRange(1, 200) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop-loss %", "Stop-loss as percentage of entry price", "Risk Management")

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._previous_rsi = 0.0
        self._donchian_high = 0.0
        self._donchian_low = 0.0
        self._donchian_middle = 0.0
        self._current_rsi = 0.0
        self._cooldown = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rsi_donchian_strategy, self).OnReseted()
        self._previous_rsi = 0.0
        self._donchian_high = 0.0
        self._donchian_low = 0.0
        self._donchian_middle = 0.0
        self._current_rsi = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(rsi_donchian_strategy, self).OnStarted2(time)
        self._previous_rsi = 0.0
        self._donchian_high = 0.0
        self._donchian_low = 0.0
        self._donchian_middle = 0.0
        self._current_rsi = 0.0
        self._cooldown = 0

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        highest = Highest()
        highest.Length = self._donchian_period.Value

        lowest = Lowest()
        lowest.Length = self._donchian_period.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, highest, lowest, self.ProcessIndicators).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def ProcessIndicators(self, candle, rsi_value, highest_value, lowest_value):
        if candle.State != CandleStates.Finished:
            return

        self._previous_rsi = self._current_rsi
        self._current_rsi = float(rsi_value)
        self._donchian_high = float(highest_value)
        self._donchian_low = float(lowest_value)
        self._donchian_middle = (self._donchian_high + self._donchian_low) / 2.0

        if self._donchian_high == 0 or self._donchian_low == 0 or self._current_rsi == 0:
            return

        price = float(candle.ClosePrice)
        is_rsi_oversold = self._current_rsi < 30
        is_rsi_overbought = self._current_rsi > 70
        is_at_lower = price <= self._donchian_low * 1.001
        is_at_upper = price >= self._donchian_high * 0.999

        if self._cooldown > 0:
            self._cooldown -= 1

        cooldown_val = int(self._cooldown_bars.Value)

        if self._cooldown == 0 and is_rsi_oversold and is_at_lower:
            if self.Position <= 0:
                self.BuyMarket()
                self._cooldown = cooldown_val
        elif self._cooldown == 0 and is_rsi_overbought and is_at_upper:
            if self.Position >= 0:
                self.SellMarket()
                self._cooldown = cooldown_val
        elif (self.Position > 0 and price < self._donchian_middle) or \
             (self.Position < 0 and price > self._donchian_middle):
            if self.Position > 0:
                self.SellMarket()
                self._cooldown = cooldown_val
            elif self.Position < 0:
                self.BuyMarket()
                self._cooldown = cooldown_val

    def CreateClone(self):
        return rsi_donchian_strategy()
