import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class m_trainer_strategy(Strategy):
    def __init__(self):
        super(m_trainer_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 10) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 30) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._std_period = self.Param("StdPeriod", 14) \
            .SetDisplay("StdDev Period", "StdDev period for SL/TP", "Indicators")
        self._sl_multiplier = self.Param("SlMultiplier", 2) \
            .SetDisplay("SL Multiplier", "ATR multiplier for stop loss", "Risk")
        self._tp_multiplier = self.Param("TpMultiplier", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("TP Multiplier", "ATR multiplier for take profit", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._entry_price = 0.0
        self._stop_loss = 0.0
        self._take_profit = 0.0

    @property
    def fast_period(self):
        return self._fast_period.Value

    @property
    def slow_period(self):
        return self._slow_period.Value

    @property
    def std_period(self):
        return self._std_period.Value

    @property
    def sl_multiplier(self):
        return self._sl_multiplier.Value

    @property
    def tp_multiplier(self):
        return self._tp_multiplier.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(m_trainer_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._entry_price = 0.0
        self._stop_loss = 0.0
        self._take_profit = 0.0

    def OnStarted(self, time):
        super(m_trainer_strategy, self).OnStarted(time)
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.fast_period
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.slow_period
        std_dev = StandardDeviation()
        std_dev.Length = self.std_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, std_dev, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast, slow, std_val):
        if candle.State != CandleStates.Finished:
            return
        # Check SL/TP for open positions
        if self.Position > 0:
            if candle.LowPrice <= self._stop_loss or candle.HighPrice >= self._take_profit:
                self.SellMarket()
                self._entry_price = 0
        elif self.Position < 0:
            if candle.HighPrice >= self._stop_loss or candle.LowPrice <= self._take_profit:
                self.BuyMarket()
                self._entry_price = 0
        if self._has_prev and std_val > 0:
            cross_up = self._prev_fast <= self._prev_slow and fast > slow
            cross_down = self._prev_fast >= self._prev_slow and fast < slow
            if cross_up and self.Position <= 0:
                self.BuyMarket()
                self._entry_price = candle.ClosePrice
                self._stop_loss = self._entry_price - std_val * self.sl_multiplier
                self._take_profit = self._entry_price + std_val * self.tp_multiplier
            elif cross_down and self.Position >= 0:
                self.SellMarket()
                self._entry_price = candle.ClosePrice
                self._stop_loss = self._entry_price + std_val * self.sl_multiplier
                self._take_profit = self._entry_price - std_val * self.tp_multiplier
        self._prev_fast = fast
        self._prev_slow = slow
        self._has_prev = True

    def CreateClone(self):
        return m_trainer_strategy()
