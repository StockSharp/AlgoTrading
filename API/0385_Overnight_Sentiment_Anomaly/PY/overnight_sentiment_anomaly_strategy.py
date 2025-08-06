import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import Math
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class overnight_sentiment_anomaly_strategy(Strategy):
    """Trades an equity ETF overnight based on external sentiment indicator."""

    def __init__(self):
        super().__init__()
        self._sentiment = self.Param("SentimentSymbol", None) \
            .SetDisplay("Sentiment Symbol", "Symbol providing sentiment", "Universe")
        self._threshold = self.Param("Threshold", 0.0) \
            .SetDisplay("Threshold", "Sentiment threshold", "Parameters")
        self._min_usd = self.Param("MinTradeUsd", 200.0) \
            .SetDisplay("Min USD", "Minimum trade value", "Risk")
        self._tf = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._latest = {}

    @property
    def sentiment_symbol(self):
        return self._sentiment.Value

    @sentiment_symbol.setter
    def sentiment_symbol(self, value):
        self._sentiment.Value = value

    @property
    def threshold(self):
        return self._threshold.Value

    @threshold.setter
    def threshold(self, value):
        self._threshold.Value = value

    @property
    def min_trade_usd(self):
        return self._min_usd.Value

    @min_trade_usd.setter
    def min_trade_usd(self, value):
        self._min_usd.Value = value

    @property
    def candle_type(self):
        return self._tf.Value

    @candle_type.setter
    def candle_type(self, value):
        self._tf.Value = value

    def GetWorkingSecurities(self):
        if self.Security is None:
            raise Exception("EquityETF not set")
        yield self.Security, self.candle_type

    def OnReseted(self):
        super().OnReseted()
        self._latest.clear()

    def OnStarted(self, time):
        super().OnStarted(time)
        if self.Security is None or self.sentiment_symbol is None:
            raise Exception("Security and SentimentSymbol must be set")
        self.SubscribeCandles(self.candle_type, True, self.Security) \
            .Bind(lambda c, s=self.Security: self._process(c, s)) \
            .Start()

    def _process(self, candle, sec):
        if candle.State != CandleStates.Finished:
            return
        self._latest[sec] = candle.ClosePrice
        self._on_minute(candle)

    def _on_minute(self, candle):
        utc = candle.OpenTime.UtcDateTime
        if utc.Hour == 20 and utc.Minute == 55:
            self._close_entry()
        elif utc.Hour == 14 and utc.Minute == 35:
            self._open_exit()

    def _close_entry(self):
        ok, s_val = self._try_get_sentiment()
        if not ok or s_val < self.threshold:
            return
        port = self.Portfolio.CurrentValue or 0.0
        price = self._latest.get(self.Security, 0)
        if price <= 0:
            return
        qty = port / price
        if qty * price < self.min_trade_usd:
            return
        self._move(qty)

    def _open_exit(self):
        self._move(0)

    def _move(self, tgt):
        diff = tgt - self._pos()
        price = self._latest.get(self.Security, 0)
        if price <= 0 or abs(diff) * price < self.min_trade_usd:
            return
        side = Sides.Buy if diff > 0 else Sides.Sell
        from StockSharp.BusinessEntities import Order
        self.RegisterOrder(Order(Security=self.Security, Portfolio=self.Portfolio,
                                 Side=side, Volume=abs(diff), Type=OrderTypes.Market,
                                 Comment="OvernightSent"))

    def _pos(self):
        val = self.GetPositionValue(self.Security, self.Portfolio)
        return val or 0.0

    def _try_get_sentiment(self):
        # placeholder: return (False, 0)
        return (False, 0)

    def CreateClone(self):
        return overnight_sentiment_anomaly_strategy()
