import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, UnitTypes, Unit
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class stochastic_rsi_cross_strategy(Strategy):
    """
    Strategy based on Stochastic RSI crossover.
    It trades the crossover of %K and %D lines in overbought/oversold zones.
    Note: This uses regular Stochastic as StockSharp doesn't have built-in Stochastic RSI.
    
    """
    
    def __init__(self):
        super(stochastic_rsi_cross_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Period for RSI calculation", "Indicators")
        
        self._stoch_period = self.Param("StochPeriod", 14) \
            .SetDisplay("Stochastic Period", "Period for Stochastic", "Indicators")
        
        self._k_period = self.Param("KPeriod", 3) \
            .SetDisplay("K Period", "Period for %K line", "Indicators")
        
        self._d_period = self.Param("DPeriod", 3) \
            .SetDisplay("D Period", "Period for %D line", "Indicators")
        
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
        
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        
        # Cache for K and D values
        self._prev_k = 0.0
        self._prev_d = 0.0
        self._is_first_candle = True

    @property
    def rsi_period(self):
        """RSI period."""
        return self._rsi_period.Value

    @rsi_period.setter
    def rsi_period(self, value):
        self._rsi_period.Value = value

    @property
    def stoch_period(self):
        """Stochastic period."""
        return self._stoch_period.Value

    @stoch_period.setter
    def stoch_period(self, value):
        self._stoch_period.Value = value

    @property
    def k_period(self):
        """K period (fast)."""
        return self._k_period.Value

    @k_period.setter
    def k_period(self, value):
        self._k_period.Value = value

    @property
    def d_period(self):
        """D period (slow)."""
        return self._d_period.Value

    @d_period.setter
    def d_period(self, value):
        self._d_period.Value = value

    @property
    def stop_loss_percent(self):
        """Stop loss percentage."""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def candle_type(self):
        """Candle type."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(stochastic_rsi_cross_strategy, self).OnReseted()
        self._prev_k = 0.0
        self._prev_d = 0.0
        self._is_first_candle = True

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(stochastic_rsi_cross_strategy, self).OnStarted(time)

        # Reset state variables
        self._prev_k = 0.0
        self._prev_d = 0.0
        self._is_first_candle = True

        # Create a StochRsi indicator (simulated using regular Stochastic)
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        
        stoch = StochasticOscillator()
        stoch.K.Length = self.k_period
        stoch.D.Length = self.d_period

        # Subscribe to candles
        subscription = self.SubscribeCandles(self.candle_type)
        
        # Create a custom binding to simulate Stochastic RSI since it's not built-in
        subscription.BindEx(stoch, rsi, self.ProcessCandle).Start()

        # Enable position protection
        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )
        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawIndicator(area, stoch)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, stoch_value, rsi_value):
        """
        Processes each finished candle and executes Stochastic RSI crossover logic.
        
        :param candle: The processed candle message.
        :param stoch_value: The current value of the Stochastic indicator.
        :param rsi_value: The current value of the RSI indicator.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Extract Stochastic values
        try:
            if hasattr(stoch_value, 'K') and stoch_value.K is not None:
                k_value = float(stoch_value.K)
            else:
                return
                
            if hasattr(stoch_value, 'D') and stoch_value.D is not None:
                d_value = float(stoch_value.D)
            else:
                return
        except:
            # If we can't extract values, skip this candle
            return

        # For the first candle, just store values and return
        if self._is_first_candle:
            self._prev_k = k_value
            self._prev_d = d_value
            self._is_first_candle = False
            return

        # Check for crossovers
        k_crossed_above_d = self._prev_k <= self._prev_d and k_value > d_value
        k_crossed_below_d = self._prev_k >= self._prev_d and k_value < d_value

        # Entry logic
        if k_crossed_above_d and k_value < 20 and self.Position <= 0:
            # Buy when %K crosses above %D in oversold territory (below 20)
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
            self.LogInfo("Buy signal: Stochastic RSI %K ({0:F2}) crossed above %D ({1:F2}) in oversold zone".format(
                k_value, d_value))
        elif k_crossed_below_d and k_value > 80 and self.Position >= 0:
            # Sell when %K crosses below %D in overbought territory (above 80)
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
            self.LogInfo("Sell signal: Stochastic RSI %K ({0:F2}) crossed below %D ({1:F2}) in overbought zone".format(
                k_value, d_value))

        # Exit logic
        if self.Position > 0 and k_value > 50:
            # Exit long when %K rises above 50 (middle zone)
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo("Exiting long position: Stochastic RSI %K reached {0:F2}".format(k_value))
        elif self.Position < 0 and k_value < 50:
            # Exit short when %K falls below 50 (middle zone)
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exiting short position: Stochastic RSI %K reached {0:F2}".format(k_value))

        # Update previous values for next comparison
        self._prev_k = k_value
        self._prev_d = d_value

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return stochastic_rsi_cross_strategy()