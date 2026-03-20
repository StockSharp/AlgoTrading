import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class range_ea_strategy(Strategy):

    def __init__(self):
        super(range_ea_strategy, self).__init__()

        self._ma_length = self.Param("MaLength", 21) \
            .SetDisplay("MA Length", "Moving average period", "Parameters")
        self._range = self.Param("Range", 2500.0) \
            .SetDisplay("Range", "Price range from MA", "Parameters")
        self._take_profit = self.Param("TakeProfit", 3000.0) \
            .SetDisplay("Take Profit", "Fixed take profit", "Parameters")
        self._stop_loss = self.Param("StopLoss", 1500.0) \
            .SetDisplay("Stop Loss", "Fixed stop loss", "Parameters")
        self._use_trailing_stop = self.Param("UseTrailingStop", False) \
            .SetDisplay("Use Trailing", "Enable trailing stop", "Parameters")
        self._trailing_stop = self.Param("TrailingStop", 250.0) \
            .SetDisplay("Trailing", "Trailing stop distance", "Parameters")
        self._use_turn = self.Param("UseTurn", False) \
            .SetDisplay("Use Turn", "Enable reversal module", "Parameters")
        self._turn = self.Param("Turn", 250.0) \
            .SetDisplay("Turn", "Reversal distance", "Parameters")
        self._lot_multiplicator = self.Param("LotMultiplicator", 1.65) \
            .SetDisplay("Lot Mult", "Volume multiplier for reversal", "Parameters")
        self._turn_take_profit = self.Param("TurnTakeProfit", 500.0) \
            .SetDisplay("Turn TP", "Take profit after reversal", "Parameters")
        self._use_step_down = self.Param("UseStepDown", False) \
            .SetDisplay("Use StepDown", "Enable averaging module", "Parameters")
        self._step_down = self.Param("StepDown", 150.0) \
            .SetDisplay("Step Down", "Averaging step", "Parameters")
        self._use_trade_time = self.Param("UseTradeTime", False) \
            .SetDisplay("Use Trade Time", "Limit trading hours", "Parameters")
        self._open_trade_time = self.Param("OpenTradeTime", TimeSpan.Parse("08:00:00")) \
            .SetDisplay("Open Time", "Trading start time", "Parameters")
        self._close_trade_time = self.Param("CloseTradeTime", TimeSpan.Parse("21:30:00")) \
            .SetDisplay("Close Time", "Trading end time", "Parameters")
        self._order_volume = self.Param("OrderVolume", 0.1) \
            .SetDisplay("Volume", "Order volume", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe", "Parameters")

        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_profit_price = 0.0
        self._next_step_price = 0.0
        self._turn_price = 0.0

    @property
    def MaLength(self):
        return self._ma_length.Value

    @MaLength.setter
    def MaLength(self, value):
        self._ma_length.Value = value

    @property
    def Range(self):
        return self._range.Value

    @Range.setter
    def Range(self, value):
        self._range.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def UseTrailingStop(self):
        return self._use_trailing_stop.Value

    @UseTrailingStop.setter
    def UseTrailingStop(self, value):
        self._use_trailing_stop.Value = value

    @property
    def TrailingStop(self):
        return self._trailing_stop.Value

    @TrailingStop.setter
    def TrailingStop(self, value):
        self._trailing_stop.Value = value

    @property
    def UseTurn(self):
        return self._use_turn.Value

    @UseTurn.setter
    def UseTurn(self, value):
        self._use_turn.Value = value

    @property
    def Turn(self):
        return self._turn.Value

    @Turn.setter
    def Turn(self, value):
        self._turn.Value = value

    @property
    def LotMultiplicator(self):
        return self._lot_multiplicator.Value

    @LotMultiplicator.setter
    def LotMultiplicator(self, value):
        self._lot_multiplicator.Value = value

    @property
    def TurnTakeProfit(self):
        return self._turn_take_profit.Value

    @TurnTakeProfit.setter
    def TurnTakeProfit(self, value):
        self._turn_take_profit.Value = value

    @property
    def UseStepDown(self):
        return self._use_step_down.Value

    @UseStepDown.setter
    def UseStepDown(self, value):
        self._use_step_down.Value = value

    @property
    def StepDown(self):
        return self._step_down.Value

    @StepDown.setter
    def StepDown(self, value):
        self._step_down.Value = value

    @property
    def UseTradeTime(self):
        return self._use_trade_time.Value

    @UseTradeTime.setter
    def UseTradeTime(self, value):
        self._use_trade_time.Value = value

    @property
    def OpenTradeTime(self):
        return self._open_trade_time.Value

    @OpenTradeTime.setter
    def OpenTradeTime(self, value):
        self._open_trade_time.Value = value

    @property
    def CloseTradeTime(self):
        return self._close_trade_time.Value

    @CloseTradeTime.setter
    def CloseTradeTime(self, value):
        self._close_trade_time.Value = value

    @property
    def OrderVolume(self):
        return self._order_volume.Value

    @OrderVolume.setter
    def OrderVolume(self, value):
        self._order_volume.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _close_position(self):
        pos = self.Position
        if pos > 0:
            self.SellMarket(pos)
        elif pos < 0:
            self.BuyMarket(-pos)

    def OnStarted(self, time):
        super(range_ea_strategy, self).OnStarted(time)

        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_profit_price = 0.0
        self._next_step_price = 0.0
        self._turn_price = 0.0

        ma = SimpleMovingAverage()
        ma.Length = self.MaLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(ma, self.ProcessCandle) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return

        time_of_day = candle.OpenTime.TimeOfDay
        if self.UseTradeTime:
            if time_of_day < self.OpenTradeTime or time_of_day > self.CloseTradeTime:
                if self.Position != 0:
                    self._close_position()
                return

        price = float(candle.ClosePrice)
        ma_val = float(ma_value)
        pos = self.Position

        if pos > 0:
            if price >= self._take_profit_price or price <= self._stop_price:
                self._close_position()
                return

            if self.UseTrailingStop and price - self._entry_price >= float(self.TrailingStop):
                self._stop_price = max(self._stop_price, price - float(self.TrailingStop))

            if self.UseTurn and price <= self._turn_price:
                self._close_position()
                self.SellMarket(float(self.OrderVolume) * float(self.LotMultiplicator))
                self._entry_price = price
                self._stop_price = price + float(self.StopLoss)
                self._take_profit_price = price - float(self.TurnTakeProfit)
                if self.UseStepDown:
                    self._next_step_price = self._entry_price + float(self.StepDown)
                if self.UseTurn:
                    self._turn_price = self._entry_price + float(self.Turn)
                return

            if self.UseStepDown and price <= self._next_step_price:
                self.BuyMarket(self.OrderVolume)
                self._next_step_price -= float(self.StepDown)

        elif pos < 0:
            if price <= self._take_profit_price or price >= self._stop_price:
                self._close_position()
                return

            if self.UseTrailingStop and self._entry_price - price >= float(self.TrailingStop):
                self._stop_price = min(self._stop_price, price + float(self.TrailingStop))

            if self.UseTurn and price >= self._turn_price:
                self._close_position()
                self.BuyMarket(float(self.OrderVolume) * float(self.LotMultiplicator))
                self._entry_price = price
                self._stop_price = price - float(self.StopLoss)
                self._take_profit_price = price + float(self.TurnTakeProfit)
                if self.UseStepDown:
                    self._next_step_price = self._entry_price - float(self.StepDown)
                if self.UseTurn:
                    self._turn_price = self._entry_price - float(self.Turn)
                return

            if self.UseStepDown and price >= self._next_step_price:
                self.SellMarket(self.OrderVolume)
                self._next_step_price += float(self.StepDown)

        else:
            if not self.IsFormedAndOnlineAndAllowTrading():
                return

            if price >= ma_val + float(self.Range):
                self.BuyMarket(self.OrderVolume)
                self._entry_price = price
                self._stop_price = price - float(self.StopLoss)
                self._take_profit_price = price + float(self.TakeProfit)
                if self.UseStepDown:
                    self._next_step_price = self._entry_price - float(self.StepDown)
                if self.UseTurn:
                    self._turn_price = self._entry_price - float(self.Turn)

            elif price <= ma_val - float(self.Range):
                self.SellMarket(self.OrderVolume)
                self._entry_price = price
                self._stop_price = price + float(self.StopLoss)
                self._take_profit_price = price - float(self.TakeProfit)
                if self.UseStepDown:
                    self._next_step_price = self._entry_price + float(self.StepDown)
                if self.UseTurn:
                    self._turn_price = self._entry_price + float(self.Turn)

    def OnReseted(self):
        super(range_ea_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_profit_price = 0.0
        self._next_step_price = 0.0
        self._turn_price = 0.0

    def CreateClone(self):
        return range_ea_strategy()
