import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import DirectionalIndex, AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy


class adx_stop_order_template_strategy(Strategy):
    def __init__(self):
        super(adx_stop_order_template_strategy, self).__init__()
        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetDisplay("ADX Period", "Calculation period for ADX and DMI.", "Indicators")
        self._adx_signal = self.Param("AdxSignal", 20.0) \
            .SetDisplay("ADX Threshold", "Minimum ADX value to allow entries.", "Indicators")
        self._pips = self.Param("Pips", 10) \
            .SetDisplay("Pending Offset", "Distance in price steps for stop orders.", "Orders")
        self._take_profit = self.Param("TakeProfit", 1000) \
            .SetDisplay("Take Profit", "Take profit size in price steps.", "Risk")
        self._stop_loss = self.Param("StopLoss", 500) \
            .SetDisplay("Stop Loss", "Stop loss size in price steps.", "Risk")
        self._max_spread = self.Param("MaxSpread", 20.0) \
            .SetDisplay("Max Spread", "Maximum allowed spread in price steps.", "Orders")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for analysis.", "General")
        self._min_di_spread = self.Param("MinDiSpread", 5.0) \
            .SetDisplay("DI Spread", "Minimum spread between DI+ and DI-.", "Filters")
        self._cooldown_bars = self.Param("CooldownBars", 6) \
            .SetDisplay("Cooldown Bars", "Completed candles to wait after a position change.", "Trading")
        self._prev_plus = None
        self._prev_minus = None
        self._cooldown_remaining = 0

    @property
    def adx_period(self):
        return self._adx_period.Value

    @property
    def adx_signal(self):
        return self._adx_signal.Value

    @property
    def pips(self):
        return self._pips.Value

    @property
    def take_profit(self):
        return self._take_profit.Value

    @property
    def stop_loss(self):
        return self._stop_loss.Value

    @property
    def max_spread(self):
        return self._max_spread.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def min_di_spread(self):
        return self._min_di_spread.Value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(adx_stop_order_template_strategy, self).OnReseted()
        self._prev_plus = None
        self._prev_minus = None
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(adx_stop_order_template_strategy, self).OnStarted(time)
        dmi = DirectionalIndex()
        dmi.Length = self.adx_period
        adx = AverageDirectionalIndex()
        adx.Length = self.adx_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(dmi, adx, self.process_candle).Start()
        step = self.Security.PriceStep if self.Security.PriceStep is not None else 1.0
        step = float(step)
        self.StartProtection(
            Unit(self.take_profit * step, UnitTypes.Absolute),
            Unit(self.stop_loss * step, UnitTypes.Absolute))
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, adx)
            self.DrawIndicator(area, dmi)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, dmi_value, adx_value):
        if candle.State != CandleStates.Finished or not dmi_value.IsFinal or not adx_value.IsFinal:
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        di_plus = dmi_value.Plus
        di_minus = dmi_value.Minus
        if di_plus is None or di_minus is None:
            return
        di_plus = float(di_plus)
        di_minus = float(di_minus)
        adx_ma = adx_value.MovingAverage
        if adx_ma is None:
            return
        adx_val = float(adx_ma)
        if self._prev_plus is None or self._prev_minus is None:
            self._prev_plus = di_plus
            self._prev_minus = di_minus
            return
        cross_up = self._prev_plus <= self._prev_minus and di_plus > di_minus
        cross_down = self._prev_plus >= self._prev_minus and di_plus < di_minus
        di_spread = abs(di_plus - di_minus)
        adx_threshold = float(self.adx_signal)
        min_di = float(self.min_di_spread)
        if self._cooldown_remaining == 0 and adx_val >= adx_threshold and di_spread >= min_di:
            if cross_up and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
                self._cooldown_remaining = self.cooldown_bars
            elif cross_down and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
                self._cooldown_remaining = self.cooldown_bars
        elif self.Position > 0 and cross_down:
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position < 0 and cross_up:
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        self._prev_plus = di_plus
        self._prev_minus = di_minus

    def CreateClone(self):
        return adx_stop_order_template_strategy()
