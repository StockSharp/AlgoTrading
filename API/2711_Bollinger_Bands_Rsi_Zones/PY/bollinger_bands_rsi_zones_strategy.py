import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    BollingerBands,
    RelativeStrengthIndex,
    StochasticOscillator,
    CandleIndicatorValue,
)
from indicator_extensions import *

class bollinger_bands_rsi_zones_strategy(Strategy):
    """Bollinger Bands RSI Zones: three Bollinger bands with RSI and Stochastic filters."""

    # Entry modes (integer enum replacement):
    # 0 = BetweenYellowAndBlue, 1 = BetweenBlueAndRed,
    # 2 = YellowLine, 3 = BlueLine, 4 = RedLine

    # Closure modes (integer enum replacement):
    # 0 = MiddleLine, 1 = BetweenYellowAndBlue, 2 = BetweenBlueAndRed,
    # 3 = YellowLine, 4 = BlueLine, 5 = RedLine

    def __init__(self):
        super(bollinger_bands_rsi_zones_strategy, self).__init__()

        self._entry_mode = self.Param("EntryMode", 0) \
            .SetDisplay("Entry Mode", "Bollinger zone used for entries", "Trading")
        self._closure_mode = self.Param("ClosureMode", 2) \
            .SetDisplay("Closure Mode", "Bollinger zone used for exits", "Trading")
        self._bands_period = self.Param("BandsPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Bands Period", "Length of all Bollinger bands", "Indicators")
        self._deviation = self.Param("Deviation", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation", "Standard deviation for yellow band", "Indicators")
        self._use_rsi_filter = self.Param("UseRsiFilter", False) \
            .SetDisplay("Use RSI Filter", "Enable RSI confirmation", "Filters")
        self._rsi_period = self.Param("RsiPeriod", 8) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "Length of RSI filter", "Filters")
        self._rsi_lower_level = self.Param("RsiLowerLevel", 70.0) \
            .SetDisplay("RSI Lower", "Short threshold (long uses 100-threshold)", "Filters")
        self._use_stochastic_filter = self.Param("UseStochasticFilter", False) \
            .SetDisplay("Use Stochastic Filter", "Enable Stochastic confirmation", "Filters")
        self._stochastic_period = self.Param("StochasticPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic Period", "Main %K period", "Filters")
        self._stochastic_lower_level = self.Param("StochasticLowerLevel", 95.0) \
            .SetDisplay("Stochastic Lower", "Overbought threshold (long uses mirror)", "Filters")
        self._bar_shift = self.Param("BarShift", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Bar Shift", "Number of finished bars for signals", "Trading")
        self._only_one_position = self.Param("OnlyOnePosition", True) \
            .SetDisplay("Only One Position", "Restrict to single open position", "Risk")
        self._order_volume = self.Param("OrderVolume", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Order Volume", "Volume sent with each market order", "Trading")
        self._pip_value = self.Param("PipValue", 0.0001) \
            .SetGreaterThanZero() \
            .SetDisplay("Pip Value", "Monetary value of one pip", "Risk")
        self._stop_loss_pips = self.Param("StopLossPips", 200.0) \
            .SetDisplay("Stop Loss", "Stop loss distance in pips", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 200.0) \
            .SetDisplay("Take Profit", "Take profit distance in pips", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles for analysis", "General")

        self._teeth_middle_history = []
        self._teeth_upper_history = []
        self._teeth_lower_history = []
        self._jaws_upper_history = []
        self._jaws_lower_history = []
        self._lips_upper_history = []
        self._lips_lower_history = []
        self._rsi_history = []
        self._stochastic_history = []
        self._long_locked = False
        self._short_locked = False

    @property
    def EntryMode(self):
        return int(self._entry_mode.Value)
    @property
    def ClosureMode(self):
        return int(self._closure_mode.Value)
    @property
    def BandsPeriod(self):
        return int(self._bands_period.Value)
    @property
    def Deviation(self):
        return float(self._deviation.Value)
    @property
    def UseRsiFilter(self):
        return self._use_rsi_filter.Value
    @property
    def RsiPeriod(self):
        return int(self._rsi_period.Value)
    @property
    def RsiLowerLevel(self):
        return float(self._rsi_lower_level.Value)
    @property
    def UseStochasticFilter(self):
        return self._use_stochastic_filter.Value
    @property
    def StochasticPeriod(self):
        return int(self._stochastic_period.Value)
    @property
    def StochasticLowerLevel(self):
        return float(self._stochastic_lower_level.Value)
    @property
    def BarShift(self):
        return int(self._bar_shift.Value)
    @property
    def OnlyOnePosition(self):
        return self._only_one_position.Value
    @property
    def OrderVolume(self):
        return float(self._order_volume.Value)
    @property
    def PipValue(self):
        return float(self._pip_value.Value)
    @property
    def StopLossPips(self):
        return float(self._stop_loss_pips.Value)
    @property
    def TakeProfitPips(self):
        return float(self._take_profit_pips.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(bollinger_bands_rsi_zones_strategy, self).OnStarted2(time)

        dev = self.Deviation

        self._teeth = BollingerBands()
        self._teeth.Length = self.BandsPeriod
        self._teeth.Width = dev

        self._jaws = BollingerBands()
        self._jaws.Length = self.BandsPeriod
        self._jaws.Width = dev / 2.0

        self._lips = BollingerBands()
        self._lips.Length = self.BandsPeriod
        self._lips.Width = dev * 2.0

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiPeriod

        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = self.StochasticPeriod
        self._stochastic.D.Length = 3

        self._teeth_middle_history = []
        self._teeth_upper_history = []
        self._teeth_lower_history = []
        self._jaws_upper_history = []
        self._jaws_lower_history = []
        self._lips_upper_history = []
        self._lips_lower_history = []
        self._rsi_history = []
        self._stochastic_history = []
        self._long_locked = False
        self._short_locked = False

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._rsi, self.process_candle).Start()

        sec = self.Security
        pip_size = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else self.PipValue
        if pip_size <= 0:
            pip_size = self.PipValue

        tp = Unit(self.TakeProfitPips * pip_size, UnitTypes.Absolute) if self.TakeProfitPips > 0 else None
        sl = Unit(self.StopLossPips * pip_size, UnitTypes.Absolute) if self.StopLossPips > 0 else None
        if tp is not None and sl is not None:
            self.StartProtection(takeProfit=tp, stopLoss=sl)
        elif tp is not None:
            self.StartProtection(takeProfit=tp)
        elif sl is not None:
            self.StartProtection(stopLoss=sl)

    def process_candle(self, candle, rsi_decimal):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        t = candle.ServerTime

        teeth_result = process_float(self._teeth, candle.ClosePrice, t, True)

        jaws_result = process_float(self._jaws, candle.ClosePrice, t, True)

        lips_result = process_float(self._lips, candle.ClosePrice, t, True)
        stoch_result = self._stochastic.Process(CandleIndicatorValue(self._stochastic, candle))

        if not self._teeth.IsFormed or not self._jaws.IsFormed or not self._lips.IsFormed:
            return

        teeth_middle = float(teeth_result.MovingAverage) if teeth_result.MovingAverage is not None else 0.0
        teeth_upper = float(teeth_result.UpBand) if teeth_result.UpBand is not None else 0.0
        teeth_lower = float(teeth_result.LowBand) if teeth_result.LowBand is not None else 0.0
        jaws_upper = float(jaws_result.UpBand) if jaws_result.UpBand is not None else 0.0
        jaws_lower = float(jaws_result.LowBand) if jaws_result.LowBand is not None else 0.0
        lips_upper = float(lips_result.UpBand) if lips_result.UpBand is not None else 0.0
        lips_lower = float(lips_result.LowBand) if lips_result.LowBand is not None else 0.0

        rsi_value = float(rsi_decimal)
        stochastic_k = float(stoch_result.K) if stoch_result.K is not None else 50.0

        rsi_ready = not self.UseRsiFilter or self._rsi.IsFormed
        stochastic_ready = not self.UseStochasticFilter or self._stochastic.IsFormed

        if not rsi_ready or not stochastic_ready:
            self._update_history(teeth_middle, teeth_upper, teeth_lower,
                                 jaws_upper, jaws_lower, lips_upper, lips_lower,
                                 rsi_value, stochastic_k)
            return

        base_teeth = self._try_get_shifted(self._teeth_middle_history)
        upper_teeth = self._try_get_shifted(self._teeth_upper_history)
        lower_teeth = self._try_get_shifted(self._teeth_lower_history)
        u_jaws = self._try_get_shifted(self._jaws_upper_history)
        l_jaws = self._try_get_shifted(self._jaws_lower_history)
        u_lips = self._try_get_shifted(self._lips_upper_history)
        l_lips = self._try_get_shifted(self._lips_lower_history)

        if (base_teeth is None or upper_teeth is None or lower_teeth is None or
                u_jaws is None or l_jaws is None or u_lips is None or l_lips is None):
            self._update_history(teeth_middle, teeth_upper, teeth_lower,
                                 jaws_upper, jaws_lower, lips_upper, lips_lower,
                                 rsi_value, stochastic_k)
            return

        rsi_shifted = 50.0
        if self.UseRsiFilter:
            r = self._try_get_shifted(self._rsi_history)
            if r is None:
                self._update_history(teeth_middle, teeth_upper, teeth_lower,
                                     jaws_upper, jaws_lower, lips_upper, lips_lower,
                                     rsi_value, stochastic_k)
                return
            rsi_shifted = r

        stochastic_shifted = 50.0
        if self.UseStochasticFilter:
            s = self._try_get_shifted(self._stochastic_history)
            if s is None:
                self._update_history(teeth_middle, teeth_upper, teeth_lower,
                                     jaws_upper, jaws_lower, lips_upper, lips_lower,
                                     rsi_value, stochastic_k)
                return
            stochastic_shifted = s

        long_entry_price = self._get_long_entry_price(lower_teeth, l_jaws, l_lips)
        short_entry_price = self._get_short_entry_price(upper_teeth, u_jaws, u_lips)

        exit_long, exit_short = self._get_exit_levels(
            short_entry_price, long_entry_price, u_jaws, l_jaws, u_lips, l_lips)

        if not self.OnlyOnePosition:
            if close >= base_teeth:
                self._long_locked = False
            if close <= base_teeth:
                self._short_locked = False

        price_hit_long = float(candle.LowPrice) <= long_entry_price
        price_hit_short = float(candle.HighPrice) >= short_entry_price

        rsi_long_ok = not self.UseRsiFilter or rsi_shifted <= 100.0 - self.RsiLowerLevel
        rsi_short_ok = not self.UseRsiFilter or rsi_shifted >= self.RsiLowerLevel

        stoch_long_ok = not self.UseStochasticFilter or stochastic_shifted < 100.0 - self.StochasticLowerLevel
        stoch_short_ok = not self.UseStochasticFilter or stochastic_shifted > self.StochasticLowerLevel

        can_open_long = self.Position == 0 if self.OnlyOnePosition else self.Position >= 0
        can_open_short = self.Position == 0 if self.OnlyOnePosition else self.Position <= 0

        if price_hit_short and rsi_short_ok and stoch_short_ok and can_open_short:
            if self.OnlyOnePosition or not self._short_locked:
                self.SellMarket()
                self._short_locked = not self.OnlyOnePosition

        if price_hit_long and rsi_long_ok and stoch_long_ok and can_open_long:
            if self.OnlyOnePosition or not self._long_locked:
                self.BuyMarket()
                self._long_locked = not self.OnlyOnePosition

        # Exit logic
        cm = self.ClosureMode
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        if cm == 0:  # MiddleLine
            if self.Position > 0 and h >= base_teeth:
                self.SellMarket()
            if self.Position < 0 and lo <= base_teeth:
                self.BuyMarket()
        elif cm == 1 or cm == 2:  # BetweenYellowAndBlue or BetweenBlueAndRed
            if self.Position > 0 and h >= exit_long:
                self.SellMarket()
            if self.Position < 0 and lo <= exit_short:
                self.BuyMarket()
        elif cm == 3:  # YellowLine
            if self.Position > 0 and h >= upper_teeth:
                self.SellMarket()
            if self.Position < 0 and lo <= lower_teeth:
                self.BuyMarket()
        elif cm == 4:  # BlueLine
            if self.Position > 0 and h >= u_jaws:
                self.SellMarket()
            if self.Position < 0 and lo <= l_jaws:
                self.BuyMarket()
        elif cm == 5:  # RedLine
            if self.Position > 0 and h >= u_lips:
                self.SellMarket()
            if self.Position < 0 and lo <= l_lips:
                self.BuyMarket()

        self._update_history(teeth_middle, teeth_upper, teeth_lower,
                             jaws_upper, jaws_lower, lips_upper, lips_lower,
                             rsi_value, stochastic_k)

    def _get_long_entry_price(self, lower_teeth, lower_jaws, lower_lips):
        em = self.EntryMode
        if em == 0:  # BetweenYellowAndBlue
            return lower_teeth - (lower_teeth - lower_jaws) / 2.0
        elif em == 1:  # BetweenBlueAndRed
            return lower_jaws - (lower_jaws - lower_lips) / 2.0
        elif em == 2:  # YellowLine
            return lower_teeth
        elif em == 3:  # BlueLine
            return lower_jaws
        elif em == 4:  # RedLine
            return lower_lips
        return lower_teeth

    def _get_short_entry_price(self, upper_teeth, upper_jaws, upper_lips):
        em = self.EntryMode
        if em == 0:  # BetweenYellowAndBlue
            return upper_teeth + (upper_jaws - upper_teeth) / 2.0
        elif em == 1:  # BetweenBlueAndRed
            return upper_jaws + (upper_lips - upper_jaws) / 2.0
        elif em == 2:  # YellowLine
            return upper_teeth
        elif em == 3:  # BlueLine
            return upper_jaws
        elif em == 4:  # RedLine
            return upper_lips
        return upper_teeth

    def _get_exit_levels(self, short_entry_price, long_entry_price,
                         upper_jaws, lower_jaws, upper_lips, lower_lips):
        cm = self.ClosureMode
        em = self.EntryMode
        if (cm == 1 and em == 0) or (cm == 2 and em == 1):
            return (short_entry_price, long_entry_price)

        default_long = upper_jaws + (upper_lips - upper_jaws) / 2.0
        default_short = lower_jaws - (lower_jaws - lower_lips) / 2.0
        return (default_long, default_short)

    def _try_get_shifted(self, history):
        shift = self.BarShift
        if shift <= 0:
            return None
        if len(history) < shift:
            return None
        return history[0]

    def _update_history(self, teeth_middle, teeth_upper, teeth_lower,
                        jaws_upper, jaws_lower, lips_upper, lips_lower,
                        rsi_value, stochastic_k):
        shift = self.BarShift
        if shift <= 0:
            return

        self._enqueue(self._teeth_middle_history, teeth_middle)
        self._enqueue(self._teeth_upper_history, teeth_upper)
        self._enqueue(self._teeth_lower_history, teeth_lower)
        self._enqueue(self._jaws_upper_history, jaws_upper)
        self._enqueue(self._jaws_lower_history, jaws_lower)
        self._enqueue(self._lips_upper_history, lips_upper)
        self._enqueue(self._lips_lower_history, lips_lower)

        if self._rsi.IsFormed:
            self._enqueue(self._rsi_history, rsi_value)

        if self._stochastic.IsFormed:
            self._enqueue(self._stochastic_history, stochastic_k)

    def _enqueue(self, history, value):
        history.append(value)
        shift = self.BarShift
        while len(history) > shift:
            history.pop(0)

    def OnReseted(self):
        super(bollinger_bands_rsi_zones_strategy, self).OnReseted()
        self._teeth_middle_history = []
        self._teeth_upper_history = []
        self._teeth_lower_history = []
        self._jaws_upper_history = []
        self._jaws_lower_history = []
        self._lips_upper_history = []
        self._lips_lower_history = []
        self._rsi_history = []
        self._stochastic_history = []
        self._long_locked = False
        self._short_locked = False

    def CreateClone(self):
        return bollinger_bands_rsi_zones_strategy()
