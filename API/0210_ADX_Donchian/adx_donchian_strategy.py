import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import AverageDirectionalIndex, DonchianChannels
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class adx_donchian_strategy(Strategy):
    """
    Strategy based on ADX and Donchian Channel indicators
    
    """

    def __init__(self):
        super(adx_donchian_strategy, self).__init__()

        # Initialize strategy parameters
        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetRange(7, 28) \
            .SetDisplay("ADX Period", "Period for ADX indicator", "Indicators") \
            .SetCanOptimize(True)

        self._donchian_period = self.Param("DonchianPeriod", 5) \
            .SetRange(5, 50) \
            .SetDisplay("Donchian Period", "Period for Donchian Channel", "Indicators") \
            .SetCanOptimize(True)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetRange(0.5, 5.0) \
            .SetDisplay("Stop-Loss %", "Stop-loss percentage from entry price", "Risk Management") \
            .SetCanOptimize(True)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._adx_threshold = self.Param("AdxThreshold", 10) \
            .SetRange(5, 40) \
            .SetDisplay("ADX Threshold", "ADX value for strong trend detection", "Indicators") \
            .SetCanOptimize(True)

        self._multiplier = self.Param("Multiplier", 0.1) \
            .SetRange(0.0, 1.0) \
            .SetDisplay("Multiplier %", "Sensitivity to Donchian Channel border (percent)", "Indicators") \
            .SetCanOptimize(True)

    @property
    def adx_period(self):
        """ADX period"""
        return self._adx_period.Value

    @adx_period.setter
    def adx_period(self, value):
        self._adx_period.Value = value

    @property
    def donchian_period(self):
        """Donchian Channel period"""
        return self._donchian_period.Value

    @donchian_period.setter
    def donchian_period(self, value):
        self._donchian_period.Value = value

    @property
    def stop_loss_percent(self):
        """Stop-loss percentage"""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def candle_type(self):
        """Candle type for strategy"""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def adx_threshold(self):
        """ADX threshold for strong trend detection"""
        return self._adx_threshold.Value

    @adx_threshold.setter
    def adx_threshold(self, value):
        self._adx_threshold.Value = value

    @property
    def multiplier(self):
        """Multiplier for Donchian Channel border sensitivity (in percent, e.g. 0.1 for 0.1%)"""
        return self._multiplier.Value

    @multiplier.setter
    def multiplier(self, value):
        self._multiplier.Value = value

    def OnStarted(self, time):
        """
        Called when the strategy starts. Initializes indicators, subscriptions, and charting.
        """
        super(adx_donchian_strategy, self).OnStarted(time)

        # Initialize indicators
        adx = AverageDirectionalIndex()
        adx.Length = self.adx_period
        donchian = DonchianChannels()
        donchian.Length = self.donchian_period

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(donchian, adx, self.ProcessCandle).Start()

        # Enable percentage-based stop-loss protection
        self.StartProtection(
            takeProfit=Unit(self.stop_loss_percent, UnitTypes.Percent),
            stopLoss=None
        )
        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, adx)
            self.DrawIndicator(area, donchian)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, donchian_value, adx_value):
        """
        Processes each finished candle and executes trading logic.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Process ADX
        if adx_value.MovingAverage is None:
            return
        adx_ma = float(adx_value.MovingAverage)

        # Get Donchian Channel values
        if (
            donchian_value.UpperBand is None
            or donchian_value.LowerBand is None
            or donchian_value.Middle is None
        ):
            return
        upper_band = float(donchian_value.UpperBand)
        middle_band = float(donchian_value.Middle)
        lower_band = float(donchian_value.LowerBand)

        price = float(candle.ClosePrice)

        # Trading logic:
        # Long: ADX > AdxThreshold && Price >= upperBorder (strong trend with breakout up)
        # Short: ADX > AdxThreshold && Price <= lowerBorder (strong trend with breakout down)

        strong_trend = adx_ma > self.adx_threshold

        upper_border = upper_band * (1 - self.multiplier / 100)
        lower_border = lower_band * (1 + self.multiplier / 100)

        if strong_trend and price >= upper_border and self.Position <= 0:
            # Buy signal - Strong trend with Donchian Channel breakout up (with multiplier)
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
        elif strong_trend and price <= lower_border and self.Position >= 0:
            # Sell signal - Strong trend with Donchian Channel breakout down (with multiplier)
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
        # Exit conditions - ADX weakness
        elif self.Position != 0 and adx_ma < self.adx_threshold - 5:
            # Exit position when ADX falls below (threshold - 5)
            if self.Position > 0:
                self.SellMarket(self.Position)
            else:
                self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return adx_donchian_strategy()
