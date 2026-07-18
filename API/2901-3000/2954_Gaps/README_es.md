# Estrategia de Brechas (Gaps)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una estrategia de price action que reacciona a brechas de apertura entre velas consecutivas. Espera a que una nueva barra abra
 más allá del máximo o mínimo anterior por una distancia en pips configurable, entra en la dirección de la reversión esperada y
 gestiona la operación con stops fijos, objetivos y un trailing stop escalonado opcional.

## Cómo funciona

1. La estrategia monitorea un solo símbolo usando el marco temporal seleccionado.
2. Cuando se forma una nueva vela, compara el precio de apertura con la vela anterior:
   - Si la apertura está por debajo del mínimo anterior menos `GapPips`, la estrategia entra en una posición larga esperando un retroceso alcista.
   - Si la apertura está por encima del máximo anterior más `GapPips`, entra en una posición corta anticipando una corrección bajista.
3. Una vez en una operación, la gestión de riesgos se maneja completamente dentro de la estrategia:
   - Se coloca un stop-loss fijo a `StopLossPips` por debajo (para largo) o por encima (para corto) del precio de entrada.
   - Se establece un take-profit fijo a `TakeProfitPips` del precio de entrada en la dirección de la operación.
   - Se puede activar un trailing stop; solo se mueve después de que el precio haya avanzado `TrailingStopPips + TrailingStepPips` y luego
     bloquea ganancias manteniendo el stop a `TrailingStopPips` del precio más favorable.
4. Los niveles de protección se evalúan en cada vela completada usando los extremos máximo/mínimo para que los toques dentro de la barra desencadenen salidas de manera confiable.
5. Las órdenes abiertas se cancelan antes de tomar una nueva posición, y las reversiones de posición cierran automáticamente el lado opuesto.

## Parámetros

- `OrderVolume` = 0.1 — volumen de trading en lotes para cada nueva posición.
- `StopLossPips` = 50 — distancia desde el precio de entrada al nivel de stop-loss en pips. Establecer en 0 para deshabilitar el stop.
- `TakeProfitPips` = 50 — distancia desde el precio de entrada al nivel de take-profit en pips. Establecer en 0 para deshabilitar el objetivo.
- `TrailingStopPips` = 5 — tamaño del trailing stop en pips. Establecer en 0 para desactivar el trailing.
- `TrailingStepPips` = 5 — mejora mínima de precio (en pips) requerida antes de que el trailing stop se mueva de nuevo.
- `GapPips` = 1 — brecha de apertura mínima, expresada en pips, necesaria para generar una señal de entrada.
- `CandleType` = marco temporal de 1 hora — velas usadas para la detección de brechas y gestión de posiciones.

## Notas de implementación

- Las entradas basadas en pips se convierten a distancias de precio absolutas usando el tamaño de tick del instrumento. Las
  cotizaciones forex de cinco y tres dígitos se ajustan automáticamente para trabajar con valores de pip verdaderos.
- La lógica de trailing stop requiere que `TrailingStepPips` sea positivo cuando `TrailingStopPips` está habilitado; de lo contrario la estrategia lanza
  una excepción al inicio, reflejando la validación MQL original.
- La estrategia evalúa los controles de riesgo solo en velas terminadas de acuerdo con las directrices de la API de alto nivel de StockSharp.
- La gestión manual de stop y objetivo se basa en órdenes de mercado, por lo que no hay órdenes de protección separadas en el libro.
- La configuración por defecto asume instrumentos forex; ajuste las distancias en pips al operar activos con diferente volatilidad o tamaños de tick.
