import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import SimpleMovingAverage

class app_price_level_cross_strategy(Strategy):
    def __init__(self):
        super(app_price_level_cross_strategy, self).__init__()

        self._app_price = self.Param("AppPrice", 65000.0) \
            .SetDisplay("App Price", "Reference level that generates trades when the close crosses it", "Trading")
        self._buy_only = self.Param("BuyOnly", True) \
            .SetDisplay("Buy Only", "Enable to trade only long entries", "Trading")
        self._fixed_volume = self.Param("FixedVolume", 0.1) \
            .SetDisplay("Fixed Volume", "Lot size used when money management is disabled", "Risk")
        self._stop_loss_points = self.Param("StopLossPoints", 140) \
            .SetDisplay("Stop Loss (points)", "Distance in price points for the protective stop", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 180) \
            .SetDisplay("Take Profit (points)", "Distance in price points for the profit target", "Risk")
        self._enable_money_management = self.Param("EnableMoneyManagement", False) \
            .SetDisplay("Enable MM", "Toggle balance-based position sizing", "Risk")
        self._lot_balance_percent = self.Param("LotBalancePercent", 10.0) \
            .SetDisplay("Balance %", "Percentage of balance used to compute the lot when MM is enabled", "Risk")
        self._min_lot = self.Param("MinLot", 0.1) \
            .SetDisplay("Minimum Lot", "Lower bound for the calculated lot size", "Risk")
        self._max_lot = self.Param("MaxLot", 5.0) \
            .SetDisplay("Maximum Lot", "Upper bound for the calculated lot size", "Risk")
        self._lot_precision = self.Param("LotPrecision", 1) \
            .SetDisplay("Lot Precision", "Number of decimal places to round the calculated lot size", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe used for signal generation", "General")

        self._previous_close = None

    @property
    def AppPrice(self):
        return self._app_price.Value

    @property
    def BuyOnly(self):
        return self._buy_only.Value

    @property
    def FixedVolume(self):
        return self._fixed_volume.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def EnableMoneyManagement(self):
        return self._enable_money_management.Value

    @property
    def LotBalancePercent(self):
        return self._lot_balance_percent.Value

    @property
    def MinLot(self):
        return self._min_lot.Value

    @property
    def MaxLot(self):
        return self._max_lot.Value

    @property
    def LotPrecision(self):
        return self._lot_precision.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(app_price_level_cross_strategy, self).OnStarted(time)

        self._dummy_sma = SimpleMovingAverage()
        self._dummy_sma.Length = 2

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._dummy_sma, self.ProcessCandle).Start()

        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        if step <= 0:
            step = 1.0

        tp_pts = int(self.TakeProfitPoints)
        sl_pts = int(self.StopLossPoints)
        if tp_pts > 0 or sl_pts > 0:
            take_dist = Unit(tp_pts * step, UnitTypes.Absolute) if tp_pts > 0 else Unit(0)
            stop_dist = Unit(sl_pts * step, UnitTypes.Absolute) if sl_pts > 0 else Unit(0)
            self.StartProtection(take_dist, stop_dist)

    def ProcessCandle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        previous_close = self._previous_close
        close_price = float(candle.ClosePrice)
        self._previous_close = close_price

        if previous_close is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        app_price = float(self.AppPrice)
        crossed_above = close_price > app_price and previous_close <= app_price
        crossed_below = close_price < app_price and previous_close >= app_price

        if crossed_above:
            self._execute_buy()
        elif crossed_below:
            self._execute_sell()

    def _execute_buy(self):
        if self.Position > 0:
            return

        base_volume = self._calculate_base_volume()
        if base_volume <= 0:
            return

        volume = base_volume
        if self.Position < 0:
            volume += abs(self.Position)

        volume = self._align_volume(volume)
        if volume <= 0:
            return

        self.BuyMarket(volume)

    def _execute_sell(self):
        if self.Position < 0:
            return

        base_volume = self._calculate_base_volume()
        if base_volume <= 0:
            return

        volume = base_volume
        if self.Position > 0:
            volume += abs(self.Position)

        volume = self._align_volume(volume)
        if volume <= 0:
            return

        self.SellMarket(volume)

    def _calculate_base_volume(self):
        if not self.EnableMoneyManagement:
            return float(self.FixedVolume)

        balance = 0.0
        if self.Portfolio is not None:
            cv = self.Portfolio.CurrentValue
            if cv is not None and float(cv) > 0:
                balance = float(cv)
            elif self.Portfolio.BeginValue is not None and float(self.Portfolio.BeginValue) > 0:
                balance = float(self.Portfolio.BeginValue)

        if balance <= 0:
            return float(self.FixedVolume)

        precision = int(self.LotPrecision)
        if precision <= 0:
            divisor = 1.0
            precision = 0
        elif precision == 2:
            divisor = 100.0
        else:
            divisor = 1000.0

        volume = float(self.LotBalancePercent) / 100.0 * balance / divisor
        volume = round(volume, precision)

        min_lot = float(self.MinLot)
        max_lot = float(self.MaxLot)
        if volume < min_lot:
            volume = min_lot
        if volume > max_lot:
            volume = max_lot

        return volume

    def _align_volume(self, volume):
        if self.Security is not None:
            min_vol = self.Security.MinVolume
            max_vol = self.Security.MaxVolume
            step = self.Security.VolumeStep

            min_v = float(min_vol) if min_vol is not None else 0.0
            max_v = float(max_vol) if max_vol is not None else 0.0
            s = float(step) if step is not None else 0.0

            if min_v > 0 and volume < min_v:
                volume = min_v
            if max_v > 0 and volume > max_v:
                volume = max_v
            if s > 0:
                volume = round(volume / s) * s

        return volume

    def OnReseted(self):
        super(app_price_level_cross_strategy, self).OnReseted()
        self._previous_close = None

    def CreateClone(self):
        return app_price_level_cross_strategy()
