# Estrategia RSI Expert con Filtro de Tendencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Conversión del asesor experto de MetaTrader 5 **RSI_Expert_v2.0** a la API de estrategia de alto nivel de StockSharp.
- Genera señales en el `CandleType` configurado (predeterminado 1 hora) y ejecuta operaciones al cierre de la vela.
- Diseñado para posiciones netas: la estrategia mantiene una única posición agregada en lugar de cubrir múltiples tickets.

## Lógica de entrada
1. **Cruce de RSI** – una configuración larga aparece cuando el último valor del RSI sube por encima de `RsiLevelDown` mientras la vela finalizada anterior estaba por debajo del nivel. Una configuración corta se activa cuando el RSI cae por debajo de `RsiLevelUp` después de estar por encima.
2. **Filtro de media móvil** – el experto original permite operar con o en contra de un cruce de media móvil. El parámetro `MaMode` reproduce las opciones:
   - `Off`: ignorar las medias móviles y operar solo con disparadores de RSI.
   - `Forward`: permitir largos solo cuando la MA rápida está por encima de la MA lenta, cortos solo cuando está por debajo.
   - `Reverse`: invertir el filtro para que los largos requieran la MA rápida por debajo de la MA lenta, coincidiendo con el modo "inverso" del EA.

Ambas condiciones deben coincidir antes de que la estrategia abra una nueva orden de mercado. Si ya hay una posición abierta o una orden en espera, se ignoran las nuevas señales hasta que termine.

## Gestión de operaciones
- El stop-loss inicial y el take-profit se expresan en pips usando el `PriceStep` del instrumento. Ambos son opcionales; establecer un valor de cero deshabilita la salida respectiva.
- Cuando `TrailingStopPips` es mayor que cero, el stop seguirá el precio una vez que el beneficio supere `TrailingStopPips + TrailingStepPips`. El valor del paso debe ser estrictamente positivo cuando el trailing está habilitado (la estrategia lanza una excepción en caso contrario).
- Si `UseMartingale` está habilitado, el siguiente volumen de la orden se duplica después de que la posición anterior cerrara con una pérdida (detectada a través del PnL realizado). Las operaciones ganadoras reinician el multiplicador.

## Gestión del capital
- `MoneyMode = FixedVolume` mantiene el mismo `VolumeOrRiskValue` para cada entrada.
- `MoneyMode = RiskPercent` trata `VolumeOrRiskValue` como un porcentaje del capital de la cartera y deriva la cantidad a partir de la distancia del stop-loss configurado. Cuando no se especifica stop-loss, la estrategia recurre al valor bruto.
- Los volúmenes se normalizan a las reglas de la bolsa usando `Security.MinVolume` y `Security.VolumeStep` para evitar tamaños de orden inválidos.

## Notas adicionales de implementación
- La lógica de trailing y las comprobaciones de stop/objetivo se evalúan en velas finalizadas para replicar el comportamiento de "nueva barra" de la versión MQL.
- La marca de martingala usa cambios de PnL realizados cuando se cierra una posición externamente, por lo que los cierres manuales también se rastrean.
- Dado que StockSharp usa posiciones agregadas, las operaciones largas y cortas simultáneas (modo de cobertura MT5) no son compatibles.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `CandleType` | Marco temporal usado para actualizaciones de indicadores y generación de señales. |
| `StopLossPips` | Distancia inicial del stop-loss en pips; cero deshabilita el stop. |
| `TakeProfitPips` | Distancia inicial del take-profit en pips; cero deshabilita el objetivo. |
| `TrailingStopPips` | Distancia del trailing stop. Requiere un `TrailingStepPips` positivo. |
| `TrailingStepPips` | Pips adicionales necesarios antes de que el trailing stop se mueva de nuevo. |
| `MoneyMode` | Selecciona dimensionamiento de lote fijo o cálculo por porcentaje de riesgo. |
| `VolumeOrRiskValue` | Tamaño de lote en modo fijo o porcentaje de riesgo en modo de riesgo. |
| `UseMartingale` | Duplica el siguiente volumen de orden después de una operación perdedora. |
| `FastMaPeriod` | Período de la media móvil rápida usada por el filtro de tendencia. |
| `SlowMaPeriod` | Período de la media móvil lenta usada por el filtro de tendencia. |
| `RsiPeriod` | Longitud de promediado para el indicador RSI. |
| `RsiLevelUp` | Umbral superior del RSI que activa configuraciones cortas. |
| `RsiLevelDown` | Umbral inferior del RSI que activa configuraciones largas. |
| `MaMode` | Habilita o invierte el filtro de confirmación de media móvil. |
