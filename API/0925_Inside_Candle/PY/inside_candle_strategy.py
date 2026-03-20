import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class inside_candle_strategy(Strategy):
    def __init__(self):
        super(inside_candle_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._risk_reward = self.Param("RiskReward", 2.0) \
            .SetDisplay("RR Ratio", "Risk/reward ratio for exits", "Risk Management")
        self._prev_candle_high = 0.0
        self._prev_candle_low = 0.0
        self._prev_candle_set = False
        self._inside_high = 0.0
        self._inside_low = 0.0
        self._waiting_for_breakout = False
        self._stop_price = 0.0
        self._take_profit_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(inside_candle_strategy, self).OnReseted()
        self._prev_candle_high = 0.0
        self._prev_candle_low = 0.0
        self._prev_candle_set = False
        self._waiting_for_breakout = False
        self._inside_high = 0.0
        self._inside_low = 0.0
        self._stop_price = 0.0
        self._take_profit_price = 0.0

    def OnStarted(self, time):
        super(inside_candle_strategy, self).OnStarted(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        rr = float(self._risk_reward.Value)
        if self.Position > 0 and self._stop_price != 0 and self._take_profit_price != 0:
            if low <= self._stop_price or high >= self._take_profit_price:
                self.SellMarket()
                self._stop_price = 0.0
                self._take_profit_price = 0.0
        elif self.Position < 0 and self._stop_price != 0 and self._take_profit_price != 0:
            if high >= self._stop_price or low <= self._take_profit_price:
                self.BuyMarket()
                self._stop_price = 0.0
                self._take_profit_price = 0.0
        if self._prev_candle_set:
            if self._waiting_for_breakout:
                if close > self._inside_high:
                    self.BuyMarket()
                    entry = close
                    self._stop_price = self._inside_low
                    self._take_profit_price = entry + (entry - self._inside_low) * rr
                    self._waiting_for_breakout = False
                elif close < self._inside_low:
                    self.SellMarket()
                    entry = close
                    self._stop_price = self._inside_high
                    self._take_profit_price = entry - (self._inside_high - entry) * rr
                    self._waiting_for_breakout = False
                else:
                    self._waiting_for_breakout = False
            elif high < self._prev_candle_high and low > self._prev_candle_low:
                self._inside_high = self._prev_candle_high
                self._inside_low = self._prev_candle_low
                self._waiting_for_breakout = True
        self._prev_candle_high = high
        self._prev_candle_low = low
        self._prev_candle_set = True

    def CreateClone(self):
        return inside_candle_strategy()
