import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import VortexIndicator
from StockSharp.Algo.Strategies import Strategy


class vortex_indicator_cross_strategy(Strategy):

    def __init__(self):
        super(vortex_indicator_cross_strategy, self).__init__()

        self._length = self.Param("Length", 14) \
            .SetDisplay("Vortex Length", "Period for Vortex indicator", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe for Vortex calculation", "General")
        self._stop_loss = self.Param("StopLoss", 1200) \
            .SetDisplay("Stop Loss", "Protective stop in price steps", "General")
        self._take_profit = self.Param("TakeProfit", 2500) \
            .SetDisplay("Take Profit", "Target profit in price steps", "General")
        self._min_spread = self.Param("MinSpread", 0.08) \
            .SetDisplay("Min Spread", "Minimum VI spread required for entry", "General")
        self._cooldown_bars = self.Param("CooldownBars", 2) \
            .SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "General")

        self._prev_plus = 0.0
        self._prev_minus = 0.0
        self._is_initialized = False
        self._bars_since_trade = 0

    @property
    def Length(self):
        return self._length.Value

    @Length.setter
    def Length(self, value):
        self._length.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

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
    def MinSpread(self):
        return self._min_spread.Value

    @MinSpread.setter
    def MinSpread(self, value):
        self._min_spread.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    def OnStarted2(self, time):
        super(vortex_indicator_cross_strategy, self).OnStarted2(time)

        vortex = VortexIndicator()
        vortex.Length = self.Length

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .BindEx(vortex, self.ProcessCandle) \
            .Start()

        self.StartProtection(
            stopLoss=Unit(self.StopLoss, UnitTypes.Absolute),
            takeProfit=Unit(self.TakeProfit, UnitTypes.Absolute))

    def ProcessCandle(self, candle, vortex_value):
        if candle.State != CandleStates.Finished:
            return

        plus_raw = vortex_value.PlusVi
        minus_raw = vortex_value.MinusVi
        if plus_raw is None or minus_raw is None:
            return

        vi_plus = float(plus_raw)
        vi_minus = float(minus_raw)

        if self._bars_since_trade < self.CooldownBars:
            self._bars_since_trade += 1

        if not self._is_initialized:
            self._prev_plus = vi_plus
            self._prev_minus = vi_minus
            self._is_initialized = True
            return

        spread = abs(vi_plus - vi_minus)
        long_signal = self._prev_plus <= self._prev_minus and vi_plus > vi_minus and spread >= float(self.MinSpread)
        short_signal = self._prev_plus >= self._prev_minus and vi_plus < vi_minus and spread >= float(self.MinSpread)

        if self._bars_since_trade >= self.CooldownBars:
            pos = self.Position
            if long_signal and pos <= 0:
                self.BuyMarket(self.Volume + abs(pos))
                self._bars_since_trade = 0
            elif short_signal and pos >= 0:
                self.SellMarket(self.Volume + abs(pos))
                self._bars_since_trade = 0

        self._prev_plus = vi_plus
        self._prev_minus = vi_minus

    def OnReseted(self):
        super(vortex_indicator_cross_strategy, self).OnReseted()
        self._prev_plus = 0.0
        self._prev_minus = 0.0
        self._is_initialized = False
        self._bars_since_trade = self.CooldownBars

    def CreateClone(self):
        return vortex_indicator_cross_strategy()
