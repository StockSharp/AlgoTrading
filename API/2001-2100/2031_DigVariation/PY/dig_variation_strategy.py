import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class dig_variation_strategy(Strategy):

    def __init__(self):
        super(dig_variation_strategy, self).__init__()

        self._period = self.Param("Period", 20) \
            .SetDisplay("Period", "Period", "General")
        self._buy_open = self.Param("BuyOpen", True) \
            .SetDisplay("Open Long", "Open Long", "General")
        self._sell_open = self.Param("SellOpen", True) \
            .SetDisplay("Open Short", "Open Short", "General")
        self._buy_close = self.Param("BuyClose", True) \
            .SetDisplay("Close Long", "Close Long", "General")
        self._sell_close = self.Param("SellClose", True) \
            .SetDisplay("Close Short", "Close Short", "General")
        self._stop_loss = self.Param("StopLoss", 1000.0) \
            .SetDisplay("Stop Loss", "Stop Loss", "General")
        self._take_profit = self.Param("TakeProfit", 2000.0) \
            .SetDisplay("Take Profit", "Take Profit", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle", "General")
        self._cooldown_period = self.Param("CooldownPeriod", 0) \
            .SetDisplay("Cooldown", "Cooldown between trades in candles", "General")

        self._prev = 0.0
        self._prev_prev = 0.0
        self._initialized = False
        self._cooldown = 0

    @property
    def Period(self):
        return self._period.Value

    @Period.setter
    def Period(self, value):
        self._period.Value = value

    @property
    def BuyOpen(self):
        return self._buy_open.Value

    @BuyOpen.setter
    def BuyOpen(self, value):
        self._buy_open.Value = value

    @property
    def SellOpen(self):
        return self._sell_open.Value

    @SellOpen.setter
    def SellOpen(self, value):
        self._sell_open.Value = value

    @property
    def BuyClose(self):
        return self._buy_close.Value

    @BuyClose.setter
    def BuyClose(self, value):
        self._buy_close.Value = value

    @property
    def SellClose(self):
        return self._sell_close.Value

    @SellClose.setter
    def SellClose(self, value):
        self._sell_close.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def CooldownPeriod(self):
        return self._cooldown_period.Value

    @CooldownPeriod.setter
    def CooldownPeriod(self, value):
        self._cooldown_period.Value = value

    def OnStarted2(self, time):
        super(dig_variation_strategy, self).OnStarted2(time)
        self._pending = None

        self.StartProtection(
            stopLoss=Unit(self.StopLoss, UnitTypes.Absolute),
            takeProfit=Unit(self.TakeProfit, UnitTypes.Absolute)
        )

        ema = SimpleMovingAverage()
        ema.Length = self.Period

        self.SubscribeCandles(self.CandleType) \
            .Bind(ema, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading() or (self._pending is not None and self._pending.State not in (OrderStates.Done, OrderStates.Failed)):
            return

        val = float(ema_value)

        if not self._initialized:
            self._prev = val
            self._prev_prev = val
            self._initialized = True
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_prev = self._prev
            self._prev = val
            return

        was_rising = self._prev > self._prev_prev
        was_falling = self._prev < self._prev_prev

        if was_rising and val > self._prev:
            if self.Position < 0 and self.SellClose:
                self._pending = self.BuyMarket(-self.Position + (self.Volume if self.BuyOpen else 0))
            elif self.Position == 0 and self.BuyOpen:
                self._pending = self.BuyMarket(self.Volume)
        elif was_falling and val < self._prev:
            if self.Position > 0 and self.BuyClose:
                self._pending = self.SellMarket(self.Position + (self.Volume if self.SellOpen else 0))
            elif self.Position == 0 and self.SellOpen:
                self._pending = self.SellMarket(self.Volume)

        self._prev_prev = self._prev
        self._prev = val

    def OnReseted(self):
        super(dig_variation_strategy, self).OnReseted()
        self._pending = None
        self._prev = 0.0
        self._prev_prev = 0.0
        self._initialized = False
        self._cooldown = 0

    def CreateClone(self):
        return dig_variation_strategy()
