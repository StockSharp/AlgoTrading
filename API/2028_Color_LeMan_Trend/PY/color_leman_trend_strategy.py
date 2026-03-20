import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, Highest, Lowest, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class color_leman_trend_strategy(Strategy):

    def __init__(self):
        super(color_leman_trend_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for indicator", "General")
        self._min_period = self.Param("Min", 13) \
            .SetDisplay("Min", "Shortest period", "Indicator")
        self._mid_period = self.Param("Midle", 21) \
            .SetDisplay("Midle", "Middle period", "Indicator")
        self._max_period = self.Param("Max", 34) \
            .SetDisplay("Max", "Longest period", "Indicator")
        self._ema_period = self.Param("PeriodEma", 3) \
            .SetDisplay("EMA Period", "Smoothing length", "Indicator")
        self._stop_loss_points = self.Param("StopLossPoints", 1000.0) \
            .SetDisplay("Stop Loss", "Stop loss in points", "Protection")
        self._take_profit_points = self.Param("TakeProfitPoints", 2000.0) \
            .SetDisplay("Take Profit", "Take profit in points", "Protection")
        self._allow_buy = self.Param("AllowBuy", True) \
            .SetDisplay("Allow Buy", "Enable long entries", "Trading")
        self._allow_sell = self.Param("AllowSell", True) \
            .SetDisplay("Allow Sell", "Enable short entries", "Trading")
        self._allow_buy_close = self.Param("AllowBuyClose", True) \
            .SetDisplay("Allow Buy Close", "Allow closing longs", "Trading")
        self._allow_sell_close = self.Param("AllowSellClose", True) \
            .SetDisplay("Allow Sell Close", "Allow closing shorts", "Trading")

        self._bulls_ema = None
        self._bears_ema = None
        self._prev_bulls = None
        self._prev_bears = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def Min(self):
        return self._min_period.Value

    @Min.setter
    def Min(self, value):
        self._min_period.Value = value

    @property
    def Midle(self):
        return self._mid_period.Value

    @Midle.setter
    def Midle(self, value):
        self._mid_period.Value = value

    @property
    def Max(self):
        return self._max_period.Value

    @Max.setter
    def Max(self, value):
        self._max_period.Value = value

    @property
    def PeriodEma(self):
        return self._ema_period.Value

    @PeriodEma.setter
    def PeriodEma(self, value):
        self._ema_period.Value = value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @StopLossPoints.setter
    def StopLossPoints(self, value):
        self._stop_loss_points.Value = value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @TakeProfitPoints.setter
    def TakeProfitPoints(self, value):
        self._take_profit_points.Value = value

    @property
    def AllowBuy(self):
        return self._allow_buy.Value

    @AllowBuy.setter
    def AllowBuy(self, value):
        self._allow_buy.Value = value

    @property
    def AllowSell(self):
        return self._allow_sell.Value

    @AllowSell.setter
    def AllowSell(self, value):
        self._allow_sell.Value = value

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

    def OnStarted(self, time):
        super(color_leman_trend_strategy, self).OnStarted(time)

        self._bulls_ema = ExponentialMovingAverage()
        self._bulls_ema.Length = self.PeriodEma
        self._bears_ema = ExponentialMovingAverage()
        self._bears_ema.Length = self.PeriodEma

        highest_min = Highest()
        highest_min.Length = self.Min
        highest_mid = Highest()
        highest_mid.Length = self.Midle
        highest_max = Highest()
        highest_max.Length = self.Max
        lowest_min = Lowest()
        lowest_min.Length = self.Min
        lowest_mid = Lowest()
        lowest_mid.Length = self.Midle
        lowest_max = Lowest()
        lowest_max.Length = self.Max

        self.SubscribeCandles(self.CandleType) \
            .Bind(highest_min, highest_mid, highest_max, lowest_min, lowest_mid, lowest_max, self.ProcessCandle) \
            .Start()

        self.StartProtection(
            takeProfit=Unit(self.TakeProfitPoints, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPoints, UnitTypes.Absolute)
        )

    def ProcessCandle(self, candle, h_min, h_mid, h_max, l_min, l_mid, l_max):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        hh = 3.0 * high - (float(h_min) + float(h_mid) + float(h_max))
        ll = (float(l_min) + float(l_mid) + float(l_max)) - 3.0 * low

        t = candle.OpenTime

        bulls_result = self._bulls_ema.Process(DecimalIndicatorValue(self._bulls_ema, hh, t, True))
        bears_result = self._bears_ema.Process(DecimalIndicatorValue(self._bears_ema, ll, t, True))

        if bulls_result.IsEmpty or bears_result.IsEmpty:
            return
        if not self._bulls_ema.IsFormed or not self._bears_ema.IsFormed:
            return

        bulls = float(bulls_result.ToDecimal())
        bears = float(bears_result.ToDecimal())

        buy_open = False
        sell_open = False
        buy_close = False
        sell_close = False

        if self._prev_bulls is not None and self._prev_bears is not None:
            if self._prev_bulls > self._prev_bears:
                if self.AllowBuy and bulls <= bears:
                    buy_open = True
                if self.AllowSellClose:
                    sell_close = True

            if self._prev_bulls < self._prev_bears:
                if self.AllowSell and bulls >= bears:
                    sell_open = True
                if self.AllowBuyClose:
                    buy_close = True

        self._prev_bulls = bulls
        self._prev_bears = bears

        if sell_close and self.Position < 0:
            self.BuyMarket()

        if buy_close and self.Position > 0:
            self.SellMarket()

        if buy_open and self.Position <= 0:
            self.BuyMarket()

        if sell_open and self.Position >= 0:
            self.SellMarket()

    def OnReseted(self):
        super(color_leman_trend_strategy, self).OnReseted()
        self._bulls_ema = None
        self._bears_ema = None
        self._prev_bulls = None
        self._prev_bears = None

    def CreateClone(self):
        return color_leman_trend_strategy()
