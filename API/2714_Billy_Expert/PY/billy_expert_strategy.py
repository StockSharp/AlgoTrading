import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import StochasticOscillator


class billy_expert_strategy(Strategy):
    """Billy Expert: dual timeframe Stochastic with decreasing highs/opens pattern for long entries."""

    def __init__(self):
        super(billy_expert_strategy, self).__init__()

        self._volume_tolerance = self.Param("VolumeTolerance", 0.0000001) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume Tolerance", "Tolerance for comparing volume sums", "Risk")
        self._trade_volume = self.Param("TradeVolume", 0.01) \
            .SetGreaterThanZero() \
            .SetDisplay("Trade Volume", "Order size for each entry", "General")
        self._stop_loss_pips = self.Param("StopLossPips", 0) \
            .SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 320) \
            .SetDisplay("Take Profit (pips)", "Profit target distance in pips", "Risk")
        self._max_positions = self.Param("MaxPositions", 6) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Positions", "Maximum number of open trades", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Signal Candle", "Primary timeframe used for price filters", "General")
        self._stochastic_time_frame1 = self.Param("StochasticTimeFrame1", TimeSpan.FromHours(1)) \
            .SetDisplay("Fast Stochastic TF", "Timeframe for the fast Stochastic", "Indicators")
        self._stochastic_time_frame2 = self.Param("StochasticTimeFrame2", TimeSpan.FromHours(4)) \
            .SetDisplay("Slow Stochastic TF", "Timeframe for the slow Stochastic", "Indicators")

        self._open1 = 0.0
        self._open2 = 0.0
        self._open3 = 0.0
        self._open4 = 0.0
        self._high1 = 0.0
        self._high2 = 0.0
        self._high3 = 0.0
        self._high4 = 0.0
        self._history_count = 0

        self._fast_main_current = 0.0
        self._fast_main_previous = 0.0
        self._fast_signal_current = 0.0
        self._fast_signal_previous = 0.0
        self._fast_has_current = False
        self._fast_has_previous = False

        self._slow_main_current = 0.0
        self._slow_main_previous = 0.0
        self._slow_signal_current = 0.0
        self._slow_signal_previous = 0.0
        self._slow_has_current = False
        self._slow_has_previous = False

        self._pip_size = 0.0

    @property
    def VolumeTolerance(self):
        return float(self._volume_tolerance.Value)
    @property
    def TradeVolume(self):
        return float(self._trade_volume.Value)
    @property
    def StopLossPips(self):
        return int(self._stop_loss_pips.Value)
    @property
    def TakeProfitPips(self):
        return int(self._take_profit_pips.Value)
    @property
    def MaxPositions(self):
        return int(self._max_positions.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def StochasticTimeFrame1(self):
        return self._stochastic_time_frame1.Value
    @property
    def StochasticTimeFrame2(self):
        return self._stochastic_time_frame2.Value

    def _calc_pip_size(self):
        sec = self.Security
        if sec is None or sec.PriceStep is None:
            return 1.0
        step = float(sec.PriceStep)
        if step <= 0:
            return 1.0
        decimals = 0
        if sec.Decimals is not None:
            decimals = int(sec.Decimals)
        else:
            v = abs(step)
            while v != int(v) and decimals < 10:
                v *= 10
                decimals += 1
        return step * 10.0 if (decimals == 3 or decimals == 5) else step

    def OnStarted2(self, time):
        super(billy_expert_strategy, self).OnStarted2(time)

        self._open1 = 0.0
        self._open2 = 0.0
        self._open3 = 0.0
        self._open4 = 0.0
        self._high1 = 0.0
        self._high2 = 0.0
        self._high3 = 0.0
        self._high4 = 0.0
        self._history_count = 0

        self._fast_main_current = 0.0
        self._fast_main_previous = 0.0
        self._fast_signal_current = 0.0
        self._fast_signal_previous = 0.0
        self._fast_has_current = False
        self._fast_has_previous = False

        self._slow_main_current = 0.0
        self._slow_main_previous = 0.0
        self._slow_signal_current = 0.0
        self._slow_signal_previous = 0.0
        self._slow_has_current = False
        self._slow_has_previous = False

        self._fast_stochastic = StochasticOscillator()
        self._fast_stochastic.K.Length = 14
        self._fast_stochastic.D.Length = 3

        self._slow_stochastic = StochasticOscillator()
        self._slow_stochastic.K.Length = 14
        self._slow_stochastic.D.Length = 3

        candle_subscription = self.SubscribeCandles(self.CandleType)
        candle_subscription.Bind(self.process_signal_candle).Start()

        fast_subscription = self.SubscribeCandles(DataType.TimeFrame(self.StochasticTimeFrame1))
        fast_subscription.BindEx(self._fast_stochastic, self.process_fast_stochastic).Start()

        slow_subscription = self.SubscribeCandles(DataType.TimeFrame(self.StochasticTimeFrame2))
        slow_subscription.BindEx(self._slow_stochastic, self.process_slow_stochastic).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, candle_subscription)
            self.DrawOwnTrades(area)

        self._pip_size = self._calc_pip_size()

        tp = Unit(self.TakeProfitPips * self._pip_size, UnitTypes.Absolute) if self.TakeProfitPips > 0 else Unit()
        sl = Unit(self.StopLossPips * self._pip_size, UnitTypes.Absolute) if self.StopLossPips > 0 else Unit()
        self.StartProtection(tp, sl)

    def process_fast_stochastic(self, candle, value):
        if candle.State != CandleStates.Finished:
            return
        if not self._fast_stochastic.IsFormed:
            return

        if self._fast_has_current:
            self._fast_main_previous = self._fast_main_current
            self._fast_signal_previous = self._fast_signal_current
            self._fast_has_previous = True

        self._fast_main_current = float(value.K) if value.K is not None else 0.0
        self._fast_signal_current = float(value.D) if value.D is not None else 0.0
        self._fast_has_current = True

    def process_slow_stochastic(self, candle, value):
        if candle.State != CandleStates.Finished:
            return
        if not self._slow_stochastic.IsFormed:
            return

        if self._slow_has_current:
            self._slow_main_previous = self._slow_main_current
            self._slow_signal_previous = self._slow_signal_current
            self._slow_has_previous = True

        self._slow_main_current = float(value.K) if value.K is not None else 0.0
        self._slow_signal_current = float(value.D) if value.D is not None else 0.0
        self._slow_has_current = True

    def process_signal_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._history_count >= 4 and self._fast_has_previous and self._slow_has_previous:
            decreasing_highs = (self._high1 < self._high2 and
                                self._high2 < self._high3 and
                                self._high3 < self._high4)
            decreasing_opens = (self._open1 < self._open2 and
                                self._open2 < self._open3 and
                                self._open3 < self._open4)
            fast_bullish = (self._fast_main_previous > self._fast_signal_previous and
                            self._fast_main_current > self._fast_signal_current)
            slow_bullish = (self._slow_main_previous > self._slow_signal_previous and
                            self._slow_main_current > self._slow_signal_current)

            max_long_volume = self.MaxPositions * self.TradeVolume
            current_long_volume = max(self.Position, 0.0)
            projected_volume = current_long_volume + self.TradeVolume

            if (decreasing_highs and decreasing_opens and fast_bullish and slow_bullish and
                    projected_volume <= max_long_volume + self.VolumeTolerance):
                self.BuyMarket()

        self._high4 = self._high3
        self._high3 = self._high2
        self._high2 = self._high1
        self._high1 = float(candle.HighPrice)

        self._open4 = self._open3
        self._open3 = self._open2
        self._open2 = self._open1
        self._open1 = float(candle.OpenPrice)

        if self._history_count < 4:
            self._history_count += 1

    def OnReseted(self):
        super(billy_expert_strategy, self).OnReseted()
        self._open1 = 0.0
        self._open2 = 0.0
        self._open3 = 0.0
        self._open4 = 0.0
        self._high1 = 0.0
        self._high2 = 0.0
        self._high3 = 0.0
        self._high4 = 0.0
        self._history_count = 0
        self._fast_main_current = 0.0
        self._fast_main_previous = 0.0
        self._fast_signal_current = 0.0
        self._fast_signal_previous = 0.0
        self._fast_has_current = False
        self._fast_has_previous = False
        self._slow_main_current = 0.0
        self._slow_main_previous = 0.0
        self._slow_signal_current = 0.0
        self._slow_signal_previous = 0.0
        self._slow_has_current = False
        self._slow_has_previous = False
        self._pip_size = 0.0

    def CreateClone(self):
        return billy_expert_strategy()
