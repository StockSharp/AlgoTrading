import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class vwap_rsi_strategy(Strategy):
    """
    VWAP RSI strategy.
    The market trades around the clock, so the session VWAP restarts with each UTC day and weighs each candle's typical price by its volume.
    A close below VWAP with RSI below RsiOversold goes long and a close above VWAP with RSI above RsiOverbought goes short,
    expecting the price to revert toward VWAP. An opposite signal reverses the position, and a percent stop limits the loss.
    """

    def __init__(self):
        super(vwap_rsi_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero().SetDisplay("RSI Period", "Period of RSI", "Indicators")
        self._rsi_oversold = self.Param("RsiOversold", 30.0).SetDisplay("RSI Oversold", "RSI level for longs below VWAP", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", 70.0).SetDisplay("RSI Overbought", "RSI level for shorts above VWAP", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._day = None
        self._cumulative_price_volume = Decimal(0)
        self._cumulative_volume = Decimal(0)

    def OnReseted(self):
        super(vwap_rsi_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(vwap_rsi_strategy, self).OnStarted2(time)

        self._reset_state()

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(rsi, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

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
                self.DrawIndicator(oscillators, rsi)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        day = candle.OpenTime.Date
        if self._day is None or self._day != day:
            self._day = day
            self._cumulative_price_volume = Decimal(0)
            self._cumulative_volume = Decimal(0)

        typical_price = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / Decimal(3)
        self._cumulative_price_volume += typical_price * candle.TotalVolume
        self._cumulative_volume += candle.TotalVolume

        if not rsi_value.IsFormed or self._cumulative_volume <= 0:
            return

        vwap = self._cumulative_price_volume / self._cumulative_volume
        rsi = rsi_value.GetValue[Decimal](None)
        close = candle.ClosePrice

        signal = 0
        if close < vwap and rsi < Decimal(self._rsi_oversold.Value):
            signal = 1
        elif close > vwap and rsi > Decimal(self._rsi_overbought.Value):
            signal = -1

        if signal == 0 or not self.IsFormedAndOnlineAndAllowTrading():
            return

        if signal > 0 and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif signal < 0 and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return vwap_rsi_strategy()
