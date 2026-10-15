import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import ArgumentException, Decimal, Math, TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class polynomial_regression_bands_channel_strategy(Strategy):
    def __init__(self):
        super(polynomial_regression_bands_channel_strategy, self).__init__()
        self._length = self.Param("Length", 100).SetGreaterThanZero() \
            .SetDisplay("Length", "Rolling regression window, including the current close", "Indicators")
        self._degree = self.Param("Degree", 2).SetRange(0, 6) \
            .SetDisplay("Degree", "Polynomial degree from zero through six", "Indicators")
        self._std_dev_multiplier = self.Param("StdDevMultiplier", Decimal.Parse("2")).SetGreaterThanZero() \
            .SetDisplay("Std Dev Multiplier", "Population deviation multiplier of fitted residuals", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of finished candles", "General")
        self._prices = []
        self._basis = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(polynomial_regression_bands_channel_strategy, self).OnReseted()
        self._prices = []
        self._basis = None

    def OnStarted2(self, time):
        super(polynomial_regression_bands_channel_strategy, self).OnStarted2(time)
        length = self._length.Value
        degree = self._degree.Value
        if degree < 0 or degree > 6 or length < degree + 2:
            raise ArgumentException("Degree must be between zero and six and Length at least Degree + 2.")
        self._prices = []
        self._basis = self._build_basis(length, degree)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    @staticmethod
    def _build_basis(length, degree):
        basis = []
        for column in range(degree + 1):
            values = [Math.Pow(2.0 * row / (length - 1) - 1.0, column) for row in range(length)]
            for unused in range(2):
                for previous in basis:
                    projection = sum(value * previous[row] for row, value in enumerate(values))
                    values = [value - projection * previous[row] for row, value in enumerate(values)]
            norm = Math.Sqrt(sum(value * value for value in values))
            basis.append([value / norm for value in values])
        return basis

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._prices.append(candle.ClosePrice)
        length = self._length.Value
        if len(self._prices) > length:
            self._prices.pop(0)
        if len(self._prices) < length or not self.IsFormedAndOnlineAndAllowTrading():
            return
        origin = self._prices[0]
        values = [float(price - origin) for price in self._prices]
        coefficients = [sum(value * column[row] for row, value in enumerate(values)) for column in self._basis]
        squared_error = 0.0
        center = 0.0
        for row, value in enumerate(values):
            fitted = sum(coefficients[column] * vector[row] for column, vector in enumerate(self._basis))
            residual = value - fitted
            squared_error += residual * residual
            if row == length - 1:
                center = fitted
        width = float(self._std_dev_multiplier.Value) * Math.Sqrt(squared_error / length)
        if values[-1] < center - width and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif values[-1] > center + width and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return polynomial_regression_bands_channel_strategy()
