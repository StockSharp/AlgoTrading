import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (MovingAverageConvergenceDivergenceSignal, RelativeStrengthIndex, ExponentialMovingAverage,
                                        AverageTrueRange, BollingerBands)
from StockSharp.Algo.Strategies import Strategy


class macd_rsi_ema_bb_atr_day_trading_strategy(Strategy):
    """
    MACD, RSI, EMA, Bollinger Bands and ATR day trading strategy.
    Goes long when MACD crosses above its signal line while the fast EMA is above the slow EMA, and short on the opposite cross with
    the fast EMA below the slow EMA. RSI must be between RsiOversold and RsiOverbought, and Bollinger Bands must not be in a squeeze
    (band width below its average over BbLength candles). The stop starts AtrMultiplier ATRs from entry and trails TrailAtrMultiplier
    ATRs behind the close, the target is RiskReward times the initial stop distance. An opposite signal reverses the position.
    """

    def __init__(self):
        super(macd_rsi_ema_bb_atr_day_trading_strategy, self).__init__()
        self._macd_fast = self.Param("MacdFast", 12).SetGreaterThanZero().SetDisplay("MACD Fast", "MACD fast period", "MACD")
        self._macd_slow = self.Param("MacdSlow", 26).SetGreaterThanZero().SetDisplay("MACD Slow", "MACD slow period", "MACD")
        self._macd_signal = self.Param("MacdSignal", 9).SetGreaterThanZero().SetDisplay("MACD Signal", "MACD signal period", "MACD")
        self._rsi_length = self.Param("RsiLength", 14).SetGreaterThanZero().SetDisplay("RSI Length", "RSI period", "RSI")
        self._rsi_overbought = self.Param("RsiOverbought", 70.0).SetDisplay("RSI Overbought", "RSI overbought level", "RSI")
        self._rsi_oversold = self.Param("RsiOversold", 30.0).SetDisplay("RSI Oversold", "RSI oversold level", "RSI")
        self._ema_fast = self.Param("EmaFast", 9).SetGreaterThanZero().SetDisplay("EMA Fast", "Fast EMA period", "Trend")
        self._ema_slow = self.Param("EmaSlow", 21).SetGreaterThanZero().SetDisplay("EMA Slow", "Slow EMA period", "Trend")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR period", "Risk")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetGreaterThanZero().SetDisplay("ATR Multiplier", "ATR multiplier of the initial stop", "Risk")
        self._trail_atr_multiplier = self.Param("TrailAtrMultiplier", 1.5).SetNotNegative().SetDisplay("Trail ATR Multiplier", "ATR multiplier of the trailing stop", "Risk")
        self._bb_length = self.Param("BbLength", 20).SetGreaterThanZero().SetDisplay("BB Length", "Bollinger Bands period", "Bollinger")
        self._bb_multiplier = self.Param("BbMultiplier", 2.0).SetGreaterThanZero().SetDisplay("BB Multiplier", "Bollinger Bands width multiplier", "Bollinger")
        self._risk_reward = self.Param("RiskReward", 2.0).SetGreaterThanZero().SetDisplay("Risk Reward", "Take profit as a multiple of the stop distance", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._widths = []
        self._prev_macd = None
        self._prev_signal = None
        self._stop_price = None
        self._take_price = None

    def OnReseted(self):
        super(macd_rsi_ema_bb_atr_day_trading_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(macd_rsi_ema_bb_atr_day_trading_strategy, self).OnStarted2(time)

        self._reset_state()

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._macd_fast.Value
        macd.Macd.LongMa.Length = self._macd_slow.Value
        macd.SignalMa.Length = self._macd_signal.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        ema_fast = ExponentialMovingAverage()
        ema_fast.Length = self._ema_fast.Value
        ema_slow = ExponentialMovingAverage()
        ema_slow.Length = self._ema_slow.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        bollinger = BollingerBands()
        bollinger.Length = self._bb_length.Value
        bollinger.Width = Decimal(self._bb_multiplier.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, rsi, ema_fast, ema_slow, atr, bollinger, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema_fast)
            self.DrawIndicator(area, ema_slow)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, macd)

    def _process_candle(self, candle, macd_value, rsi_value, ema_fast_value, ema_slow_value, atr_value, bollinger_value):
        if candle.State != CandleStates.Finished:
            return

        bb_length = self._bb_length.Value
        width = None
        if bollinger_value.IsFormed and bollinger_value.UpBand is not None and bollinger_value.LowBand is not None:
            width = bollinger_value.UpBand - bollinger_value.LowBand
            self._widths.append(width)
            while len(self._widths) > bb_length:
                self._widths.pop(0)

        if not macd_value.IsFormed or not rsi_value.IsFormed or not ema_fast_value.IsFormed or not ema_slow_value.IsFormed or not atr_value.IsFormed:
            return

        macd_line = macd_value.Macd
        signal_line = macd_value.Signal
        if macd_line is None or signal_line is None:
            return

        prev_macd = self._prev_macd
        prev_signal = self._prev_signal
        self._prev_macd = macd_line
        self._prev_signal = signal_line

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        atr = atr_value.GetValue[Decimal](None)
        close = candle.ClosePrice
        trail_mult = Decimal(self._trail_atr_multiplier.Value)

        if self.Position > 0 and self._stop_price is not None and self._take_price is not None:
            if candle.LowPrice <= self._stop_price or candle.HighPrice >= self._take_price:
                self.SellMarket(self.Position)
                self._stop_price = None
                self._take_price = None
                return
            self._stop_price = max(self._stop_price, close - atr * trail_mult)
        elif self.Position < 0 and self._stop_price is not None and self._take_price is not None:
            if candle.HighPrice >= self._stop_price or candle.LowPrice <= self._take_price:
                self.BuyMarket(-self.Position)
                self._stop_price = None
                self._take_price = None
                return
            self._stop_price = min(self._stop_price, close + atr * trail_mult)

        if prev_macd is None or prev_signal is None or width is None or len(self._widths) < bb_length:
            return

        average_width = sum(self._widths, Decimal(0)) / Decimal(len(self._widths))
        if width < average_width:
            return

        rsi = rsi_value.GetValue[Decimal](None)
        if rsi <= Decimal(self._rsi_oversold.Value) or rsi >= Decimal(self._rsi_overbought.Value):
            return

        fast = ema_fast_value.GetValue[Decimal](None)
        slow = ema_slow_value.GetValue[Decimal](None)
        stop_distance = atr * Decimal(self._atr_multiplier.Value)
        risk_reward = Decimal(self._risk_reward.Value)

        if prev_macd <= prev_signal and macd_line > signal_line and fast > slow and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - stop_distance
            self._take_price = close + stop_distance * risk_reward
        elif prev_macd >= prev_signal and macd_line < signal_line and fast < slow and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + stop_distance
            self._take_price = close - stop_distance * risk_reward

    def CreateClone(self):
        return macd_rsi_ema_bb_atr_day_trading_strategy()
