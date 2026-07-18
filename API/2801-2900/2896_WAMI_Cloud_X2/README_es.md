# Estrategia WAMI Cloud X2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia replica el comportamiento de doble marco temporal del asesor experto MetaTrader original "Exp_WAMI_Cloud_X2". Usa el Warren Momentum Indicator (WAMI) en un marco temporal superior para definir el sesgo dominante y una segunda instancia del mismo indicador en un marco temporal inferior para temporizar entradas y salidas. La línea principal WAMI se compara contra su línea de señal interna en ambos marcos temporales, lo que refleja la lógica de la implementación MQL original.

## Concepto

- **Construcción WAMI** – WAMI se construye a partir de la primera diferencia de los precios de cierre, suavizada por tres medias móviles secuenciales con métodos individualmente seleccionables (SMA, EMA, SMMA o LWMA). Una cuarta media móvil produce la línea de señal. El indicador personalizado en la estrategia reproduce esta cadena exactamente, por lo que tanto la línea principal como la de señal están disponibles en un payload de valor.
- **Filtro de tendencia (marco temporal superior)** – Las velas de seis horas predeterminadas impulsan el WAMI de tendencia. Cuando la línea principal está por encima de la línea de señal, la dirección de la tendencia se vuelve alcista; cuando está por debajo, se vuelve bajista. Se mantiene un estado neutro cuando ambas líneas son iguales o el indicador aún se está formando.
- **Motor de señal (marco temporal inferior)** – Las velas de 30 minutos predeterminadas se usan para buscar entradas. Para cada vela finalizada, la estrategia almacena los valores WAMI recientes y evalúa la última barra cerrada definida por `SignalBar`. Los cruces se detectan comparando el valor más reciente (`SignalBar`) contra el anterior (`SignalBar + 1`).

## Reglas de Trading

1. **Salidas**
   - Las posiciones largas se cierran cuando el marco temporal de señal muestra persistente bajismo (`previous.Main < previous.Signal`) si `CloseLongOnSignal` está habilitado.
   - Las posiciones cortas se cierran análogamente cuando `CloseShortOnSignal` está habilitado.
   - Cuando el marco temporal superior cambia de dirección (`_trendDirection`), el flag respectivo `CloseLongOnTrendFlip` o `CloseShortOnTrendFlip` fuerza una salida.
2. **Entradas**
   - Las entradas cortas se permiten cuando el marco temporal superior es bajista y el WAMI de señal cruza hacia arriba (`current.Main >= current.Signal` con `previous.Main < previous.Signal`). Esto coincide con el EA original que vende en la primera penetración ascendente de la línea de señal dentro de una tendencia bajista.
   - Las entradas largas son la condición inversa cuando el marco temporal superior es alcista y el WAMI de señal cruza hacia abajo (`current.Main <= current.Signal` con `previous.Main > previous.Signal`).
   - Los interruptores de entrada (`EnableBuyEntries`, `EnableSellEntries`) pueden deshabilitar cualquier lado. Cuando una posición opuesta está abierta, la estrategia envía una orden de mercado compensatoria para aplanar y revertir en un solo comando, igual que las funciones auxiliares MQL.

## Parámetros

- **WAMI de Tendencia** – `TrendPeriod1/2/3`, `TrendMethod1/2/3`, `TrendSignalPeriod`, `TrendSignalMethod`, `TrendCandleType`.
- **WAMI de Señal** – `SignalPeriod1/2/3`, `SignalMethod1/2/3`, `SignalSignalPeriod`, `SignalSignalMethod`, `SignalCandleType`.
- **Flags de Control** – `SignalBar`, `EnableBuyEntries`, `EnableSellEntries`, `CloseLongOnTrendFlip`, `CloseShortOnTrendFlip`, `CloseLongOnSignal`, `CloseShortOnSignal`.
- **Tamaño de Trading** – `TradeVolume` define el tamaño de la orden de mercado usado para nuevas entradas. Las reversiones envían el volumen opuesto más el tamaño configurado.

Todos los parámetros se exponen a través de objetos `StrategyParam<T>`, por lo que pueden ser optimizados o modificados desde la UI de StockSharp igual que los inputs de MetaTrader lo permitían.

## Valores predeterminados

- **Marco temporal de tendencia** – velas de 6 horas.
- **Marco temporal de señal** – velas de 30 minutos.
- **Todos los métodos de media móvil** – Simple (SMA).
- **Longitudes de media móvil** – 4 / 13 / 13 para las tres etapas y 4 para la línea de señal en ambos marcos temporales.
- **SignalBar** – 1 (usar la última vela cerrada).
- **TradeVolume** – 1 contrato.
- **Todos los flags de permiso** – Habilitados (true).

## Notas Adicionales

- La estrategia no establece órdenes de stop-loss o take-profit fijos. La gestión de riesgo debe configurarse externamente si se requiere.
- Los ayudantes de gráfico dibujan las velas del marco temporal de señal, ambas líneas WAMI y las operaciones ejecutadas. El marco temporal de tendencia se traza en un área separada para confirmación visual.
- La implementación evita el polling de valores de indicador (sin llamadas `GetValue`) y se adhiere a la API de suscripción de velas de alto nivel, siguiendo las directrices del proyecto.
