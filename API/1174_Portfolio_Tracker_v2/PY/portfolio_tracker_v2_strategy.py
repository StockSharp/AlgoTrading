import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class portfolio_tracker_v2_strategy(Strategy):
    """Portfolio tracker that logs total value and PnL."""

    def __init__(self):
        super(portfolio_tracker_v2_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._use_cash = self.Param("UseCash", True) \
            .SetDisplay("Use Cash", "Include cash in portfolio", "Cash")
        self._cash = self.Param("Cash", 10000.0) \
            .SetDisplay("Cash Amount", "Initial cash balance", "Cash")

        self._positions_data = []
        self._total_portfolio = 0.0
        self._total_pnl = 0.0

        for i, (enabled, symbol, qty, cost) in enumerate([
            (True, "MSFT", 1000.0, 100.0),
            (True, "AAPL", 1000.0, 100.0),
            (True, "INTC", 1000.0, 40.0),
            (False, "TWTR", 100.0, 50.0),
            (False, "FB", 100.0, 100.0),
            (False, "MSFT", 100.0, 100.0),
            (False, "MSFT", 100.0, 100.0),
            (False, "MSFT", 100.0, 100.0),
            (False, "MSFT", 100.0, 100.0),
            (False, "MSFT", 100.0, 100.0),
        ], start=1):
            en = self.Param("Enable{}".format(i), enabled) \
                .SetDisplay("Pos #{}".format(i), "Enable position #{}".format(i), "Position {}".format(i))
            sym = self.Param("Symbol{}".format(i), symbol) \
                .SetDisplay("Symbol #{}".format(i), "Ticker for position #{}".format(i), "Position {}".format(i))
            q = self.Param("Quantity{}".format(i), qty) \
                .SetDisplay("Qty #{}".format(i), "Quantity for position #{}".format(i), "Position {}".format(i))
            c = self.Param("Cost{}".format(i), cost) \
                .SetDisplay("Cost #{}".format(i), "Cost per share for position #{}".format(i), "Position {}".format(i))
            self._positions_data.append({
                "enabled": en, "symbol": sym, "quantity": q, "cost": c,
                "security": None, "last_price": 0.0
            })

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(portfolio_tracker_v2_strategy, self).OnReseted()
        for p in self._positions_data:
            p["last_price"] = 0.0
        self._total_portfolio = 0.0
        self._total_pnl = 0.0

    def OnStarted(self, time):
        super(portfolio_tracker_v2_strategy, self).OnStarted(time)

        for p in self._positions_data:
            if not p["enabled"].Value:
                continue
            sec = self.LookupById(str(p["symbol"].Value))
            if sec is None:
                continue
            p["security"] = sec
            self.SubscribeCandles(self.candle_type, True, sec) \
                .Bind(lambda c, pos=p: self._process_candle(c, pos)).Start()

        self._update_totals()

    def _process_candle(self, candle, p):
        if candle.State != CandleStates.Finished:
            return
        p["last_price"] = float(candle.ClosePrice)
        self._update_totals()

    def _update_totals(self):
        total_value = float(self._cash.Value) if self._use_cash.Value else 0.0
        total_cost = 0.0

        for p in self._positions_data:
            if not p["enabled"].Value:
                continue
            value = p["last_price"] * float(p["quantity"].Value)
            cost = float(p["cost"].Value) * float(p["quantity"].Value)
            total_value += value
            total_cost += cost

        pnl = total_value - total_cost
        self._total_portfolio = total_value
        self._total_pnl = pnl

    def CreateClone(self):
        return portfolio_tracker_v2_strategy()
