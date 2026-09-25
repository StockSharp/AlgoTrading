import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security, Order


class multi_pair_closer_strategy(Strategy):
    def __init__(self):
        super(multi_pair_closer_strategy, self).__init__()

        self._watched_symbols = self.Param("WatchedSymbols", "GBPUSD,USDCAD,USDCHF,USDSEK")
        self._profit_target = self.Param("ProfitTarget", 60.0).SetNotNegative()
        self._max_loss = self.Param("MaxLoss", 60.0).SetNotNegative()
        self._slippage = self.Param("Slippage", 10).SetNotNegative()
        self._min_age_seconds = self.Param("MinAgeSeconds", 60).SetNotNegative()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1)))

        self._watched = {}
        self._first_seen = {}

    def _resolve_watched(self):
        raw = str(self._watched_symbols.Value or "")
        ids = [item.strip() for item in raw.split(",") if item.strip()]
        if not ids:
            return [self.Security] if self.Security is not None else []

        result = []
        for sec_id in ids:
            sec = self.LookupById(sec_id)
            if sec is None:
                sec = Security()
                sec.Id = sec_id
            result.append(sec)
        return result

    def GetWorkingSecurities(self):
        return [(sec, self._candle_type.Value) for sec in self._resolve_watched()]

    def OnReseted(self):
        super(multi_pair_closer_strategy, self).OnReseted()
        self._watched = {}
        self._first_seen = {}

    def OnStarted2(self, time):
        super(multi_pair_closer_strategy, self).OnStarted2(time)

        self._watched = {}
        for security in self._resolve_watched():
            self._watched[security.Id] = security

            def make_handler(sec):
                def handler(candle):
                    if candle.State == CandleStates.Finished:
                        self._evaluate_basket(candle.CloseTime)
                return handler

            self.SubscribeCandles(self._candle_type.Value, security=security).Bind(make_handler(security)).Start()

    def _evaluate_basket(self, time):
        active = []
        for sec_id, security in self._watched.items():
            pos_value = self.GetPositionValue(security, self.Portfolio)
            value = float(pos_value) if pos_value is not None else 0.0

            if value == 0:
                self._first_seen.pop(sec_id, None)
                continue

            if sec_id not in self._first_seen:
                self._first_seen[sec_id] = time
            active.append((sec_id, security, value))

        if not active:
            return

        # Strategy.PnL is the high-level aggregate available to Python strategies.
        # The C# implementation additionally reads each Position.UnrealizedPnL.
        total_pnl = float(self.PnL)
        should_close = (
            float(self._profit_target.Value) >= 0 and total_pnl >= float(self._profit_target.Value)
        ) or (
            float(self._max_loss.Value) > 0 and total_pnl <= -float(self._max_loss.Value)
        )

        if not should_close:
            return

        min_age = int(self._min_age_seconds.Value)
        for sec_id, security, value in active:
            first_seen = self._first_seen[sec_id]
            if min_age > 0 and (time - first_seen).TotalSeconds < min_age:
                continue

            order = Order()
            order.Security = security
            order.Portfolio = self.Portfolio
            order.Type = OrderTypes.Market
            order.Side = Sides.Sell if value > 0 else Sides.Buy
            order.Volume = abs(value)
            order.Comment = "MultiPairCloser slippage={}".format(int(self._slippage.Value))
            self.RegisterOrder(order)

    def CreateClone(self):
        return multi_pair_closer_strategy()
