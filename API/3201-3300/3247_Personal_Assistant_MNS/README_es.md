# Estrategia de Personal Assistant MNS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia porta el asesor experto de MetaTrader `personal_assistant_codeBase_MNS` a StockSharp. Actúa como un asistente de trading manual: en lugar de generar señales autónomas, expone métodos C# que replican las acciones impulsadas por teclas de acceso rápido del EA original (abrir/cerrar operaciones, ajustar volumen o liquidar posiciones rentables). El asistente también registra métricas informativas sobre el símbolo, órdenes activas y niveles de riesgo configurados en cada vela terminada.

## Cómo funciona

1. La estrategia se suscribe a una serie de velas configurable (`CandleType`, 1 minuto por defecto).
2. Cada vela terminada desencadena una actualización que imprime: posición actual, PnL, número de órdenes stop/take activas, spread, valor del tick y el número mágico configurado.
3. Los comandos manuales (p. ej., `PressBuy()` o `PressSell()`) envían órdenes de mercado con el volumen del asistente actual. Los niveles opcionales de stop-loss y take-profit se traducen desde distancias de pip y se almacenan internamente en la estrategia.
4. Los niveles protectores se emulan en datos de velas: si el precio toca el stop o el objetivo almacenados, la estrategia emite salidas de mercado.
5. Una regla opcional de mover a break-even (`UseTrailingStop`) se arma después de que el precio avanza `BreakEvenTriggerPips`; una vez armada, liquida la posición si el precio retrocede al precio de entrada más `BreakEvenOffsetPips`.

## Características

- Replica los botones 1–8 del asistente MQL via métodos públicos:
  - `PressBuy()` / `PressSell()` – abrir operaciones de mercado con niveles protectores opcionales.
  - `PressCloseAll()` – aplanar toda la exposición.
  - `IncreaseVolume()` / `DecreaseVolume()` – ajustar el volumen del asistente en 0.01 lotes.
  - `CloseLongPositions()` / `CloseShortPositions()` – cerrar solo un lado.
  - `CloseProfitablePositions()` – cerrar la posición cuando el PnL flotante es positivo.
- Registra una leyenda de acción detallada al inicio cuando `DisplayLegend` está habilitado.
- Convierte las distancias de riesgo basadas en pips en precios absolutos usando el paso de precio y la precisión decimal del instrumento.
- Admite trailing de break-even para posiciones largas y cortas, imitando la rutina original `MOVETOBREAKEVEN()`.
- Mantiene niveles de stop/take almacenados independientes para operaciones largas y cortas para que al cambiar de dirección se descarten automáticamente los niveles obsoletos.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `MagicNumber` | Identificador informacional copiado del input `MagicNo` de MQL. |
| `DisplayLegend` | Habilitar para imprimir la leyenda de control y los mensajes de estado por vela. |
| `OrderVolume` | Volumen base de la orden de mercado (lotes) reutilizado por todas las acciones manuales. |
| `Slippage` | Deslizamiento máximo tolerado (en ticks), almacenado como referencia. |
| `TakeProfitPips` | Distancia de pip para el nivel de take-profit almacenado (0 lo deshabilita). |
| `StopLossPips` | Distancia de pip para el nivel de stop-loss almacenado (0 lo deshabilita). |
| `UseTrailingStop` | Habilitar o deshabilitar la lógica de trailing de break-even. |
| `BreakEvenTriggerPips` | Distancia de beneficio (en pips) requerida antes de que el stop de break-even se arme. |
| `BreakEvenOffsetPips` | Offset (en pips) añadido al precio de entrada una vez que el stop está armado. |
| `CandleType` | Serie de velas usada para monitoreo y emulación de niveles. |

## Consejos de uso

- Llamar a los métodos helper desde acciones de Designer, scripts o controles de UI para imitar las pulsaciones de teclas del panel original de MetaTrader.
- Los niveles protectores y las distancias de break-even dependen del instrumento que proporcione `PriceStep`, `StepPrice` y `Decimals`. Para instrumentos exóticos sin estos metadatos, ajustar las distancias de pip manualmente o deshabilitar las características estableciéndolas en `0`.
- Dado que los niveles stop/take se reproducen usando los máximos y mínimos de las velas, los picos intra-barra muy rápidos pueden no capturarse a menos que el marco temporal de la vela sea pequeño. Reducir el marco temporal si se requiere mayor granularidad.
- `CloseProfitablePositions()` replica el comportamiento del "botón 8": verifica el PnL flotante y cierra toda la posición solo cuando el valor es estrictamente positivo.

## Diferencias con la versión de MetaTrader

- Las etiquetas del gráfico son reemplazadas por entradas de registro porque StockSharp no expone las mismas primitivas de dibujo dentro de las estrategias.
- Las órdenes de stop-loss y take-profit se simulan a través de salidas de mercado en eventos de velas en lugar de órdenes pendientes inmediatas.
- La gestión de break-even se implementa con órdenes de mercado de StockSharp; no modifica las órdenes protectoras existentes.
- El deslizamiento se mantiene como parámetro informacional; la ejecución real es manejada por el conector de StockSharp.
