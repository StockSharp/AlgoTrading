import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class anands_strategy(Strategy):
    """
    Anand's breakout strategy based on short-term trend and price level breakouts.
    Uses EMA for trend and breakout of previous candle high/low for entry.
    """

    def __init__(self):
        super(anands_strategy, self).__init__()

        self._ema_length = self.Param("EmaLength", 5) \
            .SetDisplay("EMA Length", "EMA trend filter period", "Indicator")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Trading")
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Trading timeframe", "General")

        self._prev_high = 0.0
        self._prev_low = 0.0
        self._bar_index = 0
        self._last_trade_bar = -100

    @property
    def EmaLength(self): return self._ema_length.Value
    @EmaLength.setter
    def EmaLength(self, v): self._ema_length.Value = v
    @property
    def CooldownBars(self): return self._cooldown_bars.Value
    @CooldownBars.setter
    def CooldownBars(self, v): self._cooldown_bars.Value = v
    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v

    def OnReseted(self):
        super(anands_strategy, self).OnReseted()
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._bar_index = 0
        self._last_trade_bar = -100

    def OnStarted2(self, time):
        super(anands_strategy, self).OnStarted2(time)

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        self._bar_index += 1

        if self._prev_high == 0 or self._prev_low == 0:
            self._prev_high = float(candle.HighPrice)
            self._prev_low = float(candle.LowPrice)
            return

        close = float(candle.ClosePrice)
        cooldown_ok = self._bar_index - self._last_trade_bar > self.CooldownBars
        up_trend = close > ema_value
        down_trend = close < ema_value

        if up_trend and close > self._prev_high and self.Position <= 0 and cooldown_ok:
            self.BuyMarket()
            self._last_trade_bar = self._bar_index
        elif down_trend and close < self._prev_low and self.Position >= 0 and cooldown_ok:
            self.SellMarket()
            self._last_trade_bar = self._bar_index

        self._prev_high = float(candle.HighPrice)
        self._prev_low = float(candle.LowPrice)

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return anands_strategy()
