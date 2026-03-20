import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    MovingAverageConvergenceDivergenceHistogram,
    MovingAverageConvergenceDivergenceHistogramValue,
)


class os_ma_ster_v0_strategy(Strategy):
    """OsMaSter v0: four-bar MACD histogram pattern entries with pip-based SL/TP."""

    def __init__(self):
        super(os_ma_ster_v0_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast EMA", "Fast EMA period for MACD histogram", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow EMA", "Slow EMA period for MACD histogram", "Indicators")
        self._signal_period = self.Param("SignalPeriod", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Smoothing", "Signal moving average period", "Indicators")
        self._stop_loss_pips = self.Param("StopLossPips", 500) \
            .SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 1000) \
            .SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk")
        self._trade_volume = self.Param("TradeVolume", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Trade Volume", "Order volume in lots", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles for calculations", "General")

        self._hist_current = None
        self._hist_prev1 = None
        self._hist_prev2 = None
        self._hist_prev3 = None

    @property
    def FastPeriod(self):
        return int(self._fast_period.Value)
    @property
    def SlowPeriod(self):
        return int(self._slow_period.Value)
    @property
    def SignalPeriod(self):
        return int(self._signal_period.Value)
    @property
    def StopLossPips(self):
        return int(self._stop_loss_pips.Value)
    @property
    def TakeProfitPips(self):
        return int(self._take_profit_pips.Value)
    @property
    def TradeVolume(self):
        return float(self._trade_volume.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def _calc_pip_size(self):
        sec = self.Security
        if sec is None or sec.PriceStep is None:
            return 1.0
        step = float(sec.PriceStep)
        if step <= 0:
            return 1.0
        decimals = 0
        if sec.Decimals is not None:
            decimals = int(sec.Decimals)
        else:
            v = abs(step)
            while v != int(v) and decimals < 10:
                v *= 10
                decimals += 1
        return step * 10.0 if (decimals == 3 or decimals == 5) else step

    def OnStarted(self, time):
        super(os_ma_ster_v0_strategy, self).OnStarted(time)

        self._hist_current = None
        self._hist_prev1 = None
        self._hist_prev2 = None
        self._hist_prev3 = None

        macd = MovingAverageConvergenceDivergenceHistogram()
        macd.Macd.ShortMa.Length = self.FastPeriod
        macd.Macd.LongMa.Length = self.SlowPeriod
        macd.SignalMa.Length = self.SignalPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(macd, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

        pip_size = self._calc_pip_size()
        tp = Unit(self.TakeProfitPips * pip_size, UnitTypes.Absolute) if self.TakeProfitPips > 0 else None
        sl = Unit(self.StopLossPips * pip_size, UnitTypes.Absolute) if self.StopLossPips > 0 else None
        if tp is not None and sl is not None:
            self.StartProtection(takeProfit=tp, stopLoss=sl)
        elif tp is not None:
            self.StartProtection(takeProfit=tp)
        elif sl is not None:
            self.StartProtection(stopLoss=sl)

    def process_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        if not macd_value.IsFormed:
            return

        histogram = macd_value.Macd
        if histogram is None:
            return

        hist = float(histogram)

        self._hist_prev3 = self._hist_prev2
        self._hist_prev2 = self._hist_prev1
        self._hist_prev1 = self._hist_current
        self._hist_current = hist

        if (self._hist_current is None or self._hist_prev1 is None
                or self._hist_prev2 is None or self._hist_prev3 is None):
            return

        h0 = self._hist_current
        h1 = self._hist_prev1
        h2 = self._hist_prev2
        h3 = self._hist_prev3

        bullish = h3 > h2 and h2 < h1 and h1 < h0
        bearish = h3 < h2 and h2 > h1 and h1 > h0

        if self.Position != 0:
            return

        if bullish:
            self.BuyMarket()
        elif bearish:
            self.SellMarket()

    def OnReseted(self):
        super(os_ma_ster_v0_strategy, self).OnReseted()
        self._hist_current = None
        self._hist_prev1 = None
        self._hist_prev2 = None
        self._hist_prev3 = None

    def CreateClone(self):
        return os_ma_ster_v0_strategy()
