import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import VolumeWeightedMovingAverage, CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class vwap_cci_strategy(Strategy):
    """
    Implementation of strategy #165 - VWAP + CCI.
    Buy when price is below VWAP and CCI is below -100 (oversold).
    Sell when price is above VWAP and CCI is above 100 (overbought).
    """

    def __init__(self):
        super(vwap_cci_strategy, self).__init__()

        # Initialize strategy parameters
        self._cci_period = self.Param("CciPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("CCI Period", "Period for Commodity Channel Index", "CCI Parameters")

        self._cci_oversold = self.Param("CciOversold", -100.0) \
            .SetDisplay("CCI Oversold", "CCI level to consider market oversold", "CCI Parameters")

        self._cci_overbought = self.Param("CciOverbought", 100.0) \
            .SetDisplay("CCI Overbought", "CCI level to consider market overbought", "CCI Parameters")

        self._stop_loss = self.Param("StopLoss", Unit(2, UnitTypes.Percent)) \
            .SetDisplay("Stop Loss", "Stop loss percent or value", "Risk Management")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")

    @property
    def cci_period(self):
        """CCI period."""
        return self._cci_period.Value

    @cci_period.setter
    def cci_period(self, value):
        self._cci_period.Value = value

    @property
    def cci_oversold(self):
        """CCI oversold level."""
        return self._cci_oversold.Value

    @cci_oversold.setter
    def cci_oversold(self, value):
        self._cci_oversold.Value = value

    @property
    def cci_overbought(self):
        """CCI overbought level."""
        return self._cci_overbought.Value

    @cci_overbought.setter
    def cci_overbought(self, value):
        self._cci_overbought.Value = value

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
        """
        Resets internal state when strategy is reset.
        """
        super(vwap_cci_strategy, self).OnReseted()

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.

        :param time: The time when the strategy started.
        """
        super(vwap_cci_strategy, self).OnStarted(time)

        # Create indicators
        vwap = VolumeWeightedMovingAverage()
        cci = CommodityChannelIndex()
        cci.Length = self.cci_period

        # Setup candle subscription
        subscription = self.SubscribeCandles(self.candle_type)

        # Bind indicators to candles
        subscription.Bind(vwap, cci, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, vwap)

            # Create separate area for CCI
            cci_area = self.CreateChartArea()
            if cci_area is not None:
                self.DrawIndicator(cci_area, cci)

            self.DrawOwnTrades(area)

        # Start protective orders
        self.StartProtection(None, self.stop_loss)

    def ProcessCandle(self, candle, vwap_value, cci_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Current price
        price = candle.ClosePrice

        # Determine if price is above or below VWAP
        is_price_above_vwap = price > vwap_value

        self.LogInfo("Candle: {0}, Close: {1}, " +
                     "VWAP: {2}, Price > VWAP: {3}, " +
                     "CCI: {4}".format(candle.OpenTime, price, vwap_value, is_price_above_vwap, cci_value))

        # Trading rules
        if not is_price_above_vwap and cci_value < self.cci_oversold and self.Position <= 0:
            # Buy signal - price below VWAP and CCI oversold
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)

            self.LogInfo("Buy signal: Price below VWAP and CCI oversold ({0} < {1}). Volume: {2}".format(
                cci_value, self.cci_oversold, volume))
        elif is_price_above_vwap and cci_value > self.cci_overbought and self.Position >= 0:
            # Sell signal - price above VWAP and CCI overbought
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)

            self.LogInfo("Sell signal: Price above VWAP and CCI overbought ({0} > {1}). Volume: {2}".format(
                cci_value, self.cci_overbought, volume))
        # Exit conditions
        elif is_price_above_vwap and self.Position > 0:
            # Exit long position when price crosses above VWAP
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exit long: Price crossed above VWAP. Position: {0}".format(self.Position))
        elif not is_price_above_vwap and self.Position < 0:
            # Exit short position when price crosses below VWAP
            self.SellMarket(self.Volume)
            self.LogInfo("Exit short: Price crossed below VWAP. Position: {0}".format(self.Position))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return vwap_cci_strategy()
