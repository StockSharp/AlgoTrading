import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from StockSharp.Algo.Indicators import AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import DataType, CandleStates
from System import TimeSpan


class adx_expert_strategy(Strategy):
    def __init__(self):
        super(adx_expert_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 0.1)
        self._adx_period = self.Param("AdxPeriod", 14)
        self._adx_threshold = self.Param("AdxThreshold", 20.0)
        self._stop_loss_points = self.Param("StopLossPoints", 200.0)
        self._take_profit_points = self.Param("TakeProfitPoints", 400.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(2)))

        self._adx = None
        self._prev_plus_di = 0.0
        self._prev_minus_di = 0.0
        self._has_prev_di = False
        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(adx_expert_strategy, self).OnStarted(time)

        self._adx = AverageDirectionalIndex()
        self._adx.Length = self._adx_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        try:
            adx_result = self._adx.Process(candle)
        except Exception:
            return

        if adx_result.IsEmpty or not self._adx.IsFormed:
            return

        plus_di = float(adx_result.Dx.Plus) if adx_result.Dx.Plus is not None else 0.0
        minus_di = float(adx_result.Dx.Minus) if adx_result.Dx.Minus is not None else 0.0

        current_adx_val = adx_result.MovingAverage
        if current_adx_val is None:
            self._prev_plus_di = plus_di
            self._prev_minus_di = minus_di
            self._has_prev_di = True
            return

        current_adx = float(current_adx_val)

        if not self._has_prev_di:
            self._prev_plus_di = plus_di
            self._prev_minus_di = minus_di
            self._has_prev_di = True
            return

        # Manage SL/TP
        if self.Position != 0:
            step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
            if self.Position > 0:
                if self._stop_loss_points.Value > 0 and float(candle.LowPrice) <= self._entry_price - self._stop_loss_points.Value * step:
                    self.SellMarket(self.Position)
                    self._prev_plus_di = plus_di
                    self._prev_minus_di = minus_di
                    return
                if self._take_profit_points.Value > 0 and float(candle.HighPrice) >= self._entry_price + self._take_profit_points.Value * step:
                    self.SellMarket(self.Position)
                    self._prev_plus_di = plus_di
                    self._prev_minus_di = minus_di
                    return
            else:
                vol = abs(self.Position)
                if self._stop_loss_points.Value > 0 and float(candle.HighPrice) >= self._entry_price + self._stop_loss_points.Value * step:
                    self.BuyMarket(vol)
                    self._prev_plus_di = plus_di
                    self._prev_minus_di = minus_di
                    return
                if self._take_profit_points.Value > 0 and float(candle.LowPrice) <= self._entry_price - self._take_profit_points.Value * step:
                    self.BuyMarket(vol)
                    self._prev_plus_di = plus_di
                    self._prev_minus_di = minus_di
                    return

        bullish_cross = self._prev_plus_di <= self._prev_minus_di and plus_di > minus_di
        bearish_cross = self._prev_plus_di >= self._prev_minus_di and plus_di < minus_di

        if current_adx < self._adx_threshold.Value and self.Position == 0:
            if bullish_cross:
                self.BuyMarket(self._trade_volume.Value)
                self._entry_price = float(candle.ClosePrice)
            elif bearish_cross:
                self.SellMarket(self._trade_volume.Value)
                self._entry_price = float(candle.ClosePrice)

        self._prev_plus_di = plus_di
        self._prev_minus_di = minus_di

    def OnReseted(self):
        super(adx_expert_strategy, self).OnReseted()
        self._prev_plus_di = 0.0
        self._prev_minus_di = 0.0
        self._has_prev_di = False
        self._entry_price = 0.0

    def CreateClone(self):
        return adx_expert_strategy()
