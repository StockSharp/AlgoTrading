import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, UnitTypes, Unit, CandleStates
from StockSharp.Algo.Indicators import VolumeWeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class vwap_reversion_strategy(Strategy):
    """
    VWAP Reversion strategy that trades on deviations from Volume Weighted Average Price.
    It opens positions when price deviates by a specified percentage from VWAP
    and exits when price returns to VWAP.
    
    """
    
    def __init__(self):
        super(vwap_reversion_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._deviation_percent = self.Param("DeviationPercent", 2.0) \
            .SetDisplay("Deviation %", "Deviation percentage from VWAP required for entry", "Entry Parameters")
        
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss as percentage from entry price", "Risk Management")
        
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "Data")

    @property
    def deviation_percent(self):
        """Deviation percentage from VWAP required for entry (default: 2%)"""
        return self._deviation_percent.Value

    @deviation_percent.setter
    def deviation_percent(self, value):
        self._deviation_percent.Value = value

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
        super(vwap_reversion_strategy, self).OnReseted()

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(vwap_reversion_strategy, self).OnStarted(time)

        # Create the VWAP indicator
        vwap = VolumeWeightedMovingAverage()

        # Create subscription and bind VWAP indicator
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(vwap, self.ProcessCandle).Start()

        # Configure chart
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, vwap)
            self.DrawOwnTrades(area)

        # Setup protection with stop-loss
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle, vwap_value):
        """
        Process candle and check for VWAP deviation signals
        
        :param candle: The processed candle message.
        :param vwap_value: The current value of the VWAP indicator.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Convert VWAP value to float
        vwap_decimal = float(vwap_value)

        # Calculate deviation from VWAP
        deviation_ratio = 0.0
        
        if vwap_decimal > 0:
            deviation_ratio = float((candle.ClosePrice - vwap_decimal) / vwap_decimal)

        # Convert ratio to percentage
        deviation_percent = deviation_ratio * 100
        
        deviation_threshold = self.deviation_percent / 100  # Convert percentage to ratio for comparison

        if self.Position == 0:
            # No position - check for entry signals
            if deviation_ratio < -deviation_threshold:
                # Price is below VWAP by required percentage - buy (long)
                self.BuyMarket(self.Volume)
                self.LogInfo("Buy signal: Price {0:F2}% below VWAP. Price: {1}, VWAP: {2:F2}".format(
                    abs(deviation_percent), candle.ClosePrice, vwap_decimal))
            elif deviation_ratio > deviation_threshold:
                # Price is above VWAP by required percentage - sell (short)
                self.SellMarket(self.Volume)
                self.LogInfo("Sell signal: Price {0:F2}% above VWAP. Price: {1}, VWAP: {2:F2}".format(
                    deviation_percent, candle.ClosePrice, vwap_decimal))
        elif self.Position > 0:
            # Long position - check for exit signal
            if candle.ClosePrice > vwap_decimal:
                # Price has returned to or above VWAP - exit long
                self.SellMarket(self.Position)
                self.LogInfo("Exit long: Price {0} returned above VWAP {1:F2}".format(
                    candle.ClosePrice, vwap_decimal))
        elif self.Position < 0:
            # Short position - check for exit signal
            if candle.ClosePrice < vwap_decimal:
                # Price has returned to or below VWAP - exit short
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Exit short: Price {0} returned below VWAP {1:F2}".format(
                    candle.ClosePrice, vwap_decimal))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return vwap_reversion_strategy()