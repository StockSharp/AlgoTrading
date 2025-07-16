import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType
from StockSharp.Messages import CandleStates
from StockSharp.Messages import Unit
from StockSharp.Messages import UnitTypes
from StockSharp.Algo.Indicators import HullMovingAverage
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class hull_ma_rsi_strategy(Strategy):
    """
    Implementation of strategy - Hull Moving Average + RSI.
    Buy when HMA is rising and RSI is below 30 (oversold).
    Sell when HMA is falling and RSI is above 70 (overbought).

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(hull_ma_rsi_strategy, self).__init__()

        # Initialize strategy parameters
        self._hma_period = self.Param("HmaPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("HMA Period", "Period for Hull Moving Average", "HMA Parameters")

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "Period for Relative Strength Index", "RSI Parameters")

        self._rsi_oversold = self.Param("RsiOversold", 30.0) \
            .SetRange(1, 100) \
            .SetDisplay("RSI Oversold", "RSI level to consider market oversold", "RSI Parameters")

        self._rsi_overbought = self.Param("RsiOverbought", 70.0) \
            .SetRange(1, 100) \
            .SetDisplay("RSI Overbought", "RSI level to consider market overbought", "RSI Parameters")

        self._stop_loss = self.Param("StopLoss", Unit(2, UnitTypes.Absolute)) \
            .SetDisplay("Stop Loss", "Stop loss in ATR or value", "Risk Management")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")

        # Previous HMA value
        self._prev_hma_value = 0.0

    @property
    def hma_period(self):
        """Hull Moving Average period."""
        return self._hma_period.Value

    @hma_period.setter
    def hma_period(self, value):
        self._hma_period.Value = value

    @property
    def rsi_period(self):
        """RSI period."""
        return self._rsi_period.Value

    @rsi_period.setter
    def rsi_period(self, value):
        self._rsi_period.Value = value

    @property
    def rsi_oversold(self):
        """RSI oversold level."""
        return self._rsi_oversold.Value

    @rsi_oversold.setter
    def rsi_oversold(self, value):
        self._rsi_oversold.Value = value

    @property
    def rsi_overbought(self):
        """RSI overbought level."""
        return self._rsi_overbought.Value

    @rsi_overbought.setter
    def rsi_overbought(self, value):
        self._rsi_overbought.Value = value

    @property
    def stop_loss(self):
        """Stop-loss value."""
        return self._stop_loss.Value

    @stop_loss.setter
    def stop_loss(self, value):
        self._stop_loss.Value = value

    @property
    def candle_type(self):
        """Candle type used for strategy."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(hull_ma_rsi_strategy, self).OnReseted()
        self._prev_hma_value = 0.0

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.

        :param time: The time when the strategy started.
        """
        super(hull_ma_rsi_strategy, self).OnStarted(time)

        # Create indicators
        hma = HullMovingAverage()
        hma.Length = self.hma_period
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period

        # Reset previous HMA value
        self._prev_hma_value = 0.0

        # Setup candle subscription
        subscription = self.SubscribeCandles(self.candle_type)

        # Bind indicators to candles
        subscription.Bind(hma, rsi, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hma)

            # Create separate area for RSI
            rsi_area = self.CreateChartArea()
            if rsi_area is not None:
                self.DrawIndicator(rsi_area, rsi)

            self.DrawOwnTrades(area)

        # Start protective orders
        self.StartProtection(None, self.stop_loss)

    def ProcessCandle(self, candle, hma_value, rsi_value):
        """Process candle and apply trading rules."""
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Determine if HMA is rising or falling
        is_hma_rising = self._prev_hma_value != 0 and hma_value > self._prev_hma_value
        is_hma_falling = self._prev_hma_value != 0 and hma_value < self._prev_hma_value

        self.LogInfo(
            "Candle: {0}, Close: {1}, HMA: {2}, Previous HMA: {3}, HMA Rising: {4}, HMA Falling: {5}, RSI: {6}".format(
                candle.OpenTime, candle.ClosePrice, hma_value, self._prev_hma_value,
                is_hma_rising, is_hma_falling, rsi_value))

        # Trading rules
        if is_hma_rising and rsi_value < self.rsi_oversold and self.Position <= 0:
            # Buy signal - HMA rising and RSI oversold
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)

            self.LogInfo(
                "Buy signal: HMA rising and RSI oversold ({0} < {1}). Volume: {2}".format(
                    rsi_value, self.rsi_oversold, volume))
        elif is_hma_falling and rsi_value > self.rsi_overbought and self.Position >= 0:
            # Sell signal - HMA falling and RSI overbought
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)

            self.LogInfo(
                "Sell signal: HMA falling and RSI overbought ({0} > {1}). Volume: {2}".format(
                    rsi_value, self.rsi_overbought, volume))
        elif is_hma_falling and self.Position > 0:
            # Exit long position when HMA starts falling
            self.SellMarket(self.Position)
            self.LogInfo(
                "Exit long: HMA started falling. Position: {0}".format(self.Position))
        elif is_hma_rising and self.Position < 0:
            # Exit short position when HMA starts rising
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo(
                "Exit short: HMA started rising. Position: {0}".format(self.Position))

        # Update HMA value for next iteration
        self._prev_hma_value = hma_value

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return hull_ma_rsi_strategy()
