import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class moving_average_crossover_swing_strategy(Strategy):
    """
    Moving average crossover swing strategy.
    A fast EMA crossing above the medium EMA goes long and crossing below goes short (each side can be disabled).
    Entries can require the close to be on the trade side of the slow EMA and the MACD histogram to be on the same side of zero.
    Stop loss and take profit are ATR multiples fixed at entry, and a cross of the exit EMA pair against the position can also close it.
    """

    def __init__(self):
        super(moving_average_crossover_swing_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 5).SetGreaterThanZero().SetDisplay("Fast Period", "Fast EMA period for entries", "Indicators")
        self._medium_period = self.Param("MediumPeriod", 10).SetGreaterThanZero().SetDisplay("Medium Period", "Medium EMA period for entries", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 50).SetGreaterThanZero().SetDisplay("Slow Period", "Slow EMA period for the trend filter", "Indicators")
        self._fast_exit_period = self.Param("FastExitPeriod", 5).SetGreaterThanZero().SetDisplay("Fast Exit Period", "Fast EMA period for the exit cross", "Exit")
        self._medium_exit_period = self.Param("MediumExitPeriod", 10).SetGreaterThanZero().SetDisplay("Medium Exit Period", "Medium EMA period for the exit cross", "Exit")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "ATR period", "Risk")
        self._atr_stop_multiplier = self.Param("AtrStopMultiplier", 1.4).SetNotNegative().SetDisplay("ATR Stop Multiplier", "Stop loss distance in ATR multiples", "Risk")
        self._atr_take_multiplier = self.Param("AtrTakeMultiplier", 3.2).SetNotNegative().SetDisplay("ATR Take Multiplier", "Take profit distance in ATR multiples", "Risk")
        self._enable_slow = self.Param("EnableSlow", True).SetDisplay("Enable Slow EMA", "Require the close on the trade side of the slow EMA", "Filters")
        self._enable_macd = self.Param("EnableMacd", True).SetDisplay("Enable MACD", "Require the MACD histogram on the trade side of zero", "Filters")
        self._enable_long = self.Param("EnableLong", True).SetDisplay("Enable Long", "Allow long trades", "Trading")
        self._enable_short = self.Param("EnableShort", False).SetDisplay("Enable Short", "Allow short trades", "Trading")
        self._enable_cross_exit = self.Param("EnableCrossExit", True).SetDisplay("Enable Cross Exit", "Close when the exit EMA pair crosses against the position", "Exit")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_fast = None
        self._prev_medium = None
        self._prev_fast_exit = None
        self._prev_medium_exit = None
        self._stop_price = None
        self._take_price = None

    def OnReseted(self):
        super(moving_average_crossover_swing_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(moving_average_crossover_swing_strategy, self).OnStarted2(time)

        self._reset_state()

        fast = ExponentialMovingAverage()
        fast.Length = self._fast_period.Value
        medium = ExponentialMovingAverage()
        medium.Length = self._medium_period.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._slow_period.Value
        fast_exit = ExponentialMovingAverage()
        fast_exit.Length = self._fast_exit_period.Value
        medium_exit = ExponentialMovingAverage()
        medium_exit.Length = self._medium_exit_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        macd = MovingAverageConvergenceDivergenceSignal()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(fast, medium, slow, fast_exit, medium_exit, atr, macd, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, medium)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, macd)

    def _process_candle(self, candle, fast_value, medium_value, slow_value, fast_exit_value, medium_exit_value, atr_value, macd_value):
        if candle.State != CandleStates.Finished:
            return

        if not (fast_value.IsFormed and medium_value.IsFormed and slow_value.IsFormed and fast_exit_value.IsFormed
                and medium_exit_value.IsFormed and atr_value.IsFormed and macd_value.IsFormed):
            return

        if macd_value.Macd is None or macd_value.Signal is None:
            return

        fast = fast_value.GetValue[Decimal](None)
        medium = medium_value.GetValue[Decimal](None)
        slow = slow_value.GetValue[Decimal](None)
        fast_exit = fast_exit_value.GetValue[Decimal](None)
        medium_exit = medium_exit_value.GetValue[Decimal](None)
        atr = atr_value.GetValue[Decimal](None)
        histogram = macd_value.Macd - macd_value.Signal

        pf = self._prev_fast
        pm = self._prev_medium
        pfe = self._prev_fast_exit
        pme = self._prev_medium_exit
        self._prev_fast = fast
        self._prev_medium = medium
        self._prev_fast_exit = fast_exit
        self._prev_medium_exit = medium_exit

        if pf is None or pm is None or pfe is None or pme is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice

        if self.Position > 0:
            stop_hit = self._stop_price is not None and candle.LowPrice <= self._stop_price
            take_hit = self._take_price is not None and candle.HighPrice >= self._take_price
            cross_exit = self._enable_cross_exit.Value and pfe >= pme and fast_exit < medium_exit
            if stop_hit or take_hit or cross_exit:
                self.SellMarket(self.Position)
                self._stop_price = None
                self._take_price = None
                return
        elif self.Position < 0:
            stop_hit = self._stop_price is not None and candle.HighPrice >= self._stop_price
            take_hit = self._take_price is not None and candle.LowPrice <= self._take_price
            cross_exit = self._enable_cross_exit.Value and pfe <= pme and fast_exit > medium_exit
            if stop_hit or take_hit or cross_exit:
                self.BuyMarket(-self.Position)
                self._stop_price = None
                self._take_price = None
                return

        enable_slow = self._enable_slow.Value
        enable_macd = self._enable_macd.Value

        long_signal = (self._enable_long.Value and pf <= pm and fast > medium
                       and (not enable_slow or close > slow)
                       and (not enable_macd or histogram > 0))

        short_signal = (self._enable_short.Value and pf >= pm and fast < medium
                        and (not enable_slow or close < slow)
                        and (not enable_macd or histogram < 0))

        stop_mult = Decimal(self._atr_stop_multiplier.Value)
        take_mult = Decimal(self._atr_take_multiplier.Value)

        if long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - atr * stop_mult if stop_mult > 0 else None
            self._take_price = close + atr * take_mult if take_mult > 0 else None
        elif short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + atr * stop_mult if stop_mult > 0 else None
            self._take_price = close - atr * take_mult if take_mult > 0 else None

    def CreateClone(self):
        return moving_average_crossover_swing_strategy()
