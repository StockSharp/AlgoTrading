import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class four_bar_momentum_reversal_strategy(Strategy):
    """Four Bar Momentum Reversal Strategy."""

    def __init__(self):
        super(four_bar_momentum_reversal_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._buy_threshold = self.Param("BuyThreshold", 3) \
            .SetDisplay("Buy Threshold", "Consecutive closes below reference to trigger buy", "Strategy")
        self._lookback = self.Param("Lookback", 4) \
            .SetDisplay("Lookback", "Number of bars to compare", "Strategy")
        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("EMA Length", "EMA trend filter period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 15) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._ema = None
        self._closes = []
        self._below_count = 0
        self._prev_high = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(four_bar_momentum_reversal_strategy, self).OnReseted()
        self._ema = None
        self._closes = []
        self._below_count = 0
        self._prev_high = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(four_bar_momentum_reversal_strategy, self).OnStarted(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = int(self._ema_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._ema.IsFormed:
            self._closes.append(float(candle.ClosePrice))
            self._prev_high = float(candle.HighPrice)
            return

        close = float(candle.ClosePrice)
        lookback = int(self._lookback.Value)

        # Track past closes for lookback comparison
        if len(self._closes) >= lookback:
            past_close = self._closes[len(self._closes) - lookback]
            if close < past_close:
                self._below_count += 1
            else:
                self._below_count = 0

        self._closes.append(close)

        if len(self._closes) > lookback + 10:
            self._closes.pop(0)

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_high = float(candle.HighPrice)
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_high = float(candle.HighPrice)
            return

        buy_threshold = int(self._buy_threshold.Value)
        cooldown = int(self._cooldown_bars.Value)
        ema_v = float(ema_val)

        # Buy: consecutive closes below reference
        if self._below_count >= buy_threshold and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        # Exit long: breakout above previous high
        elif self.Position > 0 and close > self._prev_high:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        # Short: consecutive closes above reference (overbought reversal)
        elif self._below_count == 0 and len(self._closes) > lookback:
            past_close = self._closes[len(self._closes) - 1 - lookback]
            above_count = 0
            start = len(self._closes) - 1
            end = max(0, len(self._closes) - buy_threshold)
            for i in range(start, end - 1, -1):
                if self._closes[i] > past_close:
                    above_count += 1
                else:
                    break

            if above_count >= buy_threshold and self.Position >= 0 and close > ema_v:
                if self.Position > 0:
                    self.SellMarket(Math.Abs(self.Position))
                self.SellMarket(self.Volume)
                self._cooldown_remaining = cooldown

        self._prev_high = float(candle.HighPrice)

    def CreateClone(self):
        return four_bar_momentum_reversal_strategy()
