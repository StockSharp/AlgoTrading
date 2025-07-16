import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, UnitTypes, Unit
from StockSharp.Messages import CandleStates, Sides
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class rsi_failure_swing_strategy(Strategy):
    """
    Strategy that trades based on RSI Failure Swing pattern.
    A failure swing occurs when RSI reverses direction without crossing through centerline.

    """
    def __init__(self):
        super(rsi_failure_swing_strategy, self).__init__()

        # Initialize strategy parameters
        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use for analysis", "General")

        self._rsiPeriod = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Period for RSI calculation", "RSI Settings")

        self._oversoldLevel = self.Param("OversoldLevel", 30.0) \
            .SetDisplay("Oversold Level", "RSI level considered oversold", "RSI Settings")

        self._overboughtLevel = self.Param("OverboughtLevel", 70.0) \
            .SetDisplay("Overbought Level", "RSI level considered overbought", "RSI Settings")

        self._stopLossPercent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop-loss percentage from entry price", "Protection")

        # Internal state
        self._rsi = None
        self._prevRsiValue = 0.0
        self._prevPrevRsiValue = 0.0
        self._inPosition = False
        self._positionSide = None

    @property
    def CandleType(self):
        """Candle type and timeframe for the strategy."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    @property
    def RsiPeriod(self):
        """Period for RSI calculation."""
        return self._rsiPeriod.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsiPeriod.Value = value

    @property
    def OversoldLevel(self):
        """Oversold level for RSI."""
        return self._oversoldLevel.Value

    @OversoldLevel.setter
    def OversoldLevel(self, value):
        self._oversoldLevel.Value = value

    @property
    def OverboughtLevel(self):
        """Overbought level for RSI."""
        return self._overboughtLevel.Value

    @OverboughtLevel.setter
    def OverboughtLevel(self, value):
        self._overboughtLevel.Value = value

    @property
    def StopLossPercent(self):
        """Stop-loss percentage from entry price."""
        return self._stopLossPercent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercent.Value = value

    def OnStarted(self, time):
        super(rsi_failure_swing_strategy, self).OnStarted(time)

        # Initialize indicators
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiPeriod

        self._prevRsiValue = 0.0
        self._prevPrevRsiValue = 0.0
        self._inPosition = False
        self._positionSide = None

        # Create and setup subscription for candles
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicator and processor
        subscription.Bind(self._rsi, self.ProcessCandle).Start()

        # Enable stop-loss protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._rsi)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, rsi_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Need at least 3 RSI values to detect failure swing
        if self._prevRsiValue == 0 or self._prevPrevRsiValue == 0:
            self._prevPrevRsiValue = self._prevRsiValue
            self._prevRsiValue = rsi_value
            return

        # Detect Bullish Failure Swing:
        # 1. RSI falls below oversold level
        # 2. RSI rises without crossing centerline
        # 3. RSI pulls back but stays above previous low
        # 4. RSI breaks above the high point of first rise
        isBullishFailureSwing = (self._prevPrevRsiValue < self.OversoldLevel and
                                 self._prevRsiValue > self._prevPrevRsiValue and
                                 rsi_value < self._prevRsiValue and
                                 rsi_value > self._prevPrevRsiValue)

        # Detect Bearish Failure Swing:
        # 1. RSI rises above overbought level
        # 2. RSI falls without crossing centerline
        # 3. RSI bounces up but stays below previous high
        # 4. RSI breaks below the low point of first decline
        isBearishFailureSwing = (self._prevPrevRsiValue > self.OverboughtLevel and
                                 self._prevRsiValue < self._prevPrevRsiValue and
                                 rsi_value > self._prevRsiValue and
                                 rsi_value < self._prevPrevRsiValue)

        # Trading logic
        if isBullishFailureSwing and not self._inPosition:
            # Enter long position
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)

            self._inPosition = True
            self._positionSide = Sides.Buy

            self.LogInfo(
                "Bullish RSI Failure Swing detected. RSI values: {0:F2} -> {1:F2} -> {2:F2}. Long entry at {3}".format(
                    self._prevPrevRsiValue, self._prevRsiValue, rsi_value, candle.ClosePrice))
        elif isBearishFailureSwing and not self._inPosition:
            # Enter short position
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)

            self._inPosition = True
            self._positionSide = Sides.Sell

            self.LogInfo(
                "Bearish RSI Failure Swing detected. RSI values: {0:F2} -> {1:F2} -> {2:F2}. Short entry at {3}".format(
                    self._prevPrevRsiValue, self._prevRsiValue, rsi_value, candle.ClosePrice))

        # Exit conditions
        if self._inPosition:
            # For long positions: exit when RSI crosses above 50
            if self._positionSide == Sides.Buy and rsi_value > 50:
                self.SellMarket(Math.Abs(self.Position))
                self._inPosition = False
                self._positionSide = None

                self.LogInfo(
                    "Exit signal for long position: RSI ({0:F2}) crossed above 50. Closing at {1}".format(
                        rsi_value, candle.ClosePrice))
            # For short positions: exit when RSI crosses below 50
            elif self._positionSide == Sides.Sell and rsi_value < 50:
                self.BuyMarket(Math.Abs(self.Position))
                self._inPosition = False
                self._positionSide = None

                self.LogInfo(
                    "Exit signal for short position: RSI ({0:F2}) crossed below 50. Closing at {1}".format(
                        rsi_value, candle.ClosePrice))

        # Update RSI values for next iteration
        self._prevPrevRsiValue = self._prevRsiValue
        self._prevRsiValue = rsi_value

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return rsi_failure_swing_strategy()
