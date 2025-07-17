import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, UnitTypes, Unit, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class atr_reversion_strategy(Strategy):
    """
    Strategy that trades on sudden price movements measured in ATR units.
    It enters positions when price makes a significant move in one direction (N * ATR)
    and expects a reversion to the mean.
    
    """
    
    def __init__(self):
        super(atr_reversion_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for ATR calculation", "Technical Parameters")
        
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "ATR multiplier for entry signal", "Entry Parameters")
        
        self._ma_period = self.Param("MAPeriod", 20) \
            .SetDisplay("MA Period", "Period for Moving Average calculation for exit", "Exit Parameters")
        
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss as percentage from entry price", "Risk Management")
        
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "Data")
        
        # State tracking
        self._prev_close = 0.0

    @property
    def atr_period(self):
        """Period for ATR calculation (default: 14)"""
        return self._atr_period.Value

    @atr_period.setter
    def atr_period(self, value):
        self._atr_period.Value = value

    @property
    def atr_multiplier(self):
        """ATR multiplier for entry signal (default: 2.0)"""
        return self._atr_multiplier.Value

    @atr_multiplier.setter
    def atr_multiplier(self, value):
        self._atr_multiplier.Value = value

    @property
    def ma_period(self):
        """Period for Moving Average calculation for exit (default: 20)"""
        return self._ma_period.Value

    @ma_period.setter
    def ma_period(self, value):
        self._ma_period.Value = value

    @property
    def stop_loss_percent(self):
        """Stop-loss as percentage from entry price (default: 2%)"""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def candle_type(self):
        """Type of candles used for strategy calculation"""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(atr_reversion_strategy, self).OnReseted()
        self._prev_close = 0.0

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(atr_reversion_strategy, self).OnStarted(time)

        # Reset state variables
        self._prev_close = 0.0

        # Create indicators
        atr = AverageTrueRange()
        atr.Length = self.atr_period
        
        sma = SimpleMovingAverage()
        sma.Length = self.ma_period

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, sma, self.ProcessCandle).Start()

        # Configure chart
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

        # Setup protection with stop-loss
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle, atr_value, sma_value):
        """
        Process candle and check for ATR-based signals
        
        :param candle: The processed candle message.
        :param atr_value: The current value of the ATR indicator.
        :param sma_value: The current value of the SMA indicator.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Convert values to float
        atr_decimal = float(atr_value)
        sma_decimal = float(sma_value)

        # Initialize _prev_close on first formed candle
        if self._prev_close == 0:
            self._prev_close = float(candle.ClosePrice)
            return

        # Calculate price change from previous candle
        price_change = float(candle.ClosePrice - self._prev_close)
        
        # Normalize price change by ATR
        normalized_change = 0.0
        if atr_decimal > 0:
            normalized_change = price_change / atr_decimal

        if self.Position == 0:
            # No position - check for entry signals
            if normalized_change < -self.atr_multiplier:
                # Price dropped significantly (N*ATR) - buy (long) expecting reversion
                self.BuyMarket(self.Volume)
                self.LogInfo("Buy signal: Price dropped {0:F2} ATR units. Change: {1:F2}, ATR: {2:F2}".format(
                    abs(normalized_change), price_change, atr_decimal))
            elif normalized_change > self.atr_multiplier:
                # Price jumped significantly (N*ATR) - sell (short) expecting reversion
                self.SellMarket(self.Volume)
                self.LogInfo("Sell signal: Price jumped {0:F2} ATR units. Change: {1:F2}, ATR: {2:F2}".format(
                    normalized_change, price_change, atr_decimal))
        elif self.Position > 0:
            # Long position - check for exit signal
            if candle.ClosePrice > sma_decimal:
                # Price has reverted to above MA - exit long
                self.SellMarket(self.Position)
                self.LogInfo("Exit long: Price {0} reverted above MA {1:F2}".format(
                    candle.ClosePrice, sma_decimal))
        elif self.Position < 0:
            # Short position - check for exit signal
            if candle.ClosePrice < sma_decimal:
                # Price has reverted to below MA - exit short
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Exit short: Price {0} reverted below MA {1:F2}".format(
                    candle.ClosePrice, sma_decimal))

        # Update previous close price
        self._prev_close = float(candle.ClosePrice)

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return atr_reversion_strategy()