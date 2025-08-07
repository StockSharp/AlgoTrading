import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Array
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order, Security
from datatype_extensions import *

class roa_effect_stocks_strategy(Strategy):
    """ROA effect stocks strategy."""

    def __init__(self):
        super(roa_effect_stocks_strategy, self).__init__()
        self._universe = self.Param("Universe", Array.Empty[Security]()) \
            .SetDisplay("Universe", "Stocks to trade", "General")
        self._decile = self.Param("Decile", 10) \
            .SetDisplay("Decile", "Number of deciles for ranking", "General")
        self._min_usd = self.Param("MinTradeUsd", 200.0) \
            .SetDisplay("Min Trade USD", "Minimum dollar value per trade", "General")
        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._weights = {}
        self._latest_prices = {}
        self._last = None

    @property
    def Universe(self):
        return self._universe.Value

    @Universe.setter
    def Universe(self, value):
        self._universe.Value = value

    @property
    def Decile(self):
        return self._decile.Value

    @Decile.setter
    def Decile(self, value):
        self._decile.Value = value

    @property
    def MinTradeUsd(self):
        return self._min_usd.Value

    @MinTradeUsd.setter
    def MinTradeUsd(self, value):
        self._min_usd.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(roa_effect_stocks_strategy, self).OnReseted()
        self._weights.clear()
        self._latest_prices.clear()
        self._last = None

    def OnStarted(self, time):
        super(roa_effect_stocks_strategy, self).OnStarted(time)
        if not self.Universe:
            raise Exception("Universe cannot be empty.")
        trig = self.Universe[0]
        self.SubscribeCandles(self.CandleType, True, trig) \
            .Bind(lambda c: self._process_trigger_candle(c, trig)) \
            .Start()

    def _process_trigger_candle(self, candle, security):
        if candle.State != CandleStates.Finished:
            return
        self._latest_prices[security] = candle.ClosePrice
        self._on_daily(candle.OpenTime.Date)

    def _on_daily(self, d):
        if self._last == d:
            return
        self._last = d
        if d.Day != 1:
            return
        self._rebalance()

    def _rebalance(self):
        roa = {}
        for s in self.Universe:
            if self._try_get_roa(s, roa):
                pass
        if len(roa) < self.Decile * 2:
            return
        bucket = len(roa) // self.Decile
        longs = [s for s,_ in sorted(roa.items(), key=lambda kv: kv[1], reverse=True)[:bucket]]
        shorts = [s for s,_ in sorted(roa.items(), key=lambda kv: kv[1])[:bucket]]
        self._weights.clear()
        wl = 1.0/len(longs)
        ws = -1.0/len(shorts)
        for s in longs:
            self._weights[s]=wl
        for s in shorts:
            self._weights[s]=ws
        for pos in self.Positions:
            if pos.Security not in self._weights:
                self._move(pos.Security,0)
        pv = self.Portfolio.CurrentValue or 0.0
        for sec,w in self._weights.items():
            price = self._latest_prices.get(sec,0)
            if price>0:
                self._move(sec, w*pv/price)

    def _move(self, sec, tgt):
        diff = tgt - (self.GetPositionValue(sec, self.Portfolio) or 0)
        price = self._latest_prices.get(sec,0)
        if price<=0 or abs(diff)*price < self.MinTradeUsd:
            return
        self.RegisterOrder(Order(Security=sec, Portfolio=self.Portfolio,
                                 Side=Sides.Buy if diff>0 else Sides.Sell,
                                 Volume=abs(diff), Type=OrderTypes.Market,
                                 Comment="ROA"))

    def _try_get_roa(self, security, roa):
        roa[security] = 0
        return True

    def CreateClone(self):
        return roa_effect_stocks_strategy()
