import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType
from StockSharp.Messages import CandleStates
from StockSharp.Messages import Unit
from StockSharp.Messages import UnitTypes
from StockSharp.Algo.Indicators import KeltnerChannels
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class keltner_stochastic_strategy(Strategy):
    """
    Strategy that combines Keltner Channels and Stochastic Oscillator.
    Enters positions when price reaches Keltner Channel boundaries
    and Stochastic confirms oversold/overbought conditions.
    """

    def __init__(self):
        super(keltner_stochastic_strategy, self).__init__()

        # Initialize strategy parameters
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Period", "Period of the EMA for Keltner Channel", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period of the ATR for Keltner Channel", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 21, 7)

        self._keltner_multiplier = self.Param("KeltnerMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Keltner Multiplier", "Multiplier for ATR in Keltner Channel", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1.5, 3.0, 0.5)

        self._stoch_period = self.Param("StochPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic Period", "Period of the Stochastic Oscillator", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 20, 5)

        self._stoch_k = self.Param("StochK", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic %K", "Smoothing of the %K line", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1, 5, 1)

        self._stoch_d = self.Param("StochD", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic %D", "Smoothing of the %D line", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1, 5, 1)

        self._stoch_oversold = self.Param("StochOversold", 20.0) \
            .SetNotNegative() \
            .SetDisplay("Stochastic Oversold", "Level considered oversold", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10.0, 30.0, 5.0)

        self._stoch_overbought = self.Param("StochOverbought", 80.0) \
            .SetNotNegative() \
            .SetDisplay("Stochastic Overbought", "Level considered overbought", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(70.0, 90.0, 5.0)

        self._stop_loss_atr = self.Param("StopLossAtr", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss ATR", "Stop loss as ATR multiplier", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def EmaPeriod(self):
        """EMA period for Keltner Channel."""
        return self._ema_period.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._ema_period.Value = value

    @property
    def AtrPeriod(self):
        """ATR period for Keltner Channel."""
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def KeltnerMultiplier(self):
        """Keltner Channel multiplier."""
        return self._keltner_multiplier.Value

    @KeltnerMultiplier.setter
    def KeltnerMultiplier(self, value):
        self._keltner_multiplier.Value = value

    @property
    def StochPeriod(self):
        """Stochastic period."""
        return self._stoch_period.Value

    @StochPeriod.setter
    def StochPeriod(self, value):
        self._stoch_period.Value = value

    @property
    def StochK(self):
        """Stochastic %K period."""
        return self._stoch_k.Value

    @StochK.setter
    def StochK(self, value):
        self._stoch_k.Value = value

    @property
    def StochD(self):
        """Stochastic %D period."""
        return self._stoch_d.Value

    @StochD.setter
    def StochD(self, value):
        self._stoch_d.Value = value

    @property
    def StochOversold(self):
        """Stochastic oversold level."""
        return self._stoch_oversold.Value

    @StochOversold.setter
    def StochOversold(self, value):
        self._stoch_oversold.Value = value

    @property
    def StochOverbought(self):
        """Stochastic overbought level."""
        return self._stoch_overbought.Value

    @StochOverbought.setter
    def StochOverbought(self, value):
        self._stoch_overbought.Value = value

    @property
    def StopLossAtr(self):
        """Stop loss in ATR multiples."""
        return self._stop_loss_atr.Value

    @StopLossAtr.setter
    def StopLossAtr(self, value):
        self._stop_loss_atr.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy calculation."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(keltner_stochastic_strategy, self).OnStarted(time)

        # Create indicators
        # Create a full Keltner Channel indicator for visualization
        keltner = KeltnerChannels()
        keltner.Length = self.EmaPeriod
        keltner.Multiplier = self.KeltnerMultiplier

        stochastic = StochasticOscillator()
        stochastic.K.Length = self.StochK
        stochastic.D.Length = self.StochD

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)

        subscription.BindEx(keltner, stochastic, self.ProcessStochastic).Start()

        # Setup position protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),  # No take profit
            stopLoss=Unit(self.StopLossAtr, UnitTypes.Absolute)  # Stop loss as ATR multiplier
        )

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, keltner)
            self.DrawIndicator(area, stochastic)
            self.DrawOwnTrades(area)

    def ProcessStochastic(self, candle, keltner_value, stoch_value):
        """
        Process Stochastic indicator values.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        keltner_typed = keltner_value  # KeltnerChannelsValue
        ema_value = keltner_typed.Middle
        atr_value = keltner_typed.Upper - keltner_typed.Middle  # ATR is the distance from middle to upper band

        stoch_typed = stoch_value  # StochasticOscillatorValue

        # Calculate Keltner Channel bands
        upper_band = ema_value + (atr_value * self.KeltnerMultiplier)
        lower_band = ema_value - (atr_value * self.KeltnerMultiplier)

        # Long entry: price below lower Keltner band and Stochastic oversold
        if candle.ClosePrice < lower_band and stoch_typed.K < self.StochOversold and self.Position <= 0:
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
        # Short entry: price above upper Keltner band and Stochastic overbought
        elif candle.ClosePrice > upper_band and stoch_typed.K > self.StochOverbought and self.Position >= 0:
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
        # Long exit: price returns to EMA line (middle band)
        elif self.Position > 0 and candle.ClosePrice > ema_value:
            self.SellMarket(Math.Abs(self.Position))
        # Short exit: price returns to EMA line (middle band)
        elif self.Position < 0 and candle.ClosePrice < ema_value:
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return keltner_stochastic_strategy()
