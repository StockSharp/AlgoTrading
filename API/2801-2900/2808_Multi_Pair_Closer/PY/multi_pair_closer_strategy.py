import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Math, Decimal, DateTime, Action, InvalidOperationException
from System.Globalization import CultureInfo
from StockSharp.Messages import DataType, CandleStates, OrderStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Position, EntitiesExtensions

PROFIT_TARGET_REASON = "reached the profit target"
MAX_LOSS_REASON = "fell below the loss limit"


class multi_pair_closer_strategy(Strategy):
    """
    Supervises the account positions of a basket of instruments and closes them when their combined floating
    profit, as the connector reports it, reaches the profit target or drops below the loss limit.
    This utility never opens positions.
    """

    def __init__(self):
        super(multi_pair_closer_strategy, self).__init__()

        self._watched_symbols = self.Param("WatchedSymbols", "GBPUSD,USDCAD,USDCHF,USDSEK") \
            .SetDisplay("Watched Symbols", "Comma-separated security identifiers to supervise; empty means the assigned security", "Basket")
        self._profit_target = self.Param("ProfitTarget", 60.0) \
            .SetNotNegative() \
            .SetDisplay("Profit Target", "Combined floating profit in portfolio currency that closes every watched position", "Risk")
        self._max_loss = self.Param("MaxLoss", 60.0) \
            .SetNotNegative() \
            .SetDisplay("Max Loss", "Maximum acceptable combined floating loss in portfolio currency before the basket is force-closed", "Risk")
        self._slippage = self.Param("Slippage", 10) \
            .SetNotNegative() \
            .SetDisplay("Slippage", "Slippage allowed by the original script; exits are market orders, so it is only logged", "Execution")
        self._min_age_seconds = self.Param("MinAgeSeconds", 60) \
            .SetNotNegative() \
            .SetDisplay("Min Age (s)", "Minimum lifetime of a position before the strategy may close it", "Execution")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Every finished candle of this type triggers a profit evaluation", "General")

        self._watched = []
        self._first_seen = {}
        self._exit_orders = {}
        self._position_source = None
        self._start_time = None
        self._position_handler = Action[Position](self._on_reported_position_changed)

    def GetWorkingSecurities(self):
        return [(security, self._candle_type.Value) for security in self._resolve_watched(False)]

    def OnReseted(self):
        super(multi_pair_closer_strategy, self).OnReseted()

        self._detach_position_source()
        self._watched = []
        self._first_seen = {}
        self._exit_orders = {}
        self._start_time = None

    def OnStarted2(self, time):
        super(multi_pair_closer_strategy, self).OnStarted2(time)

        self._watched = self._resolve_watched(True)
        self._first_seen = {}
        self._exit_orders = {}

        # The start is taken on the market clock, which a backtest sets only once the replay begins.
        self._start_time = self._market_time()

        for security in self._watched:
            volume, _ = self._reported_position(security)
            if volume != Decimal.Zero:
                self._first_seen[security.Id.upper()] = self._start_time

        self._position_source = self.Connector
        self._position_source.PositionChanged += self._position_handler

        for security in self._watched:
            self.SubscribeCandles(self._candle_type.Value, security=security) \
                .Bind(self._process_candle) \
                .Start()

    def OnStopped(self):
        self._detach_position_source()

        super(multi_pair_closer_strategy, self).OnStopped()

    def _detach_position_source(self):
        if self._position_source is None:
            return

        self._position_source.PositionChanged -= self._position_handler
        self._position_source = None

    def _resolve_watched(self, strict):
        ids = []
        seen = set()
        for item in str(self._watched_symbols.Value or "").split(","):
            sec_id = item.strip()
            if sec_id and sec_id.upper() not in seen:
                seen.add(sec_id.upper())
                ids.append(sec_id)

        securities = []

        if not ids:
            if self.Security is not None:
                securities.append(self.Security)
            elif strict:
                raise InvalidOperationException("WatchedSymbols is empty and no Security is assigned.")

            return securities

        if self.Connector is None:
            return securities

        for sec_id in ids:
            # IronPython does not see C# extension methods, so the lookup is called on its static class.
            security = EntitiesExtensions.LookupById(self, sec_id)

            if security is not None:
                securities.append(security)
            elif strict:
                raise InvalidOperationException("Security '{0}' is not available through the connector.".format(sec_id))

        return securities

    def _on_reported_position_changed(self, position):
        security = self._find_watched(position)

        if security is None:
            return

        key = security.Id.upper()
        volume, _ = self._reported_position(security)

        if volume == Decimal.Zero:
            self._first_seen.pop(key, None)
        elif key not in self._first_seen:
            now = self._market_time()
            self._first_seen[key] = now if now is not None else self._start_time

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._evaluate_basket()

    def _evaluate_basket(self):
        now = self.CurrentTime
        if self._start_time is None:
            self._start_time = now

        summary = []
        open_positions = []
        total = Decimal.Zero

        for security in self._watched:
            volume, profit = self._reported_position(security)

            if volume == Decimal.Zero:
                summary.append("{0}: {1}".format(security.Id, self._format(Decimal.Zero)))
                continue

            open_positions.append((security, volume))
            total = None if total is None or profit is None else Decimal.Add(total, profit)
            summary.append("{0}: {1}".format(security.Id, "n/a" if profit is None else self._format(profit)))

        summary.append("Basket: {0}".format("n/a" if total is None else self._format(total)))
        self.LogInfo("; ".join(summary))

        # A position without a reported floating profit leaves the basket result unknown, so nothing is decided.
        if not open_positions or total is None:
            return

        if total >= Decimal(float(self._profit_target.Value)):
            reason = PROFIT_TARGET_REASON
        elif total < Decimal.Negate(Decimal(float(self._max_loss.Value))):
            reason = MAX_LOSS_REASON
        else:
            return

        min_age = int(self._min_age_seconds.Value)

        for security, volume in open_positions:
            key = security.Id.upper()
            pending = self._exit_orders.get(key)

            if pending is not None and pending.State != OrderStates.Done and pending.State != OrderStates.Failed:
                continue

            first_seen = self._first_seen.get(key)
            if first_seen is None:
                first_seen = self._start_time

            if (now - first_seen).TotalSeconds < min_age:
                continue

            quantity = Math.Abs(volume)
            self.LogInfo("Closing {0}: basket {1} {2}, {3} {4} at market (slippage {5}).".format(
                security.Id, self._format(total), reason, "sell" if volume > Decimal.Zero else "buy",
                self._format(quantity), int(self._slippage.Value)))

            if volume > Decimal.Zero:
                self._exit_orders[key] = self.SellMarket(quantity, security)
            else:
                self._exit_orders[key] = self.BuyMarket(quantity, security)

    def _find_watched(self, position):
        if not self._is_account_position(position):
            return None

        position_id = position.Security.Id.upper()

        for security in self._watched:
            if security.Id.upper() == position_id:
                return security

        return None

    # Floating profit is summed as the connector reports it; one missing value makes the whole sum unknown.
    def _reported_position(self, security):
        volume = Decimal.Zero
        profit = Decimal.Zero
        security_id = security.Id.upper()

        for position in self.Connector.Positions:
            if not self._is_account_position(position) or position.Security.Id.upper() != security_id:
                continue

            value = position.CurrentValue
            if value is None or value == Decimal.Zero:
                continue

            volume = Decimal.Add(volume, value)
            pnl = position.UnrealizedPnL
            profit = None if profit is None or pnl is None else Decimal.Add(profit, pnl)

        return volume, profit

    # Rows a connector keeps per strategy repeat part of the account row, so only account rows are read.
    def _is_account_position(self, position):
        portfolio = self.Portfolio

        if position.Security is None or position.StrategyId or portfolio is None:
            return False

        name = position.PortfolioName
        return name is not None and name.upper() == portfolio.Name.upper()

    def _market_time(self):
        now = self.CurrentTime
        return None if now == DateTime.MinValue else now

    @staticmethod
    def _format(value):
        return value.ToString(CultureInfo.InvariantCulture)

    def CreateClone(self):
        return multi_pair_closer_strategy()
