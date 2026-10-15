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
from StockSharp.BusinessEntities import Security, Order, EntitiesExtensions


CURRENCIES = ["USD", "EUR", "GBP", "CHF", "JPY", "AUD", "CAD", "NZD"]


class ccfp_currency_strength_strategy(Strategy):
    def __init__(self):
        super(ccfp_currency_strength_strategy, self).__init__()

        self._ids = {
            "EURUSD": self.Param("EURUSD", "EURUSD").SetDisplay("EURUSD", "EUR/USD security id.", "Securities"),
            "GBPUSD": self.Param("GBPUSD", "GBPUSD").SetDisplay("GBPUSD", "GBP/USD security id.", "Securities"),
            "AUDUSD": self.Param("AUDUSD", "AUDUSD").SetDisplay("AUDUSD", "AUD/USD security id.", "Securities"),
            "NZDUSD": self.Param("NZDUSD", "NZDUSD").SetDisplay("NZDUSD", "NZD/USD security id.", "Securities"),
            "USDCAD": self.Param("USDCAD", "USDCAD").SetDisplay("USDCAD", "USD/CAD security id.", "Securities"),
            "USDCHF": self.Param("USDCHF", "USDCHF").SetDisplay("USDCHF", "USD/CHF security id.", "Securities"),
            "USDJPY": self.Param("USDJPY", "USDJPY").SetDisplay("USDJPY", "USD/JPY security id.", "Securities"),
        }
        self._fast_ma = self.Param("FastMa", 5).SetGreaterThanZero().SetDisplay("Fast MA", "Fast SMA period.", "Indicators")
        self._slow_ma = self.Param("SlowMa", 20).SetGreaterThanZero().SetDisplay("Slow MA", "Slow SMA period.", "Indicators")
        self._strength_step = self.Param("StrengthStep", 0.001).SetGreaterThanZero().SetDisplay("Strength Step", "Minimum top/down strength spread.", "Signal")
        self._close_opposite = self.Param("CloseOpposite", True).SetDisplay("Close Opposite", "Close opposite exposure before entering.", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Common timeframe.", "General")

        self._pairs = {}
        self._previous_strengths = {}
        self._last_evaluation_time = None

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
        sec = EntitiesExtensions.LookupById(self, sec_id)
        if sec is None:
            sec = Security()
            sec.Id = sec_id
        return sec

    def GetWorkingSecurities(self):
        return [(self._security(sec_id), self._candle_type.Value) for _, sec_id, _, _ in self._definitions()]

    def OnReseted(self):
        super(ccfp_currency_strength_strategy, self).OnReseted()
        self._pairs = {}
        self._previous_strengths = {}
        self._last_evaluation_time = None

    def OnStarted2(self, time):
        super(ccfp_currency_strength_strategy, self).OnStarted2(time)

        for name, sec_id, base, quote in self._definitions():
            security = self._security(sec_id)
            fast = SimpleMovingAverage()
            fast.Length = int(self._fast_ma.Value)
            slow = SimpleMovingAverage()
            slow.Length = int(self._slow_ma.Value)

            state = {"security": security, "base": base, "quote": quote, "ratio": 0.0, "ready": False, "time": None}
            self._pairs[name] = state

            def make_handler(pair_state, fast_indicator, slow_indicator):
                def handler(candle, fast_value, slow_value):
                    if candle.State != CandleStates.Finished or not fast_indicator.IsFormed or not slow_indicator.IsFormed:
                        return
                    fast_v = float(fast_value)
                    slow_v = float(slow_value)
                    if fast_v == 0 or slow_v == 0:
                        return
                    pair_state["ratio"] = fast_v / slow_v
                    pair_state["ready"] = True
                    pair_state["time"] = candle.OpenTime

                    if not all(p["ready"] for p in self._pairs.values()):
                        return

                    times = {p["time"] for p in self._pairs.values()}
                    if len(times) != 1:
                        return

                    current_time = next(iter(times))
                    if current_time is None or current_time == self._last_evaluation_time:
                        return

                    self._last_evaluation_time = current_time
                    self._evaluate()
                return handler

            self.SubscribeCandles(self._candle_type.Value, security=security).Bind(
                fast, slow, make_handler(state, fast, slow)).Start()

    def _evaluate(self):
        # Fast/slow ratio of each currency against USD; a cross ratio is the quotient of two of them.
        ratios = {"USD": 1.0}
        for pair in self._pairs.values():
            if pair["quote"] == "USD":
                ratios[pair["base"]] = pair["ratio"]
            else:
                ratios[pair["quote"]] = 1.0 / pair["ratio"]

        strengths = {}
        for currency in CURRENCIES:
            strength = 0.0
            for other in CURRENCIES:
                if other != currency:
                    strength += ratios[currency] / ratios[other] - 1.0
            strengths[currency] = strength

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
        position_value = self.GetPositionValue(security, self.Portfolio)
        position = float(position_value) if position_value is not None else 0.0

        if (side == Sides.Buy and position > 0) or (side == Sides.Sell and position < 0):
            return

        if position != 0 and bool(self._close_opposite.Value):
            self._register_market(security, side, abs(position))

        self._register_market(security, side, float(self.Volume))

    def _register_market(self, security, side, volume):
        order = Order()
        order.Security = security
        order.Portfolio = self.Portfolio
        order.Type = OrderTypes.Market
        order.Side = side
        order.Volume = volume
        order.Comment = "(TOPDOWN)"
        self.RegisterOrder(order)

    def CreateClone(self):
        return ccfp_currency_strength_strategy()
