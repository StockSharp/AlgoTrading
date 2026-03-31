import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SmoothedMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class bars_alligator_strategy(Strategy):
    def __init__(self):
        super(bars_alligator_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._cooldown_bars = self.Param("CooldownBars", 6) \
            .SetDisplay("Cooldown Bars", "Bars between completed trades", "Trading")
        self._stop_loss_pips = self.Param("StopLossPips", 150) \
            .SetDisplay("Stop Loss", "Stop distance in pips", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 150) \
            .SetDisplay("Take Profit", "Take-profit distance in pips", "Risk")

        self._jaw = None
        self._teeth = None
        self._lips = None
        self._previous_jaw = 0.0
        self._previous_teeth = 0.0
        self._previous_lips = 0.0
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
    def StopLossPips(self):
        return self._stop_loss_pips.Value
    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    def OnReseted(self):
        super(bars_alligator_strategy, self).OnReseted()
        self._previous_jaw = 0.0
        self._previous_teeth = 0.0
        self._previous_lips = 0.0
        self._has_previous = False
        self._entry_price = None
        self._cooldown_left = 0

    def OnStarted2(self, time):
        super(bars_alligator_strategy, self).OnStarted2(time)
        self._jaw = SmoothedMovingAverage()
        self._jaw.Length = 13
        self._teeth = SmoothedMovingAverage()
        self._teeth.Length = 8
        self._lips = SmoothedMovingAverage()
        self._lips.Length = 5
        self._previous_jaw = 0.0
        self._previous_teeth = 0.0
        self._previous_lips = 0.0
        self._has_previous = False
        self._entry_price = None
        self._cooldown_left = 0
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._on_process).Start()

    def _on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_left > 0:
            self._cooldown_left -= 1

        price = (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0
        jaw_result = self._jaw.Process(DecimalIndicatorValue(self._jaw, price, candle.OpenTime))
        teeth_result = self._teeth.Process(DecimalIndicatorValue(self._teeth, price, candle.OpenTime))
        lips_result = self._lips.Process(DecimalIndicatorValue(self._lips, price, candle.OpenTime))

        if not self._jaw.IsFormed or not self._teeth.IsFormed or not self._lips.IsFormed:
            return
        if jaw_result.IsEmpty or teeth_result.IsEmpty or lips_result.IsEmpty:
            return

        jaw = float(jaw_result)
        teeth = float(teeth_result)
        lips = float(lips_result)

        if self.Position != 0 and self._entry_price is None:
            self._entry_price = float(candle.ClosePrice)

        if self._try_exit_by_risk(candle):
            self._update_previous(jaw, teeth, lips)
            return

        if not self._has_previous:
            self._update_previous(jaw, teeth, lips)
            return

        close_long = (lips < teeth and self._previous_lips >= self._previous_teeth
                      and self.Position > 0 and self._entry_price is not None
                      and float(candle.ClosePrice) >= self._entry_price)
        close_short = (lips > teeth and self._previous_lips <= self._previous_teeth
                       and self.Position < 0 and self._entry_price is not None
                       and float(candle.ClosePrice) <= self._entry_price)

        if close_long:
            self.SellMarket()
            self._entry_price = None
            self._cooldown_left = self.CooldownBars
            self._update_previous(jaw, teeth, lips)
            return

        if close_short:
            self.BuyMarket()
            self._entry_price = None
            self._cooldown_left = self.CooldownBars
            self._update_previous(jaw, teeth, lips)
            return

        if not self.IsFormedAndOnlineAndAllowTrading() or self._cooldown_left > 0:
            self._update_previous(jaw, teeth, lips)
            return

        buy_signal = (lips > jaw and self._previous_lips <= self._previous_jaw
                      and lips > teeth and teeth > jaw)
        sell_signal = (lips < jaw and self._previous_lips >= self._previous_jaw
                       and lips < teeth and teeth < jaw)

        if buy_signal and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
                self._entry_price = None
                self._cooldown_left = self.CooldownBars
            else:
                self.BuyMarket()
                self._entry_price = float(candle.ClosePrice)
                self._cooldown_left = self.CooldownBars
        elif sell_signal and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
                self._entry_price = None
                self._cooldown_left = self.CooldownBars
            else:
                self.SellMarket()
                self._entry_price = float(candle.ClosePrice)
                self._cooldown_left = self.CooldownBars

        self._update_previous(jaw, teeth, lips)

    def _try_exit_by_risk(self, candle):
        if self._entry_price is None or self.Position == 0:
            return False

        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        if step <= 0.0:
            step = 1.0

        stop_distance = self.StopLossPips * step
        take_distance = self.TakeProfitPips * step

        if self.Position > 0:
            if (stop_distance > 0 and float(candle.LowPrice) <= self._entry_price - stop_distance) or \
               (take_distance > 0 and float(candle.HighPrice) >= self._entry_price + take_distance):
                self.SellMarket()
                self._entry_price = None
                self._cooldown_left = self.CooldownBars
                return True
        elif self.Position < 0:
            if (stop_distance > 0 and float(candle.HighPrice) >= self._entry_price + stop_distance) or \
               (take_distance > 0 and float(candle.LowPrice) <= self._entry_price - take_distance):
                self.BuyMarket()
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
