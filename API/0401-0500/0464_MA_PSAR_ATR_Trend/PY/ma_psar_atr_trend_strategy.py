import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange, ParabolicSar
from StockSharp.Algo.Strategies import Strategy


class ma_psar_atr_trend_strategy(Strategy):
    """Moving Average Crossover with Parabolic SAR filter and ATR stop."""

    def __init__(self):
        super(ma_psar_atr_trend_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._fast_ma_period = self.Param("FastMaPeriod", 20) \
            .SetDisplay("Fast MA Period", "Fast EMA period", "MA")
        self._slow_ma_period = self.Param("SlowMaPeriod", 50) \
            .SetDisplay("Slow MA Period", "Slow EMA period", "MA")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period", "ATR")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "ATR stop multiplier", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._fast_ma = None
        self._slow_ma = None
        self._atr = None
        self._psar = None
        self._stop_price = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ma_psar_atr_trend_strategy, self).OnReseted()
        self._fast_ma = None
        self._slow_ma = None
        self._atr = None
        self._psar = None
        self._stop_price = 0.0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(ma_psar_atr_trend_strategy, self).OnStarted2(time)

        self._fast_ma = ExponentialMovingAverage()
        self._fast_ma.Length = int(self._fast_ma_period.Value)

        self._slow_ma = ExponentialMovingAverage()
        self._slow_ma.Length = int(self._slow_ma_period.Value)

        self._atr = AverageTrueRange()
        self._atr.Length = int(self._atr_period.Value)

        self._psar = ParabolicSar()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast_ma, self._slow_ma, self._atr, self._psar, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._fast_ma)
            self.DrawIndicator(area, self._slow_ma)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, fast_val, slow_val, atr_val, psar_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._fast_ma.IsFormed or not self._slow_ma.IsFormed or not self._atr.IsFormed or not self._psar.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        price = float(candle.ClosePrice)
        fast_v = float(fast_val)
        slow_v = float(slow_val)
        atr_v = float(atr_val)
        psar_v = float(psar_val)
        atr_mult = float(self._atr_multiplier.Value)
        cooldown = int(self._cooldown_bars.Value)

        bullish_trend = fast_v > slow_v and price > fast_v
        bearish_trend = fast_v < slow_v and price < fast_v
        psar_bull = price > psar_v
        psar_bear = price < psar_v

        if self.Position > 0 and price <= self._stop_price:
            self.SellMarket(Math.Abs(self.Position))
            self._stop_price = 0.0
            self._cooldown_remaining = cooldown
            return
        elif self.Position < 0 and price >= self._stop_price:
            self.BuyMarket(Math.Abs(self.Position))
            self._stop_price = 0.0
            self._cooldown_remaining = cooldown
            return

        if bullish_trend and psar_bull and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._stop_price = price - atr_v * atr_mult
            self._cooldown_remaining = cooldown
        elif bearish_trend and psar_bear and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._stop_price = price + atr_v * atr_mult
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and bearish_trend:
            self.SellMarket(Math.Abs(self.Position))
            self._stop_price = 0.0
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and bullish_trend:
            self.BuyMarket(Math.Abs(self.Position))
            self._stop_price = 0.0
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return ma_psar_atr_trend_strategy()
