import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, MovingAverageConvergenceDivergenceSignalValue, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class macd_with_sentiment_filter_strategy(Strategy):
    """
    MACD with Sentiment Filter strategy.
    Entry condition:
    Long: MACD > Signal && Sentiment_Score > Threshold
    Short: MACD < Signal && Sentiment_Score < -Threshold
    """

    def __init__(self):
        super(macd_with_sentiment_filter_strategy, self).__init__()

        self._macd_fast = self.Param("MacdFast", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Fast", "Fast moving average period for MACD", "MACD Settings")

        self._macd_slow = self.Param("MacdSlow", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Slow", "Slow moving average period for MACD", "MACD Settings")

        self._macd_signal = self.Param("MacdSignal", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Signal", "Signal line period for MACD", "MACD Settings")

        self._threshold = self.Param("Threshold", 0.1) \
            .SetGreaterThanZero() \
            .SetDisplay("Sentiment Threshold", "Threshold for sentiment filter", "Sentiment Settings")

        self._cooldown_bars = self.Param("CooldownBars", 24) \
            .SetNotNegative() \
            .SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "General")

        self._stop_loss = self.Param("StopLoss", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss (%)", "Stop Loss percentage from entry price", "Risk Management")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._sentiment_score = 0.0
        self._prev_macd = 0.0
        self._prev_signal = 0.0
        self._has_previous_macd = False
        self._cooldown_remaining = 0
        self._macd_ind = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(macd_with_sentiment_filter_strategy, self).OnReseted()
        self._prev_macd = 0.0
        self._prev_signal = 0.0
        self._sentiment_score = 0.0
        self._has_previous_macd = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(macd_with_sentiment_filter_strategy, self).OnStarted2(time)

        self._macd_ind = MovingAverageConvergenceDivergenceSignal()
        self._macd_ind.Macd.ShortMa.Length = int(self._macd_fast.Value)
        self._macd_ind.Macd.LongMa.Length = int(self._macd_slow.Value)
        self._macd_ind.SignalMa.Length = int(self._macd_signal.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._macd_ind)
            self.DrawOwnTrades(area)

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(float(self._stop_loss.Value), UnitTypes.Percent)
        )

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self.UpdateSentimentScore(candle)

        civ = CandleIndicatorValue(self._macd_ind, candle)
        civ.IsFinal = True
        macd_result = self._macd_ind.Process(civ)

        if not self._macd_ind.IsFormed:
            return

        macd_typed = macd_result
        macd_val = macd_typed.Macd
        signal_val = macd_typed.Signal

        if macd_val is None or signal_val is None:
            return

        macd = float(macd_val)
        signal = float(signal_val)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        if not self._has_previous_macd:
            self._prev_macd = macd
            self._prev_signal = signal
            self._has_previous_macd = True
            return

        prev_macd_over_signal = self._prev_macd > self._prev_signal
        curr_macd_over_signal = macd > signal

        threshold = float(self._threshold.Value)
        cooldown = int(self._cooldown_bars.Value)

        if self._cooldown_remaining == 0 and prev_macd_over_signal != curr_macd_over_signal and self.Position == 0:
            if curr_macd_over_signal and self._sentiment_score > threshold:
                self.BuyMarket()
                self._cooldown_remaining = cooldown
            elif not curr_macd_over_signal and self._sentiment_score < -threshold:
                self.SellMarket()
                self._cooldown_remaining = cooldown

        self._prev_macd = macd
        self._prev_signal = signal

    def UpdateSentimentScore(self, candle):
        body_size = float(abs(candle.ClosePrice - candle.OpenPrice))
        total_size = float(candle.HighPrice - candle.LowPrice)

        if total_size == 0:
            return

        body_ratio = body_size / total_size
        self._sentiment_score *= 0.85

        if candle.ClosePrice > candle.OpenPrice and body_ratio > 0.7:
            self._sentiment_score = min(self._sentiment_score + 0.25, 1.0)
        elif candle.ClosePrice < candle.OpenPrice and body_ratio > 0.7:
            self._sentiment_score = max(self._sentiment_score - 0.25, -1.0)

        self.LogInfo("Updated sentiment score: {0}".format(self._sentiment_score))

    def CreateClone(self):
        return macd_with_sentiment_filter_strategy()
