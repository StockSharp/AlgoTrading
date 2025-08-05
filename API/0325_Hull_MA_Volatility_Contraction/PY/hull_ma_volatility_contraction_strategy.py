import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import HullMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class hull_ma_volatility_contraction_strategy(Strategy):
    """
    Strategy based on Hull Moving Average with volatility contraction filter.
    """

    def __init__(self):
        super(hull_ma_volatility_contraction_strategy, self).__init__()

        # Initialize parameters
        self._hma_period = self.Param("HmaPeriod", 9) \
            .SetDisplay("Hull MA Period", "Hull Moving Average period", "Hull MA") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 20, 1)

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for ATR volatility calculation", "Volatility") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 2)

        self._volatility_contraction_factor = self.Param("VolatilityContractionFactor", 2.0) \
            .SetDisplay("Volatility Contraction Factor", "Standard deviation multiplier for volatility contraction", "Volatility") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Indicators
        self._hma = None
        self._atr = None

        # Store values for analysis
        self._atr_values = []
        self._prev_hma_value = 0.0
        self._current_hma_value = 0.0
        self._is_long_position = False
        self._is_short_position = False

    @property
    def HmaPeriod(self):
        """Hull Moving Average period."""
        return self._hma_period.Value

    @HmaPeriod.setter
    def HmaPeriod(self, value):
        self._hma_period.Value = value

    @property
    def AtrPeriod(self):
        """Average True Range period for volatility calculation."""
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def VolatilityContractionFactor(self):
        """Volatility contraction factor (standard deviation multiplier)."""
        return self._volatility_contraction_factor.Value

    @VolatilityContractionFactor.setter
    def VolatilityContractionFactor(self, value):
        self._volatility_contraction_factor.Value = value

    @property
    def CandleType(self):
        """Candle type to use for the strategy."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        """Return security and candle type used by the strategy."""
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(hull_ma_volatility_contraction_strategy, self).OnReseted()
        self._prev_hma_value = 0.0
        self._current_hma_value = 0.0
        self._is_long_position = False
        self._is_short_position = False
        self._atr_values = []

    def OnStarted(self, time):
        super(hull_ma_volatility_contraction_strategy, self).OnStarted(time)

        # Create indicators
        self._hma = HullMovingAverage()
        self._hma.Length = self.HmaPeriod

        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrPeriod

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._hma, self._atr, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._hma)
            self.DrawIndicator(area, self._atr)
            self.DrawOwnTrades(area)

        # Setup position protection
        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(2, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle, hma_value, atr_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Save previous HMA value
        self._prev_hma_value = self._current_hma_value

        # Extract values from indicators
        self._current_hma_value = float(hma_value)
        atr = float(atr_value)

        # Store ATR values for volatility analysis
        self._atr_values.append(atr)

        # Keep only needed history
        while len(self._atr_values) > self.AtrPeriod * 2:
            self._atr_values.pop(0)

        # Check for volatility contraction
        is_volatility_contracted = self.IsVolatilityContracted()

        # Determine HMA trend direction
        is_hma_rising = self._current_hma_value > self._prev_hma_value
        is_hma_falling = self._current_hma_value < self._prev_hma_value

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Log current status
        if len(self._atr_values) >= self.AtrPeriod:
            recent = self._atr_values[-self.AtrPeriod:]
            avg_atr = sum(recent) / len(recent)
            self.LogInfo(
                "HMA: {0:F2} (Prev: {1:F2}), ATR: {2:F2}, Avg ATR: {3:F2}, Volatility Contracted: {4}".format(
                    self._current_hma_value, self._prev_hma_value, atr, avg_atr, is_volatility_contracted)
            )

        # Trading logic
        # Buy when HMA is rising and volatility is contracted
        if is_hma_rising and is_volatility_contracted and self.Position <= 0:
            self.BuyMarket(self.Volume)
            self.LogInfo(
                "Buy Signal: HMA Rising ({0:F2} -> {1:F2}) with Contracted Volatility".format(
                    self._prev_hma_value, self._current_hma_value))
            self._is_long_position = True
            self._is_short_position = False
        # Sell when HMA is falling and volatility is contracted
        elif is_hma_falling and is_volatility_contracted and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo(
                "Sell Signal: HMA Falling ({0:F2} -> {1:F2}) with Contracted Volatility".format(
                    self._prev_hma_value, self._current_hma_value))
            self._is_long_position = False
            self._is_short_position = True
        # Exit long position when HMA starts falling
        elif self._is_long_position and is_hma_falling:
            self.SellMarket(self.Position)
            self.LogInfo(
                "Exit Long: HMA started falling ({0:F2} -> {1:F2})".format(
                    self._prev_hma_value, self._current_hma_value))
            self._is_long_position = False
        # Exit short position when HMA starts rising
        elif self._is_short_position and is_hma_rising:
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo(
                "Exit Short: HMA started rising ({0:F2} -> {1:F2})".format(
                    self._prev_hma_value, self._current_hma_value))
            self._is_short_position = False

    def IsVolatilityContracted(self):
        # Need enough ATR values for calculation
        if len(self._atr_values) < self.AtrPeriod:
            return False

        # Get recent ATR values for analysis
        recent_atr_values = self._atr_values[-self.AtrPeriod:]

        # Calculate mean and standard deviation
        mean = sum(recent_atr_values) / len(recent_atr_values)
        sum_squared_diff = sum((x - mean) ** 2 for x in recent_atr_values)
        standard_deviation = Math.Sqrt(sum_squared_diff / len(recent_atr_values))

        # Get current ATR (latest)
        current_atr = self._atr_values[-1]

        # Check if current ATR is less than mean minus standard deviation * factor
        is_contracted = current_atr < (mean - standard_deviation * float(self.VolatilityContractionFactor))

        # Log details if contraction is detected
        if is_contracted:
            self.LogInfo(
                "Volatility Contraction Detected: Current ATR {0:F2} < Mean {1:F2} - (StdDev {2:F2} * Factor {3})".format(
                    current_atr, mean, standard_deviation, self.VolatilityContractionFactor))

        return is_contracted

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return hull_ma_volatility_contraction_strategy()
