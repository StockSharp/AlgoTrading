import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class auto_fib_breakout_strategy(Strategy):
    """
    AutoFib breakout strategy.
    Uses Highest/Lowest channel with EMA trend filter.
    Buys on breakout above channel high in uptrend, sells on break below in downtrend.
    """

    def __init__(self):
        super(auto_fib_breakout_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._ema_length = self.Param("EmaLength", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Length", "EMA trend filter period", "Indicators")
        self._channel_length = self.Param("ChannelLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Channel Length", "Highest/Lowest lookback", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 350) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Trading")

        self._prev_highest = 0.0
        self._prev_lowest = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v
    @property
    def EmaLength(self): return self._ema_length.Value
    @EmaLength.setter
    def EmaLength(self, v): self._ema_length.Value = v
    @property
    def ChannelLength(self): return self._channel_length.Value
    @ChannelLength.setter
    def ChannelLength(self, v): self._channel_length.Value = v
    @property
    def CooldownBars(self): return self._cooldown_bars.Value
    @CooldownBars.setter
    def CooldownBars(self, v): self._cooldown_bars.Value = v

    def OnReseted(self):
        super(auto_fib_breakout_strategy, self).OnReseted()
        self._prev_highest = 0.0
        self._prev_lowest = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

    def OnStarted(self, time):
        super(auto_fib_breakout_strategy, self).OnStarted(time)

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaLength
        highest = Highest()
        highest.Length = self.ChannelLength
        lowest = Lowest()
        lowest.Length = self.ChannelLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, highest, lowest, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ema_value, highest_value, lowest_value):
        if candle.State != CandleStates.Finished:
            return

        self._bar_index += 1
        cooldown_ok = self._bar_index - self._last_trade_bar > self.CooldownBars
        close = float(candle.ClosePrice)

        break_up = self._prev_highest > 0 and close > self._prev_highest and close > ema_value
        break_down = self._prev_lowest > 0 and close < self._prev_lowest and close < ema_value

        if break_up and self.Position <= 0 and cooldown_ok:
            self.BuyMarket()
            self._last_trade_bar = self._bar_index
        elif break_down and self.Position >= 0 and cooldown_ok:
            self.SellMarket()
            self._last_trade_bar = self._bar_index

        self._prev_highest = highest_value
        self._prev_lowest = lowest_value

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return auto_fib_breakout_strategy()
