import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend
from StockSharp.Algo.Strategies import Strategy


class iu_4_bar_up_strategy(Strategy):
    def __init__(self):
        super(iu_4_bar_up_strategy, self).__init__()
        self._supertrend_length = self.Param("SupertrendLength", 14) \
            .SetDisplay("SuperTrend ATR Period", "ATR period for SuperTrend", "General")
        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 1.0) \
            .SetDisplay("SuperTrend ATR Factor", "ATR factor for SuperTrend", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(240))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_bull1 = False
        self._prev_bull2 = False
        self._prev_bull3 = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(iu_4_bar_up_strategy, self).OnReseted()
        self._prev_bull1 = False
        self._prev_bull2 = False
        self._prev_bull3 = False

    def OnStarted2(self, time):
        super(iu_4_bar_up_strategy, self).OnStarted2(time)
        st = SuperTrend()
        st.Length = self._supertrend_length.Value
        st.Multiplier = self._supertrend_multiplier.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(st, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, st)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, st_val):
        if candle.State != CandleStates.Finished:
            return
        if st_val.IsEmpty:
            return
        st_v = float(st_val)
        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        bullish = close > open_p
        four_bull = bullish and self._prev_bull1 and self._prev_bull2 and self._prev_bull3
        if self.Position <= 0 and four_bull and close > st_v:
            self.BuyMarket()
        if self.Position > 0 and close < st_v:
            self.SellMarket()
        self._prev_bull3 = self._prev_bull2
        self._prev_bull2 = self._prev_bull1
        self._prev_bull1 = bullish

    def CreateClone(self):
        return iu_4_bar_up_strategy()
