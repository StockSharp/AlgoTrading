import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergence, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class momo_trades_v3_strategy(Strategy):
    """
    Momo Trades V3: MACD momentum patterns with displaced EMA filter.
    Simplified from C# (no breakeven, no auto volume).
    """

    def __init__(self):
        super(momo_trades_v3_strategy, self).__init__()
        self._ema_period = self.Param("MaPeriod", 22).SetDisplay("EMA Period", "EMA filter length", "Indicators")
        self._ema_shift = self.Param("MaShift", 1).SetDisplay("EMA Shift", "Bars offset for EMA", "Indicators")
        self._macd_fast = self.Param("FastPeriod", 12).SetDisplay("MACD Fast", "Fast EMA", "Indicators")
        self._macd_slow = self.Param("SlowPeriod", 26).SetDisplay("MACD Slow", "Slow EMA", "Indicators")
        self._macd_shift = self.Param("MacdShift", 1).SetDisplay("MACD Shift", "MACD history offset", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 20).SetDisplay("Cooldown", "Min bars between entries", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

        self._macd_history = []
        self._ema_history = []
        self._close_history = []
        self._bars_from_signal = 20

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(momo_trades_v3_strategy, self).OnReseted()
        self._macd_history = []
        self._ema_history = []
        self._close_history = []
        self._bars_from_signal = self._cooldown_bars.Value

    def OnStarted(self, time):
        super(momo_trades_v3_strategy, self).OnStarted(time)
        macd = MovingAverageConvergenceDivergence()
        macd.ShortMa.Length = self._macd_fast.Value
        macd.LongMa.Length = self._macd_slow.Value
        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(macd, ema, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, macd_val, ema_val):
        if candle.State != CandleStates.Finished:
            return
        macd_f = float(macd_val)
        ema_f = float(ema_val)
        close = float(candle.ClosePrice)
        self._macd_history.insert(0, macd_f)
        self._ema_history.insert(0, ema_f)
        self._close_history.insert(0, close)
        if len(self._macd_history) > 64:
            self._macd_history = self._macd_history[:64]
        if len(self._ema_history) > 64:
            self._ema_history = self._ema_history[:64]
        if len(self._close_history) > 64:
            self._close_history = self._close_history[:64]
        self._bars_from_signal += 1
        if self.Position != 0:
            return
        shift = self._macd_shift.Value
        ema_shift = self._ema_shift.Value
        can_buy = self._eval_macd_buy(shift) and self._eval_ema_buy(ema_shift)
        can_sell = self._eval_macd_sell(shift) and self._eval_ema_sell(ema_shift)
        if self._bars_from_signal >= self._cooldown_bars.Value and can_buy:
            self.BuyMarket()
            self._bars_from_signal = 0
        elif self._bars_from_signal >= self._cooldown_bars.Value and can_sell:
            self.SellMarket()
            self._bars_from_signal = 0

    def _get_macd(self, idx):
        if idx < 0 or idx >= len(self._macd_history):
            return None
        return self._macd_history[idx]

    def _eval_macd_buy(self, shift):
        vals = [self._get_macd(shift + i) for i in range(3, 9)]
        if any(v is None for v in vals):
            return False
        m3, m4, m5, m6, m7, m8 = vals
        p1 = m3 > m4 and m4 > m5 and abs(m5) < 1e-8 and m5 > m6 and m6 > m7
        p2 = m3 > m4 and m4 > m5 and m5 >= 0 and m6 <= 0 and m6 > m7 and m7 > m8
        return p1 or p2

    def _eval_macd_sell(self, shift):
        vals = [self._get_macd(shift + i) for i in range(3, 9)]
        if any(v is None for v in vals):
            return False
        m3, m4, m5, m6, m7, m8 = vals
        p1 = m3 < m4 and m4 < m5 and abs(m5) < 1e-8 and m5 < m6 and m6 < m7
        p2 = m3 < m4 and m4 < m5 and m5 <= 0 and m6 >= 0 and m6 < m7 and m7 < m8
        return p1 or p2

    def _eval_ema_buy(self, shift):
        if shift >= len(self._close_history) or shift >= len(self._ema_history):
            return False
        return self._close_history[shift] - self._ema_history[shift] > 0

    def _eval_ema_sell(self, shift):
        if shift >= len(self._close_history) or shift >= len(self._ema_history):
            return False
        return self._ema_history[shift] - self._close_history[shift] > 0

    def CreateClone(self):
        return momo_trades_v3_strategy()
