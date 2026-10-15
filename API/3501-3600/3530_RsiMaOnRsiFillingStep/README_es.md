# RSI MA en RSI Estrategia de paso de llenado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **RSI MA en la RSI estrategia de paso de llenado** es una adaptación StockSharp del MetaTrader asesor experto `RSI_MAonRSI_Filling Step EA.mq5`. El sistema original mide el impulso con un índice de fuerza relativa (RSI) y suaviza ese oscilador con una media móvil. Las operaciones se inician cuando RSI cruza su media móvil mientras ambos valores permanecen en el mismo lado del nivel medio 50. La conversión mantiene los filtros de dirección comercial configurables, el temporizador de sesión opcional y la capacidad de revertir las señales mientras aprovecha los enlaces de indicadores de alto nivel de StockSharp.

## Lógica de trading
1. Suscríbase a la serie de velas seleccionada y calcule dos indicadores en cada barra terminada: `RelativeStrengthIndex` con longitud `RsiPeriod` y `MovingAverage` (`MaType`, `MaPeriod`) aplicados a la secuencia RSI.
2. Espere a que se completen las velas antes de actuar, replicando la protección de "nueva barra" de EA para que cada barra produzca como máximo una decisión comercial.
3. Se produce una configuración **alcista** cuando el valor RSI anterior estaba por debajo de su promedio móvil y el último valor cierra por encima del promedio mientras ambas lecturas permanecen por debajo de `MiddleLevel` (predeterminado 50). Una configuración **bajista** es el caso reflejado por encima del nivel medio.
4. Cuando `ReverseSignals` está habilitado, la condición alcista genera una operación corta y la condición bajista genera una operación larga, imitando la bandera inversa de EA.
5. El parámetro `Mode` limita el comercio solo a largo plazo, solo a corto plazo o en ambas direcciones. Opcionalmente, guardias adicionales cierran la exposición opuesta y bloquean nuevas entradas cuando una posición ya está abierta.
6. Una ventana de tiempo diaria idéntica a la implementación de MetaTrader puede deshabilitar señales fuera del intervalo configurado `SessionStart` → `SessionEnd`, incluidas las sesiones que finalizan hasta la medianoche.

## Parámetros
- **CandleType** – serie de datos procesada por la estrategia. El valor predeterminado son velas con un marco de tiempo de una hora.
- **RsiPeriod** – RSI longitud promedio (predeterminado 14).
- **MaPeriod**: duración de la media móvil aplicada a RSI (predeterminado 21).
- **MaType**: tipo de media móvil utilizado para el suavizado RSI (predeterminado `Simple`).
- **MiddleLevel**: nivel central RSI utilizado por ambos indicadores para validar operaciones (predeterminado 50).
- **ReverseSignals**: invierte la interpretación del cruce alcista/bajista (predeterminado `false`).
- **Modo**: filtro de dirección comercial (`BuyOnly`, `SellOnly`, `Both`).
- **CloseOppositePositions**: si se debe aplanar la posición opuesta antes de ingresar a una nueva operación (predeterminado `false`).
- **OnlyOnePosition**: evita nuevas órdenes mientras una posición ya está abierta (predeterminado `false`).
- **UseTimeWindow**: habilita el filtro de sesión de negociación diaria (predeterminado `false`).
- **SessionStart / SessionEnd**: horas de inicio y finalización de la sesión comercial permitida. Funciona con sesiones nocturnas terminando después de la medianoche.

## Notas de implementación
- Los valores del indicador se entregan a través de `Bind`, lo que elimina la necesidad de administración manual del búfer que el EA original requería con `CopyBuffer`.
- Los valores anteriores de RSI y de media móvil se almacenan en caché para reflejar el patrón de acceso `RSI[m_bar_current+1]` de MQL. El campo `_lastSignalBarTime` garantiza solo una operación por barra, al igual que las marcas de tiempo `m_last_deal_buy_in` / `m_last_deal_sell_in` de EA.
- La gestión comercial utiliza `BuyMarket()` y `SellMarket()` para reflejar la ejecución inmediata del mercado de EA. El cierre opcional de la exposición opuesta se gestiona con `ClosePosition()` antes de realizar la nueva orden.
- El filtro de tiempo replica la función `TimeControlHourMinute` de EA, incluida la lógica de la ventana nocturna donde la hora de inicio es mayor que la hora de finalización.
- Los ayudantes de gráficos dibujan velas de precios con marcadores comerciales más un panel RSI dedicado para que los cruces puedan inspeccionarse visualmente durante las pruebas retrospectivas.

## Diferencias respecto al Asesor Experto
- Las opciones de administración de dinero (`ENUM_LOT_OR_RISK`, paradas dinámicas, comprobaciones de nivel de congelación) no se reproducen. Los usuarios de StockSharp pueden adjuntar su propia lógica de protección o módulos de riesgo.
- Las confirmaciones comerciales, las verificaciones de números mágicos y las colas de pedidos manuales de EA son innecesarias porque StockSharp administra los ciclos de vida de los pedidos de manera diferente. La estrategia supone la disponibilidad inmediata de órdenes de mercado.
- Las órdenes de stop-loss y take-profit no se adjuntan automáticamente. Utilice `StartProtection` o módulos externos si ese comportamiento es necesario.

## Consejos de uso
1. Mantenga `MiddleLevel` cerca de 50 para permanecer fiel al comportamiento original de reversión a la media. Desviarse de este valor empuja al sistema hacia operaciones de ruptura.
2. Habilite `OnlyOnePosition` si prefiere transiciones estrictas de plano a posición. Deshabilítelo para permitir la pirámide con lógica de volumen personalizada.
3. Combine el filtro de tiempo con el horario de operaciones de bolsa cuando trabaje con futuros o acciones para evitar el ruido nocturno.
4. Optimice `MaPeriod`, `RsiPeriod` y `MiddleLevel` juntos al adaptar la estrategia a nuevos instrumentos.

Con estas notas puede ejecutar, personalizar y ampliar con confianza la estrategia RSI MA en RSI Paso de llenado dentro del entorno StockSharp.
