import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class improvisando_strategy(Strategy):
    """Improvisando Strategy. EMA trend + RSI filter + engulfing pattern."""

    def __init__(self):
        super(improvisando_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation.", "General")
        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("EMA Length", "EMA period", "Moving Averages")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period", "RSI")
        self._cooldown_bars = self.Param("CooldownBars", 15) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._ema = None
        self._rsi = None
        self._prev_close = 0.0
        self._prev_open = 0.0
        self._cooldown_remaining = 0
        self._entry_price = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(improvisando_strategy, self).OnReseted()
        self._ema = None
        self._rsi = None
        self._prev_close = 0.0
        self._prev_open = 0.0
        self._cooldown_remaining = 0
        self._entry_price = None

    def OnStarted(self, time):
        super(improvisando_strategy, self).OnStarted(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = int(self._ema_length.Value)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = int(self._rsi_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self._rsi, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, ema_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._ema.IsFormed or not self._rsi.IsFormed:
            self._prev_close = float(candle.ClosePrice)
            self._prev_open = float(candle.OpenPrice)
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_close = float(candle.ClosePrice)
            self._prev_open = float(candle.OpenPrice)
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_close = float(candle.ClosePrice)
            self._prev_open = float(candle.OpenPrice)
            return

        close = float(candle.ClosePrice)
        opn = float(candle.OpenPrice)
        ema = float(ema_val)
        rsi = float(rsi_val)
        cooldown = int(self._cooldown_bars.Value)

        prev_bearish = self._prev_close < self._prev_open and self._prev_close > 0
        prev_bullish = self._prev_close > self._prev_open and self._prev_close > 0

        buy_pattern = prev_bearish and close > opn and close > self._prev_open
        sell_pattern = prev_bullish and close < opn and close < self._prev_open

        # Exit long
        if self.Position > 0:
            tp = self._entry_price is not None and close > self._entry_price * 1.02
            if close < ema or tp:
                self.SellMarket(Math.Abs(self.Position))
                self._entry_price = None
                self._cooldown_remaining = cooldown
                self._prev_close = close
                self._prev_open = opn
                return

        # Exit short
        elif self.Position < 0:
            tp = self._entry_price is not None and close < self._entry_price * 0.98
            if close > ema or tp:
                self.BuyMarket(Math.Abs(self.Position))
                self._entry_price = None
                self._cooldown_remaining = cooldown
                self._prev_close = close
                self._prev_open = opn
                return

        # Buy: engulfing + above EMA + RSI not overbought
        if buy_pattern and close > ema and rsi < 65 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._entry_price = close
            self._cooldown_remaining = cooldown
        # Sell: engulfing + below EMA + RSI not oversold
        elif sell_pattern and close < ema and rsi > 35 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._entry_price = close
            self._cooldown_remaining = cooldown

        self._prev_close = close
        self._prev_open = opn

    def CreateClone(self):
        return improvisando_strategy()
