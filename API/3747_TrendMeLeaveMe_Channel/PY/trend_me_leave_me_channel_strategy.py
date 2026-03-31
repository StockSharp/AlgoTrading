import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import LinearRegression
from StockSharp.Algo.Strategies import Strategy


class trend_me_leave_me_channel_strategy(Strategy):
    """LinearRegression trend channel strategy with buy/sell zones and virtual SL/TP per direction."""

    def __init__(self):
        super(trend_me_leave_me_channel_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle aggregation used for trend estimation", "General")
        self._trend_length = self.Param("TrendLength", 100) \
            .SetGreaterThanZero() \
            .SetDisplay("Trend Length", "Number of candles used in the regression trend line", "Trend")
        self._buy_step_upper = self.Param("BuyStepUpper", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Buy Upper Offset", "Price steps added above trend line for buy stop", "Buy Orders")
        self._buy_step_lower = self.Param("BuyStepLower", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Buy Lower Offset", "Price steps below trend line that activates buy orders", "Buy Orders")
        self._sell_step_upper = self.Param("SellStepUpper", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Sell Upper Offset", "Price steps above trend line that activates sell orders", "Sell Orders")
        self._sell_step_lower = self.Param("SellStepLower", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Sell Lower Offset", "Price steps below trend line for sell stop", "Sell Orders")
        self._buy_take_profit_steps = self.Param("BuyTakeProfitSteps", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Buy Take Profit", "Take-profit distance in price steps for long trades", "Risk")
        self._buy_stop_loss_steps = self.Param("BuyStopLossSteps", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("Buy Stop Loss", "Stop-loss distance in price steps for long trades", "Risk")
        self._sell_take_profit_steps = self.Param("SellTakeProfitSteps", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Sell Take Profit", "Take-profit distance in price steps for short trades", "Risk")
        self._sell_stop_loss_steps = self.Param("SellStopLossSteps", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("Sell Stop Loss", "Stop-loss distance in price steps for short trades", "Risk")
        self._buy_volume = self.Param("BuyVolume", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Buy Volume", "Order volume for buy stop entries", "Buy Orders")
        self._sell_volume = self.Param("SellVolume", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Sell Volume", "Order volume for sell stop entries", "Sell Orders")

        self._entry_price = 0.0
        self._active_stop = None
        self._active_take = None
        self._active_direction = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def TrendLength(self):
        return self._trend_length.Value

    @property
    def BuyStepUpper(self):
        return self._buy_step_upper.Value

    @property
    def BuyStepLower(self):
        return self._buy_step_lower.Value

    @property
    def SellStepUpper(self):
        return self._sell_step_upper.Value

    @property
    def SellStepLower(self):
        return self._sell_step_lower.Value

    @property
    def BuyTakeProfitSteps(self):
        return self._buy_take_profit_steps.Value

    @property
    def BuyStopLossSteps(self):
        return self._buy_stop_loss_steps.Value

    @property
    def SellTakeProfitSteps(self):
        return self._sell_take_profit_steps.Value

    @property
    def SellStopLossSteps(self):
        return self._sell_stop_loss_steps.Value

    @property
    def BuyVolume(self):
        return self._buy_volume.Value

    @property
    def SellVolume(self):
        return self._sell_volume.Value

    def OnReseted(self):
        super(trend_me_leave_me_channel_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._active_stop = None
        self._active_take = None
        self._active_direction = 0

    def OnStarted2(self, time):
        super(trend_me_leave_me_channel_strategy, self).OnStarted2(time)

        regression = LinearRegression()
        regression.Length = self.TrendLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(regression, self._process_candle).Start()

    def _process_candle(self, candle, ind_val):
        if candle.State != CandleStates.Finished:
            return

        if not ind_val.IsFinal or not ind_val.IsFormed:
            return

        lr_raw = ind_val.LinearReg
        if lr_raw is None:
            return

        trend_value = float(lr_raw)

        price_step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            ps = float(self.Security.PriceStep)
            if ps > 0:
                price_step = ps

        self._check_protection(candle)

        close = float(candle.ClosePrice)
        middle = trend_value

        buy_lower = middle - float(self.BuyStepLower) * price_step
        sell_upper = middle + float(self.SellStepUpper) * price_step

        # Buy signal: price is below trend line in the buy zone
        if close <= middle and close >= buy_lower and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(abs(self.Position))
                self._clear_protection()

            self.BuyMarket(float(self.BuyVolume))
            self._entry_price = close
            self._active_stop = close - float(self.BuyStopLossSteps) * price_step
            self._active_take = close + float(self.BuyTakeProfitSteps) * price_step
            self._active_direction = 1
        # Sell signal: price is above trend line in the sell zone
        elif close >= middle and close <= sell_upper and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(self.Position)
                self._clear_protection()

            self.SellMarket(float(self.SellVolume))
            self._entry_price = close
            self._active_stop = close + float(self.SellStopLossSteps) * price_step
            self._active_take = close - float(self.SellTakeProfitSteps) * price_step
            self._active_direction = -1

    def _check_protection(self, candle):
        if self._active_direction == 1 and self.Position > 0 and \
                self._active_stop is not None and self._active_take is not None:
            if float(candle.LowPrice) <= self._active_stop or float(candle.HighPrice) >= self._active_take:
                self.SellMarket(self.Position)
                self._clear_protection()
        elif self._active_direction == -1 and self.Position < 0 and \
                self._active_stop is not None and self._active_take is not None:
            if float(candle.HighPrice) >= self._active_stop or float(candle.LowPrice) <= self._active_take:
                self.BuyMarket(abs(self.Position))
                self._clear_protection()

    def _clear_protection(self):
        self._active_stop = None
        self._active_take = None
        self._active_direction = 0
        self._entry_price = 0.0

    def CreateClone(self):
        return trend_me_leave_me_channel_strategy()
