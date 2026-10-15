import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class bull_row_breakout_strategy(Strategy):
    def __init__(self):
        super(bull_row_breakout_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._ema_period = self.Param("EmaPeriod", 20)
        self._bull_row_size = self.Param("BullRowSize", 2)
        self._breakout_lookback = self.Param("BreakoutLookback", 10)
        self._stop_loss_pct = self.Param("StopLossPct", 2.0)
        self._take_profit_pct = self.Param("TakeProfitPct", 3.0)

        self._candles = []
        self._entry_price = 0.0
        self._has_prev = False
        self._prev_close = 0.0
        self._prev_ema = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._ema_period.Value = value

    @property
    def BullRowSize(self):
        return self._bull_row_size.Value

    @BullRowSize.setter
    def BullRowSize(self, value):
        self._bull_row_size.Value = value

    @property
    def BreakoutLookback(self):
        return self._breakout_lookback.Value

    @BreakoutLookback.setter
    def BreakoutLookback(self, value):
        self._breakout_lookback.Value = value

    @property
    def StopLossPct(self):
        return self._stop_loss_pct.Value

    @StopLossPct.setter
    def StopLossPct(self, value):
        self._stop_loss_pct.Value = value

    @property
    def TakeProfitPct(self):
        return self._take_profit_pct.Value

    @TakeProfitPct.setter
    def TakeProfitPct(self, value):
        self._take_profit_pct.Value = value

    def OnReseted(self):
        super(bull_row_breakout_strategy, self).OnReseted()
        self._candles = []
        self._entry_price = 0.0
        self._has_prev = False
        self._prev_close = 0.0
        self._prev_ema = 0.0

    def OnStarted2(self, time):
        super(bull_row_breakout_strategy, self).OnStarted2(time)
        self._candles = []
        self._entry_price = 0.0
        self._has_prev = False
        self._prev_close = 0.0
        self._prev_ema = 0.0

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, self._process_candle).Start()

    def _has_bull_row(self):
        size = self.BullRowSize
        if len(self._candles) < size:
            return False
        for i in range(size):
            c = self._candles[-(i + 1)]
            if float(c.ClosePrice) <= float(c.OpenPrice):
                return False
        return True

    def _has_breakout(self):
        lookback = self.BreakoutLookback
        if len(self._candles) < lookback + 1:
            return False
        prev_close = float(self._candles[-1].ClosePrice)
        highest = float('-inf')
        for i in range(2, min(lookback + 1, len(self._candles) + 1)):
            if i <= len(self._candles):
                highest = max(highest, float(self._candles[-i].HighPrice))
        return prev_close > highest

    def _process_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        ema_val = float(ema_value)

        # Check SL/TP on existing position
        if self.Position != 0 and self._entry_price > 0:
            if self.Position > 0:
                pnl_pct = (close - self._entry_price) / self._entry_price * 100.0
                if pnl_pct <= -float(self.StopLossPct) or pnl_pct >= float(self.TakeProfitPct):
                    self.SellMarket()
                    self._entry_price = 0.0
            elif self.Position < 0:
                pnl_pct = (self._entry_price - close) / self._entry_price * 100.0
                if pnl_pct <= -float(self.StopLossPct) or pnl_pct >= float(self.TakeProfitPct):
                    self.BuyMarket()
                    self._entry_price = 0.0

        self._candles.append(candle)
        max_needed = max(self.BullRowSize, self.BreakoutLookback) + 5
        if len(self._candles) > max_needed:
            self._candles.pop(0)

        if self._has_prev:
            # Buy: EMA crossover + bull row + breakout
            if self._prev_close <= self._prev_ema and close > ema_val and self._has_bull_row() and self.Position <= 0:
                self.BuyMarket()
                self._entry_price = close
            elif self._prev_close >= self._prev_ema and close < ema_val and self.Position >= 0:
                self.SellMarket()
                self._entry_price = close

        self._prev_close = close
        self._prev_ema = ema_val
        self._has_prev = True

    def CreateClone(self):
        return bull_row_breakout_strategy()
