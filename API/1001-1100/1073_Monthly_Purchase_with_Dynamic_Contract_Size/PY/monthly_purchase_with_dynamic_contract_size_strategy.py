import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, DateTimeOffset, Decimal, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class monthly_purchase_with_dynamic_contract_size_strategy(Strategy):
    """
    Monthly purchase strategy with dynamic contract size.
    From StartDate on, buys on the candle whose day of month equals BuyDay. The size is PercentOfEquity of the
    current portfolio value divided by the close price. Positions are never closed; the equity drawdown is only tracked.
    """

    def __init__(self):
        super(monthly_purchase_with_dynamic_contract_size_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromDays(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._start_date = self.Param("StartDate", DateTimeOffset(2010, 1, 1, 0, 0, 0, TimeSpan.Zero)).SetDisplay("Start Date", "Purchases are allowed from this date on", "General")
        self._percent_of_equity = self.Param("PercentOfEquity", 0.03).SetGreaterThanZero().SetDisplay("Percent of Equity", "Fraction of equity spent on each purchase", "Trading")
        self._buy_day = self.Param("BuyDay", 1).SetRange(1, 31).SetDisplay("Buy Day", "Day of the month to buy", "Trading")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def max_drawdown(self):
        return self._max_drawdown

    def _reset_state(self):
        self._last_buy_date = None
        self._peak_equity = Decimal(0)
        self._max_drawdown = Decimal(0)

    def OnReseted(self):
        super(monthly_purchase_with_dynamic_contract_size_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(monthly_purchase_with_dynamic_contract_size_strategy, self).OnStarted2(time)

        self._reset_state()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        equity = Decimal(0)
        if self.Portfolio is not None and self.Portfolio.CurrentValue is not None:
            equity = self.Portfolio.CurrentValue
        if equity > self._peak_equity:
            self._peak_equity = equity
        if self._peak_equity > 0:
            self._max_drawdown = max(self._max_drawdown, (self._peak_equity - equity) / self._peak_equity)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        open_time = candle.OpenTime
        if open_time < self._start_date.Value.UtcDateTime or open_time.Day != self._buy_day.Value or self._last_buy_date == open_time.Date:
            return

        close = candle.ClosePrice
        if close <= 0 or equity <= 0:
            return

        step = Decimal(1)
        if self.Security is not None and self.Security.VolumeStep is not None and self.Security.VolumeStep > 0:
            step = self.Security.VolumeStep

        volume = Math.Floor(equity * Decimal(self._percent_of_equity.Value) / close / step) * step
        if volume <= 0:
            return

        self.BuyMarket(volume)
        self._last_buy_date = open_time.Date

    def CreateClone(self):
        return monthly_purchase_with_dynamic_contract_size_strategy()
