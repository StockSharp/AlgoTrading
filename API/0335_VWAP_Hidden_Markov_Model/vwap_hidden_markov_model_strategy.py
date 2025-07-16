import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import VolumeWeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from collections import deque
from enum import Enum


class MarketState(Enum):
    """Market states for Hidden Markov Model."""
    Neutral = 0
    Bullish = 1
    Bearish = 2


class vwap_hidden_markov_model_strategy(Strategy):
    """Strategy that trades based on VWAP with Hidden Markov Model for market state detection."""

    def __init__(self):
        super(vwap_hidden_markov_model_strategy, self).__init__()

        # Strategy parameter: Length of data to use for HMM.
        self._hmm_data_length = self.Param("HmmDataLength", 100) \
            .SetGreaterThanZero() \
            .SetDisplay("HMM Data Length", "Number of periods to use for HMM", "HMM Settings")

        # Strategy parameter: Stop-loss percentage.
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop Loss percentage from entry price", "Risk Management")

        # Strategy parameter: Candle type.
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # HMM state and data
        self._current_market_state = MarketState.Neutral
        self._price_data = deque()
        self._volume_data = deque()

        # Transition probabilities
        self._transition_matrix = [
            [0.8, 0.1, 0.1],  # Neutral -> Neutral, Bullish, Bearish
            [0.2, 0.7, 0.1],  # Bullish -> Neutral, Bullish, Bearish
            [0.2, 0.1, 0.7],  # Bearish -> Neutral, Bullish, Bearish
        ]

    @property
    def HmmDataLength(self):
        return self._hmm_data_length.Value

    @HmmDataLength.setter
    def HmmDataLength(self, value):
        self._hmm_data_length.Value = value

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

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(vwap_hidden_markov_model_strategy, self).OnStarted(time)

        # Reset state variables
        self._current_market_state = MarketState.Neutral
        self._price_data.clear()
        self._volume_data.clear()

        # Create Vwap indicator
        vwap = VolumeWeightedMovingAverage()

        # Create subscription for candles
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind VWAP indicator to subscription and start
        subscription.Bind(vwap, self.ProcessVwap).Start()

        # Add chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, vwap)
            self.DrawOwnTrades(area)

        # Start position protection with percentage-based stop-loss
        self.StartProtection(
            takeProfit=Unit(0),  # No fixed take profit
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent),
        )

    def ProcessVwap(self, candle, vwap_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Update data for HMM
        self.UpdateHmmData(candle)

        # Run HMM algorithm when enough data is collected
        if len(self._price_data) >= self.HmmDataLength and len(self._volume_data) >= self.HmmDataLength:
            # Update current market state using HMM
            self._current_market_state = self.RunHmm()

            # Log market state updates periodically
            if candle.OpenTime.Second == 0 and candle.OpenTime.Minute % 15 == 0:
                self.LogInfo(f"Current market state: {self._current_market_state}")

        # Trading logic based on VWAP and HMM state
        if self._current_market_state == MarketState.Bullish and candle.ClosePrice > vwap_value and self.Position <= 0:
            # Price above VWAP in bullish state - Buy signal
            self.LogInfo(f"Buy signal: Price ({candle.ClosePrice}) above VWAP ({vwap_value}) in bullish state")
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif self._current_market_state == MarketState.Bearish and candle.ClosePrice < vwap_value and self.Position >= 0:
            # Price below VWAP in bearish state - Sell signal
            self.LogInfo(f"Sell signal: Price ({candle.ClosePrice}) below VWAP ({vwap_value}) in bearish state")
            self.SellMarket(self.Volume + Math.Abs(self.Position))

    def UpdateHmmData(self, candle):
        # Add price data to queue
        self._price_data.append(candle.ClosePrice)
        if len(self._price_data) > self.HmmDataLength:
            self._price_data.popleft()

        # Add volume data to queue
        self._volume_data.append(candle.TotalVolume)
        if len(self._volume_data) > self.HmmDataLength:
            self._volume_data.popleft()

    def RunHmm(self):
        # This is a simplified implementation of Hidden Markov Model
        # A full implementation would use Baum-Welch algorithm for training and Viterbi algorithm for decoding

        # Convert data to observations
        observations = self.GenerateObservations()

        # Decode the most likely state sequence (simplified Viterbi)
        states = self.SimplifiedViterbi(observations)

        # Return the most recent state
        return states[-1] if states else MarketState.Neutral

    def GenerateObservations(self):
        # Generate observation sequence from price and volume data
        # This is a simplified approach - in a real implementation, we would
        # use more sophisticated techniques to generate observations

        result = []
        prices = list(self._price_data)
        volumes = list(self._volume_data)

        for i in range(1, len(prices)):
            price_change = (prices[i] - prices[i - 1]) / prices[i - 1]
            volume_ratio = volumes[i] / max(1, volumes[i - 1])

            # Classify observation:
            # 0: Price down, low volume
            # 1: Price down, high volume
            # 2: Price up, low volume
            # 3: Price up, high volume

            if price_change < 0:
                observation = 1 if volume_ratio > 1.1 else 0
            else:
                observation = 3 if volume_ratio > 1.1 else 2

            result.append(observation)

        return result

    def SimplifiedViterbi(self, observations):
        # This is a very simplified version of the Viterbi algorithm
        # For a real implementation, proper HMM libraries should be used

        # Emission probabilities: P(observation | state)
        emission_matrix = [
            [0.3, 0.2, 0.3, 0.2],  # Neutral -> obs0, obs1, obs2, obs3
            [0.1, 0.1, 0.3, 0.5],  # Bullish -> obs0, obs1, obs2, obs3
            [0.5, 0.3, 0.1, 0.1],  # Bearish -> obs0, obs1, obs2, obs3
        ]

        # Initialize with equal probabilities for each state
        current_state_probabilities = [1.0 / 3] * 3
        state_sequence = []

        # Process each observation
        for obs in observations:
            new_probabilities = [0.0, 0.0, 0.0]

            # Calculate new state probabilities based on observation and transition matrix
            for new_state in range(3):
                total_prob = 0.0

                for old_state in range(3):
                    total_prob += (
                        current_state_probabilities[old_state]
                        * self._transition_matrix[old_state][new_state]
                        * emission_matrix[new_state][obs]
                    )

                new_probabilities[new_state] = total_prob

            # Normalize probabilities
            _sum = sum(new_probabilities)
            if _sum > 0:
                new_probabilities = [p / _sum for p in new_probabilities]

            # Find most likely state
            max_index = 0
            for i in range(1, 3):
                if new_probabilities[i] > new_probabilities[max_index]:
                    max_index = i

            # Add state to sequence
            state_sequence.append(MarketState(max_index))

            # Update current probabilities
            current_state_probabilities = new_probabilities

        return state_sequence

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return vwap_hidden_markov_model_strategy()
