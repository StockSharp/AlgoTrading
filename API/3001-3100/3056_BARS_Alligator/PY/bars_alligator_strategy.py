import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SmoothedMovingAverage, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class bars_alligator_strategy(Strategy):
    def __init__(self):
        super(bars_alligator_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._cooldown_bars = self.Param("CooldownBars", 6) \
            .SetDisplay("Cooldown Bars", "Bars between completed trades", "Trading")
        self._stop_loss_percent = self.Param("StopLossPercent", Decimal(3)) \
            .SetDisplay("Stop Loss %", "Stop distance as percentage of entry price", "Risk")
        self._take_profit_percent = self.Param("TakeProfitPercent", Decimal(3)) \
            .SetDisplay("Take Profit %", "Take-profit distance as percentage of entry price", "Risk")

        self._jaw = SmoothedMovingAverage()
        self._jaw.Length = 13
        self._teeth = SmoothedMovingAverage()
        self._teeth.Length = 8
        self._lips = SmoothedMovingAverage()
        self._lips.Length = 5

        self._previous_jaw = Decimal(0)
        self._previous_teeth = Decimal(0)
        self._previous_lips = Decimal(0)
        self._has_previous = False
        self._entry_price = None
        self._cooldown_left = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @property
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    @property
    def TakeProfitPercent(self):
        return self._take_profit_percent.Value

    def OnReseted(self):
        super(bars_alligator_strategy, self).OnReseted()
        self._previous_jaw = Decimal(0)
        self._previous_teeth = Decimal(0)
        self._previous_lips = Decimal(0)
        self._has_previous = False
        self._entry_price = None
        self._cooldown_left = 0

    def OnStarted2(self, time):
        super(bars_alligator_strategy, self).OnStarted2(time)
        self.OnReseted()
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._jaw, self._teeth, self._lips, self._process_candle).Start()

    def _process_candle(self, candle, jaw, teeth, lips):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_left > 0:
            self._cooldown_left -= 1

        if self.Position != 0 and self._entry_price is None:
            self._entry_price = candle.ClosePrice

        if self._try_exit_by_risk(candle):
            self._update_previous(jaw, teeth, lips)
            return

        if not self._has_previous:
            self._update_previous(jaw, teeth, lips)
            return

        # Exit conditions: lips crosses teeth against position
        close_long = lips < teeth and self._previous_lips >= self._previous_teeth and self.Position > 0
        close_short = lips > teeth and self._previous_lips <= self._previous_teeth and self.Position < 0

        if close_long:
            self.SellMarket(self.Position)
            self._entry_price = None
            self._cooldown_left = self.CooldownBars
            self._update_previous(jaw, teeth, lips)
            return

        if close_short:
            self.BuyMarket(Math.Abs(self.Position))
            self._entry_price = None
            self._cooldown_left = self.CooldownBars
            self._update_previous(jaw, teeth, lips)
            return

        if not self.IsFormedAndOnlineAndAllowTrading() or self._cooldown_left > 0:
            self._update_previous(jaw, teeth, lips)
            return

        # Entry: lips crosses jaw with proper Alligator ordering
        buy_signal = lips > jaw and self._previous_lips <= self._previous_jaw and lips > teeth
        sell_signal = lips < jaw and self._previous_lips >= self._previous_jaw and lips < teeth

        if buy_signal and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
                self._entry_price = None
                self._cooldown_left = self.CooldownBars
            else:
                self.BuyMarket()
                self._entry_price = candle.ClosePrice
                self._cooldown_left = self.CooldownBars
        elif sell_signal and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(self.Position)
                self._entry_price = None
                self._cooldown_left = self.CooldownBars
            else:
                self.SellMarket()
                self._entry_price = candle.ClosePrice
                self._cooldown_left = self.CooldownBars

        self._update_previous(jaw, teeth, lips)

    def _try_exit_by_risk(self, candle):
        if self._entry_price is None or self.Position == 0 or self._entry_price == 0:
            return False

        entry_price = self._entry_price
        stop_distance = entry_price * self.StopLossPercent / Decimal(100)
        take_distance = entry_price * self.TakeProfitPercent / Decimal(100)

        if self.Position > 0:
            if (stop_distance > 0 and candle.LowPrice <= entry_price - stop_distance) or \
               (take_distance > 0 and candle.HighPrice >= entry_price + take_distance):
                self.SellMarket(self.Position)
                self._entry_price = None
                self._cooldown_left = self.CooldownBars
                return True
        elif self.Position < 0:
            volume = Math.Abs(self.Position)
            if (stop_distance > 0 and candle.HighPrice >= entry_price + stop_distance) or \
               (take_distance > 0 and candle.LowPrice <= entry_price - take_distance):
                self.BuyMarket(volume)
                self._entry_price = None
                self._cooldown_left = self.CooldownBars
                return True

        return False

    def _update_previous(self, jaw, teeth, lips):
        self._previous_jaw = jaw
        self._previous_teeth = teeth
        self._previous_lips = lips
        self._has_previous = True

    def CreateClone(self):
        return bars_alligator_strategy()
