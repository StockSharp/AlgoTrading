import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex, Momentum
from StockSharp.Algo.Strategies import Strategy

class limits_rsi_momentum_bot_strategy(Strategy):
    """
    RSI + Momentum confirmation with StartProtection for SL/TP.
    """

    def __init__(self):
        super(limits_rsi_momentum_bot_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14).SetDisplay("RSI Period", "RSI period", "Indicators")
        self._rsi_buy = self.Param("RsiBuyRestrict", 30.0).SetDisplay("RSI Buy", "Max RSI for buy", "Indicators")
        self._rsi_sell = self.Param("RsiSellRestrict", 70.0).SetDisplay("RSI Sell", "Min RSI for sell", "Indicators")
        self._mom_period = self.Param("MomentumPeriod", 14).SetDisplay("Mom Period", "Momentum period", "Indicators")
        self._mom_buy = self.Param("MomentumBuyRestrict", 1.0).SetDisplay("Mom Buy", "Max momentum for buy", "Indicators")
        self._mom_sell = self.Param("MomentumSellRestrict", 1.0).SetDisplay("Mom Sell", "Min momentum for sell", "Indicators")
        self._tp_points = self.Param("TakeProfit", 35).SetDisplay("TP", "Take profit in steps", "Risk")
        self._sl_points = self.Param("StopLoss", 8).SetDisplay("SL", "Stop loss in steps", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Timeframe", "General")
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(limits_rsi_momentum_bot_strategy, self).OnReseted()
        self._cooldown = 0

    def OnStarted(self, time):
        super(limits_rsi_momentum_bot_strategy, self).OnStarted(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        mom = Momentum()
        mom.Length = self._mom_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, mom, self._process_candle).Start()
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        tp = Unit(self._tp_points.Value * step, UnitTypes.Absolute)
        sl = Unit(self._sl_points.Value * step, UnitTypes.Absolute)
        self.StartProtection(tp, sl)
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, rsi_val, mom_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._cooldown > 0:
            self._cooldown -= 1
            return
        rsi = float(rsi_val)
        mom = float(mom_val)
        buy_signal = rsi < self._rsi_buy.Value and mom < self._mom_buy.Value and self.Position <= 0
        sell_signal = rsi > self._rsi_sell.Value and mom > self._mom_sell.Value and self.Position >= 0
        if buy_signal:
            self.BuyMarket()
            self._cooldown = 10
        elif sell_signal:
            self.SellMarket()
            self._cooldown = 10

    def CreateClone(self):
        return limits_rsi_momentum_bot_strategy()
