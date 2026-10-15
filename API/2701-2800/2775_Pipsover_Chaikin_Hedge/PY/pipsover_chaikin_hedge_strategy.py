import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, ExponentialMovingAverage, AccumulationDistributionLine
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class pipsover_chaikin_hedge_strategy(Strategy):
    def __init__(self):
        super(pipsover_chaikin_hedge_strategy, self).__init__()
        self._open_level = self.Param("OpenLevel", 0.01).SetGreaterThanZero().SetDisplay("Open Level", "Chaikin level for entries", "Chaikin")
        self._close_level = self.Param("CloseLevel", 0.02).SetGreaterThanZero().SetDisplay("Close Level", "Chaikin level for hedging", "Chaikin")
        self._sl_pips = self.Param("StopLossPips", 65.0).SetDisplay("Stop Loss (pips)", "Stop-loss distance", "Risk")
        self._tp_pips = self.Param("TakeProfitPips", 100.0).SetDisplay("Take Profit (pips)", "Take-profit distance", "Risk")
        self._ma_period = self.Param("MaPeriod", 20).SetGreaterThanZero().SetDisplay("MA Period", "Price MA length", "Trend")
        self._chaikin_fast = self.Param("ChaikinFastPeriod", 3).SetGreaterThanZero().SetDisplay("Chaikin Fast", "Fast Chaikin length", "Chaikin")
        self._chaikin_slow = self.Param("ChaikinSlowPeriod", 10).SetGreaterThanZero().SetDisplay("Chaikin Slow", "Slow Chaikin length", "Chaikin")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Timeframe", "Data")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(pipsover_chaikin_hedge_strategy, self).OnReseted()
        self._pip_size = 0
        self._entry_price = None
        self._stop_price = None
        self._tp_price = None
        self._prev_open = 0
        self._prev_close = 0
        self._prev_high = 0
        self._prev_low = 0
        self._prev_chaikin = 0
        self._has_prev = False
        self._has_prev_chaikin = False

    def OnStarted2(self, time):
        super(pipsover_chaikin_hedge_strategy, self).OnStarted2(time)
        self._pip_size = self._calc_pip_size()
        self._entry_price = None
        self._stop_price = None
        self._tp_price = None
        self._prev_open = 0
        self._prev_close = 0
        self._prev_high = 0
        self._prev_low = 0
        self._prev_chaikin = 0
        self._has_prev = False
        self._has_prev_chaikin = False

        self._adl = AccumulationDistributionLine()
        self._price_ma = SimpleMovingAverage()
        self._price_ma.Length = self._ma_period.Value
        self._ema_fast = ExponentialMovingAverage()
        self._ema_fast.Length = self._chaikin_fast.Value
        self._ema_slow = ExponentialMovingAverage()
        self._ema_slow.Length = self._chaikin_slow.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._price_ma, self._adl, self.OnProcess).Start()

    def _calc_pip_size(self):
        step = 0.0001
        if self.Security is not None and self.Security.PriceStep is not None and self.Security.PriceStep > 0:
            step = float(self.Security.PriceStep)
        return step

    def OnProcess(self, candle, ma_val, ad_val):
        if candle.State != CandleStates.Finished:
            return

        t = candle.ServerTime
        fast_res = process_float(self._ema_fast, Decimal(float(ad_val)), t, True)
        slow_res = process_float(self._ema_slow, Decimal(float(ad_val)), t, True)
        chaikin = float(fast_res.Value) - float(slow_res.Value)

        if not self._ema_fast.IsFormed or not self._ema_slow.IsFormed:
            self._prev_chaikin = chaikin
            self._has_prev_chaikin = True
            self._store_candle(candle)
            return

        has_prev_data = self._has_prev and self._has_prev_chaikin
        self._handle_stops(candle)

        if self.Position == 0 and has_prev_data:
            bullish = self._prev_close > self._prev_open
            bearish = self._prev_close < self._prev_open

            if bullish and self._prev_low < ma_val and self._prev_chaikin < -self._open_level.Value:
                self.BuyMarket()
                self._setup_long(candle.ClosePrice)
            elif bearish and self._prev_high > ma_val and self._prev_chaikin > self._open_level.Value:
                self.SellMarket()
                self._setup_short(candle.ClosePrice)
        elif self.Position != 0 and has_prev_data:
            bearish = self._prev_close < self._prev_open
            bullish = self._prev_close > self._prev_open

            if self.Position > 0 and bearish and self._prev_high > ma_val and self._prev_chaikin > self._close_level.Value:
                size = Math.Abs(self.Position) + self.Volume
                self.SellMarket(size)
                self._setup_short(candle.ClosePrice)
            elif self.Position < 0 and bullish and self._prev_low < ma_val and self._prev_chaikin < -self._close_level.Value:
                size = Math.Abs(self.Position) + self.Volume
                self.BuyMarket(size)
                self._setup_long(candle.ClosePrice)

        self._prev_chaikin = chaikin
        self._has_prev_chaikin = True
        self._store_candle(candle)

    def _store_candle(self, candle):
        self._prev_open = float(candle.OpenPrice)
        self._prev_close = float(candle.ClosePrice)
        self._prev_high = float(candle.HighPrice)
        self._prev_low = float(candle.LowPrice)
        self._has_prev = True

    def _handle_stops(self, candle):
        if self.Position > 0:
            if self._stop_price is not None and candle.LowPrice <= self._stop_price:
                self.SellMarket(Math.Abs(self.Position))
                self._reset_state()
            elif self._tp_price is not None and candle.HighPrice >= self._tp_price:
                self.SellMarket(Math.Abs(self.Position))
                self._reset_state()
        elif self.Position < 0:
            if self._stop_price is not None and candle.HighPrice >= self._stop_price:
                self.BuyMarket(Math.Abs(self.Position))
                self._reset_state()
            elif self._tp_price is not None and candle.LowPrice <= self._tp_price:
                self.BuyMarket(Math.Abs(self.Position))
                self._reset_state()

    def _setup_long(self, price):
        self._entry_price = float(price)
        self._stop_price = self._entry_price - self._sl_pips.Value * self._pip_size if self._sl_pips.Value > 0 else None
        self._tp_price = self._entry_price + self._tp_pips.Value * self._pip_size if self._tp_pips.Value > 0 else None

    def _setup_short(self, price):
        self._entry_price = float(price)
        self._stop_price = self._entry_price + self._sl_pips.Value * self._pip_size if self._sl_pips.Value > 0 else None
        self._tp_price = self._entry_price - self._tp_pips.Value * self._pip_size if self._tp_pips.Value > 0 else None

    def _reset_state(self):
        self._entry_price = None
        self._stop_price = None
        self._tp_price = None

    def CreateClone(self):
        return pipsover_chaikin_hedge_strategy()
