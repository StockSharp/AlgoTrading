import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, VolumeWeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class vwap_rsi_strategy(Strategy):
    """
    Strategy that uses VWAP (Volume Weighted Average Price) as a reference point
    and RSI (Relative Strength Index) for oversold/overbought conditions.
    Enters positions when price is below VWAP and RSI is oversold (for longs)
    or when price is above VWAP and RSI is overbought (for shorts).

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(vwap_rsi_strategy, self).__init__()

        # Initialize strategy parameters
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Period of the RSI indicator", "Indicators")

        self._rsi_oversold = self.Param("RsiOversold", 30.0) \
            .SetDisplay("RSI Oversold", "RSI level considered oversold", "Indicators")

        self._rsi_overbought = self.Param("RsiOverbought", 70.0) \
            .SetDisplay("RSI Overbought", "RSI level considered overbought", "Indicators")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss as percentage of entry price", "Risk Management")

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def RsiOversold(self):
        return self._rsi_oversold.Value

    @RsiOversold.setter
    def RsiOversold(self, value):
        self._rsi_oversold.Value = value

    @property
    def RsiOverbought(self):
        return self._rsi_overbought.Value

    @RsiOverbought.setter
    def RsiOverbought(self, value):
        self._rsi_overbought.Value = value

    @property
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.

        :param time: The time when the strategy started.
        """
        super(vwap_rsi_strategy, self).OnStarted(time)

        # Create indicators
        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        vwap = VolumeWeightedMovingAverage()

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)

        subscription.Bind(vwap, rsi, self.ProcessCandles).Start()

        # Setup position protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),  # No take profit
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)  # Stop loss as percentage
        )

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, vwap)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def ProcessCandles(self, candle, vwapValue, rsiValue):
        """
        Process candles and indicator values.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Long entry: price below VWAP and RSI oversold
        if candle.ClosePrice < vwapValue and rsiValue < self.RsiOversold and self.Position <= 0:
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
        # Short entry: price above VWAP and RSI overbought
        elif candle.ClosePrice > vwapValue and rsiValue > self.RsiOverbought and self.Position >= 0:
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
        # Long exit: price rises above VWAP
        elif self.Position > 0 and candle.ClosePrice > vwapValue:
            self.SellMarket(Math.Abs(self.Position))
        # Short exit: price falls below VWAP
        elif self.Position < 0 and candle.ClosePrice < vwapValue:
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return vwap_rsi_strategy()
