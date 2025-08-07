import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Array, Math
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order, Security
from datatype_extensions import *

class return_asymmetry_commodity_strategy(Strategy):
    """Commodity return asymmetry strategy."""

    class Win:
        def __init__(self):
            self.px = []

    def __init__(self):
        super(return_asymmetry_commodity_strategy, self).__init__()

        self._futs = self.Param("Futures", Array.Empty[Security]()) \
            .SetDisplay("Futures", "Commodity futures to trade", "General")
        self._window = self.Param("WindowDays", 120) \
            .SetDisplay("Window", "Lookback window in days", "General")
        self._top = self.Param("TopN", 5) \
            .SetDisplay("Top N", "Number of instruments to long/short", "General")
        self._min_usd = self.Param("MinTradeUsd", 200.0) \
            .SetDisplay("Min Trade USD", "Minimum dollar value per trade", "General")
        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._map = {}
        self._latest_prices = {}
        self._last_day = None
        self._weights = {}

    # properties
    @property
    def Futures(self):
        return self._futs.Value

    @Futures.setter
    def Futures(self, value):
        self._futs.Value = value

    @property
    def WindowDays(self):
        return self._window.Value

    @WindowDays.setter
    def WindowDays(self, value):
        self._window.Value = value

    @property
    def TopN(self):
        return self._top.Value

    @TopN.setter
    def TopN(self, value):
        self._top.Value = value

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
        super(return_asymmetry_commodity_strategy, self).OnReseted()
        self._map.clear()
        self._latest_prices.clear()
        self._last_day = None
        self._weights.clear()

    def OnStarted(self, time):
        super(return_asymmetry_commodity_strategy, self).OnStarted(time)
        if not self.Futures:
            raise Exception("Futures cannot be empty.")
        for sec in self.Futures:
            self._map[sec] = self.Win()
            self.SubscribeCandles(self.CandleType, True, sec) \
                .Bind(lambda c, s=sec: self._process_candle(c, s)) \
                .Start()

    def _process_candle(self, candle, sec):
        if candle.State != CandleStates.Finished:
            return
        self._latest_prices[sec] = candle.ClosePrice
        self._on_daily(sec, candle)

    def _on_daily(self, sec, c):
        w = self._map[sec].px
        if len(w) == self.WindowDays:
            w.pop(0)
        w.append(c.ClosePrice)
        d = c.OpenTime.Date
        if d == self._last_day:
            return
        self._last_day = d
        if d.Day != 1:
            return
        self._rebalance()

    def _rebalance(self):
        asym = {}
        for sec, win in self._map.items():
            q = win.px
            if len(q) < self.WindowDays:
                continue
            pos = 0
            neg = 0
            for i in range(1, len(q)):
                r = (q[i] - q[i-1]) / q[i-1]
                if r > 0:
                    pos += r
                else:
                    neg += r
            if neg != 0:
                asym[sec] = pos / abs(neg)
        if len(asym) < self.TopN * 2:
            return
        longs = [s for s,_ in sorted(asym.items(), key=lambda kv: kv[1], reverse=True)[:self.TopN]]
        shorts = [s for s,_ in sorted(asym.items(), key=lambda kv: kv[1])[:self.TopN]]
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
                                 Comment="AsymCom"))

    def CreateClone(self):
        return return_asymmetry_commodity_strategy()
