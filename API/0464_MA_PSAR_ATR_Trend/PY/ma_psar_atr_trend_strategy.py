import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange, ParabolicSar
from StockSharp.Algo.Strategies import Strategy


class ma_psar_atr_trend_strategy(Strategy):
    def __init__(self):
        super(ma_psar_atr_trend_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._fast_ma_period = self.Param("FastMaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast MA Period", "Fast EMA period", "MA")
        self._slow_ma_period = self.Param("SlowMaPeriod", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow MA Period", "Slow EMA period", "MA")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "ATR period", "ATR")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "ATR stop multiplier", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._stop_price = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def fast_ma_period(self):
        return self._fast_ma_period.Value
    @property
    def slow_ma_period(self):
        return self._slow_ma_period.Value
    @property
    def atr_period(self):
        return self._atr_period.Value
    @property
    def atr_multiplier(self):
        return self._atr_multiplier.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(ma_psar_atr_trend_strategy, self).OnReseted()
        self._stop_price = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(ma_psar_atr_trend_strategy, self).OnStarted(time)
        self._fast_ma = ExponentialMovingAverage()
        self._fast_ma.Length = self.fast_ma_period
        self._slow_ma = ExponentialMovingAverage()
        self._slow_ma.Length = self.slow_ma_period
        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_period
        self._psar = ParabolicSar()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription \
            .Bind(self._fast_ma, self._slow_ma, self._atr, self._psar, self.OnProcess) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._fast_ma)
            self.DrawIndicator(area, self._slow_ma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_val, slow_val, atr_val, psar_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._fast_ma.IsFormed or not self._slow_ma.IsFormed or not self._atr.IsFormed or not self._psar.IsFormed:
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        price = float(candle.ClosePrice)
        fast_v = float(fast_val)
        slow_v = float(slow_val)
        atr_v = float(atr_val)
        psar_v = float(psar_val)
        bullish_trend = fast_v > slow_v and price > fast_v
        bearish_trend = fast_v < slow_v and price < fast_v
        psar_bull = price > psar_v
        psar_bear = price < psar_v

        if self.Position > 0 and price <= self._stop_price:
            self.SellMarket(abs(self.Position))
            self._stop_price = 0.0
            self._cooldown_remaining = self.cooldown_bars
            return
        elif self.Position < 0 and self._stop_price > 0 and price >= self._stop_price:
            self.BuyMarket(abs(self.Position))
            self._stop_price = 0.0
            self._cooldown_remaining = self.cooldown_bars
            return

        if bullish_trend and psar_bull and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(abs(self.Position))
            self.BuyMarket()
            self._stop_price = price - atr_v * float(self.atr_multiplier)
            self._cooldown_remaining = self.cooldown_bars
        elif bearish_trend and psar_bear and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(abs(self.Position))
            self.SellMarket()
            self._stop_price = price + atr_v * float(self.atr_multiplier)
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position > 0 and bearish_trend:
            self.SellMarket(abs(self.Position))
            self._stop_price = 0.0
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position < 0 and bullish_trend:
            self.BuyMarket(abs(self.Position))
            self._stop_price = 0.0
            self._cooldown_remaining = self.cooldown_bars

    def CreateClone(self):
        return ma_psar_atr_trend_strategy()
