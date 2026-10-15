import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class macd_enhanced_mtf_with_stop_loss_strategy(Strategy):
    """
    MACD enhanced strategy with an ATR trailing stop line.
    Every candle gets a MACD score: plus or minus CrossScore on a MACD/signal cross, plus or minus IndicatorScore for the MACD line
    above or below zero, and plus or minus HistogramScore for a rising or falling histogram. The score turning positive goes long and
    turning negative goes short, reversing an opposite position. The stop line trails StopLossFactor ATRs (StopLossPeriod) behind the
    close and only moves in the trade direction; a close through it exits the position.
    """

    def __init__(self):
        super(macd_enhanced_mtf_with_stop_loss_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 12).SetGreaterThanZero().SetDisplay("Fast Length", "MACD fast period", "MACD")
        self._slow_length = self.Param("SlowLength", 26).SetGreaterThanZero().SetDisplay("Slow Length", "MACD slow period", "MACD")
        self._signal_length = self.Param("SignalLength", 9).SetGreaterThanZero().SetDisplay("Signal Length", "MACD signal period", "MACD")
        self._cross_score = self.Param("CrossScore", 10.0).SetNotNegative().SetDisplay("Cross Score", "Score of a MACD/signal cross", "Score")
        self._indicator_score = self.Param("IndicatorScore", 8.0).SetNotNegative().SetDisplay("Indicator Score", "Score of the MACD line side of zero", "Score")
        self._histogram_score = self.Param("HistogramScore", 2.0).SetNotNegative().SetDisplay("Histogram Score", "Score of the histogram direction", "Score")
        self._stop_loss_factor = self.Param("StopLossFactor", 1.2).SetGreaterThanZero().SetDisplay("Stop Loss Factor", "ATR multiplier of the trailing stop line", "Risk")
        self._stop_loss_period = self.Param("StopLossPeriod", 10).SetGreaterThanZero().SetDisplay("Stop Loss Period", "ATR period of the trailing stop line", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_macd = None
        self._prev_signal = None
        self._prev_score = None
        self._stop_line = None

    def OnReseted(self):
        super(macd_enhanced_mtf_with_stop_loss_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(macd_enhanced_mtf_with_stop_loss_strategy, self).OnStarted2(time)

        self._reset_state()

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._fast_length.Value
        macd.Macd.LongMa.Length = self._slow_length.Value
        macd.SignalMa.Length = self._signal_length.Value
        atr = AverageTrueRange()
        atr.Length = self._stop_loss_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, macd)

    def _process_candle(self, candle, macd_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not macd_value.IsFormed or not atr_value.IsFormed:
            return

        macd_line = macd_value.Macd
        signal_line = macd_value.Signal
        if macd_line is None or signal_line is None:
            return

        prev_macd = self._prev_macd
        prev_signal = self._prev_signal
        self._prev_macd = macd_line
        self._prev_signal = signal_line

        if prev_macd is None or prev_signal is None:
            return

        zero = Decimal(0)
        cross_score = Decimal(self._cross_score.Value)
        indicator_score = Decimal(self._indicator_score.Value)
        histogram_score = Decimal(self._histogram_score.Value)

        if prev_macd <= prev_signal and macd_line > signal_line:
            cross_part = cross_score
        elif prev_macd >= prev_signal and macd_line < signal_line:
            cross_part = -cross_score
        else:
            cross_part = zero

        if macd_line > zero:
            indicator_part = indicator_score
        elif macd_line < zero:
            indicator_part = -indicator_score
        else:
            indicator_part = zero

        histogram = macd_line - signal_line
        prev_histogram = prev_macd - prev_signal
        if histogram > prev_histogram:
            histogram_part = histogram_score
        elif histogram < prev_histogram:
            histogram_part = -histogram_score
        else:
            histogram_part = zero

        score = cross_part + indicator_part + histogram_part

        prev_score = self._prev_score
        self._prev_score = score

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice
        offset = atr_value.GetValue[Decimal](None) * Decimal(self._stop_loss_factor.Value)

        if self.Position > 0 and self._stop_line is not None:
            if close < self._stop_line:
                self.SellMarket(self.Position)
                self._stop_line = None
                return
            self._stop_line = max(self._stop_line, close - offset)
        elif self.Position < 0 and self._stop_line is not None:
            if close > self._stop_line:
                self.BuyMarket(-self.Position)
                self._stop_line = None
                return
            self._stop_line = min(self._stop_line, close + offset)

        if prev_score is None:
            return

        if prev_score <= zero and score > zero and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_line = close - offset
        elif prev_score >= zero and score < zero and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_line = close + offset

    def CreateClone(self):
        return macd_enhanced_mtf_with_stop_loss_strategy()
