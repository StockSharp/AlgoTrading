import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy


class cci_threshold_strategy(Strategy):
    """
    CCI Threshold strategy.
    Long only: buys when CCI(LookbackPeriod) is below BuyThreshold and closes the long when the close is above the previous close.
    Optional stop loss and take profit are StopLossPoints and TakeProfitPoints price steps from the entry.
    """

    def __init__(self):
        super(cci_threshold_strategy, self).__init__()
        self._lookback_period = self.Param("LookbackPeriod", 12).SetGreaterThanZero().SetDisplay("Lookback Period", "CCI period", "CCI")
        self._buy_threshold = self.Param("BuyThreshold", -90.0).SetDisplay("Buy Threshold", "CCI level below which the strategy buys", "CCI")
        self._stop_loss_points = self.Param("StopLossPoints", 100.0).SetNotNegative().SetDisplay("Stop Loss Points", "Stop loss distance in price steps", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 150.0).SetNotNegative().SetDisplay("Take Profit Points", "Take profit distance in price steps", "Risk")
        self._use_stop_loss = self.Param("UseStopLoss", False).SetDisplay("Use Stop Loss", "Enable the stop loss", "Risk")
        self._use_take_profit = self.Param("UseTakeProfit", False).SetDisplay("Use Take Profit", "Enable the take profit", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_close = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(cci_threshold_strategy, self).OnReseted()
        self._prev_close = None

    def OnStarted2(self, time):
        super(cci_threshold_strategy, self).OnStarted2(time)

        self._prev_close = None

        cci = CommodityChannelIndex()
        cci.Length = self._lookback_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(cci, self._process_candle).Start()

        step = self.Security.PriceStep if self.Security is not None and self.Security.PriceStep is not None else Decimal(1)
        take_points = Decimal(self._take_profit_points.Value)
        stop_points = Decimal(self._stop_loss_points.Value)
        take_profit = Unit(take_points * step, UnitTypes.Absolute) if self._use_take_profit.Value and take_points > 0 else Unit()
        stop_loss = Unit(stop_points * step, UnitTypes.Absolute) if self._use_stop_loss.Value and stop_points > 0 else Unit()
        self.StartProtection(take_profit, stop_loss, useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, cci)

    def _process_candle(self, candle, cci_value):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice
        pc = self._prev_close
        self._prev_close = close

        if not cci_value.IsFormed or pc is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0:
            if close > pc:
                self.SellMarket(self.Position)
        elif self.Position == 0 and cci_value.GetValue[Decimal](None) < Decimal(self._buy_threshold.Value):
            self.BuyMarket(self.Volume)

    def CreateClone(self):
        return cci_threshold_strategy()
