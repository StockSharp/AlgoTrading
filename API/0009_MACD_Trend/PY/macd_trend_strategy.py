import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy

class macd_trend_strategy(Strategy):
    """
    MACD Trend: enters long on MACD cross above signal, short on cross below.
    """

    def __init__(self):
        super(macd_trend_strategy, self).__init__()
        self._fast_ema = self.Param("FastEmaPeriod", 200).SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_ema = self.Param("SlowEmaPeriod", 500).SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._signal_period = self.Param("SignalPeriod", 200).SetDisplay("Signal Period", "Signal line period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Timeframe", "General")

        self._prev_above = False
        self._is_init = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_trend_strategy, self).OnReseted()
        self._prev_above = False
        self._is_init = False

    def OnStarted(self, time):
        super(macd_trend_strategy, self).OnStarted(time)
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._fast_ema.Value
        macd.Macd.LongMa.Length = self._slow_ema.Value
        macd.SignalMa.Length = self._signal_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        typed_val = macd_value
        if typed_val.Macd is None or typed_val.Signal is None:
            return
        macd_line = float(typed_val.Macd)
        signal_line = float(typed_val.Signal)
        is_above = macd_line > signal_line
        if not self._is_init:
            self._prev_above = is_above
            self._is_init = True
            return
        crossed_above = is_above and not self._prev_above
        crossed_below = not is_above and self._prev_above
        if crossed_above and self.Position <= 0:
            self.BuyMarket()
        elif crossed_below and self.Position >= 0:
            self.SellMarket()
        self._prev_above = is_above

    def CreateClone(self):
        return macd_trend_strategy()
