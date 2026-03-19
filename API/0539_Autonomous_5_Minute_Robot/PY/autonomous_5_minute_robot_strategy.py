import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class autonomous_5_minute_robot_strategy(Strategy):
    """
    Autonomous 5-Minute Robot strategy.
    Uses SMA trend filter with RSI momentum for entries.
    Buys when RSI crosses above 50 in uptrend, sells when RSI crosses below 50 in downtrend.
    """

    def __init__(self):
        super(autonomous_5_minute_robot_strategy, self).__init__()

        self._ma_length = self.Param("MaLength", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Trend MA Length", "Moving average length", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 350) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Trading")
        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")

        self._prev_rsi = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

    @property
    def MaLength(self): return self._ma_length.Value
    @MaLength.setter
    def MaLength(self, v): self._ma_length.Value = v
    @property
    def RsiLength(self): return self._rsi_length.Value
    @RsiLength.setter
    def RsiLength(self, v): self._rsi_length.Value = v
    @property
    def CooldownBars(self): return self._cooldown_bars.Value
    @CooldownBars.setter
    def CooldownBars(self, v): self._cooldown_bars.Value = v
    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v

    def OnReseted(self):
        super(autonomous_5_minute_robot_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

    def OnStarted(self, time):
        super(autonomous_5_minute_robot_strategy, self).OnStarted(time)

        sma = SimpleMovingAverage()
        sma.Length = self.MaLength
        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(sma, rsi, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ma_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        self._bar_index += 1
        cooldown_ok = self._bar_index - self._last_trade_bar > self.CooldownBars
        close = float(candle.ClosePrice)

        is_bullish = close > ma_value
        is_bearish = close < ma_value

        long_signal = self._prev_rsi > 0 and self._prev_rsi < 50 and rsi_value >= 50 and is_bullish
        short_signal = self._prev_rsi > 0 and self._prev_rsi > 50 and rsi_value <= 50 and is_bearish

        if long_signal and self.Position <= 0 and cooldown_ok:
            self.BuyMarket()
            self._last_trade_bar = self._bar_index
        elif short_signal and self.Position >= 0 and cooldown_ok:
            self.SellMarket()
            self._last_trade_bar = self._bar_index

        self._prev_rsi = rsi_value

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return autonomous_5_minute_robot_strategy()
