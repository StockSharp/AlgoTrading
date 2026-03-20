import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, MovingAverageConvergenceDivergenceSignal, IndicatorHelper
from StockSharp.Algo.Strategies import Strategy

class imacd_sniper_strategy(Strategy):
    """
    IMACD Sniper: MACD crossover with EMA trend filter.
    Enters when MACD crosses signal with sufficient delta and price above/below EMA.
    """

    def __init__(self):
        super(imacd_sniper_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 12) \
            .SetDisplay("MACD Fast Length", "MACD fast period", "MACD")
        self._slow_length = self.Param("SlowLength", 26) \
            .SetDisplay("MACD Slow Length", "MACD slow period", "MACD")
        self._signal_length = self.Param("SignalLength", 9) \
            .SetDisplay("MACD Signal Length", "MACD signal smoothing", "MACD")
        self._macd_delta_min = self.Param("MacdDeltaMin", 0.03) \
            .SetDisplay("Min MACD Delta", "Minimum MACD difference for entry", "Filters")
        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("EMA Length", "EMA period", "Trend")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._prev_macd = 0.0
        self._prev_signal = 0.0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(imacd_sniper_strategy, self).OnReseted()
        self._prev_macd = 0.0
        self._prev_signal = 0.0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(imacd_sniper_strategy, self).OnStarted(time)

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._fast_length.Value
        macd.Macd.LongMa.Length = self._slow_length.Value
        macd.SignalMa.Length = self._signal_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, ema, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, macd_value, ema_value):
        if candle.State != CandleStates.Finished:
            return

        macd_val = macd_value.Macd
        signal_val = macd_value.Signal
        if macd_val is None or signal_val is None:
            return

        if ema_value.IsEmpty:
            return

        macd = float(macd_val)
        signal = float(signal_val)
        ema = float(IndicatorHelper.ToDecimal(ema_value))
        close = float(candle.ClosePrice)

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_macd = macd
            self._prev_signal = signal
            return

        macd_delta = abs(macd - signal)
        delta_min = self._macd_delta_min.Value

        long_cond = (self._prev_macd != 0 and self._prev_macd <= self._prev_signal
                     and macd > signal and close > ema and macd_delta > delta_min)
        short_cond = (self._prev_macd != 0 and self._prev_macd >= self._prev_signal
                      and macd < signal and close < ema and macd_delta > delta_min)

        if long_cond and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = close
        elif short_cond and self.Position >= 0:
            self.SellMarket()
            self._entry_price = close
        elif self.Position > 0 and self._prev_macd >= self._prev_signal and macd < signal:
            self.SellMarket()
        elif self.Position < 0 and self._prev_macd <= self._prev_signal and macd > signal:
            self.BuyMarket()

        self._prev_macd = macd
        self._prev_signal = signal

    def CreateClone(self):
        return imacd_sniper_strategy()
