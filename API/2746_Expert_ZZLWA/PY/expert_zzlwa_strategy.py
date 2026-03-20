import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan

from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import (
    Highest, Lowest, SmoothedMovingAverage, SimpleMovingAverage
)
from StockSharp.Algo.Strategies import Strategy


class expert_zzlwa_strategy(Strategy):
    def __init__(self):
        super(expert_zzlwa_strategy, self).__init__()

        self._stop_loss_points = self.Param("StopLossPoints", 600)
        self._take_profit_points = self.Param("TakeProfitPoints", 700)
        self._base_volume = self.Param("BaseVolume", 0.01)
        self._use_martingale = self.Param("UseMartingale", False)
        self._martingale_multiplier = self.Param("MartingaleMultiplier", 2.0)
        self._maximum_volume = self.Param("MaximumVolume", 10.0)
        self._mode = self.Param("Mode", 2)
        self._term_level = self.Param("ZigZagTerm", 2)
        self._slow_ma_period = self.Param("SlowMaPeriod", 150)
        self._fast_ma_period = self.Param("FastMaPeriod", 10)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._highest = None
        self._lowest = None
        self._slow_ma = None
        self._fast_ma = None
        self._pending_buy = False
        self._pending_sell = False
        self._original_buy_ready = True
        self._original_sell_ready = True
        self._zigzag_direction = 0
        self._prev_slow = 0.0
        self._prev_fast = 0.0
        self._last_closed_volume = 0.01
        self._last_trade_loss = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def BaseVolume(self):
        return self._base_volume.Value

    @property
    def UseMartingale(self):
        return self._use_martingale.Value

    @property
    def MartingaleMultiplier(self):
        return self._martingale_multiplier.Value

    @property
    def MaximumVolume(self):
        return self._maximum_volume.Value

    @property
    def Mode(self):
        return self._mode.Value

    @property
    def ZigZagTerm(self):
        return self._term_level.Value

    @property
    def SlowMaPeriod(self):
        return self._slow_ma_period.Value

    @property
    def FastMaPeriod(self):
        return self._fast_ma_period.Value

    def OnStarted(self, time):
        super(expert_zzlwa_strategy, self).OnStarted(time)

        step = self._get_price_step()
        self.StartProtection(
            Unit(self.TakeProfitPoints * step, UnitTypes.Absolute),
            Unit(self.StopLossPoints * step, UnitTypes.Absolute))

        self._original_buy_ready = True
        self._original_sell_ready = True
        self._pending_buy = False
        self._pending_sell = False
        self._last_closed_volume = self.BaseVolume
        self._last_trade_loss = False

        subscription = self.SubscribeCandles(self.CandleType)

        if self.Mode == 0:
            subscription.Bind(self._process_original).Start()
        elif self.Mode == 1:
            depth = self._get_zigzag_depth()
            self._highest = Highest()
            self._highest.Length = depth
            self._lowest = Lowest()
            self._lowest.Length = depth
            subscription.Bind(self._highest, self._lowest, self._process_addition).Start()
        else:
            self._slow_ma = SmoothedMovingAverage()
            self._slow_ma.Length = self.SlowMaPeriod
            self._fast_ma = SimpleMovingAverage()
            self._fast_ma.Length = self.FastMaPeriod
            subscription.Bind(self._slow_ma, self._fast_ma, self._process_ma).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            if self.Mode == 1 and self._highest is not None:
                self.DrawIndicator(area, self._highest)
                self.DrawIndicator(area, self._lowest)
            elif self.Mode == 2 and self._slow_ma is not None:
                self.DrawIndicator(area, self._slow_ma)
                self.DrawIndicator(area, self._fast_ma)
            self.DrawOwnTrades(area)

    def _process_original(self, candle):
        if candle.State != CandleStates.Finished:
            return
        if self.Position == 0:
            if self._original_buy_ready:
                self._execute_trade(True)
                self._original_buy_ready = False
                self._original_sell_ready = True
            elif self._original_sell_ready:
                self._execute_trade(False)
                self._original_sell_ready = False
                self._original_buy_ready = True

    def _process_addition(self, candle, highest, lowest):
        if candle.State != CandleStates.Finished:
            return
        if not self._highest.IsFormed or not self._lowest.IsFormed:
            return
        hv = float(highest)
        lv = float(lowest)
        if float(candle.HighPrice) >= hv and self._zigzag_direction != 1:
            self._pending_sell = True
            self._pending_buy = False
            self._zigzag_direction = 1
        elif float(candle.LowPrice) <= lv and self._zigzag_direction != -1:
            self._pending_buy = True
            self._pending_sell = False
            self._zigzag_direction = -1
        self._dispatch_signals()

    def _process_ma(self, candle, slow, fast):
        if candle.State != CandleStates.Finished:
            return
        if not self._slow_ma.IsFormed or not self._fast_ma.IsFormed:
            return
        sv = float(slow)
        fv = float(fast)
        cross_down = self._prev_slow > self._prev_fast and sv < fv
        cross_up = self._prev_slow < self._prev_fast and sv > fv
        self._prev_slow = sv
        self._prev_fast = fv

        if cross_up:
            self._pending_buy = True
            self._pending_sell = False
        elif cross_down:
            self._pending_sell = True
            self._pending_buy = False

        self._dispatch_signals()

    def _dispatch_signals(self):
        if self._pending_buy:
            self._execute_trade(True)
            self._pending_buy = False
            self._pending_sell = False
        elif self._pending_sell:
            self._execute_trade(False)
            self._pending_sell = False
            self._pending_buy = False

    def _execute_trade(self, is_buy):
        vol = self._get_order_volume()
        if vol <= 0:
            return
        if is_buy:
            self.BuyMarket()
        else:
            self.SellMarket()

    def _get_order_volume(self):
        if not self.UseMartingale:
            return self.BaseVolume
        if not self._last_trade_loss:
            return self.BaseVolume
        nv = self._last_closed_volume * self.MartingaleMultiplier
        return nv if nv <= self.MaximumVolume else self.BaseVolume

    def _get_zigzag_depth(self):
        if self.ZigZagTerm == 0:
            return 12
        elif self.ZigZagTerm == 1:
            return 24
        return 48

    def _get_price_step(self):
        sec = self.Security
        return float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0

    def OnReseted(self):
        super(expert_zzlwa_strategy, self).OnReseted()
        self._highest = None
        self._lowest = None
        self._slow_ma = None
        self._fast_ma = None
        self._pending_buy = False
        self._pending_sell = False
        self._original_buy_ready = True
        self._original_sell_ready = True
        self._zigzag_direction = 0
        self._prev_slow = 0.0
        self._prev_fast = 0.0
        self._last_closed_volume = self.BaseVolume
        self._last_trade_loss = False

    def CreateClone(self):
        return expert_zzlwa_strategy()
