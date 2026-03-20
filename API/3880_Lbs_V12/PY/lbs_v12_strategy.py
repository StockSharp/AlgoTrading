import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class lbs_v12_strategy(Strategy):
    def __init__(self):
        super(lbs_v12_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 50) \
            .SetDisplay("EMA Period", "EMA lookback", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR lookback", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 3.0) \
            .SetDisplay("ATR Multiplier", "Channel width multiplier", "Indicators")
        self._cooldown_candles = self.Param("CooldownCandles", 100) \
            .SetDisplay("Cooldown", "Candles between signals", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._cooldown_remaining = 0

    @property
    def ema_period(self):
        return self._ema_period.Value

    @property
    def atr_period(self):
        return self._atr_period.Value

    @property
    def atr_multiplier(self):
        return self._atr_multiplier.Value

    @property
    def cooldown_candles(self):
        return self._cooldown_candles.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(lbs_v12_strategy, self).OnReseted()
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(lbs_v12_strategy, self).OnStarted(time)
        self._cooldown_remaining = 0

        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        atr = AverageTrueRange()
        atr.Length = self.atr_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, atr, self.process_candle).Start()

    def process_candle(self, candle, ema, atr):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        close = float(candle.ClosePrice)
        ema_val = float(ema)
        atr_val = float(atr)
        mult = float(self.atr_multiplier)

        if close > ema_val + atr_val * mult and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_candles
        elif close < ema_val - atr_val * mult and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_candles

    def CreateClone(self):
        return lbs_v12_strategy()
