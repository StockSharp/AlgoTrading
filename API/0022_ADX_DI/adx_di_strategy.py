import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, UnitTypes, Unit
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class adx_di_strategy(Strategy):
    """
    Strategy based on ADX and Directional Movement indicators.
    It enters long when ADX is strong and +DI > -DI,
    and short when ADX is strong and -DI > +DI.
    
    """
    
    def __init__(self):
        super(adx_di_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetDisplay("ADX Period", "Period for ADX calculation", "Indicators")
        
        self._adx_threshold = self.Param("AdxThreshold", 25.0) \
            .SetDisplay("ADX Threshold", "ADX level to confirm trend", "Indicators")
        
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR stop loss", "Risk Management")
        
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def adx_period(self):
        """ADX period."""
        return self._adx_period.Value

    @adx_period.setter
    def adx_period(self, value):
        self._adx_period.Value = value

    @property
    def adx_threshold(self):
        """ADX threshold for trend confirmation."""
        return self._adx_threshold.Value

    @adx_threshold.setter
    def adx_threshold(self, value):
        self._adx_threshold.Value = value

    @property
    def atr_multiplier(self):
        """ATR multiplier for stop-loss."""
        return self._atr_multiplier.Value

    @atr_multiplier.setter
    def atr_multiplier(self, value):
        self._atr_multiplier.Value = value

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
        super(adx_di_strategy, self).OnReseted()

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(adx_di_strategy, self).OnStarted(time)

        # Create ADX Indicator
        adx = AverageDirectionalIndex()
        adx.Length = self.adx_period
        
        atr = AverageTrueRange()
        atr.Length = 14

        # Subscribe to candles
        subscription = self.SubscribeCandles(self.candle_type)
        
        # Bind indicators and process candles
        subscription.BindEx(adx, atr, self.ProcessCandle).Start()

        # Enable position protection
        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(self.atr_multiplier, UnitTypes.Absolute)
        )
        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, adx)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, adx_value, atr_value):
        """
        Processes each finished candle and executes ADX/DI logic.
        
        :param candle: The processed candle message.
        :param adx_value: The current value of the ADX indicator.
        :param atr_value: The current value of the ATR indicator.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Get ADX and +DI/-DI values
        try:
            if hasattr(adx_value, 'MovingAverage') and adx_value.MovingAverage is not None:
                adx_main = float(adx_value.MovingAverage)
            else:
                return
                
            if hasattr(adx_value, 'Dx') and adx_value.Dx is not None:
                if hasattr(adx_value.Dx, 'Plus') and adx_value.Dx.Plus is not None:
                    plus_di = float(adx_value.Dx.Plus)
                else:
                    return
                    
                if hasattr(adx_value.Dx, 'Minus') and adx_value.Dx.Minus is not None:
                    minus_di = float(adx_value.Dx.Minus)
                else:
                    return
            else:
                return
        except:
            # If we can't extract values, skip this candle
            return

        # Trading logic
        if adx_main >= self.adx_threshold:
            # Strong trend detected
            
            # Long signal: +DI > -DI
            if plus_di > minus_di and self.Position <= 0:
                volume = self.Volume + Math.Abs(self.Position)
                self.BuyMarket(volume)
                self.LogInfo("Buy signal: ADX = {0:F2}, +DI = {1:F2}, -DI = {2:F2}".format(
                    adx_main, plus_di, minus_di))
            # Short signal: -DI > +DI
            elif minus_di > plus_di and self.Position >= 0:
                volume = self.Volume + Math.Abs(self.Position)
                self.SellMarket(volume)
                self.LogInfo("Sell signal: ADX = {0:F2}, +DI = {1:F2}, -DI = {2:F2}".format(
                    adx_main, plus_di, minus_di))

        # Exit logic when trend weakens
        if adx_main < 20:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
                self.LogInfo("Exiting long position: ADX weakened to {0:F2}".format(adx_main))
            elif self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Exiting short position: ADX weakened to {0:F2}".format(adx_main))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return adx_di_strategy()
