import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class atr_slope_breakout_strategy(Strategy):
    """
    ATR slope breakout.
    Enters when the ATR slope exceeds its average by a standard deviation multiplier,
    in the direction of the breakout candle. Exits when the slope returns to its average
    or the ATR-based stop is hit.
    """

    def __init__(self):
        super(atr_slope_breakout_strategy, self).__init__()

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period for ATR", "Indicators")
        self._slope_period = self.Param("SlopePeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Slope Period", "Period for slope statistics", "Strategy")
        self._breakout_multiplier = self.Param("BreakoutMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Breakout Multiplier", "Standard deviation multiplier for breakout", "Strategy")
        self._stop_loss_atr_multiplier = self.Param("StopLossAtrMultiplier", 2.0) \
            .SetNotNegative() \
            .SetDisplay("Stop ATR Multiplier", "Stop-loss distance in ATR multiples", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._slope_average = None
        self._slope_std_dev = None
        self._prev_atr = None
        self._stop_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(atr_slope_breakout_strategy, self).OnReseted()
        self._slope_average = None
        self._slope_std_dev = None
        self._prev_atr = None
        self._stop_price = 0.0

    def OnStarted2(self, time):
        super(atr_slope_breakout_strategy, self).OnStarted2(time)

        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        self._slope_average = SimpleMovingAverage()
        self._slope_average.Length = self._slope_period.Value
        self._slope_std_dev = StandardDeviation()
        self._slope_std_dev.Length = self._slope_period.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        atr_value = float(atr_value)

        if self._prev_atr is None:
            self._prev_atr = atr_value
            return

        slope = atr_value - self._prev_atr
        self._prev_atr = atr_value

        avg_slope = float(process_float(self._slope_average, slope, candle.ServerTime, True))
        std_slope = float(process_float(self._slope_std_dev, slope, candle.ServerTime, True))

        if not self._slope_average.IsFormed or not self._slope_std_dev.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._check_stop(candle):
            return

        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)
        stop_distance = float(self._stop_loss_atr_multiplier.Value) * atr_value

        # ATR has no direction, so the breakout candle decides the side.
        if slope > avg_slope + float(self._breakout_multiplier.Value) * std_slope:
            if close > open_price and self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
                self._stop_price = close - stop_distance if stop_distance > 0 else 0.0
                return
            if close < open_price and self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))
                self._stop_price = close + stop_distance if stop_distance > 0 else 0.0
                return

        if self.Position != 0 and slope < avg_slope:
            self._exit_position()

    def _check_stop(self, candle):
        if self._stop_price == 0.0:
            return False

        if (self.Position > 0 and float(candle.LowPrice) <= self._stop_price) or \
                (self.Position < 0 and float(candle.HighPrice) >= self._stop_price):
            self._exit_position()
            return True

        return False

    def _exit_position(self):
        if self.Position > 0:
            self.SellMarket(self.Position)
        elif self.Position < 0:
            self.BuyMarket(-self.Position)

        self._stop_price = 0.0

    def CreateClone(self):
        return atr_slope_breakout_strategy()
