import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class martin_no_loss_exit_v3_strategy(Strategy):
    def __init__(self):
        super(martin_no_loss_exit_v3_strategy, self).__init__()
        self._price_step_percent = self.Param("PriceStepPercent", 2.5) \
            .SetDisplay("Price Step %", "Step percent for martingale", "General")
        self._take_profit_percent = self.Param("TakeProfitPercent", 2.5) \
            .SetDisplay("Take Profit %", "Take profit percent", "General")
        self._increase_factor = self.Param("IncreaseFactor", 1.10) \
            .SetDisplay("Increase Factor", "Martingale increase factor", "General")
        self._max_orders = self.Param("MaxOrders", 8) \
            .SetDisplay("Max Orders", "Maximum martingale orders", "General")
        self._ema_length = self.Param("EmaLength", 50) \
            .SetDisplay("EMA Length", "EMA trend filter", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._entry_price = 0.0
        self._total_cost = 0.0
        self._total_qty = 0.0
        self._last_cash = 0.0
        self._order_count = 0
        self._in_position = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(martin_no_loss_exit_v3_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._total_cost = 0.0
        self._total_qty = 0.0
        self._last_cash = 0.0
        self._order_count = 0
        self._in_position = False

    def OnStarted2(self, time):
        super(martin_no_loss_exit_v3_strategy, self).OnStarted2(time)
        self._entry_price = 0.0
        self._total_cost = 0.0
        self._total_qty = 0.0
        self._last_cash = 0.0
        self._order_count = 0
        self._in_position = False
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self._ema_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self.OnProcess).Start()

    def OnProcess(self, candle, ema):
        if candle.State != CandleStates.Finished:
            return
        if not self._ema.IsFormed:
            return
        ev = float(ema)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        initial_cash = 100.0
        if self._in_position:
            avg_price = self._total_cost / self._total_qty if self._total_qty > 0.0 else 0.0
            tp_pct = float(self._take_profit_percent.Value) / 100.0
            take_profit_price = avg_price * (1.0 + tp_pct)
            if high >= take_profit_price and self.Position > 0:
                self.SellMarket()
                self._in_position = False
                self._entry_price = 0.0
                self._total_cost = 0.0
                self._total_qty = 0.0
                self._last_cash = 0.0
                self._order_count = 0
                return
            step_pct = float(self._price_step_percent.Value) / 100.0
            next_entry_price = self._entry_price * (1.0 - step_pct * self._order_count)
            max_orders = self._max_orders.Value
            inc_factor = float(self._increase_factor.Value)
            if self._order_count < max_orders and close <= next_entry_price:
                self.BuyMarket()
                new_cash = self._last_cash * inc_factor
                self._total_cost += new_cash
                if close > 0.0:
                    self._total_qty += new_cash / close
                self._last_cash = new_cash
                self._order_count += 1
        else:
            if close > ev:
                self.BuyMarket()
                self._entry_price = close
                self._total_cost = initial_cash
                self._total_qty = initial_cash / close if close > 0.0 else 0.0
                self._last_cash = initial_cash
                self._order_count = 1
                self._in_position = True

    def CreateClone(self):
        return martin_no_loss_exit_v3_strategy()
