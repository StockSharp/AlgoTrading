import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import VolumeWeightedMovingAverage, WilliamsR
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class vwap_williams_r_strategy(Strategy):
    """
    Strategy based on VWAP and Williams %R indicators (#201)
    """

    def __init__(self):
        super(vwap_williams_r_strategy, self).__init__()

        # Store previous values
        self._previous_williams_r = 0.0

        # Initialize strategy parameters
        self._williams_r_period = self.Param("WilliamsRPeriod", 14) \
            .SetRange(5, 50) \
            .SetDisplay("Williams %R Period", "Period for Williams %R indicator", "Indicators") \
            .SetCanOptimize(True)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetRange(0.5, 5.0) \
            .SetDisplay("Stop-Loss %", "Stop-loss percentage from entry price", "Risk Management") \
            .SetCanOptimize(True)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def williams_r_period(self):
        """Williams %R period"""
        return self._williams_r_period.Value

    @williams_r_period.setter
    def williams_r_period(self, value):
        self._williams_r_period.Value = value

    @property
    def stop_loss_percent(self):
        """Stop-loss percentage"""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def candle_type(self):
        """Candle type for strategy"""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        """!! REQUIRED!! Returns securities this strategy works with."""
        return [(self.Security, self.candle_type)]

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(vwap_williams_r_strategy, self).OnStarted(time)

        self._previous_williams_r = 0.0

        # Initialize indicators
        vwap = VolumeWeightedMovingAverage()
        williams_r = WilliamsR()
        williams_r.Length = self.williams_r_period

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(vwap, williams_r, self.ProcessCandle).Start()

        # Enable stop-loss protection
        self.StartProtection(Unit(self.stop_loss_percent, UnitTypes.Percent), None)

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, vwap)
            self.DrawIndicator(area, williams_r)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, vwap_value, williams_r_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Store previous value to detect changes
        previous_williams_r = self._previous_williams_r
        self._previous_williams_r = williams_r_value

        # Trading logic:
        # Long: Price < VWAP && Williams %R < -80 (oversold below VWAP)
        # Short: Price > VWAP && Williams %R > -20 (overbought above VWAP)

        price = candle.ClosePrice

        if price < vwap_value and williams_r_value < -80 and self.Position <= 0:
            # Buy signal - oversold condition below VWAP
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
        elif price > vwap_value and williams_r_value > -20 and self.Position >= 0:
            # Sell signal - overbought condition above VWAP
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
        # Exit conditions
        elif self.Position > 0 and price > vwap_value:
            # Exit long position when price breaks above VWAP
            self.SellMarket(self.Position)
        elif self.Position < 0 and price < vwap_value:
            # Exit short position when price breaks below VWAP
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return vwap_williams_r_strategy()
