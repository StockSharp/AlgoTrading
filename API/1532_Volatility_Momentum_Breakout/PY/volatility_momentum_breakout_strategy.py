import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class volatility_momentum_breakout_strategy(Strategy):
    def __init__(self):
        super(volatility_momentum_breakout_strategy, self).__init__()
        self._lookback = self.Param("Lookback", 40) \
            .SetDisplay("Lookback", "Breakout lookback", "General")
        self._ema_length = self.Param("EmaLength", 50) \
            .SetDisplay("EMA Length", "EMA trend filter", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period", "General")
        self._rsi_long = self.Param("RsiLong", 55.0) \
            .SetDisplay("RSI Long", "RSI above for longs", "General")
        self._rsi_short = self.Param("RsiShort", 45.0) \
            .SetDisplay("RSI Short", "RSI below for shorts", "General")
        self._risk_reward = self.Param("RiskReward", 2.0) \
            .SetDisplay("Risk/Reward", "Target ratio", "Risk")
        self._stop_mult = self.Param("StopMult", 1.5) \
            .SetDisplay("Stop Mult", "StdDev multiplier for stop", "Risk")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 12) \
            .SetDisplay("Signal Cooldown", "Bars to wait after a trade", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._highs = []
        self._lows = []
        self._entry_price = 0.0
        self._stop_dist = 0.0
        self._cooldown_remaining = 0

    @property
    def lookback(self):
        return self._lookback.Value

    @property
    def ema_length(self):
        return self._ema_length.Value

    @property
    def rsi_length(self):
        return self._rsi_length.Value

    @property
    def rsi_long(self):
        return self._rsi_long.Value

    @property
    def rsi_short(self):
        return self._rsi_short.Value

    @property
    def risk_reward(self):
        return self._risk_reward.Value

    @property
    def stop_mult(self):
        return self._stop_mult.Value

    @property
    def signal_cooldown_bars(self):
        return self._signal_cooldown_bars.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(volatility_momentum_breakout_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._entry_price = 0.0
        self._stop_dist = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(volatility_momentum_breakout_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_length
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_length
        std_dev = StandardDeviation()
        std_dev.Length = 14
        self._highs = []
        self._lows = []
        self._entry_price = 0.0
        self._stop_dist = 0.0
        self._cooldown_remaining = 0
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, rsi, std_dev, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ema_val, rsi_val, std_val):
        if candle.State != CandleStates.Finished:
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        self._highs.append(float(candle.HighPrice))
        self._lows.append(float(candle.LowPrice))
        while len(self._highs) > self.lookback + 1:
            self._highs.pop(0)
            self._lows.pop(0)
        if len(self._highs) <= self.lookback:
            return
        prev_high = -1e18
        prev_low = 1e18
        for i in range(len(self._highs) - 1):
            if self._highs[i] > prev_high:
                prev_high = self._highs[i]
            if self._lows[i] < prev_low:
                prev_low = self._lows[i]
        close = float(candle.ClosePrice)
        ema_f = float(ema_val)
        rsi_f = float(rsi_val)
        std_f = float(std_val)
        if self.Position > 0 and self._entry_price > 0 and self._stop_dist > 0:
            sl = self._entry_price - self._stop_dist
            tp = self._entry_price + self._stop_dist * self.risk_reward
            if float(candle.LowPrice) <= sl or float(candle.HighPrice) >= tp:
                self.SellMarket()
                self._entry_price = 0.0
                self._stop_dist = 0.0
                self._cooldown_remaining = self.signal_cooldown_bars
        elif self.Position < 0 and self._entry_price > 0 and self._stop_dist > 0:
            sl = self._entry_price + self._stop_dist
            tp = self._entry_price - self._stop_dist * self.risk_reward
            if float(candle.HighPrice) >= sl or float(candle.LowPrice) <= tp:
                self.BuyMarket()
                self._entry_price = 0.0
                self._stop_dist = 0.0
                self._cooldown_remaining = self.signal_cooldown_bars
        if self._cooldown_remaining == 0 and self.Position <= 0 and close > prev_high and close > ema_f and rsi_f > self.rsi_long and std_f > 0:
            self.BuyMarket()
            self._entry_price = close
            self._stop_dist = self.stop_mult * std_f
        elif self._cooldown_remaining == 0 and self.Position >= 0 and close < prev_low and close < ema_f and rsi_f < self.rsi_short and std_f > 0:
            self.SellMarket()
            self._entry_price = close
            self._stop_dist = self.stop_mult * std_f

    def CreateClone(self):
        return volatility_momentum_breakout_strategy()
