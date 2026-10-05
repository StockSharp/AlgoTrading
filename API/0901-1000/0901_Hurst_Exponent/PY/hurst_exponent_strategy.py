import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import HurstExponent, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class hurst_exponent_strategy(Strategy):
    """
    Hurst exponent strategy.
    The rescaled-range Hurst exponent of the close-to-close returns over HurstPeriod candles is smoothed with an EMA of SmoothLength.
    A smoothed value above Threshold marks a persistent (trending) regime and holds a long, a value below it holds a short, so each
    threshold cross exits the current side and reverses. A percent stop loss limits the loss.
    """

    def __init__(self):
        super(hurst_exponent_strategy, self).__init__()
        self._hurst_period = self.Param("HurstPeriod", 100).SetGreaterThanZero().SetDisplay("Hurst Period", "Number of returns used by the Hurst exponent", "Indicators")
        self._smooth_length = self.Param("SmoothLength", 10).SetGreaterThanZero().SetDisplay("Smooth Length", "EMA length that smooths the Hurst exponent", "Indicators")
        self._threshold = self.Param("Threshold", 0.5).SetDisplay("Threshold", "Regime threshold for the smoothed Hurst exponent", "Signals")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._hurst = None
        self._smooth = None
        self._prev_close = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hurst_exponent_strategy, self).OnReseted()
        self._prev_close = None

    def OnStarted2(self, time):
        super(hurst_exponent_strategy, self).OnStarted2(time)

        self._prev_close = None
        self._hurst = HurstExponent()
        self._hurst.Length = self._hurst_period.Value
        self._smooth = ExponentialMovingAverage()
        self._smooth.Length = self._smooth_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, self._smooth)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice
        prev = self._prev_close
        self._prev_close = close

        if prev is None or prev == 0:
            return

        # The exponent is measured on returns: on raw price levels the rescaled range is always near 1.
        ret = (close - prev) / prev
        hurst_value = process_value(self._hurst, ret, candle.OpenTime, True)
        if not self._hurst.IsFormed or hurst_value.IsEmpty:
            return

        smooth_value = process_value(self._smooth, to_decimal(hurst_value), candle.OpenTime, True)
        if not self._smooth.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        smoothed = to_decimal(smooth_value)
        threshold = Decimal(self._threshold.Value)

        if smoothed > threshold and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif smoothed < threshold and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return hurst_exponent_strategy()
