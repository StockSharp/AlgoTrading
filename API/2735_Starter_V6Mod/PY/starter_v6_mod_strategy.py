import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan

from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import (
    ExponentialMovingAverage, CommodityChannelIndex, RelativeStrengthIndex
)
from StockSharp.Algo.Strategies import Strategy


class starter_v6_mod_strategy(Strategy):
    def __init__(self):
        super(starter_v6_mod_strategy, self).__init__()

        self._use_manual_volume = self.Param("UseManualVolume", True)
        self._manual_volume = self.Param("ManualVolume", 1.0)
        self._risk_percent = self.Param("RiskPercent", 5.0)
        self._stop_loss_pips = self.Param("StopLossPips", 35)
        self._take_profit_pips = self.Param("TakeProfitPips", 10)
        self._trailing_stop_pips = self.Param("TrailingStopPips", 0)
        self._trailing_step_pips = self.Param("TrailingStepPips", 5)
        self._decrease_factor = self.Param("DecreaseFactor", 1.6)
        self._max_losses_per_day = self.Param("MaxLossesPerDay", 3)
        self._equity_cutoff = self.Param("EquityCutoff", 800.0)
        self._max_open_trades = self.Param("MaxOpenTrades", 10)
        self._grid_step_pips = self.Param("GridStepPips", 30)
        self._long_ema_period = self.Param("LongEmaPeriod", 120)
        self._short_ema_period = self.Param("ShortEmaPeriod", 40)
        self._cci_period = self.Param("CciPeriod", 14)
        self._angle_threshold = self.Param("AngleThreshold", 3.0)
        self._level_up = self.Param("LevelUp", 0.85)
        self._level_down = self.Param("LevelDown", 0.15)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._long_ema = None
        self._short_ema = None
        self._cci = None
        self._laguerre_proxy = None
        self._prev_long_ema = None
        self._prev_short_ema = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def UseManualVolume(self):
        return self._use_manual_volume.Value

    @property
    def ManualVolume(self):
        return self._manual_volume.Value

    @property
    def RiskPercent(self):
        return self._risk_percent.Value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def TrailingStopPips(self):
        return self._trailing_stop_pips.Value

    @property
    def TrailingStepPips(self):
        return self._trailing_step_pips.Value

    @property
    def DecreaseFactor(self):
        return self._decrease_factor.Value

    @property
    def MaxLossesPerDay(self):
        return self._max_losses_per_day.Value

    @property
    def EquityCutoff(self):
        return self._equity_cutoff.Value

    @property
    def MaxOpenTrades(self):
        return self._max_open_trades.Value

    @property
    def GridStepPips(self):
        return self._grid_step_pips.Value

    @property
    def LongEmaPeriod(self):
        return self._long_ema_period.Value

    @property
    def ShortEmaPeriod(self):
        return self._short_ema_period.Value

    @property
    def CciPeriod(self):
        return self._cci_period.Value

    @property
    def AngleThreshold(self):
        return self._angle_threshold.Value

    @property
    def LevelUp(self):
        return self._level_up.Value

    @property
    def LevelDown(self):
        return self._level_down.Value

    def OnStarted(self, time):
        super(starter_v6_mod_strategy, self).OnStarted(time)

        self._long_ema = ExponentialMovingAverage()
        self._long_ema.Length = self.LongEmaPeriod
        self._short_ema = ExponentialMovingAverage()
        self._short_ema.Length = self.ShortEmaPeriod
        self._cci = CommodityChannelIndex()
        self._cci.Length = self.CciPeriod
        self._laguerre_proxy = RelativeStrengthIndex()
        self._laguerre_proxy.Length = 14

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._long_ema, self._short_ema, self._cci, self._laguerre_proxy, self._process_candle).Start()

        self.StartProtection(
            Unit(2, UnitTypes.Percent),
            Unit(1, UnitTypes.Percent))

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._long_ema)
            self.DrawIndicator(area, self._short_ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, long_ema_v, short_ema_v, cci_v, rsi_v):
        if candle.State != CandleStates.Finished:
            return

        if self._prev_long_ema is None or self._prev_short_ema is None:
            self._prev_long_ema = long_ema_v
            self._prev_short_ema = short_ema_v
            return

        if self.Position != 0:
            self._prev_long_ema = long_ema_v
            self._prev_short_ema = short_ema_v
            return

        laguerre = rsi_v / 100.0

        buy_signal = laguerre < self.LevelDown and cci_v < 0
        sell_signal = laguerre > self.LevelUp and cci_v > 0

        if buy_signal:
            self.BuyMarket()
        elif sell_signal:
            self.SellMarket()

        self._prev_long_ema = long_ema_v
        self._prev_short_ema = short_ema_v

    def OnReseted(self):
        super(starter_v6_mod_strategy, self).OnReseted()
        self._long_ema = None
        self._short_ema = None
        self._cci = None
        self._laguerre_proxy = None
        self._prev_long_ema = None
        self._prev_short_ema = None

    def CreateClone(self):
        return starter_v6_mod_strategy()
