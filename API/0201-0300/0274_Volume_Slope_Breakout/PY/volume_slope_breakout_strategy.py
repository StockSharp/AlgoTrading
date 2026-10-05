import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class volume_slope_breakout_strategy(Strategy):
    """
    Volume slope breakout.
    Enters when the slope of the smoothed volume exceeds its average by a standard deviation multiplier,
    in the direction of the breakout candle. Exits when the slope returns to its average.
    """

    def __init__(self):
        super(volume_slope_breakout_strategy, self).__init__()

        self._volume_sma_period = self.Param("VolumeSMAPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume SMA Period", "Period of the volume moving average", "Indicators")
        self._slope_period = self.Param("SlopePeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Slope Period", "Period for slope statistics", "Strategy")
        self._breakout_multiplier = self.Param("BreakoutMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Breakout Multiplier", "Standard deviation multiplier for breakout", "Strategy")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._volume_sma = None
        self._slope_average = None
        self._slope_std_dev = None
        self._prev_volume = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(volume_slope_breakout_strategy, self).OnReseted()
        self._volume_sma = None
        self._slope_average = None
        self._slope_std_dev = None
        self._prev_volume = None

    def OnStarted2(self, time):
        super(volume_slope_breakout_strategy, self).OnStarted2(time)

        self._volume_sma = SimpleMovingAverage()
        self._volume_sma.Length = self._volume_sma_period.Value
        self._slope_average = SimpleMovingAverage()
        self._slope_average.Length = self._slope_period.Value
        self._slope_std_dev = StandardDeviation()
        self._slope_std_dev.Length = self._slope_period.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        stop = float(self._stop_loss_percent.Value)
        self.StartProtection(None, Unit(stop, UnitTypes.Percent) if stop > 0 else None)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        volume = float(process_float(self._volume_sma, candle.TotalVolume, candle.ServerTime, True))

        if not self._volume_sma.IsFormed:
            return

        if self._prev_volume is None:
            self._prev_volume = volume
            return

        slope = volume - self._prev_volume
        self._prev_volume = volume

        avg_slope = float(process_float(self._slope_average, slope, candle.ServerTime, True))
        std_slope = float(process_float(self._slope_std_dev, slope, candle.ServerTime, True))

        if not self._slope_average.IsFormed or not self._slope_std_dev.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)

        # Volume has no direction, so the breakout candle decides the side.
        if slope > avg_slope + float(self._breakout_multiplier.Value) * std_slope:
            if close > open_price and self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
                return
            if close < open_price and self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))
                return

        if slope < avg_slope:
            if self.Position > 0:
                self.SellMarket(self.Position)
            elif self.Position < 0:
                self.BuyMarket(-self.Position)

    def CreateClone(self):
        return volume_slope_breakout_strategy()
