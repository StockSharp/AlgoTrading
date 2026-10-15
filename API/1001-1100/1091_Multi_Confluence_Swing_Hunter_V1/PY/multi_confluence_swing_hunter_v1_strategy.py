import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class multi_confluence_swing_hunter_v1_strategy(Strategy):
    """
    Multi-Confluence Swing Hunter V1 strategy.
    Every finished candle gets a bullish and a bearish score built from RSI levels and turns, MACD position, histogram
    direction and signal crosses, and candle structure (wick size, candle colour, close change and swing extremes).
    A long opens when the bullish score reaches MinEntryScore and closes when the bearish score reaches MinExitScore.
    Long only, no stops.
    """

    def __init__(self):
        super(multi_confluence_swing_hunter_v1_strategy, self).__init__()
        self._macd_fast = self.Param("MacdFast", 3).SetGreaterThanZero().SetDisplay("MACD Fast", "MACD fast EMA length", "MACD")
        self._macd_slow = self.Param("MacdSlow", 10).SetGreaterThanZero().SetDisplay("MACD Slow", "MACD slow EMA length", "MACD")
        self._macd_signal = self.Param("MacdSignal", 3).SetGreaterThanZero().SetDisplay("MACD Signal", "MACD signal line length", "MACD")
        self._rsi_length = self.Param("RsiLength", 21).SetGreaterThanZero().SetDisplay("RSI Length", "RSI length", "RSI")
        self._min_entry_score = self.Param("MinEntryScore", 13).SetGreaterThanZero().SetDisplay("Min Entry Score", "Bullish score required to enter", "Scoring")
        self._min_exit_score = self.Param("MinExitScore", 13).SetGreaterThanZero().SetDisplay("Min Exit Score", "Bearish score required to exit", "Scoring")
        self._min_lower_wick_percent = self.Param("MinLowerWickPercent", 50.0).SetDisplay("Min Wick %", "Minimum wick size in percent of the candle range", "Price Action")
        self._rsi_oversold = self.Param("RsiOversold", 30.0).SetDisplay("RSI Oversold", "RSI oversold level", "RSI")
        self._rsi_extreme_oversold = self.Param("RsiExtremeOversold", 25.0).SetDisplay("RSI Extreme Oversold", "RSI extreme oversold level", "RSI")
        self._rsi_overbought = self.Param("RsiOverbought", 70.0).SetDisplay("RSI Overbought", "RSI overbought level", "RSI")
        self._rsi_extreme_overbought = self.Param("RsiExtremeOverbought", 75.0).SetDisplay("RSI Extreme Overbought", "RSI extreme overbought level", "RSI")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_rsi = None
        self._prev_macd = None
        self._prev_signal = None
        self._prev_high = None
        self._prev_low = None
        self._prev_close = None

    def OnReseted(self):
        super(multi_confluence_swing_hunter_v1_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(multi_confluence_swing_hunter_v1_strategy, self).OnStarted2(time)

        self._reset_state()

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._macd_fast.Value
        macd.Macd.LongMa.Length = self._macd_slow.Value
        macd.SignalMa.Length = self._macd_signal.Value

        subscription = self.SubscribeCandles(self.candle_type)
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

        if not rsi_value.IsFormed or not macd_value.IsFormed or macd_value.Macd is None or macd_value.Signal is None:
            return

        rsi = float(rsi_value.GetValue[Decimal](None))
        macd = float(macd_value.Macd)
        signal = float(macd_value.Signal)
        open_price = float(candle.OpenPrice)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        p_rsi = self._prev_rsi
        p_macd = self._prev_macd
        p_signal = self._prev_signal
        p_high = self._prev_high
        p_low = self._prev_low
        p_close = self._prev_close

        self._prev_rsi = rsi
        self._prev_macd = macd
        self._prev_signal = signal
        self._prev_high = high
        self._prev_low = low
        self._prev_close = close

        if p_rsi is None or p_macd is None or p_signal is None or p_high is None or p_low is None or p_close is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        oversold = float(self._rsi_oversold.Value)
        extreme_oversold = float(self._rsi_extreme_oversold.Value)
        overbought = float(self._rsi_overbought.Value)
        extreme_overbought = float(self._rsi_extreme_overbought.Value)
        min_wick = float(self._min_lower_wick_percent.Value)

        candle_range = high - low
        lower_wick_percent = (min(open_price, close) - low) / candle_range * 100.0 if candle_range > 0 else 0.0
        upper_wick_percent = (high - max(open_price, close)) / candle_range * 100.0 if candle_range > 0 else 0.0
        hist = macd - signal
        prev_hist = p_macd - p_signal

        entry_score = 0
        if rsi < extreme_oversold:
            entry_score += 3
        elif rsi < oversold:
            entry_score += 2
        if p_rsi < oversold and rsi > p_rsi:
            entry_score += 3
        if macd < 0:
            entry_score += 1
        if hist > prev_hist:
            entry_score += 2
        if p_macd <= p_signal and macd > signal:
            entry_score += 3
        if lower_wick_percent >= min_wick:
            entry_score += 3
        if close > open_price:
            entry_score += 2
        if close > p_close:
            entry_score += 1
        if low < p_low:
            entry_score += 2

        exit_score = 0
        if rsi > extreme_overbought:
            exit_score += 3
        elif rsi > overbought:
            exit_score += 2
        if p_rsi > overbought and rsi < p_rsi:
            exit_score += 3
        if macd > 0:
            exit_score += 1
        if hist < prev_hist:
            exit_score += 2
        if p_macd >= p_signal and macd < signal:
            exit_score += 3
        if upper_wick_percent >= min_wick:
            exit_score += 3
        if close < open_price:
            exit_score += 2
        if close < p_close:
            exit_score += 1
        if high > p_high:
            exit_score += 2

        if self.Position <= 0 and entry_score >= self._min_entry_score.Value:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and exit_score >= self._min_exit_score.Value:
            self.SellMarket(self.Position)

    def CreateClone(self):
        return multi_confluence_swing_hunter_v1_strategy()
