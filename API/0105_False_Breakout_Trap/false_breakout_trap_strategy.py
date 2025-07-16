import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Sides
from StockSharp.Algo.Indicators import SimpleMovingAverage, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class false_breakout_trap_strategy(Strategy):
    """
    Strategy that trades false breakouts of support and resistance levels.

    """

    def __init__(self):
        super(false_breakout_trap_strategy, self).__init__()

        # Candle type and timeframe for the strategy.
        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use for analysis", "General")

        # Period for high/low range detection.
        self._lookbackPeriod = self.Param("LookbackPeriod", 20) \
            .SetDisplay("Lookback Period", "Period for high/low range detection", "Range") \
            .SetRange(5, 50)

        # Period for moving average calculation.
        self._maPeriod = self.Param("MaPeriod", 20) \
            .SetDisplay("MA Period", "Period for moving average calculation", "Trend") \
            .SetRange(5, 50)

        # Stop-loss percentage from entry price.
        self._stopLossPercent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop-loss percentage from entry price", "Protection") \
            .SetRange(0.5, 5.0)

        self._ma = None
        self._highest = None
        self._lowest = None

        self._lastHighestValue = 0.0
        self._lastLowestValue = 0.0
        self._breakoutDetected = False
        self._breakoutSide = None
        self._breakoutPrice = 0.0

    @property
    def candle_type(self):
        """Candle type and timeframe for the strategy."""
        return self._candleType.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candleType.Value = value

    @property
    def lookback_period(self):
        """Period for high/low range detection."""
        return self._lookbackPeriod.Value

    @lookback_period.setter
    def lookback_period(self, value):
        self._lookbackPeriod.Value = value

    @property
    def ma_period(self):
        """Period for moving average calculation."""
        return self._maPeriod.Value

    @ma_period.setter
    def ma_period(self, value):
        self._maPeriod.Value = value

    @property
    def stop_loss_percent(self):
        """Stop-loss percentage from entry price."""
        return self._stopLossPercent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stopLossPercent.Value = value

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(false_breakout_trap_strategy, self).OnReseted()
        self._lastHighestValue = 0.0
        self._lastLowestValue = 0.0
        self._breakoutDetected = False
        self._breakoutSide = None
        self._breakoutPrice = 0.0

    def OnStarted(self, time):
        super(false_breakout_trap_strategy, self).OnStarted(time)

        self._lastHighestValue = 0.0
        self._lastLowestValue = 0.0
        self._breakoutDetected = False
        self._breakoutSide = None
        self._breakoutPrice = 0.0

        # Initialize indicators
        self._ma = SimpleMovingAverage()
        self._ma.Length = self.ma_period
        self._highest = Highest()
        self._highest.Length = self.lookback_period
        self._lowest = Lowest()
        self._lowest.Length = self.lookback_period

        self._breakoutDetected = False
        self._breakoutSide = None

        # Create and setup subscription for candles
        subscription = self.SubscribeCandles(self.candle_type)

        # Bind indicators and processor
        subscription.Bind(self._ma, self._highest, self._lowest, self.ProcessCandle).Start()

        # Enable stop-loss protection
        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )
        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ma, highest, lowest):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Store the last highest and lowest values
        self._lastHighestValue = highest
        self._lastLowestValue = lowest

        # First, check if we're already tracking a potential false breakout
        if self._breakoutDetected:
            # Check for false breakout confirmation
            if self._breakoutSide == Sides.Buy:
                # A false upside breakout is confirmed when price falls back below MA
                if candle.ClosePrice < ma:
                    # Enter short position
                    volume = self.Volume + Math.Abs(self.Position)
                    self.SellMarket(volume)

                    self.LogInfo("False upside breakout confirmed. Short entry at {0}. Resistance level: {1}".format(
                        candle.ClosePrice, self._lastHighestValue))

                    # Reset breakout detection
                    self._breakoutDetected = False
                    self._breakoutSide = None
            elif self._breakoutSide == Sides.Sell:
                # A false downside breakout is confirmed when price rises back above MA
                if candle.ClosePrice > ma:
                    # Enter long position
                    volume = self.Volume + Math.Abs(self.Position)
                    self.BuyMarket(volume)

                    self.LogInfo("False downside breakout confirmed. Long entry at {0}. Support level: {1}".format(
                        candle.ClosePrice, self._lastLowestValue))

                    # Reset breakout detection
                    self._breakoutDetected = False
                    self._breakoutSide = None

            # If the breakout continues beyond our threshold, abandon the false breakout idea
            if self._breakoutSide == Sides.Buy and candle.ClosePrice > self._breakoutPrice * 1.01:
                self.LogInfo("Breakout appears genuine, not a false breakout. Abandoning the setup.")
                self._breakoutDetected = False
                self._breakoutSide = None
            elif self._breakoutSide == Sides.Sell and candle.ClosePrice < self._breakoutPrice * 0.99:
                self.LogInfo("Breakout appears genuine, not a false breakout. Abandoning the setup.")
                self._breakoutDetected = False
                self._breakoutSide = None
        else:
            # Check for potential breakout
            if candle.HighPrice > self._lastHighestValue:
                # Potential upside breakout
                self._breakoutDetected = True
                self._breakoutSide = Sides.Buy
                self._breakoutPrice = candle.ClosePrice

                self.LogInfo("Potential upside breakout detected at {0}. Watching for false breakout pattern.".format(
                    candle.HighPrice))
            elif candle.LowPrice < self._lastLowestValue:
                # Potential downside breakout
                self._breakoutDetected = True
                self._breakoutSide = Sides.Sell
                self._breakoutPrice = candle.ClosePrice

                self.LogInfo("Potential downside breakout detected at {0}. Watching for false breakout pattern.".format(
                    candle.LowPrice))

        # Exit conditions based on price crossing the moving average
        if self.Position > 0 and candle.ClosePrice < ma:
            # Exit long position when price falls below MA
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo("Exit signal: Price below MA. Closed long position at {0}".format(
                candle.ClosePrice))
        elif self.Position < 0 and candle.ClosePrice > ma:
            # Exit short position when price rises above MA
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exit signal: Price above MA. Closed short position at {0}".format(
                candle.ClosePrice))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return false_breakout_trap_strategy()
