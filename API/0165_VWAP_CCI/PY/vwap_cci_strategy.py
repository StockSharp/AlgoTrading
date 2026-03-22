import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import VolumeWeightedMovingAverage, CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class vwap_cci_strategy(Strategy):
    """
    Implementation of strategy - VWAP + CCI.
    Buy when price is below VWAP and CCI is below -20 (oversold).
    Sell when price is above VWAP and CCI is above 20 (overbought).
    """

    def __init__(self):
        super(vwap_cci_strategy, self).__init__()

        self._cci_period = self.Param("CciPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("CCI Period", "Period for Commodity Channel Index", "CCI Parameters")

        self._cci_oversold = self.Param("CciOversold", -20.0) \
            .SetDisplay("CCI Oversold", "CCI level to consider market oversold", "CCI Parameters")

        self._cci_overbought = self.Param("CciOverbought", 20.0) \
            .SetDisplay("CCI Overbought", "CCI level to consider market overbought", "CCI Parameters")

        self._cooldown_bars = self.Param("CooldownBars", 120) \
            .SetRange(5, 500) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._stop_loss = self.Param("StopLoss", Unit(2, UnitTypes.Percent)) \
            .SetDisplay("Stop Loss", "Stop loss percent or value", "Risk Management")

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")

        self._cooldown = 0

    @property
    def CciPeriod(self):
        return self._cci_period.Value

    @CciPeriod.setter
    def CciPeriod(self, value):
        self._cci_period.Value = value

    @property
    def CciOversold(self):
        return self._cci_oversold.Value

    @CciOversold.setter
    def CciOversold(self, value):
        self._cci_oversold.Value = value

    @property
    def CciOverbought(self):
        return self._cci_overbought.Value

    @CciOverbought.setter
    def CciOverbought(self, value):
        self._cci_overbought.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(vwap_cci_strategy, self).OnReseted()
        self._cooldown = 0

    def OnStarted(self, time):
        super(vwap_cci_strategy, self).OnStarted(time)

        # Create indicators
        vwap = VolumeWeightedMovingAverage()
        cci = CommodityChannelIndex()
        cci.Length = self.CciPeriod

        # Setup candle subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicators to candles
        subscription.Bind(vwap, cci, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, vwap)

            cci_area = self.CreateChartArea()
            if cci_area is not None:
                self.DrawIndicator(cci_area, cci)

            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, vwap_value, cci_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Current price
        price = float(candle.ClosePrice)
        vwap_val = float(vwap_value)

        # Determine if price is above or below VWAP
        isPriceAboveVWAP = price > vwap_val

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        # Trading rules - with tolerance bands like CS
        belowVwap = price <= vwap_val * 1.001
        aboveVwap = price >= vwap_val * 0.999

        if belowVwap and cci_value <= self.CciOversold and self.Position == 0:
            # Buy signal - price below VWAP and CCI oversold
            self.BuyMarket()
            self._cooldown = self.CooldownBars

        elif aboveVwap and cci_value >= self.CciOverbought and self.Position == 0:
            # Sell signal - price above VWAP and CCI overbought
            self.SellMarket()
            self._cooldown = self.CooldownBars

        # Exit conditions
        elif isPriceAboveVWAP and self.Position > 0:
            # Exit long position when price crosses above VWAP
            self.SellMarket()
            self._cooldown = self.CooldownBars

        elif not isPriceAboveVWAP and self.Position < 0:
            # Exit short position when price crosses below VWAP
            self.BuyMarket()
            self._cooldown = self.CooldownBars

    def CreateClone(self):
        return vwap_cci_strategy()
