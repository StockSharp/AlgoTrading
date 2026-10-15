import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class macd_long_strategy(Strategy):
    """
    MACD Long strategy.
    An RSI reading below Oversold arms a long and a reading above Overbought arms a short. The next MACD crossover
    confirms or cancels the setup: a bullish MACD/signal cross opens an armed long, a bearish cross opens an armed short.
    Every opposite crossover closes the current position, or reverses it when the opposite side is armed.
    """

    def __init__(self):
        super(macd_long_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI period", "RSI")
        self._oversold = self.Param("Oversold", 30.0) \
            .SetDisplay("Oversold", "RSI oversold level", "RSI")
        self._overbought = self.Param("Overbought", 70.0) \
            .SetDisplay("Overbought", "RSI overbought level", "RSI")
        self._macd_fast = self.Param("MacdFast", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Fast", "MACD fast EMA period", "MACD")
        self._macd_slow = self.Param("MacdSlow", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Slow", "MACD slow EMA period", "MACD")
        self._macd_signal = self.Param("MacdSignal", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Signal", "MACD signal line period", "MACD")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation", "General")

        self._reset_state()

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def _reset_state(self):
        self._long_armed = False
        self._short_armed = False
        self._prev_macd = None
        self._prev_signal = None

    def OnReseted(self):
        super(macd_long_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(macd_long_strategy, self).OnStarted2(time)

        self._reset_state()

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._macd_fast.Value
        macd.Macd.LongMa.Length = self._macd_slow.Value
        macd.SignalMa.Length = self._macd_signal.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(rsi, macd, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)
                self.DrawIndicator(oscillators, macd)

    def _process_candle(self, candle, rsi_value, macd_value):
        if candle.State != CandleStates.Finished:
            return

        if not rsi_value.IsFormed or not macd_value.IsFormed:
            return

        if macd_value.Macd is None or macd_value.Signal is None:
            return

        macd = float(macd_value.Macd)
        signal = float(macd_value.Signal)
        rsi = float(rsi_value.GetValue[Decimal](None))

        if rsi < float(self._oversold.Value):
            self._long_armed = True

        if rsi > float(self._overbought.Value):
            self._short_armed = True

        prev_macd = self._prev_macd
        prev_signal = self._prev_signal
        self._prev_macd = macd
        self._prev_signal = signal

        if prev_macd is None or prev_signal is None:
            return

        cross_up = prev_macd <= prev_signal and macd > signal
        cross_down = prev_macd >= prev_signal and macd < signal

        long_signal = cross_up and self._long_armed
        short_signal = cross_down and self._short_armed

        # The first crossover after the RSI extreme decides the setup either way.
        if cross_up:
            self._long_armed = False

        if cross_down:
            self._short_armed = False

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and cross_down:
            self.SellMarket(self.Position)
        elif self.Position < 0 and cross_up:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return macd_long_strategy()
