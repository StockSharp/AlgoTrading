import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import KaufmanAdaptiveMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

ATR_PERIOD = 14


class adaptive_ema_breakout_strategy(Strategy):
    """
    Adaptive EMA (Kaufman) breakout with trend confirmation.
    Buys when price closes above a rising adaptive EMA and sells when it closes below a falling one.
    Positions are reversed on the opposite signal and protected by an ATR-multiple stop.
    """

    def __init__(self):
        super(adaptive_ema_breakout_strategy, self).__init__()

        self._fast = self.Param("Fast", 2) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast Period", "Fast smoothing period of the adaptive EMA", "Indicators")
        self._slow = self.Param("Slow", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow Period", "Slow smoothing period of the adaptive EMA", "Indicators")
        self._lookback = self.Param("Lookback", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback", "Efficiency ratio lookback of the adaptive EMA", "Indicators")
        self._stop_multiplier = self.Param("StopMultiplier", 2.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Multiplier", "Stop-loss distance in ATR multiples", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_adaptive_ema = None
        self._stop_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(adaptive_ema_breakout_strategy, self).OnReseted()
        self._prev_adaptive_ema = None
        self._stop_price = 0.0

    def OnStarted2(self, time):
        super(adaptive_ema_breakout_strategy, self).OnStarted2(time)

        adaptive_ema = KaufmanAdaptiveMovingAverage()
        adaptive_ema.Length = self._lookback.Value
        adaptive_ema.FastSCPeriod = self._fast.Value
        adaptive_ema.SlowSCPeriod = self._slow.Value
        atr = AverageTrueRange()
        atr.Length = ATR_PERIOD

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(adaptive_ema, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, adaptive_ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, adaptive_ema_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        adaptive_ema_value = float(adaptive_ema_value)
        atr_value = float(atr_value)

        prev = self._prev_adaptive_ema
        self._prev_adaptive_ema = adaptive_ema_value

        if prev is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._check_stop(candle):
            return

        close = float(candle.ClosePrice)
        stop_distance = float(self._stop_multiplier.Value) * atr_value

        if close > adaptive_ema_value and adaptive_ema_value > prev and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - stop_distance if stop_distance > 0 else 0.0
        elif close < adaptive_ema_value and adaptive_ema_value < prev and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + stop_distance if stop_distance > 0 else 0.0

    def _check_stop(self, candle):
        if self._stop_price == 0.0:
            return False

        if (self.Position > 0 and float(candle.LowPrice) <= self._stop_price) or \
                (self.Position < 0 and float(candle.HighPrice) >= self._stop_price):
            if self.Position > 0:
                self.SellMarket(self.Position)
            else:
                self.BuyMarket(-self.Position)

            self._stop_price = 0.0
            return True

        return False

    def CreateClone(self):
        return adaptive_ema_breakout_strategy()
