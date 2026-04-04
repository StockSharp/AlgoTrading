import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from System.Collections.Generic import HashSet
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class pending_order_grid_strategy(Strategy):
    def __init__(self):
        super(pending_order_grid_strategy, self).__init__()
        self._grid_spacing = self.Param("GridSpacing", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Grid Spacing %", "Percentage spacing between grid levels", "Grid")
        self._grid_levels = self.Param("GridLevels", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Grid Levels", "Number of grid levels per side", "Grid")
        self._take_profit_percent = self.Param("TakeProfitPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Take Profit %", "Take profit as percentage of entry", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss as percentage of entry", "Risk")
        self._trade_long = self.Param("TradeLong", True) \
            .SetDisplay("Enable Long", "Enable buy grid levels", "Grid")
        self._trade_short = self.Param("TradeShort", True) \
            .SetDisplay("Enable Short", "Enable sell grid levels", "Grid")

        self._initial_price = 0
        self._entry_price = 0
        self._initialized = False
        self._triggered_buy_levels = HashSet[int]()
        self._triggered_sell_levels = HashSet[int]()
        self._trade_count = 0

    def OnReseted(self):
        super(pending_order_grid_strategy, self).OnReseted()
        self._initial_price = 0
        self._entry_price = 0
        self._initialized = False
        self._triggered_buy_levels = HashSet[int]()
        self._triggered_sell_levels = HashSet[int]()
        self._trade_count = 0

    def OnStarted2(self, time):
        super(pending_order_grid_strategy, self).OnStarted2(time)

        tf = DataType.TimeFrame(TimeSpan.FromMinutes(5))

        sub = self.SubscribeCandles(tf)
        sub.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice

        # Initialize grid around the first candle's close price
        if not self._initialized:
            self._initial_price = close
            self._initialized = True
            self._triggered_buy_levels.Clear()
            self._triggered_sell_levels.Clear()
            return

        # Check if we have a position that needs TP/SL management
        if self.Position != 0 and self._entry_price > 0:
            if self.Position > 0:
                tp_price = self._entry_price * (1 + self._take_profit_percent.Value / 100)
                sl_price = self._entry_price * (1 - self._stop_loss_percent.Value / 100)

                if close >= tp_price or close <= sl_price:
                    self.SellMarket()
                    self.ResetGrid(close)
                    return
            elif self.Position < 0:
                tp_price = self._entry_price * (1 - self._take_profit_percent.Value / 100)
                sl_price = self._entry_price * (1 + self._stop_loss_percent.Value / 100)

                if close <= tp_price or close >= sl_price:
                    self.BuyMarket()
                    self.ResetGrid(close)
                    return

        # Check grid levels for new entries
        spacing = self._grid_spacing.Value / 100

        # Buy levels below initial price
        if self._trade_long.Value:
            for i in range(1, self._grid_levels.Value + 1):
                if self._triggered_buy_levels.Contains(i):
                    continue

                level = self._initial_price * (1 - i * spacing)

                if close <= level and self.Position <= 0:
                    # Close any short first
                    if self.Position < 0:
                        self.BuyMarket()

                    self.BuyMarket()
                    self._triggered_buy_levels.Add(i)
                    self._trade_count += 1
                    return

        # Sell levels above initial price
        if self._trade_short.Value:
            for i in range(1, self._grid_levels.Value + 1):
                if self._triggered_sell_levels.Contains(i):
                    continue

                level = self._initial_price * (1 + i * spacing)

                if close >= level and self.Position >= 0:
                    # Close any long first
                    if self.Position > 0:
                        self.SellMarket()

                    self.SellMarket()
                    self._triggered_sell_levels.Add(i)
                    self._trade_count += 1
                    return

    def ResetGrid(self, new_price):
        self._initial_price = new_price
        self._entry_price = 0
        self._triggered_buy_levels.Clear()
        self._triggered_sell_levels.Clear()

    def OnOwnTradeReceived(self, trade):
        super(pending_order_grid_strategy, self).OnOwnTradeReceived(trade)

        if trade is not None and trade.Trade is not None:
            self._entry_price = trade.Trade.Price

    def CreateClone(self):
        return pending_order_grid_strategy()
