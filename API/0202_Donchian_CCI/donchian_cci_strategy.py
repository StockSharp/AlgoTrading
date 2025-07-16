import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Indicators import DonchianChannels, CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class donchian_cci_strategy(Strategy):
    """
    Strategy based on Donchian Channels and CCI indicators (#202)
    """

    def __init__(self):
        super(donchian_cci_strategy, self).__init__()

        # Initialize strategy parameters
        self._donchian_period = self.Param("DonchianPeriod", 20) \
            .SetRange(10, 50) \
            .SetDisplay("Donchian Period", "Period for Donchian Channel", "Indicators") \
            .SetCanOptimize(True)

        self._cci_period = self.Param("CciPeriod", 20) \
            .SetRange(10, 50) \
            .SetDisplay("CCI Period", "Period for CCI indicator", "Indicators") \
            .SetCanOptimize(True)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetRange(0.5, 5.0) \
            .SetDisplay("Stop-Loss %", "Stop-loss percentage from entry price", "Risk Management") \
            .SetCanOptimize(True)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def DonchianPeriod(self):
        """Period for Donchian Channel"""
        return self._donchian_period.Value

    @DonchianPeriod.setter
    def DonchianPeriod(self, value):
        self._donchian_period.Value = value

    @property
    def CciPeriod(self):
        """Period for CCI indicator"""
        return self._cci_period.Value

    @CciPeriod.setter
    def CciPeriod(self, value):
        self._cci_period.Value = value

    @property
    def StopLossPercent(self):
        """Stop-loss percentage"""
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy"""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        """Overrides base to return working securities"""
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        """
        Called when the strategy starts.
        """
        super(donchian_cci_strategy, self).OnStarted(time)

        # Initialize Indicators
        donchian = DonchianChannels()
        donchian.Length = self.DonchianPeriod
        cci = CommodityChannelIndex()
        cci.Length = self.CciPeriod

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(donchian, cci, self.ProcessIndicators).Start()

        # Enable stop-loss protection
        self.StartProtection(takeProfit=None, stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent))

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, donchian)
            self.DrawIndicator(area, cci)
            self.DrawOwnTrades(area)

    def ProcessIndicators(self, candle, donchian_value, cci_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        donchian_typed = donchian_value
        upper_band = donchian_typed.UpperBand
        lower_band = donchian_typed.LowerBand
        middle_band = donchian_typed.Middle

        cci_dec = cci_value
        price = candle.ClosePrice

        # Trading logic:
        # Long: Price > Donchian Upper && CCI < -100 (breakout up with oversold conditions)
        # Short: Price < Donchian Lower && CCI > 100 (breakout down with overbought conditions)

        if price > upper_band and cci_dec < -100 and self.Position <= 0:
            # Buy signal - breakout up with oversold conditions
            volume = self.Volume + abs(self.Position)
            self.BuyMarket(volume)
        elif price < lower_band and cci_dec > 100 and self.Position >= 0:
            # Sell signal - breakout down with overbought conditions
            volume = self.Volume + abs(self.Position)
            self.SellMarket(volume)
        # Exit conditions
        elif self.Position > 0 and price < middle_band:
            # Exit long position when price falls below middle band
            self.SellMarket(self.Position)
        elif self.Position < 0 and price > middle_band:
            # Exit short position when price rises above middle band
            self.BuyMarket(abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return donchian_cci_strategy()
