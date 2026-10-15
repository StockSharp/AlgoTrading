import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy


class volume_ea_strategy(Strategy):
    def __init__(self):
        super(volume_ea_strategy, self).__init__()
        self._factor = self.Param("Factor", 1.55) \
            .SetDisplay("Factor", "Volume multiplier", "Trading")
        self._trailing_stop = self.Param("TrailingStop", 350.0) \
            .SetDisplay("Trailing Stop", "Trailing distance in steps", "Risk")
        self._cci_level1 = self.Param("CciLevel1", 50) \
            .SetDisplay("CCI Level1", "Lower CCI for buys", "Trading")
        self._cci_level2 = self.Param("CciLevel2", 190) \
            .SetDisplay("CCI Level2", "Upper CCI for buys", "Trading")
        self._cci_level3 = self.Param("CciLevel3", -50) \
            .SetDisplay("CCI Level3", "Upper CCI for sells", "Trading")
        self._cci_level4 = self.Param("CciLevel4", -190.0) \
            .SetDisplay("CCI Level4", "Lower CCI for sells", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._prev_volume = 0.0
        self._prev_prev_volume = 0.0
        self._prev_open = 0.0
        self._prev_close = 0.0
        self._long_stop = 0.0
        self._short_stop = 0.0

    @property
    def factor(self):
        return self._factor.Value

    @property
    def trailing_stop(self):
        return self._trailing_stop.Value

    @property
    def cci_level1(self):
        return self._cci_level1.Value

    @property
    def cci_level2(self):
        return self._cci_level2.Value

    @property
    def cci_level3(self):
        return self._cci_level3.Value

    @property
    def cci_level4(self):
        return self._cci_level4.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(volume_ea_strategy, self).OnReseted()
        self._prev_volume = 0.0
        self._prev_prev_volume = 0.0
        self._prev_open = 0.0
        self._prev_close = 0.0
        self._long_stop = 0.0
        self._short_stop = 0.0

    def OnStarted2(self, time):
        super(volume_ea_strategy, self).OnStarted2(time)
        cci = CommodityChannelIndex()
        cci.Length = 14
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(cci, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, cci)
            self.DrawOwnTrades(area)

    def on_process(self, candle, cci_value):
        if candle.State != CandleStates.Finished:
            return
        current_volume = candle.TotalVolume
        trail_dist = self.trailing_stop
        volume_ok = self._prev_volume > self._prev_prev_volume * self.factor
        if volume_ok:
            if self._prev_close > self._prev_open and cci_value > self.cci_level1 and cci_value < self.cci_level2 and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
                self._long_stop = candle.ClosePrice - trail_dist
                self._short_stop = 0
            elif self._prev_close < self._prev_open and cci_value < self.cci_level3 and cci_value > self.cci_level4 and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
                self._short_stop = candle.ClosePrice + trail_dist
                self._long_stop = 0
        if self.Position > 0:
            candidate = candle.ClosePrice - trail_dist
            if candidate > self._long_stop:
                self._long_stop = candidate
            if candle.ClosePrice <= self._long_stop:
                self.SellMarket()
                self._long_stop = 0
        elif self.Position < 0:
            candidate = candle.ClosePrice + trail_dist
            if self._short_stop == 0 or candidate < self._short_stop:
                self._short_stop = candidate
            if candle.ClosePrice >= self._short_stop:
                self.BuyMarket()
                self._short_stop = 0
        self._prev_prev_volume = self._prev_volume
        self._prev_volume = current_volume
        self._prev_open = candle.OpenPrice
        self._prev_close = candle.ClosePrice

    def CreateClone(self):
        return volume_ea_strategy()
