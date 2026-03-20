import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy


class adx_crossing_strategy(Strategy):

    def __init__(self):
        super(adx_crossing_strategy, self).__init__()

        self._adx_period = self.Param("AdxPeriod", 50) \
            .SetDisplay("ADX Period", "Period of ADX indicator", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles for calculations", "General")
        self._allow_buy_open = self.Param("AllowBuyOpen", True) \
            .SetDisplay("Allow Buy Open", "Enable opening long positions", "Permissions")
        self._allow_sell_open = self.Param("AllowSellOpen", True) \
            .SetDisplay("Allow Sell Open", "Enable opening short positions", "Permissions")
        self._allow_buy_close = self.Param("AllowBuyClose", True) \
            .SetDisplay("Allow Buy Close", "Enable closing long positions", "Permissions")
        self._allow_sell_close = self.Param("AllowSellClose", True) \
            .SetDisplay("Allow Sell Close", "Enable closing short positions", "Permissions")
        self._stop_loss = self.Param("StopLoss", 1000.0) \
            .SetDisplay("Stop Loss", "Absolute stop-loss in price units", "Risk")
        self._take_profit = self.Param("TakeProfit", 2000.0) \
            .SetDisplay("Take Profit", "Absolute take-profit in price units", "Risk")
        self._trend_threshold = self.Param("TrendThreshold", 15.0) \
            .SetDisplay("Trend Threshold", "Minimal ADX strength required to trade", "Indicators")

        self._prev_plus_di = 0.0
        self._prev_minus_di = 0.0
        self._is_initialized = False

    @property
    def AdxPeriod(self):
        return self._adx_period.Value

    @AdxPeriod.setter
    def AdxPeriod(self, value):
        self._adx_period.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def AllowBuyOpen(self):
        return self._allow_buy_open.Value

    @AllowBuyOpen.setter
    def AllowBuyOpen(self, value):
        self._allow_buy_open.Value = value

    @property
    def AllowSellOpen(self):
        return self._allow_sell_open.Value

    @AllowSellOpen.setter
    def AllowSellOpen(self, value):
        self._allow_sell_open.Value = value

    @property
    def AllowBuyClose(self):
        return self._allow_buy_close.Value

    @AllowBuyClose.setter
    def AllowBuyClose(self, value):
        self._allow_buy_close.Value = value

    @property
    def AllowSellClose(self):
        return self._allow_sell_close.Value

    @AllowSellClose.setter
    def AllowSellClose(self, value):
        self._allow_sell_close.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def TrendThreshold(self):
        return self._trend_threshold.Value

    @TrendThreshold.setter
    def TrendThreshold(self, value):
        self._trend_threshold.Value = value

    def OnStarted(self, time):
        super(adx_crossing_strategy, self).OnStarted(time)

        adx = AverageDirectionalIndex()
        adx.Length = self.AdxPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .BindEx(adx, self.ProcessCandle) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, adx)
            self.DrawOwnTrades(area)

        sl = Unit(self.StopLoss, UnitTypes.Absolute) if self.StopLoss > 0 else None
        tp = Unit(self.TakeProfit, UnitTypes.Absolute) if self.TakeProfit > 0 else None
        self.StartProtection(stopLoss=sl, takeProfit=tp)

    def ProcessCandle(self, candle, adx_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        plus_raw = adx_value.Dx.Plus
        minus_raw = adx_value.Dx.Minus
        if plus_raw is None or minus_raw is None:
            return

        plus_di = float(plus_raw)
        minus_di = float(minus_raw)

        if not self._is_initialized:
            self._prev_plus_di = plus_di
            self._prev_minus_di = minus_di
            self._is_initialized = True
            return

        ma_raw = adx_value.MovingAverage
        if ma_raw is None:
            self._prev_plus_di = plus_di
            self._prev_minus_di = minus_di
            return

        adx_strength = float(ma_raw)
        if adx_strength < float(self.TrendThreshold):
            self._prev_plus_di = plus_di
            self._prev_minus_di = minus_di
            return

        buy_signal = plus_di > minus_di and self._prev_plus_di <= self._prev_minus_di
        sell_signal = plus_di < minus_di and self._prev_plus_di >= self._prev_minus_di

        if buy_signal:
            if self.AllowBuyOpen and self.Position <= 0:
                volume = self.Volume + abs(self.Position) if self.Position < 0 else self.Volume
                self.BuyMarket(volume)
            elif self.AllowSellClose and self.Position < 0:
                self.BuyMarket(abs(self.Position))

        if sell_signal:
            if self.AllowSellOpen and self.Position >= 0:
                volume = self.Volume + self.Position if self.Position > 0 else self.Volume
                self.SellMarket(volume)
            elif self.AllowBuyClose and self.Position > 0:
                self.SellMarket(abs(self.Position))

        self._prev_plus_di = plus_di
        self._prev_minus_di = minus_di

    def OnReseted(self):
        super(adx_crossing_strategy, self).OnReseted()
        self._prev_plus_di = 0.0
        self._prev_minus_di = 0.0
        self._is_initialized = False

    def CreateClone(self):
        return adx_crossing_strategy()
