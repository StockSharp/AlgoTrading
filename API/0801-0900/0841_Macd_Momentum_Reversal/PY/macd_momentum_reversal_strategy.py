import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class macd_momentum_reversal_strategy(Strategy):
    """
    MACD momentum reversal strategy.
    A bullish candle with a larger body than the previous candle while the MACD histogram declines goes short; a bearish candle with a
    larger body than the previous candle while the histogram rises goes long. An opposite signal reverses the position.
    """

    def __init__(self):
        super(macd_momentum_reversal_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 12).SetGreaterThanZero().SetDisplay("Fast Length", "Fast EMA length of MACD", "MACD")
        self._slow_length = self.Param("SlowLength", 26).SetGreaterThanZero().SetDisplay("Slow Length", "Slow EMA length of MACD", "MACD")
        self._signal_length = self.Param("SignalLength", 9).SetGreaterThanZero().SetDisplay("Signal Length", "Signal line length of MACD", "MACD")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_body = None
        self._prev_hist = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_momentum_reversal_strategy, self).OnReseted()
        self._prev_body = None
        self._prev_hist = None

    def OnStarted2(self, time):
        super(macd_momentum_reversal_strategy, self).OnStarted2(time)

        self._prev_body = None
        self._prev_hist = None

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._fast_length.Value
        macd.Macd.LongMa.Length = self._slow_length.Value
        macd.SignalMa.Length = self._signal_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, macd)

    def _process_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        body = abs(candle.ClosePrice - candle.OpenPrice)
        prev_body = self._prev_body
        self._prev_body = body

        if not macd_value.IsFormed or macd_value.Macd is None or macd_value.Signal is None:
            return

        hist = macd_value.Macd - macd_value.Signal
        prev_hist = self._prev_hist
        self._prev_hist = hist

        if prev_body is None or prev_hist is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        grows = body > prev_body

        if grows and candle.ClosePrice > candle.OpenPrice and hist < prev_hist and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif grows and candle.ClosePrice < candle.OpenPrice and hist > prev_hist and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return macd_momentum_reversal_strategy()
