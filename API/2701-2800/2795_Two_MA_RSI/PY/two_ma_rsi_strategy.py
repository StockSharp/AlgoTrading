import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class two_ma_rsi_strategy(Strategy):

    def __init__(self):
        super(two_ma_rsi_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 5)
        self._slow_length = self.Param("SlowLength", 20)
        self._rsi_length = self.Param("RsiLength", 14)
        self._rsi_overbought = self.Param("RsiOverbought", 50.0)
        self._rsi_oversold = self.Param("RsiOversold", 50.0)
        self._stop_loss_points = self.Param("StopLossPoints", 500.0)
        self._take_profit_points = self.Param("TakeProfitPoints", 1500.0)
        self._balance_divider = self.Param("BalanceDivider", 1000.0)
        self._max_doublings = self.Param("MaxDoublings", 1)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._fast_ema = None
        self._slow_ema = None
        self._rsi = None
        self._previous_fast = None
        self._previous_slow = None
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_profit_price = 0.0
        self._martingale_stage = 0
        self._is_closing = False

    @property
    def FastLength(self):
        return self._fast_length.Value

    @property
    def SlowLength(self):
        return self._slow_length.Value

    @property
    def RsiLength(self):
        return self._rsi_length.Value

    @property
    def RsiOverbought(self):
        return self._rsi_overbought.Value

    @property
    def RsiOversold(self):
        return self._rsi_oversold.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def BalanceDivider(self):
        return self._balance_divider.Value

    @property
    def MaxDoublings(self):
        return self._max_doublings.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(two_ma_rsi_strategy, self).OnStarted2(time)

        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = self.FastLength
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = self.SlowLength
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        pos = float(self.Position)
        if pos == 0 and self._is_closing:
            self._is_closing = False
            self._entry_price = 0.0
            self._stop_price = 0.0
            self._take_profit_price = 0.0

        civ1 = CandleIndicatorValue(self._fast_ema, candle)
        civ1.IsFinal = True
        fast_result = self._fast_ema.Process(civ1)
        civ2 = CandleIndicatorValue(self._slow_ema, candle)
        civ2.IsFinal = True
        slow_result = self._slow_ema.Process(civ2)
        civ3 = CandleIndicatorValue(self._rsi, candle)
        civ3.IsFinal = True
        rsi_result = self._rsi.Process(civ3)

        if fast_result.IsEmpty or slow_result.IsEmpty or rsi_result.IsEmpty:
            return

        if not self._fast_ema.IsFormed or not self._slow_ema.IsFormed or not self._rsi.IsFormed:
            try:
                self._previous_fast = float(fast_result.Value)
                self._previous_slow = float(slow_result.Value)
            except:
                pass
            return

        try:
            fast = float(fast_result.Value)
            slow = float(slow_result.Value)
            rsi = float(rsi_result.Value)
        except:
            return

        point = self._get_point()
        pos = float(self.Position)

        if pos > 0:
            stop_hit = float(candle.LowPrice) <= self._stop_price
            take_hit = float(candle.HighPrice) >= self._take_profit_price

            if not self._is_closing and stop_hit:
                self._is_closing = True
                self._close_position()
                self._register_loss()
            elif not self._is_closing and take_hit:
                self._is_closing = True
                self._close_position()
                self._register_win()
        elif pos < 0:
            stop_hit = float(candle.HighPrice) >= self._stop_price
            take_hit = float(candle.LowPrice) <= self._take_profit_price

            if not self._is_closing and stop_hit:
                self._is_closing = True
                self._close_position()
                self._register_loss()
            elif not self._is_closing and take_hit:
                self._is_closing = True
                self._close_position()
                self._register_win()
        elif not self._is_closing:
            if self._previous_fast is None or self._previous_slow is None:
                self._previous_fast = fast
                self._previous_slow = slow
                return

            prev_fast = self._previous_fast
            prev_slow = self._previous_slow

            cross_up = prev_fast < prev_slow and fast > slow and rsi < float(self.RsiOversold)
            cross_down = prev_fast > prev_slow and fast < slow and rsi > float(self.RsiOverbought)

            if cross_up:
                volume = self._calculate_order_volume()
                if volume > 0:
                    self.BuyMarket(volume)
                    self._entry_price = float(candle.ClosePrice)
                    self._stop_price = self._entry_price - float(self.StopLossPoints) * point
                    self._take_profit_price = self._entry_price + float(self.TakeProfitPoints) * point
            elif cross_down:
                volume = self._calculate_order_volume()
                if volume > 0:
                    self.SellMarket(volume)
                    self._entry_price = float(candle.ClosePrice)
                    self._stop_price = self._entry_price + float(self.StopLossPoints) * point
                    self._take_profit_price = self._entry_price - float(self.TakeProfitPoints) * point

        self._previous_fast = fast
        self._previous_slow = slow

    def _get_point(self):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        return step if step > 0 else 1.0

    def _calculate_order_volume(self):
        sec = self.Security
        step = float(sec.VolumeStep) if sec is not None and sec.VolumeStep is not None else 1.0
        if step <= 0:
            step = 1.0

        base_volume = step
        divider = float(self.BalanceDivider)
        portfolio = self.Portfolio
        balance = 0.0
        if portfolio is not None:
            if portfolio.CurrentValue is not None:
                balance = float(portfolio.CurrentValue)
            elif portfolio.BeginValue is not None:
                balance = float(portfolio.BeginValue)

        if divider > 0 and balance > 0:
            import math
            count = math.floor(balance / divider)
            base_volume = count * step
            if base_volume < step:
                base_volume = step

        multiplier = self._calculate_martingale_multiplier()
        volume = base_volume * multiplier

        if volume < step:
            volume = step

        import math
        ratio = volume / step
        volume = math.ceil(ratio) * step

        return volume

    def _calculate_martingale_multiplier(self):
        if self.MaxDoublings <= 0 or self._martingale_stage <= 0:
            return 1.0
        stage = min(self._martingale_stage, self.MaxDoublings)
        return 2.0 ** stage

    def _register_win(self):
        self._martingale_stage = 0

    def _register_loss(self):
        if self.MaxDoublings <= 0:
            self._martingale_stage = 0
            return
        if self._martingale_stage < self.MaxDoublings:
            self._martingale_stage += 1
        else:
            self._martingale_stage = 0

    def _close_position(self):
        pos = float(self.Position)
        if pos > 0:
            self.SellMarket(pos)
        elif pos < 0:
            self.BuyMarket(abs(pos))

    def OnReseted(self):
        super(two_ma_rsi_strategy, self).OnReseted()
        self._fast_ema = None
        self._slow_ema = None
        self._rsi = None
        self._previous_fast = None
        self._previous_slow = None
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_profit_price = 0.0
        self._martingale_stage = 0
        self._is_closing = False

    def CreateClone(self):
        return two_ma_rsi_strategy()
