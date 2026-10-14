import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

import math

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange, Momentum
from StockSharp.Algo.Strategies import Strategy


class momentum_keltner_stochastic_combo_strategy(Strategy):
    """
    Momentum Keltner Stochastic Combo strategy.
    The Keltner stochastic places the close inside a Keltner channel (EMA basis, ATR width) on a 0-100 scale.
    Goes long when momentum is positive and the stochastic is below Threshold, short when momentum is negative and
    the stochastic is above Threshold. A long exits when the stochastic rises above Threshold and a short when it falls
    below it. Position size can grow with realized profit, and a fixed stop in points protects every position.
    """

    def __init__(self):
        super(momentum_keltner_stochastic_combo_strategy, self).__init__()
        self._mom_length = self.Param("MomLength", 7).SetGreaterThanZero().SetDisplay("Momentum Lookback", "Momentum lookback length", "Indicators")
        self._keltner_length = self.Param("KeltnerLength", 9).SetGreaterThanZero().SetDisplay("Keltner EMA Length", "EMA length for Keltner basis", "Indicators")
        self._keltner_multiplier = self.Param("KeltnerMultiplier", 0.5).SetGreaterThanZero().SetDisplay("Keltner Mult", "Keltner multiplier", "Indicators")
        self._threshold = self.Param("Threshold", 99.0).SetDisplay("Stochastic Threshold", "Threshold for Keltner stochastic", "Indicators")
        self._atr_length = self.Param("AtrLength", 20).SetGreaterThanZero().SetDisplay("ATR Length", "ATR length for Keltner", "Indicators")
        self._sl_points = self.Param("SlPoints", 1185.0).SetNotNegative().SetDisplay("Stop Loss Points", "Stop loss in price points", "Risk Management")
        self._enable_scaling = self.Param("EnableScaling", True).SetDisplay("Enable Dynamic Contracts", "Use equity based position sizing", "Money Management")
        self._base_contracts = self.Param("BaseContracts", 1).SetGreaterThanZero().SetDisplay("Base Contracts", "Initial contract size", "Money Management")
        self._initial_capital = self.Param("InitialCapital", 30000.0).SetGreaterThanZero().SetDisplay("Initial Capital", "Starting capital", "Money Management")
        self._equity_step = self.Param("EquityStep", 150000.0).SetGreaterThanZero().SetDisplay("Equity Step", "Equity step for contract change", "Money Management")
        self._max_contracts = self.Param("MaxContracts", 15).SetGreaterThanZero().SetDisplay("Max Contracts", "Maximum contracts allowed", "Money Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles for calculations", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(momentum_keltner_stochastic_combo_strategy, self).OnStarted2(time)

        ema = ExponentialMovingAverage()
        ema.Length = self._keltner_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        momentum = Momentum()
        momentum.Length = self._mom_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, atr, momentum, self._process_candle).Start()

        step = self.Security.PriceStep if self.Security is not None and self.Security.PriceStep is not None else Decimal(1)
        sl = self._sl_points.Value
        self.StartProtection(Unit(), Unit(Decimal(sl) * step, UnitTypes.Absolute) if sl > 0 else Unit(), useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, momentum)

    def _process_candle(self, candle, ema_value, atr_value, momentum_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        multiplier = float(self._keltner_multiplier.Value)
        ema = float(ema_value)
        atr = float(atr_value)
        upper = ema + multiplier * atr
        lower = ema - multiplier * atr
        width = upper - lower
        if width == 0.0:
            return

        keltner_stoch = 100.0 * (float(candle.ClosePrice) - lower) / width
        momentum = float(momentum_value)
        threshold = float(self._threshold.Value)
        size = self._get_contracts()

        if momentum > 0.0 and keltner_stoch < threshold and self.Position <= 0:
            self.BuyMarket(size + abs(self.Position))
        elif momentum < 0.0 and keltner_stoch > threshold and self.Position >= 0:
            self.SellMarket(size + abs(self.Position))
        elif self.Position > 0 and keltner_stoch > threshold:
            self.SellMarket(self.Position)
        elif self.Position < 0 and keltner_stoch < threshold:
            self.BuyMarket(-self.Position)

    def _get_contracts(self):
        base = self._base_contracts.Value
        contracts = float(base)

        if self._enable_scaling.Value:
            # Every full EquityStep of equity above InitialCapital adds one contract, losses take them away.
            initial = float(self._initial_capital.Value)
            equity = initial + float(self.PnL)
            steps = math.floor((equity - initial) / float(self._equity_step.Value))
            contracts = max(float(base), base + steps)

        return Decimal(min(contracts, float(self._max_contracts.Value)))

    def CreateClone(self):
        return momentum_keltner_stochastic_combo_strategy()
