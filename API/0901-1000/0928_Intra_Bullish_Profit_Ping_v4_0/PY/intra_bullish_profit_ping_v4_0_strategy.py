import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class intra_bullish_profit_ping_v4_0_strategy(Strategy):
    """
    Intra Bullish Strategy - Profit Ping v4.0.
    Long only. A long opens when the short EMA crosses above the long EMA on a candle where the MACD histogram is positive, RSI is above 50
    and the candle closes above its open. The position closes when the short EMA crosses below the long EMA with a negative histogram,
    RSI below 50 and a candle closing below its open. There are no stops.
    """

    def __init__(self):
        super(intra_bullish_profit_ping_v4_0_strategy, self).__init__()
        self._short_ema_length = self.Param("ShortEmaLength", 7).SetGreaterThanZero().SetDisplay("Short EMA", "Short EMA length", "EMA")
        self._long_ema_length = self.Param("LongEmaLength", 14).SetGreaterThanZero().SetDisplay("Long EMA", "Long EMA length", "EMA")
        self._rsi_length = self.Param("RsiLength", 14).SetGreaterThanZero().SetDisplay("RSI Length", "RSI period", "RSI")
        self._macd_fast = self.Param("MacdFastPeriod", 12).SetGreaterThanZero().SetDisplay("MACD Fast", "MACD fast period", "MACD")
        self._macd_slow = self.Param("MacdSlowPeriod", 26).SetGreaterThanZero().SetDisplay("MACD Slow", "MACD slow period", "MACD")
        self._macd_signal = self.Param("MacdSignalPeriod", 9).SetGreaterThanZero().SetDisplay("MACD Signal", "MACD signal period", "MACD")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_short = None
        self._prev_long = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(intra_bullish_profit_ping_v4_0_strategy, self).OnReseted()
        self._prev_short = None
        self._prev_long = None

    def OnStarted2(self, time):
        super(intra_bullish_profit_ping_v4_0_strategy, self).OnStarted2(time)

        self._prev_short = None
        self._prev_long = None

        ema_short = ExponentialMovingAverage()
        ema_short.Length = self._short_ema_length.Value
        ema_long = ExponentialMovingAverage()
        ema_long.Length = self._long_ema_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._macd_fast.Value
        macd.Macd.LongMa.Length = self._macd_slow.Value
        macd.SignalMa.Length = self._macd_signal.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ema_short, ema_long, rsi, macd, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema_short)
            self.DrawIndicator(area, ema_long)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)
                self.DrawIndicator(oscillators, macd)

    def _process_candle(self, candle, ema_short_value, ema_long_value, rsi_value, macd_value):
        if candle.State != CandleStates.Finished:
            return

        if not ema_short_value.IsFormed or not ema_long_value.IsFormed:
            return

        ema_short = ema_short_value.GetValue[Decimal](None)
        ema_long = ema_long_value.GetValue[Decimal](None)

        prev_short = self._prev_short
        prev_long = self._prev_long
        self._prev_short = ema_short
        self._prev_long = ema_long

        if prev_short is None or prev_long is None:
            return

        if not rsi_value.IsFormed or not macd_value.IsFormed or macd_value.Macd is None or macd_value.Signal is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        rsi = rsi_value.GetValue[Decimal](None)
        histogram = macd_value.Macd - macd_value.Signal

        cross_up = prev_short <= prev_long and ema_short > ema_long
        cross_down = prev_short >= prev_long and ema_short < ema_long

        if self.Position <= 0 and cross_up and histogram > 0 and rsi > 50 and candle.ClosePrice > candle.OpenPrice:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and cross_down and histogram < 0 and rsi < 50 and candle.ClosePrice < candle.OpenPrice:
            self.SellMarket(self.Position)

    def CreateClone(self):
        return intra_bullish_profit_ping_v4_0_strategy()
