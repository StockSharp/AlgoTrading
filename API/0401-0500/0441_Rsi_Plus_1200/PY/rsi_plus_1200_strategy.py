import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

EMA_SLACK = 0.01


class rsi_plus_1200_strategy(Strategy):
    """
    RSI + 1200 Strategy.
    An EMA of EmaLength bars is calculated on MtfTimeframe candles. A long opens when RSI crosses above RsiOversold and the close
    is at most 1% above that EMA; a short opens when RSI crosses below RsiOverbought and the close is no more than 1% below it.
    Longs close once RSI rises above RsiOverbought and shorts once it falls below RsiOversold.
    A stop at StopLossPercent (a fraction, 0.10 = 10%) from the entry limits the loss.
    """

    def __init__(self):
        super(rsi_plus_1200_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 14).SetGreaterThanZero().SetDisplay("RSI Length", "RSI period", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", 72.0).SetDisplay("RSI Overbought", "RSI overbought level", "Indicators")
        self._rsi_oversold = self.Param("RsiOversold", 28.0).SetDisplay("RSI Oversold", "RSI oversold level", "Indicators")
        self._ema_length = self.Param("EmaLength", 150).SetGreaterThanZero().SetDisplay("EMA Length", "EMA period on the higher time frame", "Indicators")
        self._mtf_timeframe = self.Param("MtfTimeframe", DataType.TimeFrame(TimeSpan.FromMinutes(120))).SetDisplay("MTF Timeframe", "Higher time frame of the EMA", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 0.10).SetNotNegative().SetDisplay("Stop Loss", "Stop loss as a fraction of the entry price (0.10 = 10%)", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Candle type of the RSI signals", "General")
        self._rsi = None
        self._ema = None
        self._prev_rsi = None
        self._mtf_ema = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rsi_plus_1200_strategy, self).OnReseted()
        self._prev_rsi = None
        self._mtf_ema = None

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, self._mtf_timeframe.Value)]

    def OnStarted2(self, time):
        super(rsi_plus_1200_strategy, self).OnStarted2(time)

        self._prev_rsi = None
        self._mtf_ema = None

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self._rsi_length.Value
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self._ema_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi, self._process_candle).Start()

        self.SubscribeCandles(self._mtf_timeframe.Value).Bind(self._ema, self._process_mtf_candle).Start()

        stop = float(self._stop_loss_percent.Value)
        if stop > 0:
            self.StartProtection(Unit(), Unit(Decimal(stop * 100.0), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

            # The stop has to see prices between candles, not only at their close.
            for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
                quotes = Subscription(DataType.Level1, self.Security)
                quotes.MarketData.BuildField = field
                self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, self._rsi)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_mtf_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished or not self._ema.IsFormed:
            return

        self._mtf_ema = float(ema_value)

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished or not self._rsi.IsFormed:
            return

        rsi = float(rsi_value)
        previous = self._prev_rsi
        self._prev_rsi = rsi

        if previous is None or self._mtf_ema is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ema = self._mtf_ema
        close = float(candle.ClosePrice)
        overbought = float(self._rsi_overbought.Value)
        oversold = float(self._rsi_oversold.Value)

        cross_up_oversold = previous <= oversold and rsi > oversold
        cross_down_overbought = previous >= overbought and rsi < overbought

        if cross_up_oversold and close <= ema * (1.0 + EMA_SLACK) and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif cross_down_overbought and close >= ema * (1.0 - EMA_SLACK) and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and rsi > overbought:
            self.SellMarket(self.Position)
        elif self.Position < 0 and rsi < oversold:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return rsi_plus_1200_strategy()
