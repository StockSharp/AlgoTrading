import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class keltner_width_breakout_strategy(Strategy):
    """
    Keltner Channel width breakout.
    Enters when the channel width exceeds its average by a standard deviation multiplier,
    in the direction of the close relative to the channel middle (EMA).
    Exits when the width falls back below its average or the ATR stop is hit.
    """

    def __init__(self):
        super(keltner_width_breakout_strategy, self).__init__()

        self._ema_period = self.Param("EMAPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Period", "Period of EMA for Keltner Channel", "Indicators")
        self._atr_period = self.Param("ATRPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period of ATR for Keltner Channel", "Indicators")
        self._atr_multiplier = self.Param("ATRMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR in Keltner Channel", "Indicators")
        self._avg_period = self.Param("AvgPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Average Period", "Period for width average and deviation", "Strategy")
        self._multiplier = self.Param("Multiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Multiplier", "Standard deviation multiplier for breakout", "Strategy")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._stop_multiplier = self.Param("StopMultiplier", 2) \
            .SetNotNegative() \
            .SetDisplay("Stop Multiplier", "Stop-loss distance in ATR multiples", "Risk Management")

        self._width_average = None
        self._width_std_dev = None
        self._stop_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(keltner_width_breakout_strategy, self).OnReseted()
        self._width_average = None
        self._width_std_dev = None
        self._stop_price = 0.0

    def OnStarted2(self, time):
        super(keltner_width_breakout_strategy, self).OnStarted2(time)

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        self._width_average = SimpleMovingAverage()
        self._width_average.Length = self._avg_period.Value
        self._width_std_dev = StandardDeviation()
        self._width_std_dev.Length = self._avg_period.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        ema_value = float(ema_value)
        atr_value = float(atr_value)

        # Width of the channel: (EMA + k*ATR) - (EMA - k*ATR).
        width = 2.0 * float(self._atr_multiplier.Value) * atr_value
        avg_width = float(process_float(self._width_average, width, candle.ServerTime, True))
        std_width = float(process_float(self._width_std_dev, width, candle.ServerTime, True))

        if not self._width_average.IsFormed or not self._width_std_dev.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._check_stop(candle):
            return

        close = float(candle.ClosePrice)
        stop_mult = self._stop_multiplier.Value
        stop_distance = stop_mult * atr_value

        if width > avg_width + float(self._multiplier.Value) * std_width:
            if close > ema_value and self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
                self._stop_price = close - stop_distance if stop_mult > 0 else 0.0
                return
            if close < ema_value and self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))
                self._stop_price = close + stop_distance if stop_mult > 0 else 0.0
                return

        if self.Position != 0 and width < avg_width:
            self._exit_position()

    def _check_stop(self, candle):
        if self._stop_price == 0.0:
            return False

        if self.Position > 0 and float(candle.LowPrice) <= self._stop_price:
            self._exit_position()
            return True

        if self.Position < 0 and float(candle.HighPrice) >= self._stop_price:
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
        return keltner_width_breakout_strategy()
