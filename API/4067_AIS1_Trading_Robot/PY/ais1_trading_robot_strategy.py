import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange

class ais1_trading_robot_strategy(Strategy):
    def __init__(self):
        super(ais1_trading_robot_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for analysis", "General")
        self._ema_length = self.Param("EmaLength", 50) \
            .SetDisplay("EMA Length", "Period for trend EMA", "Indicators")
        self._atr_length = self.Param("AtrLength", 20) \
            .SetDisplay("ATR Length", "Period for ATR", "Indicators")
        self._take_factor = self.Param("TakeFactor", 3.0) \
            .SetDisplay("Take Factor", "ATR multiplier for take profit", "Risk")
        self._stop_factor = self.Param("StopFactor", 1.5) \
            .SetDisplay("Stop Factor", "ATR multiplier for stop loss", "Risk")

        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def EmaLength(self):
        return self._ema_length.Value

    @property
    def AtrLength(self):
        return self._atr_length.Value

    @property
    def TakeFactor(self):
        return self._take_factor.Value

    @property
    def StopFactor(self):
        return self._stop_factor.Value

    def OnStarted(self, time):
        super(ais1_trading_robot_strategy, self).OnStarted(time)

        self._entry_price = 0.0

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.EmaLength
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._ema, self._atr, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, ema_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        ema_val = float(ema_value)
        atr_val = float(atr_value)

        if atr_val <= 0:
            return

        close = float(candle.ClosePrice)
        take_distance = atr_val * float(self.TakeFactor)
        stop_distance = atr_val * float(self.StopFactor)

        # Position management with ATR-based stops
        if self.Position > 0:
            if self._entry_price > 0:
                if close >= self._entry_price + take_distance or close <= self._entry_price - stop_distance:
                    self.SellMarket()
        elif self.Position < 0:
            if self._entry_price > 0:
                if close <= self._entry_price - take_distance or close >= self._entry_price + stop_distance:
                    self.BuyMarket()

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Entry: breakout above/below EMA
        if self.Position == 0:
            if close > ema_val + atr_val * 1.5:
                self._entry_price = close
                self.BuyMarket()
            elif close < ema_val - atr_val * 1.5:
                self._entry_price = close
                self.SellMarket()

    def OnReseted(self):
        super(ais1_trading_robot_strategy, self).OnReseted()
        self._entry_price = 0.0

    def CreateClone(self):
        return ais1_trading_robot_strategy()
