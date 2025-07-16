import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, HullMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class rsi_hull_ma_strategy(Strategy):
    """
    Strategy based on RSI and Hull Moving Average indicators (#207)
    """

    def __init__(self):
        super(rsi_hull_ma_strategy, self).__init__()

        # RSI period
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetRange(5, 30) \
            .SetDisplay("RSI Period", "Period for RSI indicator", "Indicators") \
            .SetCanOptimize(True)

        # Hull MA period
        self._hull_period = self.Param("HullPeriod", 9) \
            .SetRange(5, 20) \
            .SetDisplay("Hull MA Period", "Period for Hull Moving Average", "Indicators") \
            .SetCanOptimize(True)

        # ATR period for stop-loss
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetRange(7, 28) \
            .SetDisplay("ATR Period", "ATR period for stop-loss calculation", "Risk Management") \
            .SetCanOptimize(True)

        # ATR multiplier for stop-loss
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetRange(1.0, 4.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR-based stop-loss", "Risk Management") \
            .SetCanOptimize(True)

        # Candle type for strategy
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._previous_hull_value = 0

    @property
    def RsiPeriod(self):
        """RSI period"""
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def HullPeriod(self):
        """Hull MA period"""
        return self._hull_period.Value

    @HullPeriod.setter
    def HullPeriod(self, value):
        self._hull_period.Value = value

    @property
    def AtrPeriod(self):
        """ATR period for stop-loss"""
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def AtrMultiplier(self):
        """ATR multiplier for stop-loss"""
        return self._atr_multiplier.Value

    @AtrMultiplier.setter
    def AtrMultiplier(self, value):
        self._atr_multiplier.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy"""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(rsi_hull_ma_strategy, self).OnStarted(time)

        self._previous_hull_value = 0

        # Initialize indicators
        rsi = RelativeStrengthIndex(); rsi.Length = self.RsiPeriod
        hull_ma = HullMovingAverage(); hull_ma.Length = self.HullPeriod
        atr = AverageTrueRange(); atr.Length = self.AtrPeriod

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, hull_ma, atr, self.ProcessIndicators).Start()

        # Enable ATR-based stop protection
        self.StartProtection(None, Unit(self.AtrMultiplier, UnitTypes.Absolute))

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawIndicator(area, hull_ma)
            self.DrawOwnTrades(area)

    def ProcessIndicators(self, candle, rsi_value, hull_value, atr_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Store previous Hull value for slope detection
        previous_hull_value = self._previous_hull_value
        self._previous_hull_value = hull_value

        # Skip first candle until we have previous value
        if previous_hull_value == 0:
            return

        # Trading logic:
        # Long: RSI < 30 && HMA(t) > HMA(t-1) (oversold with rising HMA)
        # Short: RSI > 70 && HMA(t) < HMA(t-1) (overbought with falling HMA)
        hull_slope = hull_value > previous_hull_value

        if rsi_value < 30 and hull_slope and self.Position <= 0:
            # Buy signal - RSI oversold with rising HMA
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
        elif rsi_value > 70 and not hull_slope and self.Position >= 0:
            # Sell signal - RSI overbought with falling HMA
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
        # Exit conditions
        elif self.Position > 0 and rsi_value > 50:
            # Exit long position when RSI returns to neutral zone
            self.SellMarket(self.Position)
        elif self.Position < 0 and rsi_value < 50:
            # Exit short position when RSI returns to neutral zone
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return rsi_hull_ma_strategy()

