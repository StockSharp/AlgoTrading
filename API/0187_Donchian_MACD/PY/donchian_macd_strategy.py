import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import UnitTypes, Unit, DataType, CandleStates
from StockSharp.Algo.Indicators import DonchianChannels, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class donchian_macd_strategy(Strategy):
    """
    Strategy combining Donchian Channel breakout with MACD trend confirmation.
    """
    def __init__(self):
        super(donchian_macd_strategy, self).__init__()

        self._previousHighest = 0.0
        self._previousLowest = float("inf")
        self._previousMacd = None
        self._previousSignal = None
        self._entryPrice = None
        self._cooldown = 0

        self._donchianPeriod = self.Param("DonchianPeriod", 20) \
            .SetDisplay("Donchian Period", "Channel lookback period", "Indicators")

        self._macdFast = self.Param("MacdFast", 12) \
            .SetDisplay("MACD Fast Period", "Fast EMA period for MACD", "Indicators")

        self._macdSlow = self.Param("MacdSlow", 26) \
            .SetDisplay("MACD Slow Period", "Slow EMA period for MACD", "Indicators")

        self._macdSignal = self.Param("MacdSignal", 9) \
            .SetDisplay("MACD Signal Period", "Signal line period for MACD", "Indicators")

        self._cooldownBars = self.Param("CooldownBars", 50) \
            .SetRange(1, 200) \
            .SetDisplay("Cooldown Bars", "Bars between entries", "General")

        self._stopLossPercent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop-Loss %", "Stop-loss percentage from entry price", "Risk Management")

        self._candleType = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def CandleType(self):
        return self._candleType.Value

    def OnReseted(self):
        super(donchian_macd_strategy, self).OnReseted()
        self._previousHighest = 0.0
        self._previousLowest = float('inf')
        self._previousMacd = None
        self._previousSignal = None
        self._entryPrice = None
        self._cooldown = 0

    def OnStarted(self, time):
        super(donchian_macd_strategy, self).OnStarted(time)
        self._previousHighest = 0.0
        self._previousLowest = float('inf')
        self._previousMacd = None
        self._previousSignal = None
        self._entryPrice = None
        self._cooldown = 0

        donchian = DonchianChannels()
        donchian.Length = self._donchianPeriod.Value

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._macdFast.Value
        macd.Macd.LongMa.Length = self._macdSlow.Value
        macd.SignalMa.Length = self._macdSignal.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(donchian, macd, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, donchian)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, donchianValue, macdValue):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        signalValue = macdValue.Signal
        macdDec = macdValue.Macd

        if signalValue is None or macdDec is None:
            # Update donchian values
            if donchianValue.UpperBand is not None:
                self._previousHighest = float(donchianValue.UpperBand)
            if donchianValue.LowerBand is not None:
                self._previousLowest = float(donchianValue.LowerBand)
            self._previousMacd = macdDec
            self._previousSignal = signalValue
            return

        macd_f = float(macdDec)
        signal_f = float(signalValue)

        # Determine MACD crosses
        isBullishCross = False
        isBearishCross = False
        if self._previousMacd is not None and self._previousSignal is not None:
            prev_m = float(self._previousMacd)
            prev_s = float(self._previousSignal)
            isBullishCross = prev_m <= prev_s and macd_f > signal_f
            isBearishCross = prev_m >= prev_s and macd_f < signal_f

        if self._cooldown > 0:
            self._cooldown -= 1

        price = float(candle.ClosePrice)
        cooldown_val = int(self._cooldownBars.Value)

        # Long entry
        if self._cooldown == 0 and price > self._previousHighest * 1.001 and self.Position <= 0 and isBullishCross:
            self.CancelActiveOrders()
            volume = self.Volume + abs(self.Position)
            self.BuyMarket(volume)
            self._entryPrice = price
            self._cooldown = cooldown_val

        # Short entry
        elif self._cooldown == 0 and price < self._previousLowest * 0.999 and self.Position >= 0 and isBearishCross:
            self.CancelActiveOrders()
            volume = self.Volume + abs(self.Position)
            self.SellMarket(volume)
            self._entryPrice = price
            self._cooldown = cooldown_val

        # MACD trend reversal exit
        elif (self.Position > 0 and isBearishCross) or (self.Position < 0 and isBullishCross):
            self.ClosePosition()
            self._entryPrice = None
            self._cooldown = cooldown_val

        # Update previous values
        if donchianValue.UpperBand is not None:
            self._previousHighest = float(donchianValue.UpperBand)
        if donchianValue.LowerBand is not None:
            self._previousLowest = float(donchianValue.LowerBand)
        self._previousMacd = macdDec
        self._previousSignal = signalValue

    def CreateClone(self):
        return donchian_macd_strategy()
