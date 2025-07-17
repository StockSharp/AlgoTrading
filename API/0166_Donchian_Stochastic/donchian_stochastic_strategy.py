import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import UnitTypes, Unit, DataType, CandleStates
from StockSharp.Algo.Indicators import DonchianChannels, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class donchian_stochastic_strategy(Strategy):
    """
    Donchian Channel + Stochastic strategy.
    Strategy enters the market when the price breaks out of Donchian Channel with Stochastic confirming oversold/overbought conditions.

    """

    def __init__(self):
        super(donchian_stochastic_strategy, self).__init__()

        # Initialize strategy parameters
        self._donchianPeriod = self.Param("DonchianPeriod", 20) \
            .SetDisplay("Donchian Period", "Donchian Channel lookback period", "Indicators")

        self._stochPeriod = self.Param("StochPeriod", 14) \
            .SetDisplay("Stochastic Period", "Stochastic oscillator period", "Indicators")

        self._stochK = self.Param("StochK", 3) \
            .SetDisplay("Stochastic %K", "Stochastic %K period", "Indicators")

        self._stochD = self.Param("StochD", 3) \
            .SetDisplay("Stochastic %D", "Stochastic %D period", "Indicators")

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stopLossPercent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")

    @property
    def DonchianPeriod(self):
        """Donchian Channel period."""
        return self._donchianPeriod.Value

    @DonchianPeriod.setter
    def DonchianPeriod(self, value):
        self._donchianPeriod.Value = value

    @property
    def StochPeriod(self):
        """Stochastic period."""
        return self._stochPeriod.Value

    @StochPeriod.setter
    def StochPeriod(self, value):
        self._stochPeriod.Value = value

    @property
    def StochK(self):
        """Stochastic %K period."""
        return self._stochK.Value

    @StochK.setter
    def StochK(self, value):
        self._stochK.Value = value

    @property
    def StochD(self):
        """Stochastic %D period."""
        return self._stochD.Value

    @StochD.setter
    def StochD(self, value):
        self._stochD.Value = value

    @property
    def CandleType(self):
        """Candle type."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    @property
    def StopLossPercent(self):
        """Stop-loss percentage."""
        return self._stopLossPercent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercent.Value = value

    def OnStarted(self, time):
        """
        Called when the strategy starts. Creates indicators, subscriptions and charting.

        :param time: The time when the strategy started.
        """
        super(donchian_stochastic_strategy, self).OnStarted(time)

        # Create indicators
        self._donchian = DonchianChannels()
        self._donchian.Length = self.DonchianPeriod

        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = self.StochK
        self._stochastic.D.Length = self.StochD

        # Enable position protection
        take_profit = Unit(0, UnitTypes.Absolute)  # No take profit - we'll exit based on strategy rules
        stop_loss = Unit(self.StopLossPercent, UnitTypes.Percent)
        self.StartProtection(
            takeProfit=take_profit,
            stopLoss=stop_loss
        )
        # Subscribe to candles and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._donchian, self._stochastic, self.ProcessCandle).Start()

        # Setup chart
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._donchian)
            second_area = self.CreateChartArea()
            if second_area is not None:
                self.DrawIndicator(second_area, self._stochastic)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, donchian_value, stoch_value):
        """
        Process candle and execute trading logic

        :param candle: The candle message.
        :param donchian_value: The Donchian Channels indicator value.
        :param stoch_value: The Stochastic oscillator value.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if (
            donchian_value.UpperBand is None
            or donchian_value.LowerBand is None
            or donchian_value.Middle is None
        ):
            return
        upper_band = float(donchian_value.UpperBand)
        lower_band = float(donchian_value.LowerBand)
        middle_band = float(donchian_value.Middle)

        stoch_k = float(stoch_value.K)
        stoch_d = float(stoch_value.D)

        # Trading logic:
        # Buy when price breaks above upper Donchian band with Stochastic showing oversold condition
        if candle.ClosePrice >= upper_band and stoch_k < 20 and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo("Long entry: Price= float({0}, Upper Band={1}, Stochastic %K={2}".format(candle.ClosePrice, upper_band, stoch_k)))
        # Sell when price breaks below lower Donchian band with Stochastic showing overbought condition
        elif candle.ClosePrice <= lower_band and stoch_k > 80 and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo("Short entry: Price= float({0}, Lower Band={1}, Stochastic %K={2}".format(candle.ClosePrice, lower_band, stoch_k)))
        # Exit long position when price falls below middle band
        elif self.Position > 0 and candle.ClosePrice < middle_band:
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo("Long exit: Price= float({0}, Middle Band={1}".format(candle.ClosePrice, middle_band)))
        # Exit short position when price rises above middle band
        elif self.Position < 0 and candle.ClosePrice > middle_band:
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Short exit: Price= float({0}, Middle Band={1}".format(candle.ClosePrice, middle_band)))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return donchian_stochastic_strategy()