import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (ExponentialMovingAverage, RelativeStrengthIndex,
                                         MovingAverageConvergenceDivergence, AverageTrueRange)
from StockSharp.Algo.Strategies import Strategy


class scalping_ema_rsi_macd_strategy(Strategy):
    """Scalping EMA RSI MACD Strategy."""

    def __init__(self):
        super(scalping_ema_rsi_macd_strategy, self).__init__()

        self._fast_ema_length = self.Param("FastEmaLength", 12) \
            .SetDisplay("Fast EMA Length", "Length for fast EMA", "Indicators")
        self._slow_ema_length = self.Param("SlowEmaLength", 26) \
            .SetDisplay("Slow EMA Length", "Length for slow EMA", "Indicators")
        self._trend_ema_length = self.Param("TrendEmaLength", 55) \
            .SetDisplay("Trend EMA Length", "Length for trend EMA", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "Length for RSI", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", 65) \
            .SetDisplay("RSI Overbought", "Upper RSI bound", "Indicators")
        self._rsi_oversold = self.Param("RsiOversold", 35) \
            .SetDisplay("RSI Oversold", "Lower RSI bound", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "Length for ATR", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for stop-loss", "Risk")
        self._risk_reward = self.Param("RiskReward", 2.0) \
            .SetDisplay("Risk Reward", "Take profit multiplier", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._prev_fast_ema = 0.0
        self._prev_slow_ema = 0.0
        self._prev_macd = 0.0
        self._stop_price = 0.0
        self._take_profit_price = 0.0
        self._entry_price = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(scalping_ema_rsi_macd_strategy, self).OnReseted()
        self._prev_fast_ema = 0.0
        self._prev_slow_ema = 0.0
        self._prev_macd = 0.0
        self._stop_price = 0.0
        self._take_profit_price = 0.0
        self._entry_price = 0.0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(scalping_ema_rsi_macd_strategy, self).OnStarted2(time)

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = int(self._fast_ema_length.Value)
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = int(self._slow_ema_length.Value)
        trend_ema = ExponentialMovingAverage()
        trend_ema.Length = int(self._trend_ema_length.Value)
        rsi = RelativeStrengthIndex()
        rsi.Length = int(self._rsi_length.Value)
        macd = MovingAverageConvergenceDivergence()
        macd.ShortMa.Length = int(self._fast_ema_length.Value)
        macd.LongMa.Length = int(self._slow_ema_length.Value)
        atr = AverageTrueRange()
        atr.Length = int(self._atr_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, trend_ema, rsi, macd, atr, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, fast_ema, slow_ema, trend_ema, rsi, macd, atr):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_fast_ema = float(fast_ema)
            self._prev_slow_ema = float(slow_ema)
            self._prev_macd = float(macd)
            return

        close = float(candle.ClosePrice)
        fast_v = float(fast_ema)
        slow_v = float(slow_ema)
        trend_v = float(trend_ema)
        rsi_v = float(rsi)
        macd_v = float(macd)
        atr_v = float(atr)
        cooldown = int(self._cooldown_bars.Value)

        # Check stop-loss and take-profit exits first
        if self.Position > 0 and self._stop_price > 0:
            if float(candle.LowPrice) <= self._stop_price or float(candle.HighPrice) >= self._take_profit_price:
                self.SellMarket(Math.Abs(self.Position))
                self._stop_price = 0.0
                self._take_profit_price = 0.0
                self._cooldown_remaining = cooldown
                self._prev_fast_ema = fast_v
                self._prev_slow_ema = slow_v
                self._prev_macd = macd_v
                return
        elif self.Position < 0 and self._stop_price > 0:
            if float(candle.HighPrice) >= self._stop_price or float(candle.LowPrice) <= self._take_profit_price:
                self.BuyMarket(Math.Abs(self.Position))
                self._stop_price = 0.0
                self._take_profit_price = 0.0
                self._cooldown_remaining = cooldown
                self._prev_fast_ema = fast_v
                self._prev_slow_ema = slow_v
                self._prev_macd = macd_v
                return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_fast_ema = fast_v
            self._prev_slow_ema = slow_v
            self._prev_macd = macd_v
            return

        up_trend = close > trend_v and fast_v > slow_v
        down_trend = close < trend_v and fast_v < slow_v

        bull_cross = self._prev_fast_ema > 0 and self._prev_fast_ema <= self._prev_slow_ema and fast_v > slow_v
        bear_cross = self._prev_fast_ema > 0 and self._prev_fast_ema >= self._prev_slow_ema and fast_v < slow_v

        macd_rising = macd_v > self._prev_macd
        macd_falling = macd_v < self._prev_macd

        rsi_ob = float(self._rsi_overbought.Value)
        rsi_os = float(self._rsi_oversold.Value)

        long_condition = bull_cross and up_trend and rsi_v > 40 and rsi_v < rsi_ob and macd_rising
        short_condition = bear_cross and down_trend and rsi_v < 60 and rsi_v > rsi_os and macd_falling

        atr_mult = float(self._atr_multiplier.Value)
        rr = float(self._risk_reward.Value)

        if long_condition and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._entry_price = close
            self._stop_price = close - atr_v * atr_mult
            self._take_profit_price = close + (close - self._stop_price) * rr
            self._cooldown_remaining = cooldown
        elif short_condition and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._entry_price = close
            self._stop_price = close + atr_v * atr_mult
            self._take_profit_price = close - (self._stop_price - close) * rr
            self._cooldown_remaining = cooldown

        self._prev_fast_ema = fast_v
        self._prev_slow_ema = slow_v
        self._prev_macd = macd_v

    def CreateClone(self):
        return scalping_ema_rsi_macd_strategy()
