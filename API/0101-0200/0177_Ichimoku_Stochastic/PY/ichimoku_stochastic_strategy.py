import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Ichimoku, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy

class ichimoku_stochastic_strategy(Strategy):
    """
    Ichimoku Stochastic strategy.
    A close above the cloud with Tenkan-sen above Kijun-sen and %K below StochOversold goes long, a close below the cloud with Tenkan-sen
    below Kijun-sen and %K above StochOverbought goes short, reversing an opposite position; %K is the stochastic over StochPeriod candles smoothed over StochK candles. The cloud is the stop:
    a long closes when price closes below the cloud and a short when it closes above it.
    """

    def __init__(self):
        super(ichimoku_stochastic_strategy, self).__init__()
        self._tenkan_period = self.Param("TenkanPeriod", 9).SetGreaterThanZero().SetDisplay("Tenkan Period", "Period of Tenkan-sen", "Ichimoku")
        self._kijun_period = self.Param("KijunPeriod", 26).SetGreaterThanZero().SetDisplay("Kijun Period", "Period of Kijun-sen", "Ichimoku")
        self._senkou_period = self.Param("SenkouPeriod", 52).SetGreaterThanZero().SetDisplay("Senkou Period", "Period of Senkou Span B", "Ichimoku")
        self._stoch_period = self.Param("StochPeriod", 14).SetGreaterThanZero().SetDisplay("Stochastic Period", "Lookback period of the raw stochastic", "Stochastic")
        self._stoch_k = self.Param("StochK", 3).SetGreaterThanZero().SetDisplay("Stochastic %K", "Smoothing period of %K", "Stochastic")
        self._stoch_oversold = self.Param("StochOversold", 20.0).SetDisplay("Stochastic Oversold", "%K level for longs", "Stochastic")
        self._stoch_overbought = self.Param("StochOverbought", 80.0).SetDisplay("Stochastic Overbought", "%K level for shorts", "Stochastic")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(ichimoku_stochastic_strategy, self).OnStarted2(time)

        ichimoku = Ichimoku()
        ichimoku.Tenkan.Length = self._tenkan_period.Value
        ichimoku.Kijun.Length = self._kijun_period.Value
        ichimoku.SenkouB.Length = self._senkou_period.Value
        # The D line of the core oscillator is the smoothed %K.
        stochastic = StochasticOscillator()
        stochastic.K.Length = self._stoch_period.Value
        stochastic.D.Length = self._stoch_k.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ichimoku, stochastic, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ichimoku)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, stochastic)

    def _process_candle(self, candle, ichimoku_value, stochastic_value):
        if candle.State != CandleStates.Finished:
            return

        tenkan = ichimoku_value.Tenkan
        kijun = ichimoku_value.Kijun
        senkou_a = ichimoku_value.SenkouA
        senkou_b = ichimoku_value.SenkouB
        if tenkan is None or kijun is None or senkou_a is None or senkou_b is None:
            return
        if not stochastic_value.IsFormed or stochastic_value.D is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        k = stochastic_value.D
        close = candle.ClosePrice
        cloud_top = senkou_a if senkou_a > senkou_b else senkou_b
        cloud_bottom = senkou_b if senkou_a > senkou_b else senkou_a

        if close > cloud_top and tenkan > kijun and k < Decimal(self._stoch_oversold.Value) and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif close < cloud_bottom and tenkan < kijun and k > Decimal(self._stoch_overbought.Value) and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and close < cloud_bottom:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close > cloud_top:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return ichimoku_stochastic_strategy()
