import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class keltner_rsi_divergence_strategy(Strategy):
    """
    Keltner bands + RSI divergence mean reversion strategy.
    """

    def __init__(self):
        super(keltner_rsi_divergence_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 20).SetDisplay("EMA Period", "EMA period", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14).SetDisplay("ATR Period", "ATR period", "Indicators")
        self._atr_mult = self.Param("AtrMultiplier", 1.15).SetDisplay("ATR Mult", "Band width multiplier", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14).SetDisplay("RSI Period", "RSI period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 72).SetDisplay("Cooldown", "Bars between trades", "Risk")
        self._sl_pct = self.Param("StopLossPercent", 2.0).SetDisplay("SL %", "Stop loss percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

        self._prev_rsi = 50.0
        self._prev_price = 0.0
        self._is_init = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(keltner_rsi_divergence_strategy, self).OnReseted()
        self._prev_rsi = 50.0
        self._prev_price = 0.0
        self._is_init = False
        self._cooldown = 0

    def OnStarted(self, time):
        super(keltner_rsi_divergence_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, atr, rsi, self._process_candle).Start()
        self.StartProtection(None, Unit(self._sl_pct.Value, UnitTypes.Percent))
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_val, atr_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        ema = float(ema_val)
        atr = float(atr_val)
        rsi = float(rsi_val)
        price = float(candle.ClosePrice)
        if not self._is_init:
            self._prev_price = price
            self._prev_rsi = rsi
            self._is_init = True
            return
        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_price = price
            self._prev_rsi = rsi
            return
        upper = ema + self._atr_mult.Value * atr
        lower = ema - self._atr_mult.Value * atr
        bull_div = (rsi >= self._prev_rsi and price < self._prev_price) or rsi <= 30
        bear_div = (rsi <= self._prev_rsi and price > self._prev_price) or rsi >= 70
        if self.Position == 0:
            if price <= lower + atr * 0.1 and bull_div:
                self.BuyMarket()
                self._cooldown = self._cooldown_bars.Value
            elif price >= upper - atr * 0.1 and bear_div:
                self.SellMarket()
                self._cooldown = self._cooldown_bars.Value
        elif self.Position > 0:
            if price >= ema or rsi >= 50:
                self.SellMarket()
                self._cooldown = self._cooldown_bars.Value
        elif self.Position < 0:
            if price <= ema or rsi <= 50:
                self.BuyMarket()
                self._cooldown = self._cooldown_bars.Value
        self._prev_price = price
        self._prev_rsi = rsi

    def CreateClone(self):
        return keltner_rsi_divergence_strategy()
