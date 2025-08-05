import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class atr_expansion_strategy(Strategy):
    """
    Strategy that trades on volatility expansion as measured by ATR (Average True Range).
    It enters positions when ATR is increasing (volatility expansion) and price is above/below MA,
    and exits when volatility starts to contract (ATR decreasing).
    
    """
    
    def __init__(self):
        super(atr_expansion_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for ATR calculation", "Technical Parameters")
        
        self._ma_period = self.Param("MAPeriod", 20) \
            .SetDisplay("MA Period", "Period for Moving Average calculation", "Technical Parameters")
        
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "ATR multiplier for stop-loss calculation", "Risk Management")
        
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "Data")
        
        # State tracking
        self._prev_atr = 0.0

    @property
    def atr_period(self):
        """Period for ATR calculation (default: 14)"""
        return self._atr_period.Value

    @atr_period.setter
    def atr_period(self, value):
        self._atr_period.Value = value

    @property
    def ma_period(self):
        """Period for Moving Average calculation (default: 20)"""
        return self._ma_period.Value

    @ma_period.setter
    def ma_period(self, value):
        self._ma_period.Value = value

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
        super(atr_expansion_strategy, self).OnReseted()
        self._prev_atr = 0.0

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(atr_expansion_strategy, self).OnStarted(time)

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

    def ProcessCandle(self, candle, atr_value, sma_value):
        """
        Process candle and check for ATR expansion signals
        
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

        # Initialize _prev_atr on first formed candle
        if self._prev_atr == 0:
            self._prev_atr = atr_decimal
            return

        # Check if ATR is expanding (increasing)
        is_atr_expanding = atr_decimal > self._prev_atr
        
        # Determine price position relative to MA
        is_price_above_ma = candle.ClosePrice > sma_decimal
        
        # Calculate stop-loss amount based on ATR
        stop_loss_amount = atr_decimal * self.atr_multiplier

        if self.Position == 0:
            # No position - check for entry signals
            if is_atr_expanding and is_price_above_ma:
                # ATR is expanding and price is above MA - buy (long)
                self.BuyMarket(self.Volume)
                self.LogInfo("Buy signal: ATR expanding ({0:F4} > {1:F4}) and price {2} above MA {3:F2}".format(
                    atr_decimal, self._prev_atr, candle.ClosePrice, sma_decimal))
            elif is_atr_expanding and not is_price_above_ma:
                # ATR is expanding and price is below MA - sell (short)
                self.SellMarket(self.Volume)
                self.LogInfo("Sell signal: ATR expanding ({0:F4} > {1:F4}) and price {2} below MA {3:F2}".format(
                    atr_decimal, self._prev_atr, candle.ClosePrice, sma_decimal))
        elif self.Position > 0:
            # Long position - check for exit signal
            if not is_atr_expanding:
                # ATR is decreasing (volatility contracting) - exit long
                self.SellMarket(self.Position)
                self.LogInfo("Exit long: ATR contracting ({0:F4} <= {1:F4})".format(
                    atr_decimal, self._prev_atr))
        elif self.Position < 0:
            # Short position - check for exit signal
            if not is_atr_expanding:
                # ATR is decreasing (volatility contracting) - exit short
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Exit short: ATR contracting ({0:F4} <= {1:F4})".format(
                    atr_decimal, self._prev_atr))

        # Update previous ATR value
        self._prev_atr = atr_decimal

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return atr_expansion_strategy()
