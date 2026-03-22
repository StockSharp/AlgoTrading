import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import HurstExponent
from StockSharp.Algo.Strategies import Strategy


class fib_hurst_breakout_strategy(Strategy):
    def __init__(self):
        super(fib_hurst_breakout_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles for strategy", "Parameters")
        self._hurst_period = self.Param("HurstPeriod", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Hurst Period", "Period for Hurst exponent", "Parameters")
        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Period for Fib level calculation", "Parameters")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._highs = []
        self._lows = []
        self._prev_close = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(fib_hurst_breakout_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._prev_close = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(fib_hurst_breakout_strategy, self).OnStarted(time)
        hurst = HurstExponent()
        hurst.Length = int(self._hurst_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(hurst, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, hurst_val):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        hurst_v = float(hurst_val)
        lookback = int(self._lookback_period.Value)

        self._highs.append(float(candle.HighPrice))
        self._lows.append(float(candle.LowPrice))

        if len(self._highs) > lookback:
            self._highs = self._highs[-lookback:]
            self._lows = self._lows[-lookback:]

        if len(self._highs) < lookback or self._prev_close == 0:
            self._prev_close = float(candle.ClosePrice)
            return

        high = max(self._highs)
        low = min(self._lows)
        rng = high - low

        if rng <= 0:
            self._prev_close = float(candle.ClosePrice)
            return

        fib382 = low + 0.382 * rng
        fib618 = low + 0.618 * rng
        close = float(candle.ClosePrice)
        cooldown = int(self._cooldown_bars.Value)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_close = close
            return

        cross_up = self._prev_close <= fib618 and close > fib618
        cross_down = self._prev_close >= fib382 and close < fib382

        if hurst_v > 0.5 and cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif hurst_v < 0.5 and cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown

        self._prev_close = close

    def CreateClone(self):
        return fib_hurst_breakout_strategy()
