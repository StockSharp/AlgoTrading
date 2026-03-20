import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math, DayOfWeek
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Strategies import Strategy

class monday_typical_breakout_strategy(Strategy):
    def __init__(self):
        super(monday_typical_breakout_strategy, self).__init__()

        self._fixed_volume = self.Param("FixedVolume", 0.1) \
            .SetDisplay("Fixed Volume", "Lot size used for entries (set to 0 to enable equity scaling)", "Risk")
        self._open_hour = self.Param("OpenHour", 9) \
            .SetDisplay("Open Hour", "Hour of the session to evaluate Monday breakout entries", "Session")
        self._stop_loss_points = self.Param("StopLossPoints", 50) \
            .SetDisplay("Stop Loss (points)", "Protective stop distance expressed in price points", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 20) \
            .SetDisplay("Take Profit (points)", "Profit target distance expressed in price points", "Risk")
        self._initial_equity = self.Param("InitialEquity", 600.0) \
            .SetDisplay("Initial Equity", "Account equity threshold that triggers the first scaling tier", "Money Management")
        self._equity_step = self.Param("EquityStep", 300.0) \
            .SetDisplay("Equity Step", "Incremental equity required to raise the position size", "Money Management")
        self._initial_step_volume = self.Param("InitialStepVolume", 0.4) \
            .SetDisplay("Initial Step Volume", "Lot size used once the equity threshold is met", "Money Management")
        self._volume_step = self.Param("VolumeStep", 0.2) \
            .SetDisplay("Volume Step", "Additional lot size added for each equity step", "Money Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe used for detecting the Monday breakout", "General")

        self._previous_candle = None
        self._last_signal_time = None
        self._price_step = 0.0

    @property
    def FixedVolume(self):
        return self._fixed_volume.Value

    @FixedVolume.setter
    def FixedVolume(self, value):
        self._fixed_volume.Value = value

    @property
    def OpenHour(self):
        return self._open_hour.Value

    @OpenHour.setter
    def OpenHour(self, value):
        self._open_hour.Value = value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @StopLossPoints.setter
    def StopLossPoints(self, value):
        self._stop_loss_points.Value = value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @TakeProfitPoints.setter
    def TakeProfitPoints(self, value):
        self._take_profit_points.Value = value

    @property
    def InitialEquity(self):
        return self._initial_equity.Value

    @InitialEquity.setter
    def InitialEquity(self, value):
        self._initial_equity.Value = value

    @property
    def EquityStep(self):
        return self._equity_step.Value

    @EquityStep.setter
    def EquityStep(self, value):
        self._equity_step.Value = value

    @property
    def InitialStepVolume(self):
        return self._initial_step_volume.Value

    @InitialStepVolume.setter
    def InitialStepVolume(self, value):
        self._initial_step_volume.Value = value

    @property
    def VolumeStep(self):
        return self._volume_step.Value

    @VolumeStep.setter
    def VolumeStep(self, value):
        self._volume_step.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(monday_typical_breakout_strategy, self).OnStarted(time)

        ps = self.Security.PriceStep if self.Security is not None else 0
        self._price_step = float(ps) if ps is not None and float(ps) > 0 else 0.0001

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        tp = int(self.TakeProfitPoints)
        sl = int(self.StopLossPoints)

        if tp > 0 or sl > 0:
            take_dist = Unit(tp * self._price_step, UnitTypes.Absolute) if tp > 0 else Unit(0)
            stop_dist = Unit(sl * self._price_step, UnitTypes.Absolute) if sl > 0 else Unit(0)
            self.StartProtection(takeProfit=take_dist, stopLoss=stop_dist)

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        previous = self._previous_candle
        self._previous_candle = candle

        if previous is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position != 0:
            return

        candle_time = candle.OpenTime.ToLocalTime()
        if candle_time.DayOfWeek != DayOfWeek.Monday:
            return

        if candle_time.Hour != self.OpenHour:
            return

        if self._last_signal_time is not None and self._last_signal_time == candle.OpenTime:
            return

        typical_price = (float(previous.HighPrice) + float(previous.LowPrice) + float(previous.ClosePrice)) / 3.0
        if float(candle.OpenPrice) <= typical_price:
            return

        volume = self._calculate_order_volume()
        if volume <= 0:
            return

        self.BuyMarket(volume)
        self._last_signal_time = candle.OpenTime

    def _calculate_order_volume(self):
        fixed_vol = float(self.FixedVolume)
        if fixed_vol > 0:
            return fixed_vol

        min_volume = 0.01
        if self.Security is not None and self.Security.MinVolume is not None and float(self.Security.MinVolume) > 0:
            min_volume = float(self.Security.MinVolume)

        equity = 0.0
        if self.Portfolio is not None:
            cv = self.Portfolio.CurrentValue
            bv = self.Portfolio.BeginValue
            if cv is not None and float(cv) > 0:
                equity = float(cv)
            elif bv is not None and float(bv) > 0:
                equity = float(bv)

        if equity <= 0:
            return min_volume

        initial_eq = float(self.InitialEquity)
        if equity < initial_eq:
            return min_volume

        eq_step = float(self.EquityStep)
        if eq_step <= 0:
            return float(self.InitialStepVolume)

        steps_decimal = (equity - initial_eq) / eq_step
        if steps_decimal < 0:
            steps_decimal = 0
        steps = int(steps_decimal)
        dynamic_volume = float(self.InitialStepVolume) + float(self.VolumeStep) * steps

        if dynamic_volume < min_volume:
            dynamic_volume = min_volume

        return dynamic_volume

    def OnReseted(self):
        super(monday_typical_breakout_strategy, self).OnReseted()
        self._previous_candle = None
        self._last_signal_time = None
        self._price_step = 0.0

    def CreateClone(self):
        return monday_typical_breakout_strategy()
