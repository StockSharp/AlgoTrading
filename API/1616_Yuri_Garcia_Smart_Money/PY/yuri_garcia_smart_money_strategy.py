import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class yuri_garcia_smart_money_strategy(Strategy):
    def __init__(self):
        super(yuri_garcia_smart_money_strategy, self).__init__()
        self._zone_lookback = self.Param("ZoneLookback", 20) \
            .SetDisplay("Zone Lookback", "Lookback for high/low zone", "General")
        self._zone_buffer = self.Param("ZoneBuffer", 0.002) \
            .SetDisplay("Zone Buffer", "Buffer percent", "General")
        self._stop_percent = self.Param("StopPercent", 3.0) \
            .SetDisplay("Stop %", "Stop loss percentage", "Risk")
        self._risk_reward = self.Param("RiskReward", 2.0) \
            .SetDisplay("RRR", "Risk reward ratio", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._entry_price = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_bull = False
        self._prev_bear = False
        self._is_ready = False

    @property
    def zone_lookback(self):
        return self._zone_lookback.Value

    @property
    def zone_buffer(self):
        return self._zone_buffer.Value

    @property
    def stop_percent(self):
        return self._stop_percent.Value

    @property
    def risk_reward(self):
        return self._risk_reward.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(yuri_garcia_smart_money_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_bull = False
        self._prev_bear = False
        self._is_ready = False

    def OnStarted(self, time):
        super(yuri_garcia_smart_money_strategy, self).OnStarted(time)
        highest = Highest()
        highest.Length = self.zone_lookback
        lowest = Lowest()
        lowest.Length = self.zone_lookback
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, highest)
            self.DrawIndicator(area, lowest)
            self.DrawOwnTrades(area)

    def on_process(self, candle, high_zone, low_zone):
        if candle.State != CandleStates.Finished:
            return
        if not self._is_ready:
            self._prev_high = high_zone
            self._prev_low = low_zone
            self._is_ready = True
            return
        top = self._prev_high * (1 + self.zone_buffer)
        bottom = self._prev_low * (1 - self.zone_buffer)
        is_bull = candle.ClosePrice > candle.OpenPrice
        is_bear = candle.ClosePrice < candle.OpenPrice
        body = abs(candle.ClosePrice - candle.OpenPrice)
        pull_long = is_bull and self._prev_bear and candle.LowPrice <= candle.OpenPrice - body / 2
        pull_short = is_bear and self._prev_bull and candle.HighPrice >= candle.ClosePrice + body / 2
        self._prev_bull = is_bull
        self._prev_bear = is_bear
        near_support = candle.ClosePrice <= bottom * 1.02
        near_resistance = candle.ClosePrice >= top * 0.98
        if near_support and pull_long and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = candle.ClosePrice
        elif near_resistance and pull_short and self.Position >= 0:
            self.SellMarket()
            self._entry_price = candle.ClosePrice
        if self.Position > 0 and self._entry_price > 0:
            stop = self._entry_price * (1 - self.stop_percent / 100)
            target = self._entry_price * (1 + self.stop_percent * self.risk_reward / 100)
            if candle.ClosePrice <= stop or candle.ClosePrice >= target:
                self.SellMarket()
        elif self.Position < 0 and self._entry_price > 0:
            stop = self._entry_price * (1 + self.stop_percent / 100)
            target = self._entry_price * (1 - self.stop_percent * self.risk_reward / 100)
            if candle.ClosePrice >= stop or candle.ClosePrice <= target:
                self.BuyMarket()
        self._prev_high = high_zone
        self._prev_low = low_zone

    def CreateClone(self):
        return yuri_garcia_smart_money_strategy()
