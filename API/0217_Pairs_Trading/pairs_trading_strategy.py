import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, UnitTypes, Unit, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *
from StockSharp.BusinessEntities import Security, Subscription

class pairs_trading_strategy(Strategy):
    """
    Statistical Pairs Trading strategy.
    Trades the spread between two correlated assets, entering positions when
    the spread deviates significantly from its mean.
    """

    def __init__(self):
        super(pairs_trading_strategy, self).__init__()

        # Strategy parameters
        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Period for calculating spread mean and standard deviation", "Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 40, 5)

        self._deviation_multiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation Multiplier", "Number of standard deviations for entry signals", "Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.5, 3.0, 0.5)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop-loss %", "Stop-loss as percentage of spread at entry", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._second_security = self.Param[Security]("SecondSecurity", None) \
            .SetDisplay("Second Security", "Second security in the pair", "General") \
            .SetRequired()

        # Internal state
        self._spread_ma = None
        self._spread_std_dev = None
        self._spread = 0
        self._last_second_price = 0

    @property
    def lookback_period(self):
        """Period for calculating mean and standard deviation of the spread."""
        return self._lookback_period.Value

    @lookback_period.setter
    def lookback_period(self, value):
        self._lookback_period.Value = value

    @property
    def deviation_multiplier(self):
        """Number of standard deviations for entry signals."""
        return self._deviation_multiplier.Value

    @deviation_multiplier.setter
    def deviation_multiplier(self, value):
        self._deviation_multiplier.Value = value

    @property
    def stop_loss_percent(self):
        """Stop-loss percentage parameter."""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def candle_type(self):
        """Candle type parameter."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def second_security(self):
        """Second security in the pair."""
        return self._second_security.Value

    @second_security.setter
    def second_security(self, value):
        self._second_security.Value = value

    def GetWorkingSecurities(self):
        """Return the securities and candle type this strategy works with."""
        return [
            (self.Security, self.candle_type),
            (self.second_security, self.candle_type)
        ]

    def OnReseted(self):
        super(pairs_trading_strategy, self).OnReseted()
        self._spread = 0
        self._last_second_price = 0

    def OnStarted(self, time):
        super(pairs_trading_strategy, self).OnStarted(time)

        self._spread = 0
        self._last_second_price = 0

        if self.second_security is None:
            raise Exception("Second security is not specified.")

        # Initialize indicators
        self._spread_ma = SimpleMovingAverage()
        self._spread_ma.Length = self.lookback_period
        self._spread_std_dev = StandardDeviation()
        self._spread_std_dev.Length = self.lookback_period

        # Create subscriptions for both securities
        first_subscription = self.SubscribeCandles(self.candle_type)
        second_subscription = self.SubscribeCandles(Subscription(self.candle_type, self.second_security))

        # Bind to first security candles
        first_subscription.Bind(self.ProcessFirstSecurityCandle).Start()

        # Bind to second security candles
        second_subscription.Bind(self.ProcessSecondSecurityCandle).Start()

        # Enable position protection with stop-loss
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )
        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, first_subscription)
            self.DrawOwnTrades(area)

    def ProcessFirstSecurityCandle(self, candle):
        # Skip if we don't have price for the second security yet
        if self._last_second_price == 0:
            return

        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Skip if strategy is not ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate the spread: Asset1 - Asset2
        self._spread = candle.ClosePrice - self._last_second_price

        # Process the spread through indicators
        ma_value = process_float(self._spread_ma, self._spread, candle.ServerTime, True)
        std_dev_value = process_float(self._spread_std_dev, self._spread, candle.ServerTime, True)

        # Skip until indicators are formed
        if not self._spread_ma.IsFormed or not self._spread_std_dev.IsFormed:
            return

        spread_mean = float(ma_value)
        spread_std_dev = float(std_dev_value)

        # Calculate entry thresholds
        upper_threshold = spread_mean + (spread_std_dev * self.deviation_multiplier)
        lower_threshold = spread_mean - (spread_std_dev * self.deviation_multiplier)

        # Trading logic
        if self._spread < lower_threshold:
            # Spread is below lower threshold:
            # Buy Asset1 (Security), Sell Asset2 (SecondSecurity)
            if self.Position <= 0:
                # Close any existing position and enter new position
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
                self.LogInfo("Long Signal: Spread({0:F4}) < Lower Threshold({1:F4})".format(
                    self._spread, lower_threshold))
                # Note: In a real implementation, you would also place a sell order
                # for the second security here, using a different strategy instance or connector
        elif self._spread > upper_threshold:
            # Spread is above upper threshold:
            # Sell Asset1 (Security), Buy Asset2 (SecondSecurity)
            if self.Position >= 0:
                # Close any existing position and enter new position
                self.SellMarket(self.Volume + Math.Abs(self.Position))
                self.LogInfo("Short Signal: Spread({0:F4}) > Upper Threshold({1:F4})".format(
                    self._spread, upper_threshold))
                # Note: In a real implementation, you would also place a buy order
                # for the second security here, using a different strategy instance or connector
        elif (self._spread > spread_mean and self.Position > 0) or \
                (self._spread < spread_mean and self.Position < 0):
            # Exit signals: Spread returned to the mean
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
                self.LogInfo("Exit Long: Spread({0:F4}) > Mean({1:F4})".format(
                    self._spread, spread_mean))
            elif self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Exit Short: Spread({0:F4}) < Mean({1:F4})".format(
                    self._spread, spread_mean))

    def ProcessSecondSecurityCandle(self, candle):
        # Store the close price of the second security for spread calculation
        if candle.State == CandleStates.Finished:
            self._last_second_price = candle.ClosePrice

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return pairs_trading_strategy()
