import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class backbone_basket_strategy(Strategy):
    def __init__(self):
        super(backbone_basket_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe used for analysis.", "General")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for ATR indicator.", "Indicators")
        self._retrace_multiplier = self.Param("RetraceMultiplier", 2.0) \
            .SetDisplay("Retrace Multiplier", "ATR multiplier for retracement threshold.", "Signals")
        self._highest_price = 0.0
        self._lowest_price = float('inf')
        self._entry_price = 0.0
        self._last_direction = 0

    @property
    def candle_type(self):
        return self._candle_type.Value
    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def atr_period(self):
        return self._atr_period.Value
    @atr_period.setter
    def atr_period(self, value):
        self._atr_period.Value = value

    @property
    def retrace_multiplier(self):
        return self._retrace_multiplier.Value
    @retrace_multiplier.setter
    def retrace_multiplier(self, value):
        self._retrace_multiplier.Value = value

    def OnReseted(self):
        super(backbone_basket_strategy, self).OnReseted()
        self._highest_price = 0.0
        self._lowest_price = float('inf')
        self._entry_price = 0.0
        self._last_direction = 0

    def OnStarted(self, time):
        super(backbone_basket_strategy, self).OnStarted(time)
        self._highest_price = 0.0
        self._lowest_price = float('inf')
        self._entry_price = 0.0
        self._last_direction = 0
        atr = AverageTrueRange()
        atr.Length = self.atr_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return
        if atr_value <= 0:
            return

        close = float(candle.ClosePrice)

        if close > self._highest_price:
            self._highest_price = close
        if close < self._lowest_price:
            self._lowest_price = close

        threshold = float(atr_value) * self.retrace_multiplier

        # Exit existing positions on retracement
        if self.Position > 0 and close < self._highest_price - threshold:
            self.SellMarket()
            self._entry_price = 0.0
            self._last_direction = 1
            self._lowest_price = close
        elif self.Position < 0 and close > self._lowest_price + threshold:
            self.BuyMarket()
            self._entry_price = 0.0
            self._last_direction = -1
            self._highest_price = close

        # Entry logic - alternate direction
        if self.Position == 0:
            if self._last_direction != 1 and close < self._highest_price - threshold:
                self.SellMarket()
                self._entry_price = close
                self._last_direction = -1
                self._lowest_price = close
            elif self._last_direction != -1 and close > self._lowest_price + threshold:
                self.BuyMarket()
                self._entry_price = close
                self._last_direction = 1
                self._highest_price = close

    def CreateClone(self):
        return backbone_basket_strategy()
