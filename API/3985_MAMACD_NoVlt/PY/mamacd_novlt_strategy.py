import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    ExponentialMovingAverage,
    WeightedMovingAverage,
    MovingAverageConvergenceDivergence,
)

class mamacd_novlt_strategy(Strategy):
    def __init__(self):
        super(mamacd_novlt_strategy, self).__init__()

        self._first_low_wma_period = self.Param("FirstLowWmaPeriod", 85) \
            .SetDisplay("First LWMA Period", "First LWMA period on lows", "Indicators")
        self._second_low_wma_period = self.Param("SecondLowWmaPeriod", 75) \
            .SetDisplay("Second LWMA Period", "Second LWMA period on lows", "Indicators")
        self._fast_ema_period = self.Param("FastEmaPeriod", 5) \
            .SetDisplay("Fast EMA Period", "Fast EMA period on closes", "Indicators")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 26) \
            .SetDisplay("MACD Slow Period", "Slow EMA period for MACD", "Indicators")
        self._fast_signal_ema_period = self.Param("FastSignalEmaPeriod", 15) \
            .SetDisplay("MACD Fast Period", "Fast EMA period for MACD", "Indicators")
        self._stop_loss_points = self.Param("StopLossPoints", 500) \
            .SetDisplay("Stop Loss", "Stop-loss distance", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 500) \
            .SetDisplay("Take Profit", "Take-profit distance", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")

        self._is_long_setup_prepared = False
        self._is_short_setup_prepared = False
        self._previous_macd = None

    @property
    def FirstLowWmaPeriod(self):
        return self._first_low_wma_period.Value

    @property
    def SecondLowWmaPeriod(self):
        return self._second_low_wma_period.Value

    @property
    def FastEmaPeriod(self):
        return self._fast_ema_period.Value

    @property
    def SlowEmaPeriod(self):
        return self._slow_ema_period.Value

    @property
    def FastSignalEmaPeriod(self):
        return self._fast_signal_ema_period.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(mamacd_novlt_strategy, self).OnStarted(time)

        fast_close_ema = ExponentialMovingAverage()
        fast_close_ema.Length = self.FastEmaPeriod
        first_low_wma = WeightedMovingAverage()
        first_low_wma.Length = self.FirstLowWmaPeriod
        second_low_wma = WeightedMovingAverage()
        second_low_wma.Length = self.SecondLowWmaPeriod

        macd = MovingAverageConvergenceDivergence()
        macd.ShortMa.Length = self.FastSignalEmaPeriod
        macd.LongMa.Length = self.SlowEmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_close_ema, first_low_wma, second_low_wma, macd, self.ProcessCandle).Start()

        tp = Unit(self.TakeProfitPoints, UnitTypes.Absolute) if self.TakeProfitPoints > 0 else None
        sl = Unit(self.StopLossPoints, UnitTypes.Absolute) if self.StopLossPoints > 0 else None
        self.StartProtection(tp, sl)

    def ProcessCandle(self, candle, ema, first_lwma, second_lwma, macd_line):
        if candle.State != CandleStates.Finished:
            return

        ema = float(ema)
        first_lwma = float(first_lwma)
        second_lwma = float(second_lwma)
        macd_line = float(macd_line)

        if ema < first_lwma and ema < second_lwma:
            self._is_long_setup_prepared = True

        if ema > first_lwma and ema > second_lwma:
            self._is_short_setup_prepared = True

        has_prev = self._previous_macd is not None
        macd_prev = self._previous_macd if has_prev else macd_line

        macd_bullish = macd_line > 0 or (has_prev and macd_line > macd_prev)
        macd_bearish = macd_line < 0 or (has_prev and macd_line < macd_prev)

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._previous_macd = macd_line
            return

        if ema > first_lwma and ema > second_lwma and self._is_long_setup_prepared and macd_bullish and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(abs(self.Position))
            self.BuyMarket(self.Volume)
            self._is_long_setup_prepared = False

        if ema < first_lwma and ema < second_lwma and self._is_short_setup_prepared and macd_bearish and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(self.Position)
            self.SellMarket(self.Volume)
            self._is_short_setup_prepared = False

        self._previous_macd = macd_line

    def OnReseted(self):
        super(mamacd_novlt_strategy, self).OnReseted()
        self._is_long_setup_prepared = False
        self._is_short_setup_prepared = False
        self._previous_macd = None

    def CreateClone(self):
        return mamacd_novlt_strategy()
