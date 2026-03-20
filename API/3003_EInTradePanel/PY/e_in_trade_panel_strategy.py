import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class e_in_trade_panel_strategy(Strategy):
    def __init__(self):
        super(e_in_trade_panel_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR lookback", "Indicators")
        self._multiplier = self.Param("Multiplier", 1.5) \
            .SetDisplay("Multiplier", "ATR multiplier for breakout", "Indicators")

        self._prev_close = None
        self._prev_atr = None

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def AtrPeriod(self):
        return self._atr_period.Value
    @property
    def Multiplier(self):
        return self._multiplier.Value

    def OnReseted(self):
        super(e_in_trade_panel_strategy, self).OnReseted()
        self._prev_close = None
        self._prev_atr = None

    def OnStarted(self, time):
        super(e_in_trade_panel_strategy, self).OnStarted(time)
        self._prev_close = None
        self._prev_atr = None

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(atr, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return
        av = float(atr_value)
        close = float(candle.ClosePrice)
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_close = close
            self._prev_atr = av
            return
        if self._prev_close is None or self._prev_atr is None:
            self._prev_close = close
            self._prev_atr = av
            return
        threshold = self._prev_atr * float(self.Multiplier)
        diff = close - self._prev_close
        if diff > threshold and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif diff < -threshold and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_close = close
        self._prev_atr = av

    def CreateClone(self):
        return e_in_trade_panel_strategy()
