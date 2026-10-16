import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import process_float


class dema_rsi_strategy(Strategy):
    """RSI(DEMA) versus its own smoothing, with optional point protection and session."""

    def __init__(self):
        super(dema_rsi_strategy, self).__init__()

        self._ma_length = self.Param("MaLength", 20) \
            .SetGreaterThanZero().SetDisplay("MA Length", "DEMA EMA lengths", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetGreaterThanZero().SetDisplay("RSI Length", "RSI(DEMA) period", "Indicators")
        self._rsi_smoothing_length = self.Param("RsiSmoothingLength", 5) \
            .SetGreaterThanZero().SetDisplay("RSI Smoothing Length", "SMA of RSI", "Indicators")
        self._take_profit_points = self.Param("TakeProfitPoints", 0.0) \
            .SetRange(0.0, 100000.0).SetDisplay("Take Profit Points", "Price ticks, zero disables", "Risk")
        self._trail_stop_points = self.Param("TrailStopPoints", 0.0) \
            .SetRange(0.0, 100000.0).SetDisplay("Trail Stop Points", "Price ticks, zero disables", "Risk")
        self._use_session = self.Param("UseSession", False) \
            .SetDisplay("Use Session", "Limit entries to session", "Calendar")
        self._session_start = self.Param("SessionStart", TimeSpan.FromHours(9)) \
            .SetDisplay("Session Start", "Inclusive entry start", "Calendar")
        self._session_end = self.Param("SessionEnd", TimeSpan.FromHours(17)) \
            .SetDisplay("Session End", "Exclusive entry end", "Calendar")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Completed candle interval", "General")

        self._ema1 = None
        self._ema2 = None
        self._rsi = None
        self._smooth_rsi = None
        self._previous_rsi = None
        self._previous_smooth = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(dema_rsi_strategy, self).OnReseted()
        self._ema1 = None
        self._ema2 = None
        self._rsi = None
        self._smooth_rsi = None
        self._previous_rsi = None
        self._previous_smooth = None

    def OnStarted2(self, time):
        super(dema_rsi_strategy, self).OnStarted2(time)

        start = self._session_start.Value
        end = self._session_end.Value
        if start < TimeSpan.Zero or start >= TimeSpan.FromDays(1) or end < TimeSpan.Zero or end >= TimeSpan.FromDays(1):
            raise ValueError("Session times must be within one day")

        length = int(self._ma_length.Value)
        self._ema1 = ExponentialMovingAverage()
        self._ema1.Length = length
        self._ema2 = ExponentialMovingAverage()
        self._ema2.Length = length
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = int(self._rsi_length.Value)
        self._smooth_rsi = SimpleMovingAverage()
        self._smooth_rsi.Length = int(self._rsi_smoothing_length.Value)
        self._previous_rsi = None
        self._previous_smooth = None

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema1, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema1)
            self.DrawOwnTrades(area)

        tick = float(self.Security.PriceStep) if self.Security is not None else 0.0
        tp_points = float(self._take_profit_points.Value)
        trail_points = float(self._trail_stop_points.Value)
        if tick > 0 and (tp_points > 0 or trail_points > 0):
            self.StartProtection(
                Unit(tp_points * tick, UnitTypes.Absolute),
                Unit(trail_points * tick, UnitTypes.Absolute),
                trail_points > 0
            )

    def _is_in_session(self, time):
        if not bool(self._use_session.Value):
            return True

        start = self._session_start.Value
        end = self._session_end.Value
        if start == end:
            return True
        if start < end:
            return time >= start and time < end
        return time >= start or time < end

    def _process_candle(self, candle, first_ema):
        if candle.State != CandleStates.Finished:
            return

        ema1 = float(first_ema)
        ema2 = float(process_float(self._ema2, Decimal(ema1), candle.OpenTime, True))
        dema = 2.0 * ema1 - ema2
        rsi = float(process_float(self._rsi, Decimal(dema), candle.OpenTime, True))
        smoothed = float(process_float(self._smooth_rsi, Decimal(rsi), candle.OpenTime, True))

        cross_up = (
            self._previous_rsi is not None and self._previous_smooth is not None and
            self._previous_rsi <= self._previous_smooth and rsi > smoothed
        )
        cross_down = (
            self._previous_rsi is not None and self._previous_smooth is not None and
            self._previous_rsi >= self._previous_smooth and rsi < smoothed
        )

        self._previous_rsi = rsi
        self._previous_smooth = smoothed

        if not self._ema1.IsFormed or not self._ema2.IsFormed or not self._rsi.IsFormed or not self._smooth_rsi.IsFormed:
            return
        if not self.IsFormedAndOnlineAndAllowTrading() or not self._is_in_session(candle.OpenTime.TimeOfDay):
            return

        if cross_up and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif cross_down and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))

    def CreateClone(self):
        return dema_rsi_strategy()
