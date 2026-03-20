import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import VortexIndicator, VortexIndicatorValue


class vortex_oscillator_system_strategy(Strategy):
    """Vortex oscillator system: trades when VI+-VI- crosses configurable thresholds."""

    def __init__(self):
        super(vortex_oscillator_system_strategy, self).__init__()

        self._length = self.Param("Length", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Vortex Length", "Period used for the Vortex indicator", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe used to build candles", "General")
        self._use_buy_stop_loss = self.Param("UseBuyStopLoss", False) \
            .SetDisplay("Use Buy Stop Loss", "Enable oscillator-based stop loss for longs", "Risk")
        self._use_buy_take_profit = self.Param("UseBuyTakeProfit", False) \
            .SetDisplay("Use Buy Take Profit", "Enable oscillator-based take profit for longs", "Risk")
        self._use_sell_stop_loss = self.Param("UseSellStopLoss", False) \
            .SetDisplay("Use Sell Stop Loss", "Enable oscillator-based stop loss for shorts", "Risk")
        self._use_sell_take_profit = self.Param("UseSellTakeProfit", False) \
            .SetDisplay("Use Sell Take Profit", "Enable oscillator-based take profit for shorts", "Risk")
        self._buy_threshold = self.Param("BuyThreshold", -0.1) \
            .SetDisplay("Buy Threshold", "Oscillator value that triggers a long setup", "Signals")
        self._buy_stop_loss_level = self.Param("BuyStopLossLevel", -1.0) \
            .SetDisplay("Buy Stop Loss Level", "Oscillator value that closes long trades", "Signals")
        self._buy_take_profit_level = self.Param("BuyTakeProfitLevel", 0.0) \
            .SetDisplay("Buy Take Profit Level", "Oscillator value that closes long trades", "Signals")
        self._sell_threshold = self.Param("SellThreshold", 0.1) \
            .SetDisplay("Sell Threshold", "Oscillator value that triggers a short setup", "Signals")
        self._sell_stop_loss_level = self.Param("SellStopLossLevel", 1.0) \
            .SetDisplay("Sell Stop Loss Level", "Oscillator value that closes short trades", "Signals")
        self._sell_take_profit_level = self.Param("SellTakeProfitLevel", 0.0) \
            .SetDisplay("Sell Take Profit Level", "Oscillator value that closes short trades", "Signals")

    @property
    def Length(self):
        return int(self._length.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def UseBuyStopLoss(self):
        return self._use_buy_stop_loss.Value
    @property
    def UseBuyTakeProfit(self):
        return self._use_buy_take_profit.Value
    @property
    def UseSellStopLoss(self):
        return self._use_sell_stop_loss.Value
    @property
    def UseSellTakeProfit(self):
        return self._use_sell_take_profit.Value
    @property
    def BuyThreshold(self):
        return float(self._buy_threshold.Value)
    @property
    def BuyStopLossLevel(self):
        return float(self._buy_stop_loss_level.Value)
    @property
    def BuyTakeProfitLevel(self):
        return float(self._buy_take_profit_level.Value)
    @property
    def SellThreshold(self):
        return float(self._sell_threshold.Value)
    @property
    def SellStopLossLevel(self):
        return float(self._sell_stop_loss_level.Value)
    @property
    def SellTakeProfitLevel(self):
        return float(self._sell_take_profit_level.Value)

    def OnStarted(self, time):
        super(vortex_oscillator_system_strategy, self).OnStarted(time)

        self._vortex = VortexIndicator()
        self._vortex.Length = self.Length

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._vortex, self.process_candle).Start()

    def process_candle(self, candle, vortex_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._vortex.IsFormed:
            return

        vi_plus = float(vortex_value.PlusVi) if vortex_value.PlusVi is not None else 0.0
        vi_minus = float(vortex_value.MinusVi) if vortex_value.MinusVi is not None else 0.0
        oscillator = vi_plus - vi_minus

        long_setup = False
        short_setup = False

        if self.UseBuyStopLoss:
            if oscillator <= self.BuyThreshold and oscillator > self.BuyStopLossLevel:
                long_setup = True
                short_setup = False
        elif oscillator <= self.BuyThreshold:
            long_setup = True
            short_setup = False

        if self.UseSellStopLoss:
            if oscillator >= self.SellThreshold and oscillator < self.SellStopLossLevel:
                short_setup = True
                long_setup = False
        elif oscillator >= self.SellThreshold:
            short_setup = True
            long_setup = False

        if oscillator >= self.BuyThreshold and oscillator <= self.SellThreshold:
            long_setup = False
            short_setup = False

        if long_setup and self.Position <= 0:
            self.BuyMarket()
        elif short_setup and self.Position >= 0:
            self.SellMarket()

        if self.Position > 0:
            if self.UseBuyStopLoss and oscillator <= self.BuyStopLossLevel:
                self.SellMarket()
                return
            if self.UseBuyTakeProfit and oscillator >= self.BuyTakeProfitLevel:
                self.SellMarket()
                return
        elif self.Position < 0:
            if self.UseSellStopLoss and oscillator >= self.SellStopLossLevel:
                self.BuyMarket()
                return
            if self.UseSellTakeProfit and oscillator <= self.SellTakeProfitLevel:
                self.BuyMarket()

    def OnReseted(self):
        super(vortex_oscillator_system_strategy, self).OnReseted()

    def CreateClone(self):
        return vortex_oscillator_system_strategy()
