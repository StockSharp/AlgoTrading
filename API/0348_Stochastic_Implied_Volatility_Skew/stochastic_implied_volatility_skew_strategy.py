import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math, Random
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, ICandleMessage
from StockSharp.Algo.Indicators import StochasticOscillator, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class stochastic_implied_volatility_skew_strategy(Strategy):
    """Stochastic strategy with Implied Volatility Skew."""

    def __init__(self):
        super(stochastic_implied_volatility_skew_strategy, self).__init__()

        # Stochastic length parameter.
        self._stoch_length = self.Param("StochLength", 14) \
            .SetRange(5, 30) \
            .SetCanOptimize(True) \
            .SetDisplay("Stoch Length", "Period for Stochastic Oscillator", "Indicators")

        # Stochastic %K smoothing parameter.
        self._stoch_k = self.Param("StochK", 3) \
            .SetRange(1, 10) \
            .SetCanOptimize(True) \
            .SetDisplay("Stoch %K", "Smoothing for Stochastic %K line", "Indicators")

        # Stochastic %D smoothing parameter.
        self._stoch_d = self.Param("StochD", 3) \
            .SetRange(1, 10) \
            .SetCanOptimize(True) \
            .SetDisplay("Stoch %D", "Smoothing for Stochastic %D line", "Indicators")

        # IV Skew averaging period.
        self._iv_period = self.Param("IvPeriod", 20) \
            .SetRange(10, 50) \
            .SetCanOptimize(True) \
            .SetDisplay("IV Period", "Period for IV Skew averaging", "Options")

        # Stop loss percentage.
        self._stop_loss = self.Param("StopLoss", 2.0) \
            .SetRange(1.0, 5.0) \
            .SetCanOptimize(True) \
            .SetDisplay("Stop Loss %", "Stop Loss percentage", "Risk Management")

        # Candle type for strategy calculation.
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Internal fields
        self._stochastic = None
        self._iv_skew_sma = None
        self._current_iv_skew = 0.0
        self._avg_iv_skew = 0.0

    @property
    def StochLength(self):
        """Stochastic length parameter."""
        return self._stoch_length.Value

    @StochLength.setter
    def StochLength(self, value):
        self._stoch_length.Value = value

    @property
    def StochK(self):
        """Stochastic %K smoothing parameter."""
        return self._stoch_k.Value

    @StochK.setter
    def StochK(self, value):
        self._stoch_k.Value = value

    @property
    def StochD(self):
        """Stochastic %D smoothing parameter."""
        return self._stoch_d.Value

    @StochD.setter
    def StochD(self, value):
        self._stoch_d.Value = value

    @property
    def IvPeriod(self):
        """IV Skew averaging period."""
        return self._iv_period.Value

    @IvPeriod.setter
    def IvPeriod(self, value):
        self._iv_period.Value = value

    @property
    def StopLoss(self):
        """Stop loss percentage."""
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy calculation."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        """!! REQUIRED !! Returns securities for strategy."""
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(stochastic_implied_volatility_skew_strategy, self).OnStarted(time)

        # Create Stochastic Oscillator
        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = self.StochK
        self._stochastic.D.Length = self.StochD

        # Create IV Skew SMA
        self._iv_skew_sma = SimpleMovingAverage()
        self._iv_skew_sma.Length = self.IvPeriod

        # Reset state variables
        self._current_iv_skew = 0
        self._avg_iv_skew = 0

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._stochastic, self.ProcessCandle).Start()

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._stochastic)
            self.DrawOwnTrades(area)

        # Start position protection
        self.StartProtection(
            Unit(2, UnitTypes.Percent),   # Take profit 2%
            Unit(self.StopLoss, UnitTypes.Percent)  # Stop loss based on parameter
        )

    def ProcessCandle(self, candle: ICandleMessage, stoch_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Simulate IV Skew data (in real implementation, this would come from options data provider)
        self.SimulateIvSkew(candle)

        # Process IV Skew with SMA
        iv_skew_sma_value = self._iv_skew_sma.Process(self._current_iv_skew, candle.ServerTime, candle.State == CandleStates.Finished)
        self._avg_iv_skew = iv_skew_sma_value.ToDecimal()

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        stoch_typed = stoch_value
        stoch_k = stoch_typed.K
        stoch_d = stoch_typed.D

        # Entry logic
        if stoch_k < 20 and self._current_iv_skew > self._avg_iv_skew and self.Position <= 0:
            # Stochastic in oversold territory and IV Skew above average - Long entry
            self.BuyMarket(self.Volume)
            self.LogInfo(f"Buy Signal: Stoch %K={stoch_k}, IV Skew={self._current_iv_skew}, Avg IV Skew={self._avg_iv_skew}")
        elif stoch_k > 80 and self._current_iv_skew < self._avg_iv_skew and self.Position >= 0:
            # Stochastic in overbought territory and IV Skew below average - Short entry
            self.SellMarket(self.Volume)
            self.LogInfo(f"Sell Signal: Stoch %K={stoch_k}, IV Skew={self._current_iv_skew}, Avg IV Skew={self._avg_iv_skew}")

        # Exit logic
        if self.Position > 0 and stoch_k > 50:
            # Exit long position when Stochastic returns to neutral zone
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo(f"Exit Long: Stoch %K={stoch_k}")
        elif self.Position < 0 and stoch_k < 50:
            # Exit short position when Stochastic returns to neutral zone
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo(f"Exit Short: Stoch %K={stoch_k}")

    def SimulateIvSkew(self, candle: ICandleMessage):
        # This is a placeholder for real IV Skew data
        # In a real implementation, this would connect to an options data provider
        # IV Skew measures the difference in IV between calls and puts at equidistant strikes

        # Create pseudo-random but somewhat realistic values
        random = Random()

        # Base IV Skew values on price movement and volatility
        price_up = candle.OpenPrice < candle.ClosePrice
        candle_range = (candle.HighPrice - candle.LowPrice) / candle.LowPrice

        # When prices are rising, puts are often bid up for protection (negative skew)
        # When prices are falling, calls become relatively cheaper (positive skew)
        if price_up:
            # During uptrends, skew tends to be more negative
            self._current_iv_skew = -0.1 - candle_range - random.NextDouble() * 0.2
        else:
            # During downtrends, skew can become less negative or even positive
            self._current_iv_skew = 0.05 - candle_range + random.NextDouble() * 0.2

        # Add some randomness for market events
        if random.NextDouble() > 0.95:
            # Occasional extreme skew events (e.g., market fear or greed)
            self._current_iv_skew *= 1.5

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return stochastic_implied_volatility_skew_strategy()
