import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Indicators import KeltnerChannels
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class keltner_with_rl_signal_strategy(Strategy):
    """
    Keltner with Reinforcement Learning Signal strategy.
    Entry condition:
    Long: Price > EMA + k*ATR && RL_Signal = Buy
    Short: Price < EMA - k*ATR && RL_Signal = Sell
    Exit condition:
    Long: Price < EMA
    Short: Price > EMA
    """

    def __init__(self):
        super(keltner_with_rl_signal_strategy, self).__init__()

        # EMA period.
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Period", "Period for the exponential moving average", "Keltner Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        # ATR period.
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period for the average true range", "Keltner Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 21, 7)

        # ATR multiplier for Keltner channel.
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR in Keltner Channels", "Keltner Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(1.5, 3.0, 0.5)

        # Stop loss in ATR multiples.
        self._stop_loss_atr = self.Param("StopLossAtr", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss (ATR)", "Stop Loss in multiples of ATR", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        # Type of candles to use.
        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        class RLSignal:
            None_ = 0
            Buy = 1
            Sell = 2

        self._RLSignal = RLSignal
        self._current_signal = RLSignal.None_

        # State variables for RL
        self._last_price = 0
        self._previous_ema = 0
        self._previous_atr = 0
        self._previous_price = 0
        self._previous_signal_price = 0
        self._consecutive_wins = 0
        self._consecutive_losses = 0

    @property
    def EmaPeriod(self):
        """EMA period."""
        return self._ema_period.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._ema_period.Value = value

    @property
    def AtrPeriod(self):
        """ATR period."""
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def AtrMultiplier(self):
        """ATR multiplier for Keltner channel."""
        return self._atr_multiplier.Value

    @AtrMultiplier.setter
    def AtrMultiplier(self, value):
        self._atr_multiplier.Value = value

    @property
    def StopLossAtr(self):
        """Stop loss in ATR multiples."""
        return self._stop_loss_atr.Value

    @StopLossAtr.setter
    def StopLossAtr(self, value):
        self._stop_loss_atr.Value = value

    @property
    def CandleType(self):
        """Type of candles to use."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        """!! REQUIRED!! override to return securities used by the strategy."""
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(keltner_with_rl_signal_strategy, self).OnStarted(time)

        # Initialize RL state variables
        self._current_signal = self._RLSignal.None_
        self._consecutive_wins = 0
        self._consecutive_losses = 0
        self._last_price = 0
        self._previous_ema = 0
        self._previous_atr = 0
        self._previous_price = 0
        self._previous_signal_price = 0

        # Create Keltner Channels using EMA and ATR
        keltner = KeltnerChannels()
        keltner.Length = self.EmaPeriod
        keltner.Multiplier = self.AtrMultiplier

        # Subscribe to candles and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(keltner, self.ProcessCandle).Start()

        # Subscribe to own trades for reinforcement learning feedback
        self.WhenOwnTradeReceived().Do(self.ProcessOwnTrade).Apply(self)

        # Create chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, keltner)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, keltner_value):
        """Process each candle and Keltner Channel values."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        keltner_typed = keltner_value  # KeltnerChannelsValue

        upper_band = keltner_typed.Upper
        lower_band = keltner_typed.Lower
        middle_band = keltner_typed.Middle

        if upper_band is None or lower_band is None or middle_band is None:
            return

        # Calculate current ATR value (upper - middle)/multiplier
        current_atr = (upper_band - middle_band) / self.AtrMultiplier

        # Update price and RL state
        self._last_price = candle.ClosePrice

        # Generate RL signal based on current state
        self.UpdateRLSignal(candle, middle_band, current_atr)

        # Trading logic
        price = candle.ClosePrice
        price_above_upper_band = price > upper_band
        price_below_lower_band = price < lower_band

        # Entry conditions

        # Long entry: Price above upper band and RL signal is Buy
        if price_above_upper_band and self._current_signal == self._RLSignal.Buy and self.Position <= 0:
            self.LogInfo(f"Long signal: Price {price} > Upper Band {upper_band}, RL Signal = Buy")
            self.BuyMarket(self.Volume)
            self._previous_signal_price = price
        # Short entry: Price below lower band and RL signal is Sell
        elif price_below_lower_band and self._current_signal == self._RLSignal.Sell and self.Position >= 0:
            self.LogInfo(f"Short signal: Price {price} < Lower Band {lower_band}, RL Signal = Sell")
            self.SellMarket(self.Volume)
            self._previous_signal_price = price

        # Exit conditions

        # Exit long: Price drops below EMA (middle band)
        if self.Position > 0 and price < middle_band:
            self.LogInfo(f"Exit long: Price {price} < EMA {middle_band}")
            self.SellMarket(Math.Abs(self.Position))
        # Exit short: Price rises above EMA (middle band)
        elif self.Position < 0 and price > middle_band:
            self.LogInfo(f"Exit short: Price {price} > EMA {middle_band}")
            self.BuyMarket(Math.Abs(self.Position))

        # Set stop loss based on ATR
        self.ApplyAtrStopLoss(price, current_atr)

        # Update previous values for next iteration
        self._previous_ema = middle_band
        self._previous_atr = current_atr
        self._previous_price = price

    def UpdateRLSignal(self, candle, ema, atr):
        """Update Reinforcement Learning signal based on current state.
        This is a simplified RL model (Q-learning) for demonstration.
        In a real system, this would likely be a more sophisticated model."""
        # Features for RL decision:
        # 1. Price position relative to EMA
        price_above_ema = candle.ClosePrice > ema

        # 2. Recent momentum
        price_increasing = candle.ClosePrice > self._previous_price

        # 3. Volatility
        volatility_increasing = atr > self._previous_atr

        # 4. Candle pattern (bullish/bearish)
        bullish_candle = candle.ClosePrice > candle.OpenPrice

        # 5. Previous trade outcome
        # More conservative after losses, more aggressive after wins
        aggressive_mode = self._consecutive_wins > self._consecutive_losses

        # Simplified Q-learning decision matrix
        if bullish_candle and price_above_ema and (price_increasing or aggressive_mode):
            self._current_signal = self._RLSignal.Buy
            self.LogInfo("RL Signal: Buy")
        elif not bullish_candle and not price_above_ema and (not price_increasing or aggressive_mode):
            self._current_signal = self._RLSignal.Sell
            self.LogInfo("RL Signal: Sell")
        else:
            # If conditions are mixed, maintain current signal or go neutral
            if volatility_increasing:
                # High volatility might warrant reducing exposure
                self._current_signal = self._RLSignal.None_
                self.LogInfo("RL Signal: None (high volatility)")
            # Otherwise keep current signal

    def ProcessOwnTrade(self, trade):
        """Process own trades for reinforcement learning feedback."""
        # Skip if we don't have a previous signal price (first trade)
        if self._previous_signal_price == 0:
            return

        # Determine if the trade was profitable
        if trade.Order.Side == Sides.Buy:
            # For buys, it's profitable if current price > entry price
            profitable = self._last_price > trade.Trade.Price
        else:
            # For sells, it's profitable if current price < entry price
            profitable = self._last_price < trade.Trade.Price

        # Update consecutive win/loss counters for RL state
        if profitable:
            self._consecutive_wins += 1
            self._consecutive_losses = 0
            self.LogInfo(f"Profitable trade: Win streak = {self._consecutive_wins}")
        else:
            self._consecutive_losses += 1
            self._consecutive_wins = 0
            self.LogInfo(f"Unprofitable trade: Loss streak = {self._consecutive_losses}")

    def ApplyAtrStopLoss(self, price, atr):
        """Apply ATR-based stop loss."""
        # Dynamic stop loss based on ATR
        if self.Position > 0:  # Long position
            stop_level = price - (self.StopLossAtr * atr)
            if self._last_price < stop_level:
                self.LogInfo(f"ATR Stop Loss triggered for long position: Current {self._last_price} < Stop {stop_level}")
                self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0:  # Short position
            stop_level = price + (self.StopLossAtr * atr)
            if self._last_price > stop_level:
                self.LogInfo(f"ATR Stop Loss triggered for short position: Current {self._last_price} > Stop {stop_level}")
                self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return keltner_with_rl_signal_strategy()

