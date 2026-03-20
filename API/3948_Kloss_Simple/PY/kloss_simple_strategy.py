import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ExponentialMovingAverage, CommodityChannelIndex, StochasticOscillator, DecimalIndicatorValue

class kloss_simple_strategy(Strategy):
    def __init__(self):
        super(kloss_simple_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 0.1).SetDisplay("Volume", "Base order volume", "Trading")
        self._ma_period = self.Param("MaPeriod", 5).SetDisplay("EMA Period", "Length of the exponential moving average", "Indicators")
        self._cci_period = self.Param("CciPeriod", 10).SetDisplay("CCI Period", "Length of the commodity channel index", "Indicators")
        self._cci_level = self.Param("CciLevel", 120.0).SetDisplay("CCI Level", "Distance from zero to trigger signals", "Indicators")
        self._stochastic_k_period = self.Param("StochasticKPeriod", 5).SetDisplay("Stochastic %K", "Period of the %K line", "Indicators")
        self._stochastic_d_period = self.Param("StochasticDPeriod", 3).SetDisplay("Stochastic %D", "Period of the %D line", "Indicators")
        self._stochastic_smooth = self.Param("StochasticSmooth", 3).SetDisplay("Stochastic Smooth", "Smoothing factor for %K", "Indicators")
        self._stochastic_level = self.Param("StochasticLevel", 25.0).SetDisplay("Stochastic Level", "Distance from 50 to trigger signals", "Indicators")
        self._max_orders = self.Param("MaxOrders", 3).SetDisplay("Max Orders", "Maximum number of positions per direction", "Trading")
        self._stop_loss_points = self.Param("StopLossPoints", 0.0).SetDisplay("Stop Loss (pts)", "Stop loss distance in points", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 0.0).SetDisplay("Take Profit (pts)", "Take profit distance in points", "Risk")
        self._risk_percentage = self.Param("RiskPercentage", 10.0).SetDisplay("Risk %", "Portfolio percentage for dynamic position sizing", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Primary candle series for calculations", "General")

        self._ema = None
        self._cci = None
        self._stochastic = None
        self._previous_close = None
        self._previous_ma = None
        self._previous_cci = None
        self._previous_stochastic = None

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

    def OnStarted(self, time):
        super(kloss_simple_strategy, self).OnStarted(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.MaPeriod
        self._cci = CommodityChannelIndex()
        self._cci.Length = self.CciPeriod
        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = self.StochasticKPeriod
        self._stochastic.D.Length = self.StochasticDPeriod

        self.Volume = self.OrderVolume

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

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

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        weighted_close = (close * 2.0 + high + low) / 4.0

        ma_result = self._ema.Process(DecimalIndicatorValue(self._ema, weighted_close, candle.CloseTime))
        ma_val = ma_result.ToNullableDecimal()
        cci_result = self._cci.Process(candle)
        cci_val = cci_result.ToNullableDecimal()
        stoch_value = self._stochastic.Process(candle)

        if ma_val is None or cci_val is None:
            return

        stoch_k = stoch_value.K
        if stoch_k is None:
            return

        ma_val = float(ma_val)
        cci_val = float(cci_val)
        stoch_k_val = float(stoch_k)

        if self._previous_close is None or self._previous_ma is None or self._previous_cci is None or self._previous_stochastic is None:
            self._previous_close = close
            self._previous_ma = ma_val
            self._previous_cci = cci_val
            self._previous_stochastic = stoch_k_val
            return

        prev_close = self._previous_close
        prev_ma = self._previous_ma
        prev_cci = self._previous_cci
        prev_stoch = self._previous_stochastic

        lower_stoch = 50.0 - float(self.StochasticLevel)
        upper_stoch = 50.0 + float(self.StochasticLevel)
        cci_lev = float(self.CciLevel)

        if prev_cci < -cci_lev and prev_stoch < lower_stoch and prev_close > prev_ma:
            self._close_short_positions()
            self._try_enter_long(candle)
        elif prev_cci > cci_lev and prev_stoch > upper_stoch and prev_close < prev_ma:
            self._close_long_positions()
            self._try_enter_short(candle)

        self._previous_close = close
        self._previous_ma = ma_val
        self._previous_cci = cci_val
        self._previous_stochastic = stoch_k_val

    def _close_long_positions(self):
        if self.Position > 0:
            self.SellMarket(self.Position)

    def _close_short_positions(self):
        if self.Position < 0:
            self.BuyMarket(abs(self.Position))

    def _try_enter_long(self, candle):
        volume = float(self.OrderVolume)
        if volume <= 0:
            return
        max_orders = int(self.MaxOrders)
        current_long = float(self.Position) if self.Position > 0 else 0.0
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
        volume = float(self.OrderVolume)
        if volume <= 0:
            return
        max_orders = int(self.MaxOrders)
        current_short = abs(float(self.Position)) if self.Position < 0 else 0.0
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

    def OnReseted(self):
        super(kloss_simple_strategy, self).OnReseted()
        self._previous_close = None
        self._previous_ma = None
        self._previous_cci = None
        self._previous_stochastic = None

    def CreateClone(self):
        return kloss_simple_strategy()
