import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class heiken_ashi_simplified_ea_strategy(Strategy):
    def __init__(self):
        super(heiken_ashi_simplified_ea_strategy, self).__init__()
        self._max_positions = self.Param("MaxPositions", 1) \
            .SetDisplay("Max Positions", "Maximum number of positions in direction", "General")
        self._distance_points = self.Param("DistancePoints", 400) \
            .SetDisplay("Distance Points", "Minimum distance in price steps from last HA open", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for Heikin Ashi calculation", "General")
        self._cooldown_bars = self.Param("CooldownBars", 6) \
            .SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading")
        self._ha_open1 = 0.0
        self._ha_open2 = 0.0
        self._ha_open3 = 0.0
        self._ha_open4 = 0.0
        self._ha_close1 = 0.0
        self._ha_close2 = 0.0
        self._ha_close3 = 0.0
        self._price_distance = 0.0
        self._position_count = 0
        self._cooldown_remaining = 0

    @property
    def max_positions(self):
        return self._max_positions.Value
    @property
    def distance_points(self):
        return self._distance_points.Value
    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(heiken_ashi_simplified_ea_strategy, self).OnReseted()
        self._ha_open1 = 0.0
        self._ha_open2 = 0.0
        self._ha_open3 = 0.0
        self._ha_open4 = 0.0
        self._ha_close1 = 0.0
        self._ha_close2 = 0.0
        self._ha_close3 = 0.0
        self._price_distance = 0.0
        self._position_count = 0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(heiken_ashi_simplified_ea_strategy, self).OnStarted(time)
        step = self.Security.PriceStep if self.Security.PriceStep is not None else 1.0
        self._price_distance = self.distance_points * float(step)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        if self.Position == 0:
            self._position_count = 0
        if self._ha_open4 != 0:
            direction = 0
            if self._ha_close1 > self._ha_open1 and self._ha_close2 > self._ha_open2 and self._ha_close3 > self._ha_open3:
                direction = 1
            elif self._ha_close1 < self._ha_open1 and self._ha_close2 < self._ha_open2 and self._ha_close3 < self._ha_open3:
                direction = -1
            if direction != 0:
                if self.Position * direction < 0:
                    if self.Position > 0:
                        self.SellMarket()
                    elif self.Position < 0:
                        self.BuyMarket()
                    self._position_count = 0
                    self._cooldown_remaining = self.cooldown_bars
                distance_from_anchor = float(candle.ClosePrice) - self._ha_open1
                if self._cooldown_remaining == 0 and distance_from_anchor * direction > 0 and abs(distance_from_anchor) >= self._price_distance:
                    if direction > 0 and self._position_count < self.max_positions and self.Position <= 0:
                        self.BuyMarket()
                        self._position_count = 1
                        self._cooldown_remaining = self.cooldown_bars
                    elif direction < 0 and self._position_count > -self.max_positions and self.Position >= 0:
                        self.SellMarket()
                        self._position_count = -1
                        self._cooldown_remaining = self.cooldown_bars
        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        c = float(candle.ClosePrice)
        ha_close = (o + h + l + c) / 4.0
        if self._ha_open1 == 0 and self._ha_close1 == 0:
            ha_open = (o + c) / 2.0
        else:
            ha_open = (self._ha_open1 + self._ha_close1) / 2.0
        self._ha_open4 = self._ha_open3
        self._ha_open3 = self._ha_open2
        self._ha_open2 = self._ha_open1
        self._ha_open1 = ha_open
        self._ha_close3 = self._ha_close2
        self._ha_close2 = self._ha_close1
        self._ha_close1 = ha_close

    def CreateClone(self):
        return heiken_ashi_simplified_ea_strategy()
