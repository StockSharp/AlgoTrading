import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

import random

class cci_put_call_ratio_divergence_strategy(Strategy):
    """CCI strategy with Put/Call Ratio Divergence."""

    def __init__(self):
        super(cci_put_call_ratio_divergence_strategy, self).__init__()

        # Initialize strategy parameters
        self._cci_period = self.Param("CciPeriod", 20) \
            .SetRange(10, 50) \
            .SetCanOptimize(True) \
            .SetDisplay("CCI Period", "Period for CCI calculation", "Indicators")

        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetRange(1.0, 5.0) \
            .SetCanOptimize(True) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR-based stop loss", "Risk Management")

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Indicators will be created in OnStarted
        self._cci = None
        self._atr = None

        # Internal state variables
        self._prev_pcr = 0.0
        self._current_pcr = 0.0
        self._prev_price = 0.0

    # region Properties
    @property
    def CciPeriod(self):
        """CCI Period."""
        return self._cci_period.Value

    @CciPeriod.setter
    def CciPeriod(self, value):
        self._cci_period.Value = value

    @property
    def AtrMultiplier(self):
        """ATR multiplier for stop loss."""
        return self._atr_multiplier.Value

    @AtrMultiplier.setter
    def AtrMultiplier(self, value):
        self._atr_multiplier.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy calculation."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value
    # endregion

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(cci_put_call_ratio_divergence_strategy, self).OnStarted(time)

        # Create indicators
        self._cci = CommodityChannelIndex()
        self._cci.Length = self.CciPeriod

        self._atr = AverageTrueRange()
        self._atr.Length = 14  # Standard ATR period

        # Initialize state variables
        self._prev_pcr = 0.0
        self._current_pcr = 0.0
        self._prev_price = 0.0

        # Create candle subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicators
        subscription.BindEx(self._cci, self._atr, self.ProcessCandle).Start()

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._cci)
            self.DrawIndicator(area, self._atr)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, cci_value, atr_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Get current price
        price = float(candle.ClosePrice)

        # Simulate Put/Call Ratio (in real implementation, this would come from options data)
        self.UpdatePutCallRatio(candle)

        # For first candle just initialize values
        if self._prev_price == 0:
            self._prev_price = price
            self._prev_pcr = self._current_pcr
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Check for divergences
        bullish_divergence = price < self._prev_price and self._current_pcr > self._prev_pcr
        bearish_divergence = price > self._prev_price and self._current_pcr < self._prev_pcr

        # Entry logic - using CCI with PCR divergence
        if cci_value < -100 and bullish_divergence and self.Position <= 0:
            # CCI oversold with bullish PCR divergence - Long entry
            self.BuyMarket(self.Volume)
            self.LogInfo("Buy Signal: CCI={0}, PCR={1}, Price={2}, Bullish Divergence".format(cci_value, self._current_pcr, price))
        elif cci_value > 100 and bearish_divergence and self.Position >= 0:
            # CCI overbought with bearish PCR divergence - Short entry
            self.SellMarket(self.Volume)
            self.LogInfo("Sell Signal: CCI={0}, PCR={1}, Price={2}, Bearish Divergence".format(cci_value, self._current_pcr, price))

        # Exit logic
        if self.Position > 0 and cci_value > 0:
            # Exit long position when CCI crosses above zero
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo("Exit Long: CCI={0}".format(cci_value))
        elif self.Position < 0 and cci_value < 0:
            # Exit short position when CCI crosses below zero
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exit Short: CCI={0}".format(cci_value))

        # Dynamic stop loss using ATR
        if self.Position != 0:
            stop_distance = atr_value * self.AtrMultiplier

            if self.Position > 0:
                # For long positions, set stop below entry price - ATR*multiplier
                stop_price = price - stop_distance
                self.UpdateStopLoss(stop_price)
            else:
                # For short positions, set stop above entry price + ATR*multiplier
                stop_price = price + stop_distance
                self.UpdateStopLoss(stop_price)

        # Update previous values
        self._prev_price = price
        self._prev_pcr = self._current_pcr

    def UpdatePutCallRatio(self, candle):
        # This is a placeholder for real Put/Call Ratio data
        # In a real implementation, this would connect to an options data provider

        # Base PCR on price movement (inverse relation usually exists)
        price_up = candle.OpenPrice < candle.ClosePrice
        price_change = float(Math.Abs((candle.ClosePrice - candle.OpenPrice) / candle.OpenPrice))

        if price_up:
            # When price rises, PCR often falls (less put buying)
            self._current_pcr = 0.7 - price_change + random.random() * 0.2
        else:
            # When price falls, PCR often rises (more put buying for protection)
            self._current_pcr = 1.0 + price_change + random.random() * 0.3

        # Add some randomness for market events
        if random.random() > 0.9:
            # Occasional PCR spikes
            self._current_pcr *= 1.3

        # Keep PCR in realistic bounds
        self._current_pcr = Math.Max(0.5, Math.Min(2.0, self._current_pcr))

    def UpdateStopLoss(self, stop_price):
        # In a real implementation, this would update the stop loss level
        # This could be done via order modification or canceling existing stops and placing new ones

        # For this example, we'll just log the new stop level
        self.LogInfo("Updated Stop Loss: {0}".format(stop_price))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return cci_put_call_ratio_divergence_strategy()
