import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class cci_vwap_strategy(Strategy):
    """
    Strategy that uses CCI and VWAP indicators to identify oversold and overbought conditions.
    Enters long when CCI is below -100 and price is below VWAP.
    Enters short when CCI is above 100 and price is above VWAP.

    """

    def __init__(self):
        super(cci_vwap_strategy, self).__init__()

        # Strategy constructor.
        self._cci_period = self.Param("CciPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("CCI period", "CCI indicator period", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop-loss %", "Stop-loss as percentage of entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle type", "Type of candles to use", "General")

        self._cci = None
        self._current_vwap = 0.0

    @property
    def cci_period(self):
        """CCI period parameter."""
        return self._cci_period.Value

    @cci_period.setter
    def cci_period(self, value):
        self._cci_period.Value = value

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

    def OnReseted(self):
        super(cci_vwap_strategy, self).OnReseted()
        self._current_vwap = 0.0

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.

        :param time: The time when the strategy started.
        """
        super(cci_vwap_strategy, self).OnStarted(time)

        # Initialize CCI indicator
        self._cci = CommodityChannelIndex()
        self._cci.Length = self.cci_period

        # Create subscription for candles
        candles_subscription = self.SubscribeCandles(self.candle_type)

        # Create subscription for Level1 to get VWAP
        level1_subscription = self.SubscribeLevel1()
        level1_subscription.Bind(self.ProcessLevel1).Start()

        # Bind CCI to candle subscription
        candles_subscription.Bind(self._cci, self.ProcessCandle).Start()

        # Enable position protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )
        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, candles_subscription)
            self.DrawIndicator(area, self._cci)
            self.DrawOwnTrades(area)

    def ProcessLevel1(self, level1):
        if level1.Changes.ContainsKey(Level1Fields.VWAP):
            self._current_vwap = float(level1.Changes[Level1Fields.VWAP])

    def ProcessCandle(self, candle, cci_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Skip if strategy is not ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Skip if we don't have VWAP yet
        if self._current_vwap == 0:
            return

        # Long signal: CCI below -100 and price below VWAP
        if cci_value < -100 and candle.ClosePrice < self._current_vwap and self.Position <= 0:
            self.BuyMarket(self.Volume)
            self.LogInfo(f"Buy signal: CCI={cci_value:.2f}, Price={candle.ClosePrice}, VWAP={self._current_vwap}")
        # Short signal: CCI above 100 and price above VWAP
        elif cci_value > 100 and candle.ClosePrice > self._current_vwap and self.Position >= 0:
            self.SellMarket(self.Volume)
            self.LogInfo(f"Sell signal: CCI={cci_value:.2f}, Price={candle.ClosePrice}, VWAP={self._current_vwap}")
        # Exit long position: Price crosses above VWAP
        elif self.Position > 0 and candle.ClosePrice > self._current_vwap:
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo(f"Exit long: Price={candle.ClosePrice}, VWAP={self._current_vwap}")
        # Exit short position: Price crosses below VWAP
        elif self.Position < 0 and candle.ClosePrice < self._current_vwap:
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo(f"Exit short: Price={candle.ClosePrice}, VWAP={self._current_vwap}")

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return cci_vwap_strategy()
