import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, DateTime
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class she_kanskigor_daily_strategy(Strategy):
    def __init__(self):
        super(she_kanskigor_daily_strategy, self).__init__()

        self._take_profit_steps = self.Param("TakeProfitSteps", 35.0) \
            .SetDisplay("Take Profit", "Profit target in steps", "Risk")
        self._stop_loss_steps = self.Param("StopLossSteps", 55.0) \
            .SetDisplay("Stop Loss", "Loss limit in steps", "Risk")
        self._start_time = self.Param("StartTime", TimeSpan(0, 5, 0)) \
            .SetDisplay("Start Time", "Time of day to evaluate entries", "Schedule")
        self._trade_window_minutes = self.Param("TradeWindowMinutes", 5) \
            .SetDisplay("Window (min)", "Trading window duration in minutes", "Schedule")
        self._intraday_candle_type = self.Param("IntradayCandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Intraday Candle", "Candle type for intraday checks", "Data")

        self._daily_candle_type = DataType.TimeFrame(TimeSpan.FromMinutes(5))

        self._current_date = DateTime.MinValue
        self._trade_placed = False
        self._daily_ready = False
        self._previous_open = 0.0
        self._previous_close = 0.0
        self._entry_price = 0.0

    @property
    def TakeProfitSteps(self):
        return self._take_profit_steps.Value

    @TakeProfitSteps.setter
    def TakeProfitSteps(self, value):
        self._take_profit_steps.Value = value

    @property
    def StopLossSteps(self):
        return self._stop_loss_steps.Value

    @StopLossSteps.setter
    def StopLossSteps(self, value):
        self._stop_loss_steps.Value = value

    @property
    def StartTime(self):
        return self._start_time.Value

    @StartTime.setter
    def StartTime(self, value):
        self._start_time.Value = value

    @property
    def TradeWindowMinutes(self):
        return self._trade_window_minutes.Value

    @TradeWindowMinutes.setter
    def TradeWindowMinutes(self, value):
        self._trade_window_minutes.Value = value

    @property
    def IntradayCandleType(self):
        return self._intraday_candle_type.Value

    @IntradayCandleType.setter
    def IntradayCandleType(self, value):
        self._intraday_candle_type.Value = value

    def OnStarted(self, time):
        super(she_kanskigor_daily_strategy, self).OnStarted(time)

        intraday = self.SubscribeCandles(self.IntradayCandleType)
        intraday.Bind(self.ProcessIntraday).Start()

        daily = self.SubscribeCandles(self._daily_candle_type)
        daily.Bind(self.ProcessDaily).Start()

        self.StartProtection(None, None)

    def ProcessDaily(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._previous_open = float(candle.OpenPrice)
        self._previous_close = float(candle.ClosePrice)
        self._daily_ready = True

    def ProcessIntraday(self, candle):
        if candle.State != CandleStates.Finished:
            return

        open_time = candle.OpenTime
        if open_time.Date != self._current_date:
            self._current_date = open_time.Date
            self._trade_placed = False

        self._manage_position(float(candle.ClosePrice))

        start = self.StartTime
        end = start.Add(TimeSpan.FromMinutes(self.TradeWindowMinutes))
        current_tod = open_time.TimeOfDay

        if current_tod < start or current_tod > end:
            return

        if self._trade_placed:
            return

        if not self._daily_ready:
            return

        if self.Position != 0:
            self._trade_placed = True
            return

        if self._previous_open > self._previous_close:
            self.BuyMarket(self.Volume)
            self._trade_placed = True
        elif self._previous_open < self._previous_close:
            self.SellMarket(self.Volume)
            self._trade_placed = True
        else:
            self._trade_placed = True

    def _manage_position(self, close_price):
        if self.Position == 0:
            return

        step = float(self.Security.PriceStep) if self.Security is not None else 0.0
        if step <= 0 or self._entry_price == 0:
            return

        tp = float(self.TakeProfitSteps)
        sl = float(self.StopLossSteps)

        if self.Position > 0:
            target = self._entry_price + tp * step
            stop = self._entry_price - sl * step

            if tp > 0 and close_price >= target:
                self.SellMarket(self.Position)
                return

            if sl > 0 and close_price <= stop:
                self.SellMarket(self.Position)
        else:
            target = self._entry_price - tp * step
            stop = self._entry_price + sl * step

            if tp > 0 and close_price <= target:
                self.BuyMarket(-self.Position)
                return

            if sl > 0 and close_price >= stop:
                self.BuyMarket(-self.Position)

    def OnOwnTradeReceived(self, trade):
        super(she_kanskigor_daily_strategy, self).OnOwnTradeReceived(trade)
        if trade.Order.Security != self.Security:
            return
        self._entry_price = float(trade.Trade.Price)

    def OnReseted(self):
        super(she_kanskigor_daily_strategy, self).OnReseted()
        self._current_date = DateTime.MinValue
        self._trade_placed = False
        self._daily_ready = False
        self._previous_open = 0.0
        self._previous_close = 0.0
        self._entry_price = 0.0

    def CreateClone(self):
        return she_kanskigor_daily_strategy()
