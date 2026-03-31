import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import CommodityChannelIndex, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class robust_ea_template_strategy(Strategy):
    def __init__(self):
        super(robust_ea_template_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(8))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("CCI Period", "Commodity Channel Index period", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Relative Strength Index period", "Indicators")
        self._take_profit_pct = self.Param("TakeProfitPct", 3.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._stop_loss_pct = self.Param("StopLossPct", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def cci_period(self):
        return self._cci_period.Value
    @property
    def rsi_period(self):
        return self._rsi_period.Value
    @property
    def take_profit_pct(self):
        return self._take_profit_pct.Value
    @property
    def stop_loss_pct(self):
        return self._stop_loss_pct.Value

    def OnStarted2(self, time):
        super(robust_ea_template_strategy, self).OnStarted2(time)
        cci = CommodityChannelIndex()
        cci.Length = self.cci_period
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(cci, rsi, self.on_candle).Start()
        self.StartProtection(
            takeProfit=Unit(self.take_profit_pct, UnitTypes.Percent),
            stopLoss=Unit(self.stop_loss_pct, UnitTypes.Percent),
            useMarketOrders=True)
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def on_candle(self, candle, cci_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        if not cci_val.IsFormed or not rsi_val.IsFormed:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        cci_value = float(cci_val)
        rsi_value = float(rsi_val)
        long_signal = cci_value < -50.0 and rsi_value < 40.0
        short_signal = cci_value > 50.0 and rsi_value > 60.0
        if long_signal and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif short_signal and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return robust_ea_template_strategy()
