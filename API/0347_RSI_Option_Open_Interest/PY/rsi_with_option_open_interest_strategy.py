import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

import random

class rsi_with_option_open_interest_strategy(Strategy):
    """
    RSI with Option Open Interest Strategy.
    """

    def __init__(self):
        super(rsi_with_option_open_interest_strategy, self).__init__()

        # RSI Period.
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetRange(5, 30) \
            .SetCanOptimize(True) \
            .SetDisplay("RSI Period", "Period for RSI calculation", "Indicators")

        # Candle type for strategy calculation.
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Open Interest averaging period.
        self._oi_period = self.Param("OiPeriod", 20) \
            .SetRange(10, 50) \
            .SetCanOptimize(True) \
            .SetDisplay("OI Period", "Period for Open Interest averaging", "Options")

        # Standard deviation multiplier for OI threshold.
        self._oi_deviation_factor = self.Param("OiDeviationFactor", 2.0) \
            .SetRange(1.0, 3.0) \
            .SetCanOptimize(True) \
            .SetDisplay("OI StdDev Factor", "Standard deviation multiplier for OI threshold", "Options")

        # Stop loss percentage.
        self._stop_loss = self.Param("StopLoss", 2.0) \
            .SetRange(1.0, 5.0) \
            .SetCanOptimize(True) \
            .SetDisplay("Stop Loss %", "Stop Loss percentage", "Risk Management")

        # Internal indicators
        self._rsi = None
        self._call_oi_sma = None
        self._put_oi_sma = None
        self._call_oi_stddev = None
        self._put_oi_stddev = None

        # State variables
        self._current_call_oi = 0.0
        self._current_put_oi = 0.0
        self._avg_call_oi = 0.0
        self._avg_put_oi = 0.0
        self._stddev_call_oi = 0.0
        self._stddev_put_oi = 0.0

    @property
    def RsiPeriod(self):
        """RSI Period."""
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy calculation."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def OiPeriod(self):
        """Open Interest averaging period."""
        return self._oi_period.Value

    @OiPeriod.setter
    def OiPeriod(self, value):
        self._oi_period.Value = value

    @property
    def OiDeviationFactor(self):
        """Standard deviation multiplier for OI threshold."""
        return self._oi_deviation_factor.Value

    @OiDeviationFactor.setter
    def OiDeviationFactor(self, value):
        self._oi_deviation_factor.Value = value

    @property
    def StopLoss(self):
        """Stop loss percentage."""
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(rsi_with_option_open_interest_strategy, self).OnReseted()
        if self._rsi:
            self._rsi.Reset()
            self._rsi = None
        if self._call_oi_sma:
            self._call_oi_sma.Reset()
            self._call_oi_sma = None
        if self._put_oi_sma:
            self._put_oi_sma.Reset()
            self._put_oi_sma = None
        if self._call_oi_stddev:
            self._call_oi_stddev.Reset()
            self._call_oi_stddev = None
        if self._put_oi_stddev:
            self._put_oi_stddev.Reset()
            self._put_oi_stddev = None
        self._current_call_oi = 0
        self._current_put_oi = 0
        self._avg_call_oi = 0
        self._avg_put_oi = 0
        self._stddev_call_oi = 0
        self._stddev_put_oi = 0

    def OnStarted(self, time):
        super(rsi_with_option_open_interest_strategy, self).OnStarted(time)

        # Create indicators
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiPeriod

        # Indicators for Call options open interest
        self._call_oi_sma = SimpleMovingAverage()
        self._call_oi_sma.Length = self.OiPeriod

        self._call_oi_stddev = StandardDeviation()
        self._call_oi_stddev.Length = self.OiPeriod

        # Indicators for Put options open interest
        self._put_oi_sma = SimpleMovingAverage()
        self._put_oi_sma.Length = self.OiPeriod

        self._put_oi_stddev = StandardDeviation()
        self._put_oi_stddev.Length = self.OiPeriod

        # Create candle subscription and bind RSI
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._rsi, self.ProcessCandle).Start()

        # Create subscription for option OI data (would be implemented in a real system)
        # Here we'll just simulate the data in the ProcessCandle method

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._rsi)
            self.DrawOwnTrades(area)

        # Start position protection
        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(self.StopLoss, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle, rsi_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Get current RSI value
        rsi = float(rsi_value)

        # Simulate option open interest data (in real implementation, this would come from a data provider)
        self.SimulateOptionOI(candle)

        # Process option OI with indicators
        call_oi_value_sma = process_float(self._call_oi_sma, self._current_call_oi, candle.ServerTime, candle.State == CandleStates.Finished)
        put_oi_value_sma = process_float(self._put_oi_sma, self._current_put_oi, candle.ServerTime, candle.State == CandleStates.Finished)

        call_oi_value_stddev = process_float(self._call_oi_stddev, self._current_call_oi, candle.ServerTime, candle.State == CandleStates.Finished)
        put_oi_value_stddev = process_float(self._put_oi_stddev, self._current_put_oi, candle.ServerTime, candle.State == CandleStates.Finished)

        # Update state variables
        self._avg_call_oi = float(call_oi_value_sma)
        self._avg_put_oi = float(put_oi_value_sma)
        self._stddev_call_oi = float(call_oi_value_stddev)
        self._stddev_put_oi = float(put_oi_value_stddev)

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate OI thresholds
        call_oi_threshold = self._avg_call_oi + self.OiDeviationFactor * self._stddev_call_oi
        put_oi_threshold = self._avg_put_oi + self.OiDeviationFactor * self._stddev_put_oi

        # Entry logic
        if rsi < 30 and self._current_call_oi > call_oi_threshold and self.Position <= 0:
            # RSI in oversold territory and Call OI spiking - Long entry
            self.BuyMarket(self.Volume)
            self.LogInfo("Buy Signal: RSI={0}, Call OI={1}, Threshold={2}".format(rsi, self._current_call_oi, call_oi_threshold))
        elif rsi > 70 and self._current_put_oi > put_oi_threshold and self.Position >= 0:
            # RSI in overbought territory and Put OI spiking - Short entry
            self.SellMarket(self.Volume)
            self.LogInfo("Sell Signal: RSI={0}, Put OI={1}, Threshold={2}".format(rsi, self._current_put_oi, put_oi_threshold))

        # Exit logic
        if self.Position > 0 and rsi > 50:
            # Exit long position when RSI returns to neutral zone
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo("Exit Long: RSI={0}".format(rsi))
        elif self.Position < 0 and rsi < 50:
            # Exit short position when RSI returns to neutral zone
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exit Short: RSI={0}".format(rsi))

    def SimulateOptionOI(self, candle):
        # This is a placeholder for real option open interest data
        # In a real implementation, this would connect to an options data provider
        # We'll simulate some values based on price action for demonstration

        # Base OI values on price movement
        price_up = candle.OpenPrice < candle.ClosePrice

        # Simulate bullish sentiment with higher call OI when price is rising
        # Simulate bearish sentiment with higher put OI when price is falling
        if price_up:
            self._current_call_oi = float(candle.TotalVolume * (1 + random.random() * 0.5))
            self._current_put_oi = float(candle.TotalVolume * (0.7 + random.random() * 0.3))
        else:
            self._current_call_oi = float(candle.TotalVolume * (0.7 + random.random() * 0.3))
            self._current_put_oi = float(candle.TotalVolume * (1 + random.random() * 0.5))

        # Add some randomness for spikes
        if random.random() > 0.9:
            # Occasional spikes in OI
            self._current_call_oi *= 1.5

        if random.random() > 0.9:
            # Occasional spikes in OI
            self._current_put_oi *= 1.5

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return rsi_with_option_open_interest_strategy()