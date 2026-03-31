import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class iu_ema_channel_strategy(Strategy):
    def __init__(self):
        super(iu_ema_channel_strategy, self).__init__()
        self._ema_length = self.Param("EmaLength", 100) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Length", "EMA period", "General")
        self._risk_to_reward = self.Param("RiskToReward", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Risk To Reward", "Reward to risk ratio", "General")
        self._max_entries = self.Param("MaxEntries", 35) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Entries", "Maximum number of entries per test run", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "General")
        self._prev_close = 0.0
        self._prev_high_ema = 0.0
        self._prev_low_ema = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._is_initialized = False
        self._entries_executed = 0
        self._entry_pending = False
        self._exit_pending = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(iu_ema_channel_strategy, self).OnReseted()
        self._is_initialized = False
        self._prev_close = 0.0
        self._prev_high_ema = 0.0
        self._prev_low_ema = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._entries_executed = 0
        self._entry_pending = False
        self._exit_pending = False

    def OnStarted2(self, time):
        super(iu_ema_channel_strategy, self).OnStarted2(time)
        high_ema = ExponentialMovingAverage()
        high_ema.Length = self._ema_length.Value
        low_ema = ExponentialMovingAverage()
        low_ema.Length = self._ema_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(high_ema, low_ema, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, high_ema)
            self.DrawIndicator(area, low_ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, high_ema_val, low_ema_val):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        h_ema = float(high_ema_val)
        l_ema = float(low_ema_val)
        rr = float(self._risk_to_reward.Value)
        if not self._is_initialized:
            self._prev_close = close
            self._prev_high_ema = h_ema
            self._prev_low_ema = l_ema
            self._prev_high = high
            self._prev_low = low
            self._is_initialized = True
            return
        if self.Position == 0:
            self._exit_pending = False
            self._entry_pending = False
        else:
            self._entry_pending = False
        if self.Position == 0:
            if self._entries_executed >= self._max_entries.Value or self._entry_pending:
                self._prev_close = close
                self._prev_high_ema = h_ema
                self._prev_low_ema = l_ema
                self._prev_high = high
                self._prev_low = low
                return
            cross_up = self._prev_close <= self._prev_high_ema and close > h_ema
            cross_down = self._prev_close >= self._prev_low_ema and close < l_ema
            if cross_up:
                self._stop_price = self._prev_low
                self._take_price = close + (close - self._stop_price) * rr
                self.BuyMarket()
                self._entries_executed += 1
                self._entry_pending = True
            elif cross_down:
                self._stop_price = self._prev_high
                self._take_price = close - (self._stop_price - close) * rr
                self.SellMarket()
                self._entries_executed += 1
                self._entry_pending = True
        elif self.Position > 0:
            if not self._exit_pending and (low <= self._stop_price or high >= self._take_price):
                self.SellMarket()
                self._exit_pending = True
        elif self.Position < 0:
            if not self._exit_pending and (high >= self._stop_price or low <= self._take_price):
                self.BuyMarket()
                self._exit_pending = True
        self._prev_close = close
        self._prev_high_ema = h_ema
        self._prev_low_ema = l_ema
        self._prev_high = high
        self._prev_low = low

    def CreateClone(self):
        return iu_ema_channel_strategy()
