import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class volatility_pulse_with_dynamic_exit_strategy(Strategy):
    def __init__(self):
        super(volatility_pulse_with_dynamic_exit_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._std_length = self.Param("StdLength", 14) \
            .SetDisplay("StdDev Length", "Volatility period", "Parameters")
        self._momentum_length = self.Param("MomentumLength", 20) \
            .SetDisplay("Momentum Length", "Momentum lookback", "Parameters")
        self._vol_threshold = self.Param("VolThreshold", 1.2) \
            .SetDisplay("Vol Threshold", "StdDev expansion multiplier", "Parameters")
        self._exit_bars = self.Param("ExitBars", 42) \
            .SetDisplay("Exit Bars", "Time-based exit after N bars", "Risk")
        self._risk_reward = self.Param("RiskReward", 2) \
            .SetDisplay("Risk Reward", "TP to SL ratio", "Risk")
        self._stop_pct = self.Param("StopPct", 1) \
            .SetDisplay("Stop %", "Stop loss percent", "Risk")
        self._closes = []
        self._bar_index = 0
        self._entry_bar_index = -1
        self._entry_price = 0.0
        self._stop_dist = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def std_length(self):
        return self._std_length.Value

    @property
    def momentum_length(self):
        return self._momentum_length.Value

    @property
    def vol_threshold(self):
        return self._vol_threshold.Value

    @property
    def exit_bars(self):
        return self._exit_bars.Value

    @property
    def risk_reward(self):
        return self._risk_reward.Value

    @property
    def stop_pct(self):
        return self._stop_pct.Value

    def OnReseted(self):
        super(volatility_pulse_with_dynamic_exit_strategy, self).OnReseted()
        self._closes = []
        self._bar_index = 0
        self._entry_bar_index = -1
        self._entry_price = 0.0
        self._stop_dist = 0.0

    def OnStarted(self, time):
        super(volatility_pulse_with_dynamic_exit_strategy, self).OnStarted(time)
        std_dev = StandardDeviation()
        std_dev.Length = self.std_length
        sma = SimpleMovingAverage()
        sma.Length = self.std_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(std_dev, sma, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, std_dev)
            self.DrawOwnTrades(area)

    def on_process(self, candle, std_val, sma_val):
        if candle.State != CandleStates.Finished:
            return
        std_val = float(std_val)
        sma_val = float(sma_val)
        close = float(candle.ClosePrice)
        self._closes.append(close)
        while len(self._closes) > self.momentum_length + 1:
            self._closes.pop(0)
        self._bar_index += 1
        if len(self._closes) <= self.momentum_length or std_val <= 0 or sma_val <= 0:
            return
        # Momentum = current close - close N bars ago
        momentum = close - self._closes[0]
        # Volatility expansion: stdDev relative to price vs average
        vol_ratio = std_val / sma_val
        vol_expansion = vol_ratio > float(self.vol_threshold) * 0.01
        momentum_up = momentum > 0
        momentum_down = momentum < 0
        # TP/SL management
        if self.Position > 0 and self._entry_price > 0 and self._stop_dist > 0:
            sl = self._entry_price - self._stop_dist
            tp = self._entry_price + self._stop_dist * float(self.risk_reward)
            if close <= sl or close >= tp:
                self.SellMarket()
                self._entry_price = 0
                self._stop_dist = 0
                self._entry_bar_index = -1
            # Time-based exit
            elif self._entry_bar_index >= 0 and self._bar_index - self._entry_bar_index >= self.exit_bars:
                self.SellMarket()
                self._entry_price = 0
                self._stop_dist = 0
                self._entry_bar_index = -1
        elif self.Position < 0 and self._entry_price > 0 and self._stop_dist > 0:
            sl = self._entry_price + self._stop_dist
            tp = self._entry_price - self._stop_dist * float(self.risk_reward)
            if close >= sl or close <= tp:
                self.BuyMarket()
                self._entry_price = 0
                self._stop_dist = 0
                self._entry_bar_index = -1
            # Time-based exit
            elif self._entry_bar_index >= 0 and self._bar_index - self._entry_bar_index >= self.exit_bars:
                self.BuyMarket()
                self._entry_price = 0
                self._stop_dist = 0
                self._entry_bar_index = -1
        # Entry signals
        if self.Position <= 0 and vol_expansion and momentum_up:
            self.BuyMarket()
            self._entry_price = close
            self._stop_dist = close * float(self.stop_pct) / 100.0
            self._entry_bar_index = self._bar_index
        elif self.Position >= 0 and vol_expansion and momentum_down:
            self.SellMarket()
            self._entry_price = close
            self._stop_dist = close * float(self.stop_pct) / 100.0
            self._entry_bar_index = self._bar_index

    def CreateClone(self):
        return volatility_pulse_with_dynamic_exit_strategy()
