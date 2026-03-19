import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class four_bar_momentum_reversal_strategy(Strategy):
    """
    Four Bar Momentum Reversal: enters long after consecutive closes below
    the close from N bars ago. Exits on breakout above previous high.
    Uses EMA as trend filter.
    """

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

        self._closes = []
        self._below_count = 0
        self._prev_high = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(four_bar_momentum_reversal_strategy, self).OnReseted()
        self._closes = []
        self._below_count = 0
        self._prev_high = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(four_bar_momentum_reversal_strategy, self).OnStarted(time)

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return

        ema_val = float(ema_val)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        lookback = self._lookback.Value

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._closes.append(close)
            if len(self._closes) > lookback + 10:
                self._closes.pop(0)
            self._prev_high = high
            return

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

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_high = high
            return

        buy_threshold = self._buy_threshold.Value

        # Buy: consecutive closes below reference
        if self._below_count >= buy_threshold and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self._cooldown_bars.Value
        # Exit long: breakout above previous high
        elif self.Position > 0 and close > self._prev_high:
            self.SellMarket()
            self._cooldown_remaining = self._cooldown_bars.Value
        # Short: consecutive closes above reference
        elif self._below_count == 0 and len(self._closes) > lookback:
            past_close = self._closes[len(self._closes) - 1 - lookback]
            above_count = 0
            for i in range(len(self._closes) - 1, max(-1, len(self._closes) - 1 - buy_threshold), -1):
                if i < 0:
                    break
                if self._closes[i] > past_close:
                    above_count += 1
                else:
                    break

            if above_count >= buy_threshold and self.Position >= 0 and close > ema_val:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
                self._cooldown_remaining = self._cooldown_bars.Value

        self._prev_high = high

    def CreateClone(self):
        return four_bar_momentum_reversal_strategy()
