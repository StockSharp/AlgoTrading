import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

RSI_OVERBOUGHT = 70.0
RSI_OVERSOLD = 30.0


class ema_sma_rsi_strategy(Strategy):
    """
    EMA/SMA + RSI crossover strategy.
    Goes long when the fast EMA crosses above the medium EMA on a bullish candle closing above the slow EMA, and short
    on the mirrored setup. A long closes when RSI rises above 70 or when it has been held XBars bars and is in profit;
    a short closes when RSI falls below 30 or after XBars bars in profit.
    """

    def __init__(self):
        super(ema_sma_rsi_strategy, self).__init__()
        self._ema_fast = self.Param("EMA_fast", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Fast", "Fast EMA period", "Moving Averages")
        self._ema_medium = self.Param("EMA_medium", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Medium", "Medium EMA period", "Moving Averages")
        self._ema_slow = self.Param("EMA_slow", 100) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Slow", "Slow EMA period", "Moving Averages")
        self._rsi_length = self.Param("RSI_length", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI period", "RSI")
        self._x_bars = self.Param("XBars", 24) \
            .SetNotNegative() \
            .SetDisplay("X Bars", "Bars after which a profitable position is closed", "Exit")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation", "General")

        self._reset_state()

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def _reset_state(self):
        self._prev_fast = None
        self._prev_medium = None
        self._entry_price = 0.0
        self._bars_in_position = 0

    def OnReseted(self):
        super(ema_sma_rsi_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(ema_sma_rsi_strategy, self).OnStarted2(time)

        self._reset_state()

        fast = ExponentialMovingAverage()
        fast.Length = self._ema_fast.Value
        medium = ExponentialMovingAverage()
        medium.Length = self._ema_medium.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._ema_slow.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(fast, medium, slow, rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, medium)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)
            rsi_area = self.CreateChartArea()
            if rsi_area is not None:
                self.DrawIndicator(rsi_area, rsi)

    def _process_candle(self, candle, fast_value, medium_value, slow_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not fast_value.IsFormed or not medium_value.IsFormed or not slow_value.IsFormed or not rsi_value.IsFormed:
            return

        fast = float(fast_value.GetValue[Decimal](None))
        medium = float(medium_value.GetValue[Decimal](None))
        slow = float(slow_value.GetValue[Decimal](None))
        rsi = float(rsi_value.GetValue[Decimal](None))

        prev_fast = self._prev_fast
        prev_medium = self._prev_medium
        self._prev_fast = fast
        self._prev_medium = medium

        if self.Position != 0:
            self._bars_in_position += 1

        if prev_fast is None or prev_medium is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)

        long_signal = fast > medium and prev_fast <= prev_medium and close > slow and close > open_price
        short_signal = fast < medium and prev_fast >= prev_medium and close < slow and close < open_price

        x_bars = int(self._x_bars.Value)
        time_up = x_bars > 0 and self._bars_in_position >= x_bars

        if long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._entry_price = close
            self._bars_in_position = 0
        elif short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._entry_price = close
            self._bars_in_position = 0
        elif self.Position > 0 and (rsi > RSI_OVERBOUGHT or (time_up and close > self._entry_price)):
            self.SellMarket(self.Position)
        elif self.Position < 0 and (rsi < RSI_OVERSOLD or (time_up and close < self._entry_price)):
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return ema_sma_rsi_strategy()
