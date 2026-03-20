import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class arpit_bollinger_band_strategy(Strategy):
    """Bollinger Band reversal strategy.
    Buys when price crosses below lower band then returns above.
    Sells when price crosses above upper band then returns below.
    """

    def __init__(self):
        super(arpit_bollinger_band_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._bollinger_length = self.Param("BollingerLength", 20) \
            .SetDisplay("Length", "Bollinger Bands length", "Bollinger")
        self._bollinger_multiplier = self.Param("BollingerMultiplier", 2.0) \
            .SetDisplay("Multiplier", "StdDev multiplier", "Bollinger")
        self._cooldown_bars = self.Param("CooldownBars", 350) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Trading")

        self._prev_close = 0.0
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(arpit_bollinger_band_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

    def OnStarted(self, time):
        super(arpit_bollinger_band_strategy, self).OnStarted(time)

        bollinger = BollingerBands()
        bollinger.Length = self._bollinger_length.Value
        bollinger.Width = self._bollinger_multiplier.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(bollinger, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return

        self._bar_index += 1

        if bb_value.IsEmpty:
            return

        upper = bb_value.UpBand
        lower = bb_value.LowBand

        if upper is None or lower is None:
            return

        upper = float(upper)
        lower = float(lower)

        if upper == 0 or lower == 0:
            return

        close = float(candle.ClosePrice)
        cooldown_ok = self._bar_index - self._last_trade_bar > self._cooldown_bars.Value

        cross_up_from_below = self._prev_close <= self._prev_lower and self._prev_lower > 0 and close > lower
        cross_down_from_above = self._prev_close >= self._prev_upper and self._prev_upper > 0 and close < upper

        if cross_up_from_below and self.Position <= 0 and cooldown_ok:
            self.BuyMarket()
            self._last_trade_bar = self._bar_index
        elif cross_down_from_above and self.Position >= 0 and cooldown_ok:
            self.SellMarket()
            self._last_trade_bar = self._bar_index

        self._prev_close = close
        self._prev_upper = upper
        self._prev_lower = lower

    def CreateClone(self):
        return arpit_bollinger_band_strategy()
