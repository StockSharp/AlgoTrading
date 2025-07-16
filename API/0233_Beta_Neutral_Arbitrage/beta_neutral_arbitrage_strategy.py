import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class beta_neutral_arbitrage_strategy(Strategy):
    """
    Beta Neutral Arbitrage strategy that trades pairs of assets based on their beta-adjusted prices.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(beta_neutral_arbitrage_strategy, self).__init__()

        # Initialize strategy parameters
        self._asset1_param = self.Param("Asset1") \
            .SetDisplay("Asset 1", "First asset for beta-neutral arbitrage", "Instruments")

        self._asset2_param = self.Param("Asset2") \
            .SetDisplay("Asset 2", "Second asset for beta-neutral arbitrage", "Instruments")

        self._market_index_param = self.Param("MarketIndex") \
            .SetDisplay("Market Index", "Market index for beta calculation", "Instruments")

        self._candle_type_param = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._lookback_period_param = self.Param("LookbackPeriod", 20) \
            .SetDisplay("Lookback Period", "Period for spread calculation", "Strategy") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5) \
            .SetGreaterThanZero()

        self._stop_loss_percent_param = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management") \
            .SetNotNegative()

        # Initialize indicators
        self._spread_sma = SimpleMovingAverage()
        self._spread_std_dev = StandardDeviation()

        # State variables
        self._asset1_beta = 1.0
        self._asset2_beta = 1.0
        self._last_spread = 0.0
        self._avg_spread = 0.0
        self._bar_count = 0
        self._asset1_last_price = 0.0
        self._asset2_last_price = 0.0

    @property
    def asset1(self):
        """First asset for beta-neutral arbitrage."""
        return self._asset1_param.Value

    @asset1.setter
    def asset1(self, value):
        self._asset1_param.Value = value

    @property
    def asset2(self):
        """Second asset for beta-neutral arbitrage."""
        return self._asset2_param.Value

    @asset2.setter
    def asset2(self, value):
        self._asset2_param.Value = value

    @property
    def market_index(self):
        """Market index for beta calculation."""
        return self._market_index_param.Value

    @market_index.setter
    def market_index(self, value):
        self._market_index_param.Value = value

    @property
    def candle_type(self):
        """Candle type for data."""
        return self._candle_type_param.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type_param.Value = value

    @property
    def lookback_period(self):
        """Lookback period for calculations."""
        return self._lookback_period_param.Value

    @lookback_period.setter
    def lookback_period(self, value):
        self._lookback_period_param.Value = value
        self._spread_sma.Length = value
        self._spread_std_dev.Length = value

    @property
    def stop_loss_percent(self):
        """Stop loss percentage."""
        return self._stop_loss_percent_param.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent_param.Value = value

    def OnStarted(self, time):
        """Called when the strategy starts. Sets up subscriptions and charting."""
        super(beta_neutral_arbitrage_strategy, self).OnStarted(time)

        self._bar_count = 0
        self._asset1_beta = 1
        self._asset2_beta = 1
        self._avg_spread = 0
        self._last_spread = 0
        self._asset1_last_price = 0
        self._asset2_last_price = 0

        self._spread_sma.Length = self.lookback_period
        self._spread_std_dev.Length = self.lookback_period

        if self.asset1 and self.asset2 and self.market_index and self.candle_type:
            self.LogInfo("Calculating initial betas using historical data...")

            # In a real implementation, this would be done using historical data
            # For this example, we'll just set default values
            self._asset1_beta = 1.2
            self._asset2_beta = 0.8

            self.LogInfo(
                "Initial betas: Asset1={0}, Asset2={1}".format(self._asset1_beta, self._asset2_beta)
            )

            asset1_subscription = self.SubscribeCandles(self.candle_type, security=self.asset1)
            asset2_subscription = self.SubscribeCandles(self.candle_type, security=self.asset2)
            market_subscription = self.SubscribeCandles(self.candle_type, security=self.market_index)

            asset1_subscription.Bind(self.ProcessAsset1Candle).Start()
            asset2_subscription.Bind(self.ProcessAsset2Candle).Start()
            market_subscription.Bind(self.ProcessMarketCandle).Start()

            area = self.CreateChartArea()
            if area is not None:
                self.DrawCandles(area, asset1_subscription)
                self.DrawCandles(area, asset2_subscription)
                self.DrawCandles(area, market_subscription)
                self.DrawOwnTrades(area)
        else:
            self.LogWarning("Assets or market index not specified. Strategy won't work properly.")

        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )
    def ProcessAsset1Candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._asset1_last_price = candle.ClosePrice
        self.UpdateSpread(candle)

    def ProcessAsset2Candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._asset2_last_price = candle.ClosePrice
        self.UpdateSpread(candle)

    def ProcessMarketCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        # In a real implementation, this would update betas based on new market data
        pass

    def UpdateSpread(self, candle):
        if self._asset1_last_price == 0 or self._asset2_last_price == 0:
            return

        beta_adjusted_asset1 = self._asset1_last_price / self._asset1_beta
        beta_adjusted_asset2 = self._asset2_last_price / self._asset2_beta
        self._last_spread = beta_adjusted_asset1 - beta_adjusted_asset2

        sma_value = self._spread_sma.Process(
            self._last_spread, candle.ServerTime, candle.State == CandleStates.Finished
        )
        std_dev_value = self._spread_std_dev.Process(
            self._last_spread, candle.ServerTime, candle.State == CandleStates.Finished
        )

        self._bar_count += 1

        if not self._spread_sma.IsFormed:
            return

        self._avg_spread = float(sma_value)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        spread_std_dev = float(std_dev_value)
        threshold = 2.0  # Standard deviations from mean to trigger

        if (
            self._last_spread < self._avg_spread - threshold * spread_std_dev
            and self._get_position_value(self.asset1) <= 0
            and self._get_position_value(self.asset2) >= 0
        ):
            self.LogInfo(
                "Spread below threshold: {0} < {1}".format(
                    self._last_spread, self._avg_spread - threshold * spread_std_dev
                )
            )

            asset1_volume = self.Volume
            asset2_volume = self.Volume * (self._asset1_beta / self._asset2_beta)

            self.BuyMarket(asset1_volume, self.asset1)
            self.SellMarket(asset2_volume, self.asset2)
        elif (
            self._last_spread > self._avg_spread + threshold * spread_std_dev
            and self._get_position_value(self.asset1) >= 0
            and self._get_position_value(self.asset2) <= 0
        ):
            self.LogInfo(
                "Spread above threshold: {0} > {1}".format(
                    self._last_spread, self._avg_spread + threshold * spread_std_dev
                )
            )

            asset1_volume = self.Volume
            asset2_volume = self.Volume * (self._asset1_beta / self._asset2_beta)

            self.SellMarket(asset1_volume, self.asset1)
            self.BuyMarket(asset2_volume, self.asset2)
        elif abs(self._last_spread - self._avg_spread) < 0.2 * spread_std_dev:
            self.LogInfo(
                "Spread returned to average: {0} \u2248 {1}".format(
                    self._last_spread, self._avg_spread
                )
            )

            if self._get_position_value(self.asset1) > 0:
                self.SellMarket(abs(self._get_position_value(self.asset1)), self.asset1)

            if self._get_position_value(self.asset1) < 0:
                self.BuyMarket(abs(self._get_position_value(self.asset1)), self.asset1)

            if self._get_position_value(self.asset2) > 0:
                self.SellMarket(abs(self._get_position_value(self.asset2)), self.asset2)

            if self._get_position_value(self.asset2) < 0:
                self.BuyMarket(abs(self._get_position_value(self.asset2)), self.asset2)

    def _get_position_value(self, security):
        if security is None:
            return 0
        return Strategy.GetPositionValue(self, security, self.Portfolio) or 0

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return beta_neutral_arbitrage_strategy()
