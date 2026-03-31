import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy

class pos_neg_di_crossover_strategy(Strategy):
    def __init__(self):
        super(pos_neg_di_crossover_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        self._adx_period = self.Param("AdxPeriod", 14)
        self._stop_loss_pct = self.Param("StopLossPct", 2.0)
        self._take_profit_pct = self.Param("TakeProfitPct", 3.0)

        self._prev_plus_di = 0.0
        self._prev_minus_di = 0.0
        self._di_initialized = False
        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def AdxPeriod(self):
        return self._adx_period.Value

    @AdxPeriod.setter
    def AdxPeriod(self, value):
        self._adx_period.Value = value

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
        super(pos_neg_di_crossover_strategy, self).OnReseted()
        self._prev_plus_di = 0.0
        self._prev_minus_di = 0.0
        self._di_initialized = False
        self._entry_price = 0.0

    def OnStarted2(self, time):
        super(pos_neg_di_crossover_strategy, self).OnStarted2(time)
        self._prev_plus_di = 0.0
        self._prev_minus_di = 0.0
        self._di_initialized = False
        self._entry_price = 0.0

        adx = AverageDirectionalIndex()
        adx.Length = self.AdxPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(adx, self._process_candle).Start()

    def _process_candle(self, candle, adx_value):
        if not adx_value.IsFinal:
            return

        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)

        plus_di = adx_value.Dx.Plus
        minus_di = adx_value.Dx.Minus

        if plus_di is None or minus_di is None:
            return

        plus_di_val = float(plus_di)
        minus_di_val = float(minus_di)

        # Check SL/TP on existing position
        if self.Position != 0 and self._entry_price > 0:
            if self.Position > 0:
                pnl_pct = (close - self._entry_price) / self._entry_price * 100.0
                if pnl_pct <= -float(self.StopLossPct) or pnl_pct >= float(self.TakeProfitPct):
                    self.SellMarket()
                    self._entry_price = 0.0
                    self._prev_plus_di = plus_di_val
                    self._prev_minus_di = minus_di_val
                    return
            elif self.Position < 0:
                pnl_pct = (self._entry_price - close) / self._entry_price * 100.0
                if pnl_pct <= -float(self.StopLossPct) or pnl_pct >= float(self.TakeProfitPct):
                    self.BuyMarket()
                    self._entry_price = 0.0
                    self._prev_plus_di = plus_di_val
                    self._prev_minus_di = minus_di_val
                    return

        if not self._di_initialized:
            self._prev_plus_di = plus_di_val
            self._prev_minus_di = minus_di_val
            self._di_initialized = True
            return

        bullish_cross = plus_di_val > minus_di_val and self._prev_plus_di <= self._prev_minus_di
        bearish_cross = plus_di_val < minus_di_val and self._prev_plus_di >= self._prev_minus_di

        if bullish_cross and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = close
        elif bearish_cross and self.Position >= 0:
            self.SellMarket()
            self._entry_price = close

        self._prev_plus_di = plus_di_val
        self._prev_minus_di = minus_di_val

    def CreateClone(self):
        return pos_neg_di_crossover_strategy()
