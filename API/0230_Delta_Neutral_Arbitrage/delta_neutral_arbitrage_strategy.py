import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class delta_neutral_arbitrage_strategy(Strategy):
    """
    Strategy that creates delta neutral arbitrage positions between two correlated assets.
    Goes long one asset and short another when spread deviates from the mean.
    """

    def __init__(self):
        super(delta_neutral_arbitrage_strategy, self).__init__()

        # Secondary security for pair trading.
        self._asset2_security = self.Param("Asset2Security") \
            .SetDisplay("Asset 2", "Secondary asset for arbitrage", "Securities")

        # Portfolio for trading second asset.
        self._asset2_portfolio = self.Param("Asset2Portfolio") \
            .SetDisplay("Portfolio 2", "Portfolio for trading Asset 2", "Portfolios")

        # Period for spread statistics calculation.
        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetDisplay("Lookback period", "Period for spread statistics calculation", "Strategy parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        # Threshold for entries, in standard deviations.
        self._entry_threshold = self.Param("EntryThreshold", 2.0) \
            .SetDisplay("Entry threshold", "Entry threshold in standard deviations", "Strategy parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.5, 3.0, 0.5)

        # Stop-loss percentage.
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop-loss %", "Stop-loss as percentage from entry spread", "Risk management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        # Type of candles to use.
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle type", "Type of candles to use", "General")

        self._spread_sma = None
        self._spread_std_dev = None
        self._current_spread = 0.0
        self._last_asset1_price = 0.0
        self._last_asset2_price = 0.0
        self._asset1_volume = 0.0
        self._asset2_volume = 0.0

    @property
    def asset2_security(self):
        """Secondary security for pair trading."""
        return self._asset2_security.Value

    @asset2_security.setter
    def asset2_security(self, value):
        self._asset2_security.Value = value

    @property
    def asset2_portfolio(self):
        """Portfolio for trading second asset."""
        return self._asset2_portfolio.Value

    @asset2_portfolio.setter
    def asset2_portfolio(self, value):
        self._asset2_portfolio.Value = value

    @property
    def lookback_period(self):
        """Period for spread statistics calculation."""
        return self._lookback_period.Value

    @lookback_period.setter
    def lookback_period(self, value):
        self._lookback_period.Value = value

    @property
    def entry_threshold(self):
        """Threshold for entries, in standard deviations."""
        return self._entry_threshold.Value

    @entry_threshold.setter
    def entry_threshold(self, value):
        self._entry_threshold.Value = value

    @property
    def stop_loss_percent(self):
        """Stop-loss percentage."""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def candle_type(self):
        """Type of candles to use."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [
            (self.Security, self.candle_type),
            (self.asset2_security, self.candle_type)
        ]

    def OnReseted(self):
        super(delta_neutral_arbitrage_strategy, self).OnReseted()
        self._current_spread = 0.0
        self._last_asset1_price = 0.0
        self._last_asset2_price = 0.0
        self._asset1_volume = 0.0
        self._asset2_volume = 0.0

    def OnStarted(self, time):
        super(delta_neutral_arbitrage_strategy, self).OnStarted(time)

        self._current_spread = 0.0
        self._last_asset1_price = 0.0
        self._last_asset2_price = 0.0
        self._asset1_volume = 0.0
        self._asset2_volume = 0.0

        if self.asset2_security is None:
            raise Exception("Asset2Security is not specified.")

        if self.asset2_portfolio is None:
            raise Exception("Asset2Portfolio is not specified.")

        # Initialize indicators for spread statistics
        self._spread_sma = SimpleMovingAverage()
        self._spread_sma.Length = self.lookback_period
        self._spread_std_dev = StandardDeviation()
        self._spread_std_dev.Length = self.lookback_period

        # Create subscriptions to both securities
        asset1_subscription = self.SubscribeCandles(self.candle_type)
        asset2_subscription = self.SubscribeCandles(self.candle_type, security=self.asset2_security)

        # Subscribe to candle processing for Asset 1
        asset1_subscription.Bind(self.ProcessAsset1Candle).Start()

        # Subscribe to candle processing for Asset 2
        asset2_subscription.Bind(self.ProcessAsset2Candle).Start()

        # Calculate volumes to maintain beta neutrality (simplified approach)
        # In a real implementation, beta would be calculated dynamically
        self._asset1_volume = self.Volume
        self._asset2_volume = self.Volume  # Simplified, in reality would be Volume * Beta ratio

        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, asset1_subscription)
            self.DrawOwnTrades(area)

    def ProcessAsset1Candle(self, candle):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Update asset1 price
        self._last_asset1_price = candle.ClosePrice

        # Process spread if we have both prices
        self.ProcessSpreadIfReady(candle)

    def ProcessAsset2Candle(self, candle):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Update asset2 price
        self._last_asset2_price = candle.ClosePrice

        # Process spread if we have both prices
        self.ProcessSpreadIfReady(candle)

    def ProcessSpreadIfReady(self, candle):
        # Ensure we have both prices
        if self._last_asset1_price == 0 or self._last_asset2_price == 0:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate the spread
        self._current_spread = self._last_asset1_price - self._last_asset2_price

        # Process the spread with our indicators
        spread_value = self._spread_sma.Process(self._current_spread, candle.ServerTime, candle.State == CandleStates.Finished)
        std_dev_value = self._spread_std_dev.Process(self._current_spread, candle.ServerTime, candle.State == CandleStates.Finished)

        # Check if indicators are formed
        if not self._spread_sma.IsFormed or not self._spread_std_dev.IsFormed:
            return

        spread_sma = float(spread_value)
        spread_std_dev = float(std_dev_value)

        # Calculate z-score
        z_score = 0 if spread_std_dev == 0 else (self._current_spread - spread_sma) / spread_std_dev

        self.LogInfo("Current spread: {0}, SMA: {1}, StdDev: {2}, Z-score: {3}".format(
            self._current_spread, spread_sma, spread_std_dev, z_score))

        # Trading logic
        if Math.Abs(self.Position) == 0:  # No position, check for entry
            # Spread is too low (Asset1 cheap relative to Asset2)
            if z_score < -self.entry_threshold:
                self.EnterLongSpread()
                self.LogInfo(
                    "Long spread entry: Asset1 price={0}, Asset2 price={1}, Spread={2}".format(
                        self._last_asset1_price, self._last_asset2_price, self._current_spread))
            # Spread is too high (Asset1 expensive relative to Asset2)
            elif z_score > self.entry_threshold:
                self.EnterShortSpread()
                self.LogInfo(
                    "Short spread entry: Asset1 price={0}, Asset2 price={1}, Spread={2}".format(
                        self._last_asset1_price, self._last_asset2_price, self._current_spread))
        else:  # Have position, check for exit
            if (self.Position > 0 and self._current_spread >= spread_sma) or \
                    (self.Position < 0 and self._current_spread <= spread_sma):  # Long spread and spread has reverted to mean / Short spread and spread has reverted to mean
                self.ClosePositions()
                self.LogInfo(
                    "Spread exit: Asset1 price={0}, Asset2 price={1}, Spread={2}".format(
                        self._last_asset1_price, self._last_asset2_price, self._current_spread))

    def EnterLongSpread(self):
        # Buy Asset1
        asset1_order = self.CreateOrder(Sides.Buy, self._last_asset1_price, self._asset1_volume)
        asset1_order.Security = self.Security
        asset1_order.Portfolio = self.Portfolio
        self.RegisterOrder(asset1_order)

        # Sell Asset2
        asset2_order = self.CreateOrder(Sides.Sell, self._last_asset2_price, self._asset2_volume)
        asset2_order.Security = self.asset2_security
        asset2_order.Portfolio = self.asset2_portfolio
        self.RegisterOrder(asset2_order)

    def EnterShortSpread(self):
        # Sell Asset1
        asset1_order = self.CreateOrder(Sides.Sell, self._last_asset1_price, self._asset1_volume)
        asset1_order.Security = self.Security
        asset1_order.Portfolio = self.Portfolio
        self.RegisterOrder(asset1_order)

        # Buy Asset2
        asset2_order = self.CreateOrder(Sides.Buy, self._last_asset2_price, self._asset2_volume)
        asset2_order.Security = self.asset2_security
        asset2_order.Portfolio = self.asset2_portfolio
        self.RegisterOrder(asset2_order)

    def ClosePositions(self):
        # Close position in Asset1
        if self.Position > 0:
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0:
            self.BuyMarket(Math.Abs(self.Position))

        # Note: In a real implementation, you would also close the position
        # in Asset2 by checking its position via separate portfolio tracking
        # For simplicity, this example assumes symmetrical positions

        # Close position in Asset2 (simplified example)
        asset2_order = self.CreateOrder(
            Sides.Buy if self.Position > 0 else Sides.Sell,
            self._last_asset2_price,
            self._asset2_volume)

        asset2_order.Security = self.asset2_security
        asset2_order.Portfolio = self.asset2_portfolio

        self.RegisterOrder(asset2_order)

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return delta_neutral_arbitrage_strategy()
