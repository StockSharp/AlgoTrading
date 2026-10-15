# Estrategia de IA de Cyberia Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una conversión StockSharp del asesor experto **CyberiaTrader.mq4 (compilación 8553)**. El programa original MQL mezcla una
Motor de probabilidad con una colección de filtros de tendencias opcionales. El port de C# mantiene la misma estructura: un modelo de probabilidad busca
para el período de muestreo más confiable y luego los filtros opcionales MACD, EMA y de reversión pueden vetar las operaciones.

## Indicadores y modelo interno

- **Motor de probabilidad**: itera períodos de muestreo candidatos (`MaxPeriod`) y evalúa `SamplesPerPeriod` segmentos históricos.
Para cada período el motor calcula:
  - Dirección de decisión (compra/venta/plana) basada en velas consecutivas alcistas/bajistas de un minuto espaciadas por el período de muestreo.
  - Amplitudes promedio de "posibilidades" para compra, venta y resultados indefinidos y la proporción de resultados exitosos anteriores
`SpreadThreshold`.
  - Ratios de éxito que seleccionan el periodo de mejor rendimiento.
- **EMA Filtro de tendencia**: media móvil exponencial opcional (`EnableMa`) que bloquea las operaciones contra la pendiente actual.
- Filtro **MACD**: convergencia/divergencia de media móvil opcional (`EnableMacd`) que prohíbe operar contra el impulso.
- **Detector de reversión**: detector de picos opcional (`EnableReversalDetector`) que invierte los permisos cuando las probabilidades aumentan
`ReversalFactor` múltiplos de sus promedios.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| `MaxPeriod` | Mayor paso de muestreo inspeccionado por el motor de probabilidad. |
| `SamplesPerPeriod` | Número de segmentos procesados por candidato de período (refleja el MQL `ValuesPeriodCount`). |
| `SpreadThreshold` | Amplitud mínima que cuenta como un resultado de probabilidad exitoso. |
| `EnableCyberiaLogic` | Habilita los interruptores de probabilidad de Cyberia que pueden deshabilitar compras o ventas. |
| `EnableMacd` | Habilita el filtro de impulso MACD. |
| `EnableMa` | Habilita el filtro de pendiente EMA. |
| `EnableReversalDetector` | Habilita el detector de reversión para alternar permisos en picos extremos. |
| `MaPeriod` | EMA longitud utilizada por el filtro de tendencias. |
| `MacdFast` / `MacdSlow` / `MacdSignal` | MACD EMA rápida, EMA lenta y períodos de señal. |
| `ReversalFactor` | Multiplicador que activa el detector de reversión. |
| `CandleType` | Tipo de datos de vela procesados por el modelo (predeterminado 1 minuto). |
| `TakeProfitPercent` | Toma de ganancias protectora opcional expresada como porcentaje. |
| `StopLossPercent` | Stop loss de protección opcional expresado como porcentaje. |

## Lógica de trading

1. Cada vela completa actualiza la cola del historial local y vuelve a calcular las estadísticas de probabilidad para cada período del 1 al
`MaxPeriod`. El período con mayor índice de éxito se convierte en la configuración activa.
2. La lógica de Cyberia establece indicadores `DisableBuy`/`DisableSell` usando las mismas comparaciones que el código MQL:
   - Compara las posibilidades promedio de compra/venta y sus variantes ponderadas por éxito cuando el período aumenta o disminuye.
   - Desactiva las entradas si las nuevas posibilidades superan el doble de sus promedios exitosos.
3. Los filtros opcionales se aplican en orden: MACD, EMA pendiente y luego el detector de inversión.
4. Cuando no hay ninguna posición abierta, la estrategia entra si la decisión actual es comprar (o vender) y la posibilidad correspondiente excede
su promedio exitoso mientras la dirección opuesta está deshabilitada.
5. Mientras existe una posición, el código verifica las mismas condiciones para cerrar cuando el motor de probabilidad cambia o cuando los filtros prohíben la posición.
dirección actual.
6. `StartProtection` reproduce los bloques de administración de dinero originales cuando se proporcionan parámetros de riesgo distintos de cero.

## Notas sobre la conversión

- El puerto mantiene los cálculos estadísticos pero reemplaza la verificación de propagación basada en ticks con el `SpreadThreshold` configurable.
- El diagnóstico automático de tamaño de lote y equilibrio del script MQL no está implementado; El volumen StockSharp se controla mediante `Volume`.
- Los módulos MoneyTrain y Pipsator se condensan en la lógica unificada de entrada/salida descrita anteriormente para coincidir con el uso de alto nivel de API.
- La estrategia agrega dibujo de gráficos para velas, EMA y MACD para facilitar la validación en el diseñador.
