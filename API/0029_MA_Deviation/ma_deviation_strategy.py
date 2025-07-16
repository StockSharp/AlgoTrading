import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class ma_deviation_strategy(Strategy):
    """
    Strategy that trades when price deviates significantly from its moving average.
    It opens positions when price deviates by a specified percentage from MA
    and closes when price returns to MA.
    
    """
    
    def __init__(self):
        super(ma_deviation_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._ma_period = self.Param("MAPeriod", 20) \
            .SetDisplay("MA Period", "Period for Moving Average calculation", "Technical Parameters")
        
        self._deviation_percent = self.Param("DeviationPercent", 5.0) \
            .SetDisplay("Deviation %", "Deviation percentage from MA required for entry", "Entry Parameters")
        
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for ATR calculation", "Risk Management")
        
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "ATR multiplier for stop-loss calculation", "Risk Management")
        
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "Data")

    @property
    def ma_period(self):
        """Period for Moving Average calculation (default: 20)"""
        return self._ma_period.Value

    @ma_period.setter
    def ma_period(self, value):
        self._ma_period.Value = value

    @property
    def deviation_percent(self):
        """Deviation percentage from MA required for entry (default: 5%)"""
        return self._deviation_percent.Value

    @deviation_percent.setter
    def deviation_percent(self, value):
        self._deviation_percent.Value = value

    @property
    def atr_period(self):
        """Period for ATR calculation (default: 14)"""
        return self._atr_period.Value

    @atr_period.setter
    def atr_period(self, value):
        self._atr_period.Value = value

    @property
    def atr_multiplier(self):
        """ATR multiplier for stop-loss calculation (default: 2.0)"""
        return self._atr_multiplier.Value

    @atr_multiplier.setter
    def atr_multiplier(self, value):
        self._atr_multiplier.Value = value

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
        super(ma_deviation_strategy, self).OnReseted()

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(ma_deviation_strategy, self).OnStarted(time)

        # Create indicators
        sma = SimpleMovingAverage()
        sma.Length = self.ma_period
        
        atr = AverageTrueRange()
        atr.Length = self.atr_period

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, atr, self.ProcessCandle).Start()

        # Configure chart
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ma_value, atr_value):
        """
        Process candle and check for MA deviation signals
        
        :param candle: The processed candle message.
        :param ma_value: The current value of the moving average.
        :param atr_value: The current value of the ATR indicator.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Convert values to float
        ma_decimal = float(ma_value)
        atr_decimal = float(atr_value)

        # Calculate deviation from MA as a percentage
        deviation = (candle.ClosePrice - ma_decimal) / ma_decimal * 100
        
        # Calculate stop-loss level based on ATR
        stop_loss = atr_decimal * self.atr_multiplier

        if self.Position == 0:
            # No position - check for entry signals
            if deviation < -self.deviation_percent:
                # Price is below MA by required percentage - buy (long)
                self.BuyMarket(self.Volume)
                self.LogInfo("Buy signal: Price deviation {0:F2}% below MA (threshold: -{1:F2}%)".format(
                    deviation, self.deviation_percent))
            elif deviation > self.deviation_percent:
                # Price is above MA by required percentage - sell (short)
                self.SellMarket(self.Volume)
                self.LogInfo("Sell signal: Price deviation {0:F2}% above MA (threshold: {1:F2}%)".format(
                    deviation, self.deviation_percent))
        elif self.Position > 0:
            # Long position - check for exit signal
            if candle.ClosePrice > ma_decimal:
                # Price has returned to or above MA - exit long
                self.SellMarket(self.Position)
                self.LogInfo("Exit long: Price {0} returned above MA {1:F2}".format(
                    candle.ClosePrice, ma_decimal))
        elif self.Position < 0:
            # Short position - check for exit signal
            if candle.ClosePrice < ma_decimal:
                # Price has returned to or below MA - exit short
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Exit short: Price {0} returned below MA {1:F2}".format(
                    candle.ClosePrice, ma_decimal))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return ma_deviation_strategy()