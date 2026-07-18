import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class best_dollar_cost_average_strategy(Strategy):
    def __init__(self):
        super(best_dollar_cost_average_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._buy_interval_bars = self.Param("BuyIntervalBars", 350) \
            .SetGreaterThanZero() \
            .SetDisplay("Buy Interval", "Bars between DCA buys", "DCA")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "RSI period for sell signal", "Indicators")
        self._rsi_sell_level = self.Param("RsiSellLevel", 60.0) \
            .SetDisplay("RSI Sell Level", "RSI level to trigger sell", "Indicators")
        self._max_accumulation_bars = self.Param("MaxAccumulationBars", 1200) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Accumulation", "Max bars before forced sell", "DCA")
        self._bars_since_last_buy = 0
        self._total_bars_in_position = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(best_dollar_cost_average_strategy, self).OnReseted()
        self._bars_since_last_buy = 0
        self._total_bars_in_position = 0

    def OnStarted2(self, time):
        super(best_dollar_cost_average_strategy, self).OnStarted2(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
        rsi_area = self.CreateChartArea()
        if rsi_area is not None:
            self.DrawIndicator(rsi_area, rsi)

    def OnProcess(self, candle, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        rsi_v = float(rsi_val)
        self._bars_since_last_buy += 1
        if self.Position > 0:
            self._total_bars_in_position += 1
        max_acc = self._max_accumulation_bars.Value
        if self.Position > 0 and self._total_bars_in_position >= max_acc:
            self.SellMarket()
            self._total_bars_in_position = 0
            self._bars_since_last_buy = 0
            return
        sell_level = float(self._rsi_sell_level.Value)
        interval = self._buy_interval_bars.Value
        if self.Position > 0 and rsi_v >= sell_level and self._total_bars_in_position >= interval:
            self.SellMarket()
            self._total_bars_in_position = 0
            self._bars_since_last_buy = 0
            return
        if self._bars_since_last_buy >= interval:
            self.BuyMarket()
            self._bars_since_last_buy = 0
            if self._total_bars_in_position == 0:
                self._total_bars_in_position = 1

    def CreateClone(self):
        return best_dollar_cost_average_strategy()
