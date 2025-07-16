import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, UnitTypes, Unit, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class atr_macd_strategy(Strategy):
    """
    Strategy that uses ATR (Average True Range) for volatility detection
    and MACD for trend direction confirmation.
    Enters positions when volatility increases and MACD confirms trend direction.
    """

    def __init__(self):
        super(atr_macd_strategy, self).__init__()

        # Initialize strategy parameters
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "ATR indicator period", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 21, 7)

        self._atr_avg_period = self.Param("AtrAvgPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Avg Period", "ATR average period", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._atr_multiplier = self.Param("AtrMultiplier", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Multiplier", "ATR comparison multiplier", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 2.0, 0.1)

        self._macd_fast = self.Param("MacdFast", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Fast", "MACD fast period", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(8, 16, 4)

        self._macd_slow = self.Param("MacdSlow", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Slow", "MACD slow period", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(20, 32, 4)

        self._macd_signal = self.Param("MacdSignal", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Signal", "MACD signal period", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 13, 4)

        self._stop_loss_atr = self.Param("StopLossAtr", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss ATR", "Stop loss as ATR multiplier", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._atr_delta_percent = self.Param("AtrDeltaPercent", 10.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Delta %", "Minimum ATR increase percent compared to previous value", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(5.0, 20.0, 1.0)

        # Internal variables
        self._prev_atr_avg = 0.0
        self._prev_atr = 0.0
        self._atr_avg = None
        self._bars_since_last_trade = 0

    @property
    def AtrPeriod(self):
        """ATR indicator period."""
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def AtrAvgPeriod(self):
        """Period for averaging ATR values."""
        return self._atr_avg_period.Value

    @AtrAvgPeriod.setter
    def AtrAvgPeriod(self, value):
        self._atr_avg_period.Value = value

    @property
    def AtrMultiplier(self):
        """Multiplier for ATR comparison."""
        return self._atr_multiplier.Value

    @AtrMultiplier.setter
    def AtrMultiplier(self, value):
        self._atr_multiplier.Value = value

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

    @property
    def AtrDeltaPercent(self):
        """Minimum percentage increase in ATR relative to the previous value."""
        return self._atr_delta_percent.Value

    @AtrDeltaPercent.setter
    def AtrDeltaPercent(self, value):
        self._atr_delta_percent.Value = value

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(atr_macd_strategy, self).OnStarted(time)

        # Initialize variables
        self._prev_atr_avg = 0
        self._prev_atr = 0
        self._bars_since_last_trade = 1000

        # Create indicators
        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        self._atr_avg = SimpleMovingAverage()
        self._atr_avg.Length = self.AtrAvgPeriod

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.MacdFast
        macd.Macd.LongMa.Length = self.MacdSlow
        macd.SignalMa.Length = self.MacdSignal

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(atr, macd, self.ProcessIndicators).Start()

        # Setup position protection
        self.StartProtection(
            Unit(0, UnitTypes.Absolute),  # No take profit
            Unit(self.StopLossAtr, UnitTypes.Absolute)  # Stop loss as ATR multiplier
        )

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, atr)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def ProcessIndicators(self, candle, atr_value, macd_value):
        """Process MACD indicator values."""
        if not atr_value.IsFinal:
            return

        # Process ATR through averaging indicator
        current_atr = to_float(atr_value)
        avg_value = self._atr_avg.Process(atr_value)
        if not avg_value.IsFinal:
            return

        # Store current ATR average value
        current_atr_avg = to_float(avg_value)
        self._prev_atr_avg = current_atr_avg

        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading() or self._prev_atr_avg == 0:
            return

        self._bars_since_last_trade += 1

        macd_dec = macd_value.Macd
        signal_value = macd_value.Signal

        # ATR must be greater than average * multiplier AND greater than previous ATR by AtrDeltaPercent
        atr_delta = 0 if self._prev_atr == 0 else (current_atr - self._prev_atr) / self._prev_atr * 100.0
        is_volatility_increasing = (
            current_atr > current_atr_avg * self.AtrMultiplier and
            (self._prev_atr != 0 and current_atr > self._prev_atr * (1 + self.AtrDeltaPercent / 100))
        )
        self.LogInfo(
            "ATR: {0:F4}, PrevATR: {1:F4}, ATRAvg: {2:F4}, ATRDelta: {3:F2}%, isVolatilityIncreasing: {4}, BarsSinceLastTrade: {5}".format(
                current_atr, self._prev_atr, current_atr_avg, atr_delta, is_volatility_increasing, self._bars_since_last_trade)
        )

        self._prev_atr = current_atr
        self._prev_atr_avg = current_atr_avg

        bars_between_trades = 300

        if is_volatility_increasing and self._bars_since_last_trade >= bars_between_trades:
            # Long entry: MACD above Signal in rising volatility
            if macd_dec > signal_value and self.Position <= 0:
                volume = self.Volume + Math.Abs(self.Position)
                self.BuyMarket(volume)
                self.LogInfo("Buy: MACD {0:F4} > Signal {1:F4}".format(macd_dec, signal_value))
                self._bars_since_last_trade = 0
            # Short entry: MACD below Signal in rising volatility
            elif macd_dec < signal_value and self.Position >= 0:
                volume = self.Volume + Math.Abs(self.Position)
                self.SellMarket(volume)
                self.LogInfo("Sell: MACD {0:F4} < Signal {1:F4}".format(macd_dec, signal_value))
                self._bars_since_last_trade = 0

        # Exit conditions based on MACD crossovers
        if self.Position > 0 and macd_dec < signal_value:
            self.SellMarket(self.Position)
            self.LogInfo("Exit Long: MACD {0:F4} < Signal {1:F4}".format(macd_dec, signal_value))
            self._bars_since_last_trade = 0
        elif self.Position < 0 and macd_dec > signal_value:
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exit Short: MACD {0:F4} > Signal {1:F4}".format(macd_dec, signal_value))
            self._bars_since_last_trade = 0

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return atr_macd_strategy()
