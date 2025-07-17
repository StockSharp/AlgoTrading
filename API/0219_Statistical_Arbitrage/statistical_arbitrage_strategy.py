import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, ICandleMessage
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security
from datatype_extensions import *
from indicator_extensions import *

class statistical_arbitrage_strategy(Strategy):
    """
    Statistical Arbitrage strategy that trades pairs of securities based on their relative mean reversion.
    Enters when one asset is below its mean while the other is above its mean.
    """

    def __init__(self):
        super(statistical_arbitrage_strategy, self).__init__()

        # Initialize strategy parameters
        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Period for calculating moving averages", "Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop-loss %", "Stop-loss as percentage of entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._second_security = self.Param[Security]("SecondSecurity", None) \
            .SetDisplay("Second Security", "Second security in the pair", "General") \
            .SetRequired()

        # State variables
        self._first_ma = None
        self._second_ma = None
        self._last_first_price = 0
        self._last_second_price = 0
        self._entry_spread = 0

    @property
    def LookbackPeriod(self):
        return self._lookback_period.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookback_period.Value = value

    @property
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def SecondSecurity(self):
        return self._second_security.Value

    @SecondSecurity.setter
    def SecondSecurity(self, value):
        self._second_security.Value = value

    def GetWorkingSecurities(self):
        """!! REQUIRED !! Returns securities for strategy."""
        return [
            (self.Security, self.CandleType),
            (self.SecondSecurity, self.CandleType)
        ]

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(statistical_arbitrage_strategy, self).OnReseted()
        self._last_first_price = 0
        self._last_second_price = 0
        self._entry_spread = 0

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.

        :param time: The time when the strategy started.
        """
        super(statistical_arbitrage_strategy, self).OnStarted(time)

        self._last_first_price = 0
        self._last_second_price = 0
        self._entry_spread = 0

        if self.SecondSecurity is None:
            raise Exception("Second security is not specified.")

        # Initialize indicators
        self._first_ma = SimpleMovingAverage()
        self._first_ma.Length = self.LookbackPeriod
        self._second_ma = SimpleMovingAverage()
        self._second_ma.Length = self.LookbackPeriod

        # Create subscriptions for both securities
        first_security_subscription = self.SubscribeCandles(self.CandleType)
        second_security_subscription = self.SubscribeCandles(self.CandleType, security=self.SecondSecurity)

        # Bind to first security candles
        first_security_subscription.Bind(self._first_ma, self.ProcessFirstSecurityCandle).Start()

        # Bind to second security candles
        second_security_subscription.Bind(self.ProcessSecondSecurityCandle).Start()

        # Enable position protection with stop-loss
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, first_security_subscription)
            self.DrawIndicator(area, self._first_ma)
            self.DrawOwnTrades(area)

    def ProcessFirstSecurityCandle(self, candle, first_ma_value):
        """
        Process candle of the first security.

        :param candle: The candle message.
        :param first_ma_value: The moving average value for the first security.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Skip if strategy is not ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Store current price
        self._last_first_price = float(candle.ClosePrice)

        # Skip if we don't have both prices or if indicators aren't formed
        if self._last_second_price == 0 or not self._first_ma.IsFormed or not self._second_ma.IsFormed:
            return

        # Get last second MA value
        second_ma_value = self._second_ma.GetCurrentValue()

        # Trading logic
        is_first_below_ma = self._last_first_price < first_ma_value
        is_second_above_ma = self._last_second_price > second_ma_value
        is_first_above_ma = self._last_first_price > first_ma_value
        is_second_below_ma = self._last_second_price < second_ma_value

        current_spread = self._last_first_price - self._last_second_price

        # Long signal: First asset below MA, Second asset above MA
        if is_first_below_ma and is_second_above_ma:
            # If we're not already in a long position
            if self.Position <= 0:
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
                self._entry_spread = current_spread
                self.LogInfo(
                    "Long Signal: {0}({1:F4}) < MA({2:F4}) && {3}({4:F4}) > MA({5:F4})".format(
                        self.Security.Code, self._last_first_price, first_ma_value,
                        self.SecondSecurity.Code, self._last_second_price, second_ma_value))
                # Note: In a real implementation, you would also place a sell order
                # for the second security here, using a different strategy instance or connector
        # Short signal: First asset above MA, Second asset below MA
        elif is_first_above_ma and is_second_below_ma:
            # If we're not already in a short position
            if self.Position >= 0:
                self.SellMarket(self.Volume + Math.Abs(self.Position))
                self._entry_spread = current_spread
                self.LogInfo(
                    "Short Signal: {0}({1:F4}) > MA({2:F4}) && {3}({4:F4}) < MA({5:F4})".format(
                        self.Security.Code, self._last_first_price, first_ma_value,
                        self.SecondSecurity.Code, self._last_second_price, second_ma_value))
                # Note: In a real implementation, you would also place a buy order
                # for the second security here, using a different strategy instance or connector
        # Exit signals
        elif (self.Position > 0 and is_first_above_ma) or (self.Position < 0 and is_first_below_ma):
            # Exit position when first asset crosses its moving average
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
                self.LogInfo(
                    "Exit Long: {0}({1:F4}) > MA({2:F4})".format(
                        self.Security.Code, self._last_first_price, first_ma_value))
            elif self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo(
                    "Exit Short: {0}({1:F4}) < MA({2:F4})".format(
                        self.Security.Code, self._last_first_price, first_ma_value))

    def ProcessSecondSecurityCandle(self, candle):
        """
        Process candle of the second security.

        :param candle: The second security candle message.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Store current price
        self._last_second_price = float(candle.ClosePrice)

        # Process through MA indicator
        process_float(self._second_ma, candle.ClosePrice, candle.ServerTime, candle.State == CandleStates.Finished)

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return statistical_arbitrage_strategy()
