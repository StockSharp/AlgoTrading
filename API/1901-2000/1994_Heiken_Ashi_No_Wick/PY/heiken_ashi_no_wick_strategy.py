import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, OrderStates
from StockSharp.Algo.Strategies import Strategy


class heiken_ashi_no_wick_strategy(Strategy):
    """Contrarian Heiken Ashi no-wick pattern on completed candles only."""

    def __init__(self):
        super(heiken_ashi_no_wick_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._previous_ha_open = None
        self._previous_ha_close = None
        self._previous_body = None
        self._pending_order = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(heiken_ashi_no_wick_strategy, self).OnReseted()
        self._previous_ha_open = None
        self._previous_ha_close = None
        self._previous_body = None
        self._pending_order = None

    def OnStarted2(self, time):
        super(heiken_ashi_no_wick_strategy, self).OnStarted2(time)
        self._previous_ha_open = None
        self._previous_ha_close = None
        self._previous_body = None
        self._pending_order = None
        self.SubscribeCandles(self.CandleType).Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        c = float(candle.ClosePrice)
        ha_close = (o + h + l + c) / 4.0
        if self._previous_ha_open is None:
            ha_open = (o + c) / 2.0
        else:
            ha_open = (self._previous_ha_open + self._previous_ha_close) / 2.0

        body = abs(ha_close - ha_open)
        larger = self._previous_body is not None and body > self._previous_body
        bullish_no_lower = ha_close > ha_open and l >= ha_open
        bearish_no_upper = ha_close < ha_open and h <= ha_open

        # Update the HA recurrence even if the strategy cannot place an order.
        self._previous_ha_open = ha_open
        self._previous_ha_close = ha_close
        self._previous_body = body

        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return

        # A bullish no-lower-wick candle closes a contrarian long, and a bearish
        # no-upper-wick candle closes a contrarian short. The stronger pattern
        # also enters the opposite side, with one atomic reversing order.
        if bullish_no_lower:
            if self.Position > 0:
                self._pending_order = self.SellMarket(self.Position + (self.Volume if larger else 0))
            elif self.Position == 0 and larger:
                self._pending_order = self.SellMarket(self.Volume)
        elif bearish_no_upper:
            if self.Position < 0:
                self._pending_order = self.BuyMarket(-self.Position + (self.Volume if larger else 0))
            elif self.Position == 0 and larger:
                self._pending_order = self.BuyMarket(self.Volume)

    def CreateClone(self):
        return heiken_ashi_no_wick_strategy()
