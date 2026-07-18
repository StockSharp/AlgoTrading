import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class market_predictor_strategy(Strategy):
    def __init__(self):
        super(market_predictor_strategy, self).__init__()
        self._initial_alpha = self.Param("InitialAlpha", 0.1) \
            .SetDisplay("Initial Alpha", "Default amplitude before ATR is formed", "Prediction")
        self._initial_beta = self.Param("InitialBeta", 0.1) \
            .SetDisplay("Initial Beta", "Fractal weight placeholder coefficient", "Prediction")
        self._initial_gamma = self.Param("InitialGamma", 0.1) \
            .SetDisplay("Initial Gamma", "Fractal damping constant", "Prediction")
        self._kappa = self.Param("Kappa", 1.0) \
            .SetDisplay("Kappa", "Sigmoid sensitivity parameter", "Prediction")
        self._initial_mu = self.Param("InitialMu", 1.0) \
            .SetDisplay("Initial Mu", "Fallback mean price", "Prediction")
        self._sigma = self.Param("Sigma", 10.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Sigma", "Deviation threshold for trades", "Trading")
        self._monte_carlo_sims = self.Param("MonteCarloSimulations", 1000) \
            .SetGreaterThanZero() \
            .SetDisplay("Monte Carlo Simulations", "Number of simulations per candle", "Prediction")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for candle subscription", "General")

        self._alpha = 0.1
        self._mu = 1.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(market_predictor_strategy, self).OnReseted()
        self._alpha = float(self._initial_alpha.Value)
        self._mu = float(self._initial_mu.Value)

    def OnStarted2(self, time):
        super(market_predictor_strategy, self).OnStarted2(time)
        self._alpha = float(self._initial_alpha.Value)
        self._mu = float(self._initial_mu.Value)

        self._sma = SimpleMovingAverage()
        self._sma.Length = 14
        self._atr = AverageTrueRange()
        self._atr.Length = 14

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sma, self._atr, self.process_candle).Start()

    def process_candle(self, candle, sma_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        sma_val = float(sma_value)
        atr_val = float(atr_value)

        if self._sma.IsFormed:
            self._mu = sma_val
        else:
            self._mu = float(self._initial_mu.Value)

        if self._atr.IsFormed and atr_val > 0:
            self._alpha = atr_val * 0.1
        else:
            self._alpha = float(self._initial_alpha.Value)

        current_price = float(candle.ClosePrice)
        sigma = float(self._sigma.Value)
        deviation = sigma * self._alpha if self._alpha > 0 else sigma

        if current_price < self._mu - deviation and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif current_price > self._mu + deviation and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        elif self.Position > 0 and current_price >= self._mu:
            self.SellMarket()
        elif self.Position < 0 and current_price <= self._mu:
            self.BuyMarket()

    def CreateClone(self):
        return market_predictor_strategy()
