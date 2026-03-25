import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import DeMarker
from StockSharp.Algo.Strategies import Strategy


class universum_30_strategy(Strategy):
    def __init__(self):
        super(universum_30_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        self._demarker_period = self.Param("DemarkerPeriod", 10)
        self._take_profit_points = self.Param("TakeProfitPoints", 50.0)
        self._stop_loss_points = self.Param("StopLossPoints", 50.0)
        self._initial_volume = self.Param("InitialVolume", 1.0)
        self._losses_limit = self.Param("LossesLimit", 100)

        self._current_volume = 0.0
        self._losses = 0
        self._last_pnl = 0.0
        self._prev_demarker = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def DemarkerPeriod(self):
        return self._demarker_period.Value

    @DemarkerPeriod.setter
    def DemarkerPeriod(self, value):
        self._demarker_period.Value = value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @TakeProfitPoints.setter
    def TakeProfitPoints(self, value):
        self._take_profit_points.Value = value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @StopLossPoints.setter
    def StopLossPoints(self, value):
        self._stop_loss_points.Value = value

    @property
    def InitialVolume(self):
        return self._initial_volume.Value

    @InitialVolume.setter
    def InitialVolume(self, value):
        self._initial_volume.Value = value

    @property
    def LossesLimit(self):
        return self._losses_limit.Value

    @LossesLimit.setter
    def LossesLimit(self, value):
        self._losses_limit.Value = value

    def OnStarted(self, time):
        super(universum_30_strategy, self).OnStarted(time)

        self._current_volume = float(self.InitialVolume)
        self._losses = 0
        self._last_pnl = 0.0
        self._prev_demarker = 0.0
        self._has_prev = False

        self.StartProtection(
            Unit(float(self.TakeProfitPoints), UnitTypes.Absolute),
            Unit(float(self.StopLossPoints), UnitTypes.Absolute))

        demarker = DeMarker()
        demarker.Length = self.DemarkerPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(demarker, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, demarker_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        dv = float(demarker_value)

        buy_signal = self._has_prev and self._prev_demarker <= 0.3 and dv > 0.3
        sell_signal = self._has_prev and self._prev_demarker >= 0.7 and dv < 0.7

        pos = float(self.Position)

        if buy_signal and pos <= 0:
            volume = self._current_volume + abs(pos)
            self.BuyMarket(volume)
        elif sell_signal and pos >= 0:
            volume = self._current_volume + abs(pos)
            self.SellMarket(volume)

        self._prev_demarker = dv
        self._has_prev = True

    def OnPositionReceived(self, position):
        super(universum_30_strategy, self).OnPositionReceived(position)

        if float(self.Position) != 0:
            return

        trade_pnl = float(self.PnL) - self._last_pnl
        self._last_pnl = float(self.PnL)

        if trade_pnl > 0:
            self._current_volume = float(self.InitialVolume)
            self._losses = 0
        elif trade_pnl < 0:
            self._current_volume *= 2
            self._losses += 1
            if self._losses >= int(self.LossesLimit):
                self.Stop()

    def OnReseted(self):
        super(universum_30_strategy, self).OnReseted()
        self._current_volume = 0.0
        self._losses = 0
        self._last_pnl = 0.0
        self._prev_demarker = 0.0
        self._has_prev = False

    def CreateClone(self):
        return universum_30_strategy()
