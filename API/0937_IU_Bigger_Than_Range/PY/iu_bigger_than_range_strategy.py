import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class iu_bigger_than_range_strategy(Strategy):
    def __init__(self):
        super(iu_bigger_than_range_strategy, self).__init__()
        self._lookback_period = self.Param("LookbackPeriod", 22) \
            .SetDisplay("Lookback Period", "Length for range calculation", "Parameters")
        self._risk_to_reward = self.Param("RiskToReward", 3) \
            .SetDisplay("Risk To Reward", "Risk to reward ratio", "Parameters")
        self._atr_factor = self.Param("AtrFactor", 2.0) \
            .SetDisplay("ATR Factor", "ATR multiplier", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(120))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_range_size = 0.0
        self._prev_candle_high = 0.0
        self._prev_candle_low = 0.0
        self._stop_price = 0.0
        self._target_price = 0.0
        self._entry_price = 0.0
        self._bar_count = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(iu_bigger_than_range_strategy, self).OnReseted()
        self._prev_range_size = 0.0
        self._prev_candle_high = 0.0
        self._prev_candle_low = 0.0
        self._stop_price = 0.0
        self._target_price = 0.0
        self._entry_price = 0.0
        self._bar_count = 0

    def OnStarted(self, time):
        super(iu_bigger_than_range_strategy, self).OnStarted(time)
        atr = AverageTrueRange()
        atr.Length = self._lookback_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, atr_val):
        if candle.State != CandleStates.Finished:
            return
        self._bar_count += 1
        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        atr_v = float(atr_val)
        range_size = high - low
        candle_body = abs(close - open_p)
        lookback = self._lookback_period.Value
        if self._bar_count < lookback:
            self._prev_range_size = range_size
            self._prev_candle_high = high
            self._prev_candle_low = low
            return
        rr = self._risk_to_reward.Value
        factor = float(self._atr_factor.Value)
        if self.Position > 0:
            if low <= self._stop_price or close >= self._target_price:
                self.SellMarket()
                self._stop_price = 0.0
                self._target_price = 0.0
                self._entry_price = 0.0
        elif self.Position < 0:
            if high >= self._stop_price or close <= self._target_price:
                self.BuyMarket()
                self._stop_price = 0.0
                self._target_price = 0.0
                self._entry_price = 0.0
        is_body_strong = candle_body >= self._prev_range_size and candle_body >= atr_v * 0.8
        if self.Position == 0 and is_body_strong:
            if close > open_p and close > self._prev_candle_high:
                self.BuyMarket()
                self._entry_price = close
                self._stop_price = self._entry_price - atr_v * factor
                self._target_price = self._entry_price + (self._entry_price - self._stop_price) * rr
            elif close < open_p and close < self._prev_candle_low:
                self.SellMarket()
                self._entry_price = close
                self._stop_price = self._entry_price + atr_v * factor
                self._target_price = self._entry_price - (self._stop_price - self._entry_price) * rr
        self._prev_range_size = range_size
        self._prev_candle_high = high
        self._prev_candle_low = low

    def CreateClone(self):
        return iu_bigger_than_range_strategy()
