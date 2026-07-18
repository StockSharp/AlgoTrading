import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit, Level1Fields
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ExponentialMovingAverage, CommodityChannelIndex, StochasticOscillator

class kloss_simple_strategy(Strategy):
    def __init__(self):
        super(kloss_simple_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 0.1).SetDisplay("Volume", "Base order volume", "Trading")
        self._ma_period = self.Param("MaPeriod", 5).SetDisplay("EMA Period", "Length of the exponential moving average", "Indicators")
        self._cci_period = self.Param("CciPeriod", 10).SetDisplay("CCI Period", "Length of the commodity channel index", "Indicators")
        self._cci_level = self.Param("CciLevel", 200.0).SetDisplay("CCI Level", "Distance from zero to trigger signals", "Indicators")
        self._stochastic_k_period = self.Param("StochasticKPeriod", 5).SetDisplay("Stochastic %K", "Period of the %K line", "Indicators")
        self._stochastic_d_period = self.Param("StochasticDPeriod", 3).SetDisplay("Stochastic %D", "Period of the %D line", "Indicators")
        self._stochastic_smooth = self.Param("StochasticSmooth", 3).SetDisplay("Stochastic Smooth", "Smoothing factor for %K", "Indicators")
        self._stochastic_level = self.Param("StochasticLevel", 30.0).SetDisplay("Stochastic Level", "Distance from 50 to trigger signals", "Indicators")
        self._max_orders = self.Param("MaxOrders", 1).SetDisplay("Max Orders", "Maximum number of positions per direction", "Trading")
        self._stop_loss_points = self.Param("StopLossPoints", 0.0).SetDisplay("Stop Loss (pts)", "Stop loss distance in points", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 0.0).SetDisplay("Take Profit (pts)", "Take profit distance in points", "Risk")
        self._risk_percentage = self.Param("RiskPercentage", 10.0).SetDisplay("Risk %", "Portfolio percentage for dynamic position sizing", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Primary candle series for calculations", "General")

        self._previous_cci = None

    @property
    def OrderVolume(self): return self._order_volume.Value
    @property
    def MaPeriod(self): return self._ma_period.Value
    @property
    def CciPeriod(self): return self._cci_period.Value
    @property
    def CciLevel(self): return self._cci_level.Value
    @property
    def StochasticKPeriod(self): return self._stochastic_k_period.Value
    @property
    def StochasticDPeriod(self): return self._stochastic_d_period.Value
    @property
    def StochasticSmooth(self): return self._stochastic_smooth.Value
    @property
    def StochasticLevel(self): return self._stochastic_level.Value
    @property
    def MaxOrders(self): return self._max_orders.Value
    @property
    def StopLossPoints(self): return self._stop_loss_points.Value
    @property
    def TakeProfitPoints(self): return self._take_profit_points.Value
    @property
    def RiskPercentage(self): return self._risk_percentage.Value
    @property
    def CandleType(self): return self._candle_type.Value

    def OnStarted2(self, time):
        super(kloss_simple_strategy, self).OnStarted2(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.MaPeriod
        self._cci = CommodityChannelIndex()
        self._cci.Length = self.CciPeriod
        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = self.StochasticKPeriod
        self._stochastic.D.Length = self.StochasticDPeriod

        self.Volume = self.OrderVolume

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._ema, self._cci, self._stochastic, self.ProcessIndicators).Start()

        price_step = 0.0
        if self.Security is not None and self.Security.PriceStep is not None:
            price_step = float(self.Security.PriceStep)

        sl_unit = None
        tp_unit = None
        sl_pts = float(self.StopLossPoints)
        tp_pts = float(self.TakeProfitPoints)

        if sl_pts > 0 and price_step > 0:
            sl_unit = Unit(sl_pts * price_step, UnitTypes.Absolute)
        if tp_pts > 0 and price_step > 0:
            tp_unit = Unit(tp_pts * price_step, UnitTypes.Absolute)

        if sl_unit is not None or tp_unit is not None:
            self.StartProtection(stopLoss=sl_unit, takeProfit=tp_unit)

    def ProcessIndicators(self, candle, ema_value, cci_value, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        if ema_value.IsEmpty or cci_value.IsEmpty or stoch_value.IsEmpty:
            return

        stoch_k = stoch_value.K if hasattr(stoch_value, 'K') else None
        if stoch_k is None:
            return

        cci_val = float(cci_value)
        stoch_k_val = float(stoch_k)

        lower_stoch = 50.0 - float(self.StochasticLevel)
        upper_stoch = 50.0 + float(self.StochasticLevel)
        cci_lev = float(self.CciLevel)

        # Buy signal: CCI crosses up through -level from oversold territory
        cci_buy_xover = self._previous_cci is not None and self._previous_cci < -cci_lev and cci_val >= -cci_lev
        # Sell signal: CCI crosses down through +level from overbought territory
        cci_sell_xover = self._previous_cci is not None and self._previous_cci > cci_lev and cci_val <= cci_lev

        if cci_buy_xover and stoch_k_val < lower_stoch:
            self._close_short_positions()
            self._try_enter_long(candle)
        elif cci_sell_xover and stoch_k_val > upper_stoch:
            self._close_long_positions()
            self._try_enter_short(candle)

        self._previous_cci = cci_val

    def _close_long_positions(self):
        long_volume = float(self.Position) if self.Position > 0 else 0.0
        if long_volume <= 0:
            return
        self.SellMarket(long_volume)

    def _close_short_positions(self):
        short_volume = abs(float(self.Position)) if self.Position < 0 else 0.0
        if short_volume <= 0:
            return
        self.BuyMarket(short_volume)

    def _try_enter_long(self, candle):
        volume = self._calculate_order_volume(float(candle.ClosePrice))
        if volume <= 0:
            return
        current_long = float(self.Position) if self.Position > 0 else 0.0
        max_orders = int(self.MaxOrders)
        if max_orders > 0:
            max_vol = volume * max_orders
            if current_long >= max_vol:
                return
            additional = min(volume, max_vol - current_long)
            if additional <= 0:
                return
            self.BuyMarket(additional)
        else:
            self.BuyMarket(volume)

    def _try_enter_short(self, candle):
        volume = self._calculate_order_volume(float(candle.ClosePrice))
        if volume <= 0:
            return
        current_short = abs(float(self.Position)) if self.Position < 0 else 0.0
        max_orders = int(self.MaxOrders)
        if max_orders > 0:
            max_vol = volume * max_orders
            if current_short >= max_vol:
                return
            additional = min(volume, max_vol - current_short)
            if additional <= 0:
                return
            self.SellMarket(additional)
        else:
            self.SellMarket(volume)

    def _calculate_order_volume(self, reference_price):
        volume = float(self.OrderVolume)

        risk_pct = float(self.RiskPercentage)
        if risk_pct > 0:
            portfolio_value = 0.0
            if self.Portfolio is not None:
                if self.Portfolio.CurrentValue is not None and float(self.Portfolio.CurrentValue) != 0:
                    portfolio_value = float(self.Portfolio.CurrentValue)
                elif self.Portfolio.BeginValue is not None:
                    portfolio_value = float(self.Portfolio.BeginValue)

            risk_capital = portfolio_value * risk_pct / 100.0

            if risk_capital > 0:
                margin = 0.0
                if reference_price > 0:
                    volume = risk_capital / reference_price

        volume = self._round_volume(volume)

        if self.Security is not None:
            min_vol = self.Security.MinVolume
            if min_vol is not None and float(min_vol) > 0 and volume < float(min_vol):
                volume = float(min_vol)
            max_vol = self.Security.MaxVolume
            if max_vol is not None and float(max_vol) > 0 and volume > float(max_vol):
                volume = float(max_vol)

        return volume

    def _round_volume(self, volume):
        if volume <= 0:
            return 0.0
        step = 0.0
        if self.Security is not None and self.Security.VolumeStep is not None:
            step = float(self.Security.VolumeStep)
        if step <= 0:
            return volume
        steps = Math.Floor(volume / step)
        rounded = steps * step
        if rounded <= 0:
            rounded = step
        return rounded

    def OnReseted(self):
        super(kloss_simple_strategy, self).OnReseted()
        self._previous_cci = None

    def CreateClone(self):
        return kloss_simple_strategy()
