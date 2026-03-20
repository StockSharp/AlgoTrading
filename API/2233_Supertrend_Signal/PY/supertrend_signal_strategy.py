import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend
from StockSharp.Algo.Strategies import Strategy


class supertrend_signal_strategy(Strategy):
    def __init__(self):
        super(supertrend_signal_strategy, self).__init__()
        self._atr_period = self.Param("AtrPeriod", 5) \
            .SetDisplay("ATR Period", "ATR period for SuperTrend", "Parameters")
        self._multiplier = self.Param("Multiplier", 3.0) \
            .SetDisplay("Multiplier", "ATR multiplier for SuperTrend", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "Parameters")
        self._prev_is_up_trend = None

    @property
    def atr_period(self):
        return self._atr_period.Value

    @property
    def multiplier(self):
        return self._multiplier.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(supertrend_signal_strategy, self).OnReseted()
        self._prev_is_up_trend = None

    def OnStarted(self, time):
        super(supertrend_signal_strategy, self).OnStarted(time)
        st = SuperTrend()
        st.Length = self.atr_period
        st.Multiplier = self.multiplier
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(st, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, st)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, st_value):
        if candle.State != CandleStates.Finished:
            return
        if not st_value.IsFormed:
            return
        is_up_trend = st_value.IsUpTrend
        if self._prev_is_up_trend is not None:
            if is_up_trend and not self._prev_is_up_trend and self.Position <= 0:
                self.BuyMarket()
            elif not is_up_trend and self._prev_is_up_trend and self.Position >= 0:
                self.SellMarket()
        self._prev_is_up_trend = is_up_trend

    def CreateClone(self):
        return supertrend_signal_strategy()
