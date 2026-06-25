# Estrategia Ichimoku Barabashkakvn
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia recrea el asesor experto Ichimoku de Vladimir Karputov (edición barabashkakvn) sobre la API de alto nivel de StockSharp. Combina el clásico cruce Tenkan/Kijun con la confirmación de la nube Kumo y añade una gestión de riesgo detallada idéntica al original de MetaTrader.

## Cómo funciona

- **Pila de indicadores** – un único indicador Ichimoku Kinko Hyo suministra los valores de Tenkan-sen, Kijun-sen, Senkou Span A y Senkou Span B. Los períodos predeterminados siguen siendo 9/26/52.
- **Entradas largas** – se activan cuando Tenkan cruza hacia arriba a través de Kijun y el precio de cierre está por encima de Senkou Span B. La detección del cruce usa el valor anterior de Tenkan, reflejando la lógica barra por barra del EA.
- **Entradas cortas** – aparecen cuando Tenkan cruza hacia abajo a través de Kijun mientras el cierre está por debajo de Senkou Span A.
- **Gestión de posición** – solo se mantiene una posición neta. Las señales opuestas cierran primero las operaciones existentes, reproduciendo el flujo de reversión de dos pasos del script.
- **Ventana de trading** – un filtro de horas opcional permite al sistema operar solo entre horas de inicio/fin configuradas (inclusive) usando la misma comparación que la versión MQL.

## Gestión de riesgo

- **Stops y objetivos direccionales** – las posiciones largas y cortas usan distancias independientes de stop-loss/take-profit en pips. Los pips se convierten a unidades de precio usando el tamaño de paso del instrumento con un ajuste de ×10 para cotizaciones de 3 y 5 decimales, coincidiendo con el manejo de puntos del EA.
- **Trailing stop** – cada dirección tiene su propia distancia de trailing más un paso de trailing común. El stop avanza solo después de que el movimiento supere `(distancia de trailing + paso de trailing)`, exactamente como en el código original.
- **Ejecución de protección** – las verificaciones de stop-loss y take-profit ocurren en cada vela completada para que los niveles de protección virtuales se comporten como las órdenes gestionadas por el broker de MetaTrader.

## Parámetros

- `TenkanPeriod` *(predeterminado 9)* – longitud de Tenkan-sen.
- `KijunPeriod` *(predeterminado 26)* – longitud de Kijun-sen.
- `SenkouSpanBPeriod` *(predeterminado 52)* – longitud de Senkou Span B.
- `CandleType` *(predeterminado velas de 1 hora)* – fuente de datos para los cálculos.
- `OrderVolume` *(predeterminado 1 lote)* – tamaño de la operación.
- `BuyStopLossPips` / `SellStopLossPips` *(predeterminado 100)* – distancias de stop-loss en pips.
- `BuyTakeProfitPips` / `SellTakeProfitPips` *(predeterminado 300)* – distancias de take-profit en pips.
- `BuyTrailingStopPips` / `SellTrailingStopPips` *(predeterminado 50)* – distancias de trailing en pips.
- `TrailingStepPips` *(predeterminado 5)* – incremento mínimo de ganancia requerido para desplazar el trailing stop.
- `UseTradeHours` *(predeterminado false)* – habilitar el filtro de sesión.
- `StartHour` / `EndHour` *(predeterminados 0/23)* – límites inclusivos de la ventana de trading (0–23).

Estos valores predeterminados coinciden con el EA publicado. Todos los parámetros están expuestos a través de objetos `StrategyParam<T>`, por lo que pueden optimizarse o ajustarse dentro de StockSharp Designer sin tocar el código fuente.
