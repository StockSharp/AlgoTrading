import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class supertrend_rsi_divergence_strategy(Strategy):
    """
    Strategy that uses Supertrend indicator along with RSI divergence to identify trading opportunities.
    """

    def __init__(self):
        super(supertrend_rsi_divergence_strategy, self).__init__()

        # Parameters
        self._supertrend_period = self.Param("SupertrendPeriod", 10) \
            .SetDisplay("Supertrend Period", "Supertrend ATR period", "Supertrend") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 20, 1)

        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 3.0) \
            .SetDisplay("Supertrend Multiplier", "Supertrend ATR multiplier", "Supertrend") \
            .SetCanOptimize(True) \
            .SetOptimize(2.0, 5.0, 0.5)

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period for divergence detection", "RSI") \
            .SetCanOptimize(True) \
            .SetOptimize(8, 20, 2)

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(15).TimeFrame()) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Indicator instances
        self._supertrend = None
        self._rsi = None

        # Data for divergence detection
        self._prices = []
        self._rsi_values = []
        self._is_long_position = False
        self._is_short_position = False

        # Supertrend state tracking
        self._supertrend_value = 0
        self._trend_direction = None
        # Trend direction enum for tracking Supertrend state

    @property
    def SupertrendPeriod(self):
        """Supertrend period."""
        return self._supertrend_period.Value

    @SupertrendPeriod.setter
    def SupertrendPeriod(self, value):
        self._supertrend_period.Value = value

    @property
    def SupertrendMultiplier(self):
        """Supertrend multiplier."""
        return self._supertrend_multiplier.Value

    @SupertrendMultiplier.setter
    def SupertrendMultiplier(self, value):
        self._supertrend_multiplier.Value = value

    @property
    def RsiPeriod(self):
        """RSI period."""
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def CandleType(self):
        """Candle type to use for the strategy."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(supertrend_rsi_divergence_strategy, self).OnStarted(time)

        self._prices = []
        self._rsi_values = []
        self._is_long_position = False
        self._is_short_position = False
        self._trend_direction = None
        self._supertrend_value = 0

        # Create indicators
        self._supertrend = SuperTrend()
        self._supertrend.Length = self.SupertrendPeriod
        self._supertrend.Multiplier = self.SupertrendMultiplier

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiPeriod

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._supertrend, self._rsi, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._supertrend)
            self.DrawIndicator(area, self._rsi)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, supertrend_value, rsi_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Extract values from indicators
        self._supertrend_value = supertrend_value
        rsi = float(rsi_value)

        # Store values for divergence calculation
        self._prices.append(candle.ClosePrice)
        self._rsi_values.append(rsi)

        # Keep reasonable history
        while len(self._prices) > 50:
            self._prices.pop(0)
            self._rsi_values.pop(0)

        # Determine Supertrend trend direction
        previous_direction = self._trend_direction

        if candle.ClosePrice > self._supertrend_value:
            self._trend_direction = "Up"
        elif candle.ClosePrice < self._supertrend_value:
            self._trend_direction = "Down"

        # Check for trend direction change
        trend_direction_changed = (
            previous_direction is not None
            and previous_direction != self._trend_direction
        )

        # Check for divergence
        bullish_divergence = self.CheckBullishDivergence()
        bearish_divergence = self.CheckBearishDivergence()

        # Trading logic
        if candle.ClosePrice > self._supertrend_value and bullish_divergence and self.Position <= 0:
            # Bullish setup - price above Supertrend with bullish divergence
            self.BuyMarket(self.Volume)
            self.LogInfo(
                "Buy Signal: Price {0:F2} > Supertrend {1:F2} with bullish RSI divergence".format(
                    candle.ClosePrice, self._supertrend_value
                )
            )
            self._is_long_position = True
            self._is_short_position = False
        elif candle.ClosePrice < self._supertrend_value and bearish_divergence and self.Position >= 0:
            # Bearish setup - price below Supertrend with bearish divergence
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo(
                "Sell Signal: Price {0:F2} < Supertrend {1:F2} with bearish RSI divergence".format(
                    candle.ClosePrice, self._supertrend_value
                )
            )
            self._is_long_position = False
            self._is_short_position = True
        elif self._is_long_position and candle.ClosePrice < self._supertrend_value:
            # Exit long position when price falls below Supertrend
            self.SellMarket(self.Position)
            self.LogInfo(
                "Exit Long: Price {0:F2} fell below Supertrend {1:F2}".format(
                    candle.ClosePrice, self._supertrend_value
                )
            )
            self._is_long_position = False
        elif self._is_short_position and candle.ClosePrice > self._supertrend_value:
            # Exit short position when price rises above Supertrend
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo(
                "Exit Short: Price {0:F2} rose above Supertrend {1:F2}".format(
                    candle.ClosePrice, self._supertrend_value
                )
            )
            self._is_short_position = False

    def CheckBullishDivergence(self):
        # Need at least a few candles for divergence check
        if len(self._prices) < 5 or len(self._rsi_values) < 5:
            return False

        # Check for bullish divergence: price making lower lows while RSI making higher lows
        # Look at the last 5 candles for a simple check
        current_price = self._prices[-1]
        previous_price = self._prices[-2]

        current_rsi = self._rsi_values[-1]
        previous_rsi = self._rsi_values[-2]

        # Bullish divergence: price lower but RSI higher
        divergence = current_price < previous_price and current_rsi > previous_rsi

        if divergence:
            self.LogInfo(
                "Bullish Divergence Detected: Price {0:F2}->{1:F2}, RSI {2:F2}->{3:F2}".format(
                    previous_price,
                    current_price,
                    previous_rsi,
                    current_rsi,
                )
            )

        return divergence

    def CheckBearishDivergence(self):
        # Need at least a few candles for divergence check
        if len(self._prices) < 5 or len(self._rsi_values) < 5:
            return False

        # Check for bearish divergence: price making higher highs while RSI making lower highs
        # Look at the last 5 candles for a simple check
        current_price = self._prices[-1]
        previous_price = self._prices[-2]

        current_rsi = self._rsi_values[-1]
        previous_rsi = self._rsi_values[-2]

        # Bearish divergence: price higher but RSI lower
        divergence = current_price > previous_price and current_rsi < previous_rsi

        if divergence:
            self.LogInfo(
                "Bearish Divergence Detected: Price {0:F2}->{1:F2}, RSI {2:F2}->{3:F2}".format(
                    previous_price,
                    current_price,
                    previous_rsi,
                    current_rsi,
                )
            )

        return divergence

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return supertrend_rsi_divergence_strategy()

