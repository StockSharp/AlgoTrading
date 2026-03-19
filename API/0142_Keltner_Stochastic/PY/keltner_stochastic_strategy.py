import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class keltner_stochastic_strategy(Strategy):
    """
    Keltner Channel + manual Stochastic %K. Mean reversion at band extremes.
    """

    def __init__(self):
        super(keltner_stochastic_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 20).SetDisplay("EMA Period", "Keltner EMA period", "Indicators")
        self._keltner_mult = self.Param("KeltnerMultiplier", 2.0).SetDisplay("Keltner Mult", "ATR multiplier", "Indicators")
        self._stoch_oversold = self.Param("StochOversold", 20.0).SetDisplay("Stoch Oversold", "Oversold level", "Indicators")
        self._stoch_overbought = self.Param("StochOverbought", 80.0).SetDisplay("Stoch Overbought", "Overbought level", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 100).SetDisplay("Cooldown", "Bars between trades", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

        self._atr_value = 0.0
        self._cooldown = 0
        self._highs = []
        self._lows = []
        self._stoch_period = 14

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(keltner_stochastic_strategy, self).OnReseted()
        self._atr_value = 0.0
        self._cooldown = 0
        self._highs = []
        self._lows = []

    def OnStarted(self, time):
        super(keltner_stochastic_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value
        atr = AverageTrueRange()
        atr.Length = 14
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(atr, self._on_atr)
        subscription.Bind(ema, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _on_atr(self, candle, atr_val):
        if atr_val.IsFinal:
            self._atr_value = float(atr_val.ToDecimal())

    def _process_candle(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._atr_value <= 0:
            return
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        self._highs.append(high)
        self._lows.append(low)
        max_buf = self._stoch_period * 2
        if len(self._highs) > max_buf:
            self._highs = self._highs[-max_buf:]
            self._lows = self._lows[-max_buf:]
        if len(self._highs) < self._stoch_period:
            return
        recent_h = self._highs[-self._stoch_period:]
        recent_l = self._lows[-self._stoch_period:]
        hh = max(recent_h)
        ll = min(recent_l)
        diff = hh - ll
        if diff == 0:
            return
        stoch_k = 100.0 * (close - ll) / diff
        ema = float(ema_val)
        upper = ema + self._keltner_mult.Value * self._atr_value
        lower = ema - self._keltner_mult.Value * self._atr_value
        if self._cooldown > 0:
            self._cooldown -= 1
            return
        if close < lower and stoch_k < self._stoch_oversold.Value and self.Position == 0:
            self.BuyMarket()
            self._cooldown = self._cooldown_bars.Value
        elif close > upper and stoch_k > self._stoch_overbought.Value and self.Position == 0:
            self.SellMarket()
            self._cooldown = self._cooldown_bars.Value
        if self.Position > 0 and close > ema:
            self.SellMarket()
            self._cooldown = self._cooldown_bars.Value
        elif self.Position < 0 and close < ema:
            self.BuyMarket()
            self._cooldown = self._cooldown_bars.Value

    def CreateClone(self):
        return keltner_stochastic_strategy()
