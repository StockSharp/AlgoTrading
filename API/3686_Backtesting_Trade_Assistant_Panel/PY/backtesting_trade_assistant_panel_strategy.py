import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class backtesting_trade_assistant_panel_strategy(Strategy):
    def __init__(self):
        super(backtesting_trade_assistant_panel_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._stop_loss_pips = self.Param("StopLossPips", 50.0)
        self._take_profit_pips = self.Param("TakeProfitPips", 100.0)

        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._direction = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @StopLossPips.setter
    def StopLossPips(self, value):
        self._stop_loss_pips.Value = value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @TakeProfitPips.setter
    def TakeProfitPips(self, value):
        self._take_profit_pips.Value = value

    def OnReseted(self):
        super(backtesting_trade_assistant_panel_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._direction = 0

    def OnStarted(self, time):
        super(backtesting_trade_assistant_panel_strategy, self).OnStarted(time)
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._direction = 0

        sma = SimpleMovingAverage()
        sma.Length = 20

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(sma, self._process_candle).Start()

    def _process_candle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)
        sma_val = float(sma_value)
        sl_pips = float(self.StopLossPips)
        tp_pips = float(self.TakeProfitPips)

        # Use price-based pip approximation (0.01% of price per pip)
        pip = price * 0.0001 if price > 0 else 1.0

        # Check stop-loss and take-profit
        if self.Position != 0 and self._entry_price > 0:
            if self._direction > 0:
                if self._stop_price is not None and price <= self._stop_price:
                    self.SellMarket()
                    self._entry_price = 0.0
                    self._stop_price = None
                    self._take_price = None
                    self._direction = 0
                    return
                if self._take_price is not None and price >= self._take_price:
                    self.SellMarket()
                    self._entry_price = 0.0
                    self._stop_price = None
                    self._take_price = None
                    self._direction = 0
                    return
            elif self._direction < 0:
                if self._stop_price is not None and price >= self._stop_price:
                    self.BuyMarket()
                    self._entry_price = 0.0
                    self._stop_price = None
                    self._take_price = None
                    self._direction = 0
                    return
                if self._take_price is not None and price <= self._take_price:
                    self.BuyMarket()
                    self._entry_price = 0.0
                    self._stop_price = None
                    self._take_price = None
                    self._direction = 0
                    return

        # Entry: SMA crossover
        if self.Position == 0:
            if price > sma_val:
                self.BuyMarket()
                self._entry_price = price
                self._stop_price = price - sl_pips * pip if sl_pips > 0 else None
                self._take_price = price + tp_pips * pip if tp_pips > 0 else None
                self._direction = 1
            elif price < sma_val:
                self.SellMarket()
                self._entry_price = price
                self._stop_price = price + sl_pips * pip if sl_pips > 0 else None
                self._take_price = price - tp_pips * pip if tp_pips > 0 else None
                self._direction = -1

    def CreateClone(self):
        return backtesting_trade_assistant_panel_strategy()
