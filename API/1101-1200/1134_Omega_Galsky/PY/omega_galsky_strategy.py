import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class omega_galsky_strategy(Strategy):
    """
    Omega Galsky strategy.
    A long opens when the fast EMA crosses above the middle EMA while the close is above the slow EMA, a short on the opposite cross
    with the close below the slow EMA; an opposite signal reverses the position. The stop and the target are fractions of the entry
    price (SlPercentage and TpPercentage). Once price moves FixedRiskReward times the initial risk in favour, the stop moves to break-even.
    """

    def __init__(self):
        super(omega_galsky_strategy, self).__init__()
        self._ema8_period = self.Param("Ema8Period", 8).SetGreaterThanZero().SetDisplay("EMA 8 Period", "Fast EMA period", "Indicators")
        self._ema21_period = self.Param("Ema21Period", 21).SetGreaterThanZero().SetDisplay("EMA 21 Period", "Middle EMA period", "Indicators")
        self._ema89_period = self.Param("Ema89Period", 89).SetGreaterThanZero().SetDisplay("EMA 89 Period", "Slow EMA period used as price confirmation", "Indicators")
        self._fixed_risk_reward = self.Param("FixedRiskReward", 1.0).SetNotNegative().SetDisplay("Break-Even R", "Profit in multiples of risk that moves the stop to break-even", "Risk")
        self._sl_percentage = self.Param("SlPercentage", 0.001).SetNotNegative().SetDisplay("Stop Loss", "Stop loss as a fraction of entry price", "Risk")
        self._tp_percentage = self.Param("TpPercentage", 0.0025).SetNotNegative().SetDisplay("Take Profit", "Take profit as a fraction of entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_fast = None
        self._prev_middle = None
        self._entry_price = Decimal(0)
        self._stop_price = Decimal(0)
        self._take_price = Decimal(0)
        self._break_even_done = False

    def OnReseted(self):
        super(omega_galsky_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(omega_galsky_strategy, self).OnStarted2(time)

        self._reset_state()

        ema8 = ExponentialMovingAverage()
        ema8.Length = self._ema8_period.Value
        ema21 = ExponentialMovingAverage()
        ema21.Length = self._ema21_period.Value
        ema89 = ExponentialMovingAverage()
        ema89.Length = self._ema89_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema8, ema21, ema89, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema8)
            self.DrawIndicator(area, ema21)
            self.DrawIndicator(area, ema89)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast, middle, slow):
        if candle.State != CandleStates.Finished:
            return

        prev_fast = self._prev_fast
        prev_middle = self._prev_middle
        self._prev_fast = fast
        self._prev_middle = middle

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._manage_position(candle):
            return

        if prev_fast is None or prev_middle is None:
            return

        close = candle.ClosePrice

        if prev_fast <= prev_middle and fast > middle and close > slow and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._set_levels(close, True)
        elif prev_fast >= prev_middle and fast < middle and close < slow and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._set_levels(close, False)

    def _set_levels(self, entry, is_long):
        self._entry_price = entry
        self._break_even_done = False

        sl = Decimal(self._sl_percentage.Value)
        tp = Decimal(self._tp_percentage.Value)
        stop_distance = entry * sl
        take_distance = entry * tp

        if sl > 0:
            self._stop_price = entry - stop_distance if is_long else entry + stop_distance
        else:
            self._stop_price = Decimal(0)
        if tp > 0:
            self._take_price = entry + take_distance if is_long else entry - take_distance
        else:
            self._take_price = Decimal(0)

    # Returns True when the position was closed on this candle.
    def _manage_position(self, candle):
        risk = self._entry_price * Decimal(self._sl_percentage.Value)
        trigger = risk * Decimal(self._fixed_risk_reward.Value)

        if self.Position > 0:
            if self._stop_price > 0 and candle.LowPrice <= self._stop_price:
                self.SellMarket(self.Position)
                return True
            if self._take_price > 0 and candle.HighPrice >= self._take_price:
                self.SellMarket(self.Position)
                return True
            if not self._break_even_done and risk > 0 and candle.HighPrice - self._entry_price >= trigger:
                self._stop_price = self._entry_price
                self._break_even_done = True
        elif self.Position < 0:
            if self._stop_price > 0 and candle.HighPrice >= self._stop_price:
                self.BuyMarket(-self.Position)
                return True
            if self._take_price > 0 and candle.LowPrice <= self._take_price:
                self.BuyMarket(-self.Position)
                return True
            if not self._break_even_done and risk > 0 and self._entry_price - candle.LowPrice >= trigger:
                self._stop_price = self._entry_price
                self._break_even_done = True

        return False

    def CreateClone(self):
        return omega_galsky_strategy()
