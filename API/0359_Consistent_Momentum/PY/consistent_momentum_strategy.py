import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RateOfChange, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security


class consistent_momentum_strategy(Strategy):
    """Consistent momentum strategy using dual securities with medium and long-term ROC."""

    def __init__(self):
        super(consistent_momentum_strategy, self).__init__()

        self._security2_id = self.Param("Security2Id", "TONUSDT@BNBFT") \
            .SetDisplay("Benchmark Security Id", "Identifier of the benchmark security", "General")

        self._medium_momentum_length = self.Param("MediumMomentumLength", 18) \
            .SetRange(5, 80) \
            .SetDisplay("Medium Momentum Length", "Medium-term momentum length", "Indicators")

        self._long_momentum_length = self.Param("LongMomentumLength", 60) \
            .SetRange(20, 200) \
            .SetDisplay("Long Momentum Length", "Long-term momentum length", "Indicators")

        self._entry_margin = self.Param("EntryMargin", 1.5) \
            .SetRange(0.1, 20.0) \
            .SetDisplay("Entry Margin", "Minimum relative edge required to open a position", "Signals")

        self._exit_margin = self.Param("ExitMargin", 0.4) \
            .SetRange(0.0, 10.0) \
            .SetDisplay("Exit Margin", "Relative edge threshold used to close a position", "Signals")

        self._cooldown_bars = self.Param("CooldownBars", 8) \
            .SetRange(0, 100) \
            .SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "Risk")

        self._stop_loss = self.Param("StopLoss", 2.5) \
            .SetRange(0.5, 10.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle series for both instruments", "General")

        self._benchmark = None
        self._primary_medium_mom = None
        self._primary_long_mom = None
        self._benchmark_medium_mom = None
        self._benchmark_long_mom = None
        self._primary_updated = False
        self._benchmark_updated = False
        self._primary_medium_value = 0.0
        self._primary_long_value = 0.0
        self._benchmark_medium_value = 0.0
        self._benchmark_long_value = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        result = []
        if self.Security is not None:
            result.append((self.Security, self.candle_type))
        sec2_id = str(self._security2_id.Value)
        if sec2_id:
            s = Security()
            s.Id = sec2_id
            result.append((s, self.candle_type))
        return result

    def OnReseted(self):
        super(consistent_momentum_strategy, self).OnReseted()
        self._benchmark = None
        self._primary_medium_mom = None
        self._primary_long_mom = None
        self._benchmark_medium_mom = None
        self._benchmark_long_mom = None
        self._primary_updated = False
        self._benchmark_updated = False
        self._primary_medium_value = 0.0
        self._primary_long_value = 0.0
        self._benchmark_medium_value = 0.0
        self._benchmark_long_value = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(consistent_momentum_strategy, self).OnStarted(time)

        sec2_id = str(self._security2_id.Value)
        if not sec2_id:
            raise Exception("Benchmark security identifier is not specified.")

        s = Security()
        s.Id = sec2_id
        self._benchmark = s

        med_len = int(self._medium_momentum_length.Value)
        long_len = int(self._long_momentum_length.Value)

        self._primary_medium_mom = RateOfChange()
        self._primary_medium_mom.Length = med_len
        self._primary_long_mom = RateOfChange()
        self._primary_long_mom.Length = long_len
        self._benchmark_medium_mom = RateOfChange()
        self._benchmark_medium_mom.Length = med_len
        self._benchmark_long_mom = RateOfChange()
        self._benchmark_long_mom.Length = long_len

        primary_subscription = self.SubscribeCandles(self.candle_type, True, self.Security)
        benchmark_subscription = self.SubscribeCandles(self.candle_type, True, self._benchmark)

        primary_subscription.Bind(self.ProcessPrimaryCandle).Start()
        benchmark_subscription.Bind(self.ProcessBenchmarkCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, primary_subscription)
            self.DrawCandles(area, benchmark_subscription)
            self.DrawOwnTrades(area)

        self.StartProtection(
            Unit(2, UnitTypes.Percent),
            Unit(float(self._stop_loss.Value), UnitTypes.Percent)
        )

    def ProcessPrimaryCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        civ_med = CandleIndicatorValue(self._primary_medium_mom, candle)
        civ_med.IsFinal = True
        med_result = self._primary_medium_mom.Process(civ_med)

        civ_long = CandleIndicatorValue(self._primary_long_mom, candle)
        civ_long.IsFinal = True
        long_result = self._primary_long_mom.Process(civ_long)

        if not med_result.IsEmpty and not long_result.IsEmpty and self._primary_medium_mom.IsFormed and self._primary_long_mom.IsFormed:
            self._primary_medium_value = float(med_result)
            self._primary_long_value = float(long_result)
            self._primary_updated = True
            self.TryProcessSignal()

    def ProcessBenchmarkCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        civ_med = CandleIndicatorValue(self._benchmark_medium_mom, candle)
        civ_med.IsFinal = True
        med_result = self._benchmark_medium_mom.Process(civ_med)

        civ_long = CandleIndicatorValue(self._benchmark_long_mom, candle)
        civ_long.IsFinal = True
        long_result = self._benchmark_long_mom.Process(civ_long)

        if not med_result.IsEmpty and not long_result.IsEmpty and self._benchmark_medium_mom.IsFormed and self._benchmark_long_mom.IsFormed:
            self._benchmark_medium_value = float(med_result)
            self._benchmark_long_value = float(long_result)
            self._benchmark_updated = True
            self.TryProcessSignal()

    def TryProcessSignal(self):
        if not self._primary_updated or not self._benchmark_updated:
            return

        self._primary_updated = False
        self._benchmark_updated = False

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        entry_margin = float(self._entry_margin.Value)
        exit_margin = float(self._exit_margin.Value)
        cooldown = int(self._cooldown_bars.Value)

        medium_edge = self._primary_medium_value - self._benchmark_medium_value
        long_edge = self._primary_long_value - self._benchmark_long_value

        bullish_consistent = medium_edge >= entry_margin and long_edge >= entry_margin
        bearish_consistent = medium_edge <= -entry_margin and long_edge <= -entry_margin
        bullish_exit = medium_edge <= exit_margin or long_edge <= exit_margin
        bearish_exit = medium_edge >= -exit_margin or long_edge >= -exit_margin

        if self._cooldown_remaining == 0 and self.Position == 0:
            if bullish_consistent:
                self.BuyMarket()
                self._cooldown_remaining = cooldown
            elif bearish_consistent:
                self.SellMarket()
                self._cooldown_remaining = cooldown
        elif self.Position > 0 and bullish_exit:
            self.SellMarket(self.Position)
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and bearish_exit:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return consistent_momentum_strategy()
