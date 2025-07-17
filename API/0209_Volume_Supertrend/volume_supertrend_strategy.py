import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange, VolumeIndicator
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class volume_supertrend_strategy(Strategy):
    """Strategy based on Volume and Supertrend indicators (#209)"""

    def __init__(self):
        """Constructor"""
        super(volume_supertrend_strategy, self).__init__()

        # Volume average period
        self._volume_avg_period = self.Param("VolumeAvgPeriod", 20) \
            .SetRange(10, 50) \
            .SetDisplay("Volume Avg Period", "Period for volume average calculation", "Volume") \
            .SetCanOptimize(True)

        # Supertrend ATR period
        self._supertrend_period = self.Param("SupertrendPeriod", 10) \
            .SetRange(5, 30) \
            .SetDisplay("Supertrend Period", "ATR period for Supertrend", "Supertrend") \
            .SetCanOptimize(True)

        # Supertrend multiplier
        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 3.0) \
            .SetRange(1.0, 5.0) \
            .SetDisplay("Supertrend Multiplier", "Multiplier for Supertrend calculation", "Supertrend") \
            .SetCanOptimize(True)

        # Stop-loss percentage
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop-loss %", "Stop-loss as percentage of entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        # Candle type for strategy
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def volume_avg_period(self):
        """Volume average period"""
        return self._volume_avg_period.Value

    @volume_avg_period.setter
    def volume_avg_period(self, value):
        self._volume_avg_period.Value = value

    @property
    def supertrend_period(self):
        """Supertrend ATR period"""
        return self._supertrend_period.Value

    @supertrend_period.setter
    def supertrend_period(self, value):
        self._supertrend_period.Value = value

    @property
    def supertrend_multiplier(self):
        """Supertrend multiplier"""
        return self._supertrend_multiplier.Value

    @supertrend_multiplier.setter
    def supertrend_multiplier(self, value):
        self._supertrend_multiplier.Value = value

    @property
    def candle_type(self):
        """Candle type for strategy"""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def StopLossPercent(self):
        """Stop-loss percentage."""
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    def OnStarted(self, time):
        super(volume_supertrend_strategy, self).OnStarted(time)

        # Initialize indicators
        volume_ma = SimpleMovingAverage()
        volume_ma.Length = self.volume_avg_period

        # Create custom Supertrend indicator - StockSharp doesn't have built-in Supertrend
        atr = AverageTrueRange()
        atr.Length = self.supertrend_period

        # Create subscription
        subscription = self.SubscribeCandles(self.candle_type)

        # Current Supertrend state variables
        supertrend_value = 0.0
        supertrend_direction = 0  # 1 for up (bullish), -1 for down (bearish)

        # Bind indicators to handle each candle
        def handle_candle(candle, atr_value):
            # Calculate volume average
            volume_value = to_float(process_float(volume_ma, candle.TotalVolume, candle.ServerTime, candle.State == CandleStates.Finished))

            # Calculate Supertrend
            if not atr.IsFormed:
                return

            high_price = float(candle.HighPrice)
            low_price = float(candle.LowPrice)
            close_price = float(candle.ClosePrice)

            # Calculate bands
            multiplier = self.supertrend_multiplier
            atr_amount = atr_value * multiplier

            upper_band = ((high_price + low_price) / 2) + atr_amount
            lower_band = ((high_price + low_price) / 2) - atr_amount

            nonlocal supertrend_value, supertrend_direction

            # Initialize Supertrend
            if supertrend_value == 0 and supertrend_direction == 0:
                supertrend_value = close_price
                supertrend_direction = 1

            # Update Supertrend
            if supertrend_direction == 1:  # Previous trend was up
                # Update lower band only - trailing
                supertrend_value = Math.Max(lower_band, supertrend_value)

                # Check for trend reversal
                if close_price < supertrend_value:
                    supertrend_direction = -1
                    supertrend_value = upper_band
            else:  # Previous trend was down
                # Update upper band only - trailing
                supertrend_value = Math.Min(upper_band, supertrend_value)

                # Check for trend reversal
                if close_price > supertrend_value:
                    supertrend_direction = 1
                    supertrend_value = lower_band

            # Current volume
            current_volume = float(candle.TotalVolume)

            # Process trading signals
            self.ProcessSignals(candle, current_volume, volume_value, supertrend_value, supertrend_direction)

        subscription.Bind(atr, handle_candle).Start()

        # Enable position protection
        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, volume_ma)

            # Create a secondary area for volume
            volume_area = self.CreateChartArea()
            if volume_area is not None:
                # Use Volume indicator to visualize volume
                volume_indicator = VolumeIndicator()
                self.DrawIndicator(volume_area, volume_indicator)

            self.DrawOwnTrades(area)

    def ProcessSignals(self, candle, current_volume, volume_avg, supertrend_value, supertrend_direction):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Trading logic:
        # Long: Volume > Avg(Volume) && Price > Supertrend (volume surge with uptrend)
        # Short: Volume > Avg(Volume) && Price < Supertrend (volume surge with downtrend)
        volume_surge = current_volume > volume_avg

        if volume_surge and supertrend_direction == 1 and self.Position <= 0:
            # Buy signal - Volume surge with Supertrend uptrend
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
        elif volume_surge and supertrend_direction == -1 and self.Position >= 0:
            # Sell signal - Volume surge with Supertrend downtrend
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
        # Exit conditions based on Supertrend reversal
        elif self.Position > 0 and supertrend_direction == -1:
            # Exit long position when Supertrend turns down
            self.SellMarket(self.Position)
        elif self.Position < 0 and supertrend_direction == 1:
            # Exit short position when Supertrend turns up
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return volume_supertrend_strategy()
