import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (ExponentialMovingAverage, MovingAverageConvergenceDivergenceSignal,
                                         RelativeStrengthIndex, IndicatorHelper)
from StockSharp.Algo.Strategies import Strategy


class ema_macd_rsi_strategy(Strategy):
    """Strategy combining EMA trend, MACD crossovers, and RSI levels."""

    def __init__(self):
        super(ema_macd_rsi_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._fast_ema_length = self.Param("FastEmaLength", 50) \
            .SetDisplay("Fast EMA", "Fast EMA length", "Indicators")
        self._slow_ema_length = self.Param("SlowEmaLength", 200) \
            .SetDisplay("Slow EMA", "Slow EMA length", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period", "Indicators")
        self._rsi_buy_level = self.Param("RsiBuyLevel", 40.0) \
            .SetDisplay("RSI Buy Level", "Min RSI for buy", "Trading")
        self._rsi_sell_level = self.Param("RsiSellLevel", 60.0) \
            .SetDisplay("RSI Sell Level", "Max RSI for sell", "Trading")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._fast_ema = None
        self._slow_ema = None
        self._macd_signal = None
        self._rsi = None
        self._prev_macd = 0.0
        self._prev_signal = 0.0
        self._is_first = True
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ema_macd_rsi_strategy, self).OnReseted()
        self._fast_ema = None
        self._slow_ema = None
        self._macd_signal = None
        self._rsi = None
        self._prev_macd = 0.0
        self._prev_signal = 0.0
        self._is_first = True
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(ema_macd_rsi_strategy, self).OnStarted2(time)

        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = int(self._fast_ema_length.Value)

        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = int(self._slow_ema_length.Value)

        self._macd_signal = MovingAverageConvergenceDivergenceSignal()
        self._macd_signal.Macd.ShortMa.Length = 12
        self._macd_signal.Macd.LongMa.Length = 26
        self._macd_signal.SignalMa.Length = 9

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = int(self._rsi_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._fast_ema, self._slow_ema, self._macd_signal, self._rsi, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._fast_ema)
            self.DrawIndicator(area, self._slow_ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, fast_ema_val, slow_ema_val, macd_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._fast_ema.IsFormed or not self._slow_ema.IsFormed or not self._macd_signal.IsFormed or not self._rsi.IsFormed:
            return

        if fast_ema_val.IsEmpty or slow_ema_val.IsEmpty or macd_val.IsEmpty or rsi_val.IsEmpty:
            return

        if macd_val.Macd is None or macd_val.Signal is None:
            return

        macd = float(macd_val.Macd)
        signal = float(macd_val.Signal)
        fast_ema = float(IndicatorHelper.ToDecimal(fast_ema_val))
        slow_ema = float(IndicatorHelper.ToDecimal(slow_ema_val))
        rsi = float(IndicatorHelper.ToDecimal(rsi_val))

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_macd = macd
            self._prev_signal = signal
            self._is_first = False
            return

        if self._is_first:
            self._prev_macd = macd
            self._prev_signal = signal
            self._is_first = False
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_macd = macd
            self._prev_signal = signal
            return

        rsi_buy = float(self._rsi_buy_level.Value)
        rsi_sell = float(self._rsi_sell_level.Value)
        cooldown = int(self._cooldown_bars.Value)

        is_bullish = fast_ema > slow_ema
        is_bearish = fast_ema < slow_ema
        macd_bull_cross = self._prev_macd <= self._prev_signal and macd > signal
        macd_bear_cross = self._prev_macd >= self._prev_signal and macd < signal
        rsi_bullish = rsi > rsi_buy and rsi < 70.0
        rsi_bearish = rsi < rsi_sell and rsi > 30.0

        if is_bullish and macd_bull_cross and rsi_bullish and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif is_bearish and macd_bear_cross and rsi_bearish and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown

        self._prev_macd = macd
        self._prev_signal = signal

    def CreateClone(self):
        return ema_macd_rsi_strategy()
