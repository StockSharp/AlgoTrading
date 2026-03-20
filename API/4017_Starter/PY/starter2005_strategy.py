import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ExponentialMovingAverage, CommodityChannelIndex

class starter2005_strategy(Strategy):
    def __init__(self):
        super(starter2005_strategy, self).__init__()

        self._base_volume = self.Param("BaseVolume", 1.2) \
            .SetDisplay("Base Volume", "Initial lot size used when risk-based sizing is unavailable", "Risk Management")
        self._maximum_risk = self.Param("MaximumRisk", 0.036) \
            .SetDisplay("Maximum Risk", "Fraction of account equity considered for sizing", "Risk Management")
        self._risk_divider = self.Param("RiskDivider", 500.0) \
            .SetDisplay("Risk Divider", "Divisor applied to risk capital", "Risk Management")
        self._decrease_factor = self.Param("DecreaseFactor", 2.0) \
            .SetDisplay("Decrease Factor", "Lot reduction factor after consecutive losses", "Risk Management")
        self._ma_period = self.Param("MaPeriod", 5) \
            .SetDisplay("EMA Period", "Length of the exponential moving average", "Indicators")
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("CCI Period", "Commodity Channel Index lookback length", "Indicators")
        self._cci_threshold = self.Param("CciThreshold", 5.0) \
            .SetDisplay("CCI Threshold", "Absolute CCI level required for signals", "Indicators")
        self._laguerre_gamma = self.Param("LaguerreGamma", 0.66) \
            .SetDisplay("Laguerre Gamma", "Smoothing factor of the Laguerre RSI filter", "Indicators")
        self._laguerre_entry_tolerance = self.Param("LaguerreEntryTolerance", 0.02) \
            .SetDisplay("Laguerre Entry Tolerance", "Closeness to 0/1 required for entry", "Signals")
        self._laguerre_exit_high = self.Param("LaguerreExitHigh", 0.9) \
            .SetDisplay("Laguerre Exit High", "Upper exit level for long positions", "Signals")
        self._laguerre_exit_low = self.Param("LaguerreExitLow", 0.1) \
            .SetDisplay("Laguerre Exit Low", "Lower exit level for short positions", "Signals")
        self._take_profit_points = self.Param("TakeProfitPoints", 10.0) \
            .SetDisplay("Take Profit (points)", "Distance in price points before profit is locked", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Primary timeframe processed by the strategy", "General")

        self._ema = None
        self._cci = None
        self._previous_ma = None
        self._lag_l0 = 0.0
        self._lag_l1 = 0.0
        self._lag_l2 = 0.0
        self._lag_l3 = 0.0
        self._laguerre_formed = False
        self._entry_price = None
        self._entry_volume = 0.0
        self._entry_side = None
        self._consecutive_losses = 0

    @property
    def BaseVolume(self):
        return self._base_volume.Value

    @property
    def MaximumRisk(self):
        return self._maximum_risk.Value

    @property
    def RiskDivider(self):
        return self._risk_divider.Value

    @property
    def DecreaseFactor(self):
        return self._decrease_factor.Value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @property
    def CciPeriod(self):
        return self._cci_period.Value

    @property
    def CciThreshold(self):
        return self._cci_threshold.Value

    @property
    def LaguerreGamma(self):
        return self._laguerre_gamma.Value

    @property
    def LaguerreEntryTolerance(self):
        return self._laguerre_entry_tolerance.Value

    @property
    def LaguerreExitHigh(self):
        return self._laguerre_exit_high.Value

    @property
    def LaguerreExitLow(self):
        return self._laguerre_exit_low.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(starter2005_strategy, self).OnStarted(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.MaPeriod
        self._cci = CommodityChannelIndex()
        self._cci.Length = self.CciPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._ema, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return

        ma = float(ma_value)

        cci_result = self._cci.Process(candle)
        cci = float(cci_result.ToDecimal()) if cci_result is not None else 0.0

        if not self._ema.IsFormed or not self._cci.IsFormed:
            self._previous_ma = ma
            return

        laguerre = self._calculate_laguerre(float(candle.ClosePrice))
        if not self._laguerre_formed:
            self._previous_ma = ma
            return

        previous_ma = self._previous_ma
        self._previous_ma = ma
        if previous_ma is None:
            return

        ma_rising = ma > previous_ma
        ma_falling = ma < previous_ma
        entry_tolerance = float(self.LaguerreEntryTolerance)
        tp_distance = self._get_take_profit_distance()
        price = self._get_decision_price(candle)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position == 0:
            if ma_rising and laguerre <= entry_tolerance and cci < -float(self.CciThreshold):
                volume = self._calculate_order_volume(price)
                if volume > 0:
                    self.BuyMarket(volume)
                    self._entry_side = Sides.Buy
                    self._entry_price = price
                    self._entry_volume = volume

            elif ma_falling and laguerre >= 1.0 - entry_tolerance and cci > float(self.CciThreshold):
                volume = self._calculate_order_volume(price)
                if volume > 0:
                    self.SellMarket(volume)
                    self._entry_side = Sides.Sell
                    self._entry_price = price
                    self._entry_volume = volume

        if self._entry_side == Sides.Buy and self.Position > 0 and self._entry_price is not None:
            gain = price - self._entry_price
            exit_high = float(self.LaguerreExitHigh)
            if (exit_high > 0 and laguerre >= exit_high) or (tp_distance > 0 and gain >= tp_distance):
                volume = abs(self.Position)
                if volume <= 0:
                    volume = self._entry_volume
                if volume > 0:
                    self.SellMarket(volume)
                    self._register_trade_result(gain)
                    self._reset_position_state()

        elif self._entry_side == Sides.Sell and self.Position < 0 and self._entry_price is not None:
            gain = self._entry_price - price
            exit_low = float(self.LaguerreExitLow)
            if (exit_low > 0 and laguerre <= exit_low) or (tp_distance > 0 and gain >= tp_distance):
                volume = abs(self.Position)
                if volume <= 0:
                    volume = self._entry_volume
                if volume > 0:
                    self.BuyMarket(volume)
                    self._register_trade_result(gain)
                    self._reset_position_state()

        elif self.Position == 0:
            self._reset_position_state()

    def _calculate_laguerre(self, price):
        gamma = float(self.LaguerreGamma)
        l0_prev = self._lag_l0
        l1_prev = self._lag_l1
        l2_prev = self._lag_l2
        l3_prev = self._lag_l3

        self._lag_l0 = (1.0 - gamma) * price + gamma * l0_prev
        self._lag_l1 = -gamma * self._lag_l0 + l0_prev + gamma * l1_prev
        self._lag_l2 = -gamma * self._lag_l1 + l1_prev + gamma * l2_prev
        self._lag_l3 = -gamma * self._lag_l2 + l2_prev + gamma * l3_prev

        cu = 0.0
        cd = 0.0

        if self._lag_l0 >= self._lag_l1:
            cu = self._lag_l0 - self._lag_l1
        else:
            cd = self._lag_l1 - self._lag_l0

        if self._lag_l1 >= self._lag_l2:
            cu += self._lag_l1 - self._lag_l2
        else:
            cd += self._lag_l2 - self._lag_l1

        if self._lag_l2 >= self._lag_l3:
            cu += self._lag_l2 - self._lag_l3
        else:
            cd += self._lag_l3 - self._lag_l2

        denominator = cu + cd
        result = 0.0 if denominator == 0 else cu / denominator

        self._laguerre_formed = True
        return result

    def _calculate_order_volume(self, price):
        volume = float(self.BaseVolume)

        max_risk = float(self.MaximumRisk)
        risk_divider = float(self.RiskDivider)

        if max_risk > 0 and risk_divider > 0:
            equity = 0.0
            if self.Portfolio is not None:
                cv = self.Portfolio.CurrentValue
                if cv is not None and float(cv) > 0:
                    equity = float(cv)
                elif self.Portfolio.BeginValue is not None:
                    equity = float(self.Portfolio.BeginValue)

            if equity > 0 and price > 0:
                risk_volume = equity * max_risk / risk_divider
                risk_volume = risk_volume / price
                if risk_volume > volume:
                    volume = risk_volume

        decrease_factor = float(self.DecreaseFactor)
        if decrease_factor > 0 and self._consecutive_losses > 1:
            reduction = volume * self._consecutive_losses / decrease_factor
            volume -= reduction

        return self._normalize_volume(volume)

    def _normalize_volume(self, volume):
        if self.Security is not None:
            vs = self.Security.VolumeStep
            step = float(vs) if vs is not None and float(vs) > 0 else 1.0

            min_vol_sec = self.Security.MinVolume
            min_vol = float(min_vol_sec) if min_vol_sec is not None else step

            max_vol_sec = self.Security.MaxVolume

            import math
            steps = math.floor(volume / step)
            if steps < 1:
                steps = 1
            volume = steps * step

            if volume < min_vol:
                volume = min_vol

            if max_vol_sec is not None and float(max_vol_sec) > 0 and volume > float(max_vol_sec):
                volume = float(max_vol_sec)

        if volume <= 0:
            volume = 1.0

        return volume

    def _get_take_profit_distance(self):
        tp_pts = float(self.TakeProfitPoints)
        if tp_pts <= 0:
            return 0.0

        point = 0.0
        if self.Security is not None and self.Security.PriceStep is not None:
            point = float(self.Security.PriceStep)

        if point <= 0:
            decimals = 4
            if self.Security is not None and self.Security.Decimals is not None:
                decimals = int(self.Security.Decimals)
            point = 1.0
            for _ in range(decimals):
                point /= 10.0

        return tp_pts * point

    def _get_decision_price(self, candle):
        cp = float(candle.ClosePrice)
        if cp > 0:
            return cp
        return float(candle.OpenPrice)

    def _register_trade_result(self, gain):
        if gain > 0:
            self._consecutive_losses = 0
        elif gain < 0:
            self._consecutive_losses += 1

    def _reset_position_state(self):
        self._entry_price = None
        self._entry_volume = 0.0
        self._entry_side = None

    def OnReseted(self):
        super(starter2005_strategy, self).OnReseted()
        self._ema = None
        self._cci = None
        self._previous_ma = None
        self._lag_l0 = 0.0
        self._lag_l1 = 0.0
        self._lag_l2 = 0.0
        self._lag_l3 = 0.0
        self._laguerre_formed = False
        self._entry_price = None
        self._entry_volume = 0.0
        self._entry_side = None
        self._consecutive_losses = 0

    def CreateClone(self):
        return starter2005_strategy()
