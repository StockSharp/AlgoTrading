# Estrategia de RSI EA v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port StockSharp del asesor experto MetaTrader 5 **"RSI EA v2"**. Automatiza el trading en torno a los cruces de umbral del Relative Strength Index (RSI) mientras replica los controles de gestión de dinero, trailing stop y ventana de trading del asesor original. Por defecto, la estrategia procesa velas de un minuto, pero cualquier tipo de vela puede proporcionarse a través de parámetros.

## Lógica de trading

- **Condiciones de entrada**
  - Las posiciones largas se abren cuando RSI sube por encima del *nivel de Compra* configurado después de estar por debajo en la vela finalizada anterior, y las horas de trading permiten nuevas órdenes.
  - Las posiciones cortas se abren cuando RSI cae por debajo del *nivel de Venta* configurado después de estar por encima previamente, y la ventana de trading está abierta.
  - Cuando ya existe una posición opuesta, la estrategia dimensiona la nueva orden de mercado para aplanar la exposición actual y establecer la dirección solicitada (solo posiciones netas).
- **Condiciones de salida**
  - Los niveles de stop-loss y take-profit expresados en pips se adjuntan tan pronto como se detecta una nueva posición.
  - Un trailing stop imita el EA original: se activa después de que el precio avanza *Trailing stop + Trailing step* y luego se mueve al menos el paso de trailing.
  - La lógica opcional de "cerrar por señal" sale de posiciones largas cuando RSI cruza hacia abajo a través del nivel de venta, y sale de posiciones cortas cuando RSI cruza hacia arriba a través del nivel de compra.
  - Los stops y señales se evalúan solo en velas finalizadas, manteniendo el comportamiento determinista en backtests.

## Gestión de riesgo y trading

- **Stop-loss / Take-profit** – definidos en pips, convertidos a incrementos de precio que respetan la precisión del instrumento (incluidos símbolos forex de 3/5 decimales).
- **Trailing stop** – deshabilitado cuando la distancia es cero. Se requiere un paso de trailing positivo siempre que la distancia de trailing sea distinta de cero.
- **Dimensionamiento de posición** – ya sea un volumen fijo o un volumen automático calculado a partir del porcentaje de riesgo y la distancia del stop. El dimensionamiento de riesgo requiere acceso a la cartera y metadatos de paso de precio válidos.
- **Ventana de trading** – filtro diario opcional definido por horas de inicio inclusivas y fin exclusivas (0–23). Cuando inicio es igual a fin, el mercado se considera cerrado.

## Parámetros

| Nombre | Descripción |
| ---- | ----------- |
| `OpenBuy` / `OpenSell` | Activa/desactiva entradas largas o cortas independientemente. |
| `CloseBySignal` | Habilita salidas en cruces de RSI opuestos. |
| `StopLossPips` | Distancia del stop-loss en pips (0 deshabilita el stop). |
| `TakeProfitPips` | Distancia del take-profit en pips (0 deshabilita el objetivo). |
| `TrailingStopPips` | Distancia del trailing stop en pips. Debe ser cero si no se desea trailing. |
| `TrailingStepPips` | Progreso adicional (en pips) requerido antes de mover el trailing stop. Debe ser positivo cuando el trailing está activo. |
| `RsiPeriod` | Longitud del indicador RSI. |
| `RsiBuyLevel` / `RsiSellLevel` | Umbrales para entradas/salidas largas y cortas. |
| `UseRiskSizing` | Cambia entre volumen fijo y dimensionamiento por porcentaje de riesgo. |
| `FixedVolume` | Tamaño de orden base para modo de volumen fijo o respaldo cuando no se puede calcular el dimensionamiento de riesgo. |
| `RiskPercent` | Porcentaje del patrimonio del portafolio arriesgado por operación. Usado solo cuando `UseRiskSizing` es verdadero y existe una distancia de stop positiva. |
| `UseTimeControl` | Habilita el filtro de ventana de trading diaria. |
| `StartHour` / `EndHour` | Hora de inicio inclusiva y fin exclusiva (0–23) de la ventana de trading. |
| `CandleType` | Tipo de datos de vela que impulsa los cálculos del indicador. |

## Notas de implementación

- Usa la API de suscripción de velas de alto nivel con el binding del indicador `RSI`.
- Convierte las distancias en pips usando la precisión del instrumento (`PriceStep` y `Decimals`) para coincidir con la lógica de 3/5 dígitos de MetaTrader.
- Normaliza los volúmenes de órdenes al paso de volumen y los límites del instrumento (volumen mín/máx).
- La lógica de trailing solo actualiza las referencias de stop internas; las salidas se realizan con órdenes de mercado cuando se superan los niveles.
- Mantiene estado separado para posiciones largas y cortas para preservar los niveles de trailing y protectores entre velas.

## Uso

1. Adjunte la estrategia a un conector StockSharp con metadatos de instrumento y portafolio apropiados.
2. Configure los umbrales, las distancias en pips y la ventana de tiempo opcional para que coincidan con el mercado deseado.
3. Habilite el dimensionamiento basado en riesgo si la información del portafolio está disponible; de lo contrario déjelo deshabilitado para usar un lote fijo.
4. Inicie la estrategia – esperará velas finalizadas, aplicará la lógica RSI y gestionará posiciones activas según las protecciones configuradas.
