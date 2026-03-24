import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, TripleExponentialMovingAverage, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class zero_lag_tema_crosses_pakun_strategy(Strategy):
    def __init__(self):
        super(zero_lag_tema_crosses_pakun_strategy, self).__init__()
        self._lookback = self.Param("Lookback", 20) \
            .SetDisplay("Lookback", "Lookback period", "Indicators")
        self._fast_period = self.Param("FastPeriod", 20) \
            .SetDisplay("Fast Period", "Fast TEMA length", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 50) \
            .SetDisplay("Slow Period", "Slow TEMA length", "Indicators")
        self._risk_reward = self.Param("RiskReward", 1.5) \
            .SetDisplay("Risk/Reward", "Take profit ratio", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._stop_price = 0.0
        self._take_profit_price = 0.0
        self._entry_placed = False

    @property
    def lookback(self):
        return self._lookback.Value

    @property
    def fast_period(self):
        return self._fast_period.Value

    @property
    def slow_period(self):
        return self._slow_period.Value

    @property
    def risk_reward(self):
        return self._risk_reward.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(zero_lag_tema_crosses_pakun_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._stop_price = 0.0
        self._take_profit_price = 0.0
        self._entry_placed = False

    def OnStarted(self, time):
        super(zero_lag_tema_crosses_pakun_strategy, self).OnStarted(time)
        fast_tema = TripleExponentialMovingAverage()
        fast_tema.Length = self.fast_period
        slow_tema = TripleExponentialMovingAverage()
        slow_tema.Length = self.slow_period
        highest = Highest()
        highest.Length = self.lookback
        lowest = Lowest()
        lowest.Length = self.lookback
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_tema, slow_tema, highest, lowest, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast, slow, highest_val, lowest_val):
        if candle.State != CandleStates.Finished:
            return
        if self._prev_fast == 0:
            self._prev_fast = fast
            self._prev_slow = slow
            return
        cross_up = self._prev_fast <= self._prev_slow and fast > slow
        cross_down = self._prev_fast >= self._prev_slow and fast < slow
        self._prev_fast = fast
        self._prev_slow = slow
        price = float(candle.ClosePrice)
        if not self._entry_placed:
            if cross_up and self.Position <= 0:
                self.BuyMarket()
                self._entry_placed = True
                self._stop_price = float(lowest_val)
                self._take_profit_price = price + (price - self._stop_price) * float(self.risk_reward)
            elif cross_down and self.Position >= 0:
                self.SellMarket()
                self._entry_placed = True
                self._stop_price = float(highest_val)
                self._take_profit_price = price - (self._stop_price - price) * float(self.risk_reward)
        else:
            if self.Position > 0:
                if float(candle.LowPrice) <= self._stop_price or float(candle.HighPrice) >= self._take_profit_price:
                    self.SellMarket()
                    self._entry_placed = False
            elif self.Position < 0:
                if float(candle.HighPrice) >= self._stop_price or float(candle.LowPrice) <= self._take_profit_price:
                    self.BuyMarket()
                    self._entry_placed = False

    def CreateClone(self):
        return zero_lag_tema_crosses_pakun_strategy()
