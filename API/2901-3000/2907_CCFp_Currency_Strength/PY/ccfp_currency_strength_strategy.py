import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security, Order


class ccfp_currency_strength_strategy(Strategy):
    def __init__(self):
        super(ccfp_currency_strength_strategy, self).__init__()

        self._ids = {
            "EURUSD": self.Param("EURUSD", "EURUSD"),
            "GBPUSD": self.Param("GBPUSD", "GBPUSD"),
            "AUDUSD": self.Param("AUDUSD", "AUDUSD"),
            "NZDUSD": self.Param("NZDUSD", "NZDUSD"),
            "USDCAD": self.Param("USDCAD", "USDCAD"),
            "USDCHF": self.Param("USDCHF", "USDCHF"),
            "USDJPY": self.Param("USDJPY", "USDJPY"),
        }
        self._fast_ma = self.Param("FastMa", 5).SetGreaterThanZero()
        self._slow_ma = self.Param("SlowMa", 20).SetGreaterThanZero()
        self._strength_step = self.Param("StrengthStep", 0.001).SetGreaterThanZero()
        self._close_opposite = self.Param("CloseOpposite", True)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._pairs = {}
        self._previous_strengths = {}
        self._directions = {}

    def _definitions(self):
        return [
            ("EURUSD", str(self._ids["EURUSD"].Value), "EUR", "USD"),
            ("GBPUSD", str(self._ids["GBPUSD"].Value), "GBP", "USD"),
            ("AUDUSD", str(self._ids["AUDUSD"].Value), "AUD", "USD"),
            ("NZDUSD", str(self._ids["NZDUSD"].Value), "NZD", "USD"),
            ("USDCAD", str(self._ids["USDCAD"].Value), "USD", "CAD"),
            ("USDCHF", str(self._ids["USDCHF"].Value), "USD", "CHF"),
            ("USDJPY", str(self._ids["USDJPY"].Value), "USD", "JPY"),
        ]

    def _security(self, sec_id):
        sec = Security()
        sec.Id = sec_id
        return sec

    def GetWorkingSecurities(self):
        return [(self._security(sec_id), self._candle_type.Value) for _, sec_id, _, _ in self._definitions()]

    def OnReseted(self):
        super(ccfp_currency_strength_strategy, self).OnReseted()
        self._pairs = {}
        self._previous_strengths = {}
        self._directions = {}

    def OnStarted2(self, time):
        super(ccfp_currency_strength_strategy, self).OnStarted2(time)

        for name, sec_id, base, quote in self._definitions():
            security = self._security(sec_id)
            fast = SimpleMovingAverage()
            fast.Length = int(self._fast_ma.Value)
            slow = SimpleMovingAverage()
            slow.Length = int(self._slow_ma.Value)

            state = {"security": security, "base": base, "quote": quote, "ratio": 0.0, "ready": False}
            self._pairs[name] = state

            def make_handler(pair_state, fast_indicator, slow_indicator):
                def handler(candle, fast_value, slow_value):
                    if candle.State != CandleStates.Finished or not fast_indicator.IsFormed or not slow_indicator.IsFormed:
                        return
                    slow_v = float(slow_value)
                    if slow_v == 0:
                        return
                    pair_state["ratio"] = (float(fast_value) - slow_v) / slow_v
                    pair_state["ready"] = True
                    if all(p["ready"] for p in self._pairs.values()):
                        self._evaluate()
                return handler

            self.SubscribeCandles(self._candle_type.Value, security=security).Bind(
                fast, slow, make_handler(state, fast, slow)).Start()

    def _evaluate(self):
        strengths = {c: 0.0 for c in ["USD", "EUR", "GBP", "CHF", "JPY", "AUD", "CAD", "NZD"]}

        for pair in self._pairs.values():
            strengths[pair["base"]] += pair["ratio"]
            strengths[pair["quote"]] -= pair["ratio"]

        top = max(strengths, key=strengths.get)
        down = min(strengths, key=strengths.get)
        spread = strengths[top] - strengths[down]

        if len(self._previous_strengths) == len(strengths):
            previous_spread = self._previous_strengths[top] - self._previous_strengths[down]
            if (previous_spread < float(self._strength_step.Value) <= spread and
                    strengths[top] > self._previous_strengths[top] and
                    strengths[down] < self._previous_strengths[down]):
                self._trade_spread(top, down)

        self._previous_strengths = dict(strengths)

    def _trade_spread(self, top, down):
        if top == "USD":
            self._trade_against_usd(down, False)
        elif down == "USD":
            self._trade_against_usd(top, True)
        else:
            self._trade_against_usd(top, True)
            self._trade_against_usd(down, False)

    def _trade_against_usd(self, currency, want_currency_long):
        pair = None
        for candidate in self._pairs.values():
            if ((candidate["base"] == currency and candidate["quote"] == "USD") or
                    (candidate["base"] == "USD" and candidate["quote"] == currency)):
                pair = candidate
                break

        if pair is None:
            return

        currency_is_base = pair["base"] == currency
        buy_pair = want_currency_long == currency_is_base
        self._submit(pair["security"], Sides.Buy if buy_pair else Sides.Sell)

    def _submit(self, security, side):
        desired = 1 if side == Sides.Buy else -1
        current = self._directions.get(security.Id, 0)
        volume = float(self.Volume)

        if current != 0 and current != desired:
            if not bool(self._close_opposite.Value):
                return
            volume *= 2.0
        elif current == desired:
            return

        order = Order()
        order.Security = security
        order.Portfolio = self.Portfolio
        order.Type = OrderTypes.Market
        order.Side = side
        order.Volume = volume
        order.Comment = "(TOPDOWN)"
        self.RegisterOrder(order)
        self._directions[security.Id] = desired

    def CreateClone(self):
        return ccfp_currency_strength_strategy()
