import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class three_commas_ha_ma_strategy(Strategy):
    def __init__(self):
        super(three_commas_ha_ma_strategy, self).__init__()
        self._ma_fast = self.Param("MaFast", 9) \
            .SetDisplay("MA Fast", "Fast moving average period", "MA")
        self._ma_slow = self.Param("MaSlow", 18) \
            .SetDisplay("MA Slow", "Slow moving average period", "MA")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ha_open_prev = 0.0
        self._ha_close_prev = 0.0
        self._stop_price = 0.0

    @property
    def ma_fast(self):
        return self._ma_fast.Value

    @property
    def ma_slow(self):
        return self._ma_slow.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(three_commas_ha_ma_strategy, self).OnReseted()
        self._ha_open_prev = 0.0
        self._ha_close_prev = 0.0
        self._stop_price = 0.0

    def OnStarted(self, time):
        super(three_commas_ha_ma_strategy, self).OnStarted(time)
        ma1 = ExponentialMovingAverage()
        ma1.Length = self.ma_fast
        ma2 = ExponentialMovingAverage()
        ma2.Length = self.ma_slow
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma1, ma2, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma1)
            self.DrawIndicator(area, ma2)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ma1, ma2):
        if candle.State != CandleStates.Finished:
            return
        ha_close = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4
        ha_open = ((candle.OpenPrice + candle.ClosePrice) / 2 if (self._ha_open_prev == 0 and self._ha_close_prev == 0) else (self._ha_open_prev + self._ha_close_prev) / 2)
        ha_bull = ha_close > ha_open
        ha_bear_prev = self._ha_close_prev < self._ha_open_prev
        ha_bull_prev = self._ha_close_prev > self._ha_open_prev
        if ha_bear_prev and ma1 > ma2 and ha_bull and candle.ClosePrice > ma1 and self.Position <= 0:
            self._stop_price = candle.LowPrice
            self.BuyMarket()
        elif ha_bull_prev and ma1 < ma2 and not ha_bull and candle.ClosePrice < ma1 and self.Position >= 0:
            self._stop_price = candle.HighPrice
            self.SellMarket()
        if self.Position > 0 and candle.ClosePrice < ma2:
            self.SellMarket()
        elif self.Position < 0 and candle.ClosePrice > ma2:
            self.BuyMarket()
        self._ha_open_prev = ha_open
        self._ha_close_prev = ha_close

    def CreateClone(self):
        return three_commas_ha_ma_strategy()
