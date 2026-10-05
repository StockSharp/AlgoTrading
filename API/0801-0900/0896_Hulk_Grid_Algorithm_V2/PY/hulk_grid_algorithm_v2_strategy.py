import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

LEVELS_PER_SIDE = 5

class hulk_grid_algorithm_v2_strategy(Strategy):
    """
    Hulk Grid Algorithm V2 strategy.
    Places ten buy limit orders at MidPrice +/- 1..5 GridStep (MidPrice 0 means the current close). An order k steps from the mid
    has volume Lot * (6 - k), so orders closer to the mid are larger. When price touches StopLossPercent below the lowest level or
    TakeProfitPercent above the highest level, the position is closed and the remaining orders are cancelled. With MidPrice 0
    a new grid is then built around the current close.
    """

    def __init__(self):
        super(hulk_grid_algorithm_v2_strategy, self).__init__()
        self._mid_price = self.Param("MidPrice", 0.0).SetNotNegative().SetDisplay("Mid Price", "Grid mid price, 0 uses the current close", "Grid")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss in percent below the lowest grid level", "Risk")
        self._take_profit_percent = self.Param("TakeProfitPercent", 2.0).SetNotNegative().SetDisplay("Take Profit %", "Take profit in percent above the highest grid level", "Risk")
        self._grid_step = self.Param("GridStep", 200.0).SetGreaterThanZero().SetDisplay("Grid Step", "Price distance between grid levels", "Grid")
        self._lot = self.Param("Lot", 50.0).SetGreaterThanZero().SetDisplay("Lot", "Base order volume", "Grid")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._grid_active = False
        self._grid_completed = False
        self._stop_price = 0.0
        self._take_price = 0.0

    def OnReseted(self):
        super(hulk_grid_algorithm_v2_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(hulk_grid_algorithm_v2_strategy, self).OnStarted2(time)

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

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        mid_param = float(self._mid_price.Value)

        if self._grid_active:
            if float(candle.LowPrice) <= self._stop_price or float(candle.HighPrice) >= self._take_price:
                self.CancelActiveOrders()

                if self.Position > 0:
                    self.SellMarket(self.Position)

                self._grid_active = False
                self._grid_completed = mid_param > 0
            return

        if self._grid_completed or self.Position != 0:
            return

        self._place_grid(mid_param if mid_param > 0 else float(candle.ClosePrice))

    def _place_grid(self, mid):
        step = 1.0
        if self.Security.PriceStep is not None and float(self.Security.PriceStep) > 0:
            step = float(self.Security.PriceStep)

        grid_step = float(self._grid_step.Value)
        lot = float(self._lot.Value)
        lowest = None
        highest = None

        for k in range(-LEVELS_PER_SIDE, LEVELS_PER_SIDE + 1):
            if k == 0:
                continue

            price = round((mid + k * grid_step) / step) * step
            if price <= 0:
                continue

            self.BuyLimit(Decimal(price), Decimal(lot * (LEVELS_PER_SIDE + 1 - abs(k))))

            lowest = price if lowest is None else min(lowest, price)
            highest = price if highest is None else max(highest, price)

        if lowest is None:
            return

        self._stop_price = lowest * (1.0 - float(self._stop_loss_percent.Value) / 100.0)
        self._take_price = highest * (1.0 + float(self._take_profit_percent.Value) / 100.0)
        self._grid_active = True

    def CreateClone(self):
        return hulk_grid_algorithm_v2_strategy()
