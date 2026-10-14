import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import LinearReg, AverageTrueRange, StandardDeviation
from StockSharp.Algo.Strategies import Strategy

BAND_WIDTH = 2


class multi_regression_strategy(Strategy):
    """
    Multi regression strategy.
    A close crossing above the linear regression line goes long and a close crossing below it goes short, reversing an opposite
    position. The selected risk measure (ATR, standard deviation, Bollinger or Keltner half-width) times RiskMultiplier sets the
    distance of the optional stop loss and take profit bounds from the entry price.
    """

    def __init__(self):
        super(multi_regression_strategy, self).__init__()
        self._length = self.Param("Length", 90).SetGreaterThanZero().SetDisplay("Length", "Regression and risk measure period", "Regression")
        self._risk_measure = self.Param("RiskMeasure", "Atr").SetDisplay("Risk Measure", "Volatility measure used for the bounds: Atr, StdDev, Bollinger or Keltner", "Risk")
        self._risk_multiplier = self.Param("RiskMultiplier", 1.0).SetGreaterThanZero().SetDisplay("Risk Multiplier", "Multiplier applied to the risk measure", "Risk")
        self._use_stop_loss = self.Param("UseStopLoss", True).SetDisplay("Use Stop Loss", "Exit at the adverse bound", "Risk")
        self._use_take_profit = self.Param("UseTakeProfit", True).SetDisplay("Use Take Profit", "Exit at the favorable bound", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_close = None
        self._prev_regression = None
        self._stop_price = None
        self._take_price = None

    def OnReseted(self):
        super(multi_regression_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(multi_regression_strategy, self).OnStarted2(time)

        self._reset_state()

        regression = LinearReg()
        regression.Length = self._length.Value
        atr = AverageTrueRange()
        atr.Length = self._length.Value
        std_dev = StandardDeviation()
        std_dev.Length = self._length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(regression, atr, std_dev, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, regression)
            self.DrawOwnTrades(area)

    def _get_risk_distance(self, atr, std_dev):
        measure = str(self._risk_measure.Value)
        if measure == "StdDev":
            value = std_dev
        elif measure == "Bollinger":
            value = std_dev * Decimal(BAND_WIDTH)
        elif measure == "Keltner":
            value = atr * Decimal(BAND_WIDTH)
        else:
            value = atr
        return value * Decimal(self._risk_multiplier.Value)

    def _process_candle(self, candle, regression, atr, std_dev):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice
        prev_close = self._prev_close
        prev_regression = self._prev_regression
        self._prev_close = close
        self._prev_regression = regression

        # The bounds are checked against the candle range before a new crossing is acted on.
        if self.Position > 0:
            if (self._stop_price is not None and candle.LowPrice <= self._stop_price) or \
                    (self._take_price is not None and candle.HighPrice >= self._take_price):
                self.SellMarket(self.Position)
                self._stop_price = None
                self._take_price = None
                return
        elif self.Position < 0:
            if (self._stop_price is not None and candle.HighPrice >= self._stop_price) or \
                    (self._take_price is not None and candle.LowPrice <= self._take_price):
                self.BuyMarket(-self.Position)
                self._stop_price = None
                self._take_price = None
                return

        if prev_close is None or prev_regression is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        cross_up = prev_close <= prev_regression and close > regression
        cross_down = prev_close >= prev_regression and close < regression
        distance = self._get_risk_distance(atr, std_dev)

        if cross_up and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - distance if self._use_stop_loss.Value else None
            self._take_price = close + distance if self._use_take_profit.Value else None
        elif cross_down and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + distance if self._use_stop_loss.Value else None
            self._take_price = close - distance if self._use_take_profit.Value else None

    def CreateClone(self):
        return multi_regression_strategy()
