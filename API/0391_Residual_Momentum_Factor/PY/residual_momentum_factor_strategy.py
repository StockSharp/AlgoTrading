import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order
from datatype_extensions import *

class residual_momentum_factor_strategy(Strategy):
    """Residual momentum factor strategy."""

    def __init__(self):
        super(residual_momentum_factor_strategy, self).__init__()

        self._universe = self.Param("Universe", list()) \
            .SetDisplay("Universe", "Securities to trade", "General")
        self._decile = self.Param("Decile", 10) \
            .SetDisplay("Decile", "Number of deciles for ranking", "General")
        self._min_usd = self.Param("MinTradeUsd", 200.0) \
            .SetDisplay("Min Trade USD", "Minimum dollar value per trade", "General")
        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._weights = {}
        self._latest_prices = {}
        self._last = None

    # region properties
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
    # endregion

    def OnReseted(self):
        super(residual_momentum_factor_strategy, self).OnReseted()
        self._weights.clear()
        self._latest_prices.clear()
        self._last = None

    def OnStarted(self, time):
        super(residual_momentum_factor_strategy, self).OnStarted(time)
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

    def _on_daily(self, day):
        if self._last == day:
            return
        self._last = day
        if day.Day != 1:
            return
        self._rebalance()

    def _rebalance(self):
        score = {}
        for s in self.Universe:
            if self._try_get_residual_momentum(s, score):
                pass
        if len(score) < self.Decile * 2:
            return
        bucket = len(score) // self.Decile
        longs = [s for s, _ in sorted(score.items(), key=lambda kv: kv[1], reverse=True)[:bucket]]
        shorts = [s for s, _ in sorted(score.items(), key=lambda kv: kv[1])[:bucket]]
        self._weights.clear()
        wl = 1.0 / len(longs)
        ws = -1.0 / len(shorts)
        for s in longs:
            self._weights[s] = wl
        for s in shorts:
            self._weights[s] = ws
        for pos in self.Positions:
            if pos.Security not in self._weights:
                self._move(pos.Security, 0)
        pv = self.Portfolio.CurrentValue or 0.0
        for sec, w in self._weights.items():
            price = self._latest_prices.get(sec, 0)
            if price > 0:
                self._move(sec, w * pv / price)

    def _move(self, sec, tgt):
        diff = tgt - (self.GetPositionValue(sec, self.Portfolio) or 0)
        price = self._latest_prices.get(sec, 0)
        if price <= 0 or abs(diff) * price < self.MinTradeUsd:
            return
        self.RegisterOrder(Order(Security=sec, Portfolio=self.Portfolio,
                                 Side=Sides.Buy if diff > 0 else Sides.Sell,
                                 Volume=abs(diff), Type=OrderTypes.Market,
                                 Comment="ResMom"))

    def _try_get_residual_momentum(self, security, score):
        # Placeholder for external residual momentum feed
        score[security] = 0
        return True

    def CreateClone(self):
        return residual_momentum_factor_strategy()
