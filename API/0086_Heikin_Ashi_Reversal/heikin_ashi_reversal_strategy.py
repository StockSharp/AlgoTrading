import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class heikin_ashi_reversal_strategy(Strategy):
    """
    Heikin Ashi Reversal Strategy.
    Enters long when Heikin-Ashi candles change from bearish to bullish.
    Enters short when Heikin-Ashi candles change from bullish to bearish.

    """
    def __init__(self):
        """Initializes a new instance of the HeikinAshiReversalStrategy."""
        super(heikin_ashi_reversal_strategy, self).__init__()

        # Initialize strategy parameters
        self._candleType = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stopLoss = self.Param("StopLoss", Unit(2, UnitTypes.Percent)) \
            .SetDisplay("Stop Loss", "Stop loss as percentage from entry price", "Risk Management") \
            .SetRange(1.0, 3.0) \
            .SetCanOptimize(True)

        # Internal state
        self._prevIsBullish = None

    @property
    def CandleType(self):
        """Type of candles to use."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    @property
    def StopLoss(self):
        """Stop loss percentage from entry price."""
        return self._stopLoss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stopLoss.Value = value

    def OnStarted(self, time):
        super(heikin_ashi_reversal_strategy, self).OnStarted(time)

        # Enable position protection using stop-loss
        self.StartProtection(
            takeProfit=None,
            stopLoss=self.StopLoss,
            isStopTrailing=False,
            useMarketOrders=True
        )
        # Initialize previous value
        self._prevIsBullish = None

        # Create subscription to candles
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind candle handler
        subscription.Bind(self.ProcessCandle).Start()

        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        """
        Process new candle.

        :param candle: New candle.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate Heikin-Ashi candle values
        if self._prevIsBullish is None:
            # First candle - initialize HA values
            ha_open = float((candle.OpenPrice + candle.ClosePrice) / 2)
            ha_close = float((candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4)
            ha_high = Math.Max(float(candle.HighPrice), Math.Max(ha_open, ha_close))
            ha_low = Math.Min(float(candle.LowPrice), Math.Min(ha_open, ha_close))

            # Store the initial bullish/bearish state
            self._prevIsBullish = ha_close > ha_open
            return

        # Calculate previous HA open/close based on previous state
        prev_ha_open = float(self._prevIsBullish and min(candle.OpenPrice, candle.ClosePrice) or max(candle.OpenPrice, candle.ClosePrice))
        prev_ha_close = float((candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4)

        # Calculate current HA values
        ha_open = (prev_ha_open + prev_ha_close) / 2
        ha_close = float((candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4)
        ha_high = Math.Max(float(candle.HighPrice), Math.Max(ha_open, ha_close))
        ha_low = Math.Min(float(candle.LowPrice), Math.Min(ha_open, ha_close))

        # Determine if current HA candle is bullish or bearish
        is_bullish = ha_close > ha_open

        # Check for trend reversal
        bullish_reversal = (not self._prevIsBullish) and is_bullish
        bearish_reversal = self._prevIsBullish and not is_bullish

        # Long entry: Bullish reversal
        if bullish_reversal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self.LogInfo("Long entry: Heikin-Ashi reversal from bearish to bullish")
        # Short entry: Bearish reversal
        elif bearish_reversal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self.LogInfo("Short entry: Heikin-Ashi reversal from bullish to bearish")

        # Update previous state
        self._prevIsBullish = is_bullish

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return heikin_ashi_reversal_strategy()