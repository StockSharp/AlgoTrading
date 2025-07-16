import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class macd_hidden_markov_model_strategy(Strategy):
    """
    MACD strategy with Hidden Markov Model for state detection.
    """

    # Hidden Markov Model states
    class MarketState:
        Bullish = 0
        Neutral = 1
        Bearish = 2

    def __init__(self):
        super(macd_hidden_markov_model_strategy, self).__init__()

        # Initialize strategy parameters
        self._macd_fast = self.Param("MacdFast", 12) \
            .SetDisplay("MACD Fast Period", "Fast EMA period for MACD", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(8, 20, 2)

        self._macd_slow = self.Param("MacdSlow", 26) \
            .SetDisplay("MACD Slow Period", "Slow EMA period for MACD", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(20, 40, 2)

        self._macd_signal = self.Param("MacdSignal", 9) \
            .SetDisplay("MACD Signal Period", "Signal EMA period for MACD", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 15, 1)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._hmm_history_length = self.Param("HmmHistoryLength", 100) \
            .SetDisplay("HMM History Length", "Length of history for Hidden Markov Model", "HMM Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(50, 200, 10)

        # Data for HMM calculations
        self._price_changes = []
        self._volumes = []
        self._prev_price = 0

        self._current_state = self.MarketState.Neutral
        self._macd = None

    @property
    def MacdFast(self):
        """MACD fast period."""
        return self._macd_fast.Value

    @MacdFast.setter
    def MacdFast(self, value):
        self._macd_fast.Value = value

    @property
    def MacdSlow(self):
        """MACD slow period."""
        return self._macd_slow.Value

    @MacdSlow.setter
    def MacdSlow(self, value):
        self._macd_slow.Value = value

    @property
    def MacdSignal(self):
        """MACD signal period."""
        return self._macd_signal.Value

    @MacdSignal.setter
    def MacdSignal(self, value):
        self._macd_signal.Value = value

    @property
    def CandleType(self):
        """Candle type to use for the strategy."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def HmmHistoryLength(self):
        """Length of history for Hidden Markov Model."""
        return self._hmm_history_length.Value

    @HmmHistoryLength.setter
    def HmmHistoryLength(self, value):
        self._hmm_history_length.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(macd_hidden_markov_model_strategy, self).OnStarted(time)

        self._current_state = self.MarketState.Neutral
        self._prev_price = 0
        self._price_changes = []
        self._volumes = []

        # Create MACD indicator
        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._macd.Macd.ShortMa.Length = self.MacdFast
        self._macd.Macd.LongMa.Length = self.MacdSlow
        self._macd.SignalMa.Length = self.MacdSignal
        # Create subscription and bind indicator
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._macd, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._macd)
            self.DrawOwnTrades(area)

        # Setup position protection
        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(2, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle, macd_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Update HMM data
        self.UpdateHmmData(candle)

        # Determine market state using HMM
        self.CalculateMarketState()

        macd = macd_value.Macd
        signal = macd_value.Signal

        # Generate trade signals based on MACD and HMM state
        if macd > signal and self._current_state == self.MarketState.Bullish and self.Position <= 0:
            # Buy signal - MACD above signal line and bullish state
            self.BuyMarket(self.Volume)
            self.LogInfo("Buy Signal: MACD ({0:F6}) > Signal ({1:F6}) in Bullish state".format(macd, signal))
        elif macd < signal and self._current_state == self.MarketState.Bearish and self.Position >= 0:
            # Sell signal - MACD below signal line and bearish state
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo("Sell Signal: MACD ({0:F6}) < Signal ({1:F6}) in Bearish state".format(macd, signal))
        elif ((self.Position > 0 and (self._current_state == self.MarketState.Neutral or self._current_state == self.MarketState.Bearish)) or
              (self.Position < 0 and (self._current_state == self.MarketState.Neutral or self._current_state == self.MarketState.Bullish))):
            # Exit position if market state changes
            self.ClosePosition()
            self.LogInfo("Exit Position: Market state changed to {0}".format(self._current_state))

    def UpdateHmmData(self, candle):
        # Calculate price change
        if self._prev_price > 0:
            price_change = candle.ClosePrice - self._prev_price
            self._price_changes.append(price_change)
            self._volumes.append(candle.TotalVolume)

            # Maintain the desired history length
            while len(self._price_changes) > self.HmmHistoryLength:
                self._price_changes.pop(0)
                self._volumes.pop(0)

        self._prev_price = candle.ClosePrice

    def CalculateMarketState(self):
        # Only perform state calculation when we have enough data
        if len(self._price_changes) < 10:
            return

        # Simple HMM approximation using recent price changes and volume patterns
        # Note: This is a simplified implementation - a real HMM would use proper state transition probabilities

        # Calculate statistics of recent price changes
        recent_changes = self._price_changes[-10:]
        positive_changes = len([c for c in recent_changes if c > 0])
        negative_changes = len([c for c in recent_changes if c < 0])

        # Calculate average volume for up and down days
        up_volume = 0
        down_volume = 0
        up_count = 0
        down_count = 0

        for i in range(max(0, len(self._price_changes) - 10), len(self._price_changes)):
            if self._price_changes[i] > 0:
                up_volume += self._volumes[i]
                up_count += 1
            elif self._price_changes[i] < 0:
                down_volume += self._volumes[i]
                down_count += 1

        up_volume = up_volume / up_count if up_count > 0 else 0
        down_volume = down_volume / down_count if down_count > 0 else 0

        # Determine market state based on price change direction and volume
        if positive_changes >= 7 or (positive_changes >= 6 and up_volume > down_volume * 1.5):
            self._current_state = self.MarketState.Bullish
        elif negative_changes >= 7 or (negative_changes >= 6 and down_volume > up_volume * 1.5):
            self._current_state = self.MarketState.Bearish
        else:
            self._current_state = self.MarketState.Neutral

        self.LogInfo(
            "Market State: {0}, Positive Changes: {1}, Negative Changes: {2}".format(
                self._current_state,
                positive_changes,
                negative_changes,
            )
        )

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return macd_hidden_markov_model_strategy()

