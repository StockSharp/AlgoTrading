import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import DonchianChannels, RSI
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *


class donchian_rsi_strategy(Strategy):
    """
    Strategy combining Donchian Channels and RSI indicators.
    Buys on Donchian breakouts when RSI confirms trend is not overextended.
    """

    def __init__(self):
        super(donchian_rsi_strategy, self).__init__()

        # Initialize strategy parameters
        self._donchian_period = self.Param("DonchianPeriod", 20) \
            .SetDisplay("Donchian Period", "Period for Donchian Channels calculation", "Indicators")

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Period for RSI calculation", "Indicators")

        self._rsi_overbought_level = self.Param("RsiOverboughtLevel", 70.0) \
            .SetDisplay("RSI Overbought", "RSI level considered overbought", "Trading Levels")

        self._rsi_oversold_level = self.Param("RsiOversoldLevel", 30.0) \
            .SetDisplay("RSI Oversold", "RSI level considered oversold", "Trading Levels")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Variables to store previous high and low values for breakout detection
        self._prev_upper_band = 0.0
        self._prev_lower_band = 0.0
        self._is_first_calculation = True

    @property
    def donchian_period(self):
        """Period for Donchian Channels calculation."""
        return self._donchian_period.Value

    @donchian_period.setter
    def donchian_period(self, value):
        self._donchian_period.Value = value

    @property
    def rsi_period(self):
        """Period for RSI calculation."""
        return self._rsi_period.Value

    @rsi_period.setter
    def rsi_period(self, value):
        self._rsi_period.Value = value

    @property
    def rsi_overbought_level(self):
        """RSI overbought level."""
        return self._rsi_overbought_level.Value

    @rsi_overbought_level.setter
    def rsi_overbought_level(self, value):
        self._rsi_overbought_level.Value = value

    @property
    def rsi_oversold_level(self):
        """RSI oversold level."""
        return self._rsi_oversold_level.Value

    @rsi_oversold_level.setter
    def rsi_oversold_level(self, value):
        self._rsi_oversold_level.Value = value

    @property
    def stop_loss_percent(self):
        """Stop loss percentage."""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def candle_type(self):
        """Candle type for strategy calculation."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(donchian_rsi_strategy, self).OnReseted()
        self._prev_upper_band = 0.0
        self._prev_lower_band = 0.0
        self._is_first_calculation = True

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.

        :param time: The time when the strategy started.
        """
        super(donchian_rsi_strategy, self).OnStarted(time)

        self._prev_upper_band = 0.0
        self._prev_lower_band = 0.0
        self._is_first_calculation = True

        # Create indicators
        donchian = DonchianChannels()
        donchian.Length = self.donchian_period
        rsi = RSI()
        rsi.Length = self.rsi_period

        # Create subscription
        subscription = self.SubscribeCandles(self.candle_type)

        # Bind indicators to candles
        subscription.BindEx(donchian, rsi, self.ProcessCandle).Start()

        # Enable stop-loss
        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent),
            isStopTrailing=False,
            useMarketOrders=True
        )
        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, donchian)

            # Create second area for RSI
            rsi_area = self.CreateChartArea()
            self.DrawIndicator(rsi_area, rsi)

            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, donchian_value, rsi_value):
        """
        Processes each finished candle and executes trading logic.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        try:
            upper_band = float(donchian_value.UpperBand)
            lower_band = float(donchian_value.LowerBand)
            middle_band = float(donchian_value.Middle)
        except Exception:
            return

        # Store current bands before comparison
        current_upper = upper_band
        current_lower = lower_band

        # Skip first calculation to avoid false breakouts
        if self._is_first_calculation:
            self._is_first_calculation = False
            self._prev_upper_band = current_upper
            self._prev_lower_band = current_lower
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        rsi_dec = to_float(rsi_value) if hasattr(rsi_value, 'ToDecimal') else float(rsi_value)

        # Detect breakouts by comparing current price to previous Donchian bands
        upper_breakout = candle.ClosePrice > self._prev_upper_band
        lower_breakout = candle.ClosePrice < self._prev_lower_band

        # Trading logic
        if upper_breakout and rsi_dec < self.rsi_overbought_level and self.Position <= 0:
            # Upward breakout with RSI not overbought - Buy
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
        elif lower_breakout and rsi_dec > self.rsi_oversold_level and self.Position >= 0:
            # Downward breakout with RSI not oversold - Sell
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
        elif self.Position > 0 and candle.ClosePrice < middle_band:
            # Exit long position when price falls below middle band
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0 and candle.ClosePrice > middle_band:
            # Exit short position when price rises above middle band
            self.BuyMarket(Math.Abs(self.Position))

        # Update previous bands for next comparison
        self._prev_upper_band = current_upper
        self._prev_lower_band = current_lower

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return donchian_rsi_strategy()
