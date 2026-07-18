# Estrategia Fractured Fractals
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Port del clásico asesor experto MetaTrader "Fractured Fractals". La estrategia rastrea fractales de Williams confirmados, coloca órdenes stop en nuevos niveles de ruptura y sigue un stop de protección en el fractal opuesto.

## Detalles

- **Fuente**: Convertido de `MQL/20127/Fractured Fractals.mq5`.
- **Régimen de mercado**: Continuación de ruptura en cualquier instrumento compatible con StockSharp.
- **Tipos de órdenes**: Usa órdenes stop para entradas y órdenes stop de protección para salidas.
- **Dimensionamiento de posición**: Basado en riesgo, controlado por `MaximumRiskPercent` y la lógica de racha adaptativa `DecreaseFactor`.
- **Parámetros predeterminados**:
  - `MaximumRiskPercent` = 2%
  - `DecreaseFactor` = 10
  - `ExpirationHours` = 1 hora
  - `CandleType` = Marco temporal de 1 hora
- **Indicadores principales**: Fractales nativos de Williams de cinco barras calculados al vuelo.
- **Tipo de estrategia**: Ruptura larga/corta con gestión dinámica de stops.

## Lógica de la estrategia

### Seguimiento de la secuencia de fractales

- Mantiene colas de los últimos cinco máximos y mínimos de velas para simular el búfer `iFractals` en MT5.
- Cada fractal confirmado desplaza tres ranuras rodantes: más joven, media y antigua. Los valores duplicados se ignoran usando el paso de precio del instrumento como tolerancia.
- Las señales largas requieren que el fractal alto más reciente supere el fractal medio; las señales cortas requieren que el fractal bajo más reciente sea inferior al anterior.

### Órdenes de entrada y vencimiento

- Cuando no existe posición larga ni orden de compra stop pendiente, la estrategia coloca un buy stop en el fractal alto más reciente con un stop loss en el fractal bajo más reciente.
- Simétricamente, las entradas cortas colocan un sell stop en el fractal bajo más reciente con un stop de protección en el fractal alto más reciente.
- Las órdenes pendientes heredan una expiración definida por `ExpirationHours`. Si el tiempo de la vela supera la expiración o la jerarquía de fractales invalida la configuración (nuevo fractal alto más bajo para largos o fractal bajo más alto para cortos), la orden se cancela.
- El bot mantiene el libro limpio cancelando órdenes opuestas una vez que se abre una posición.

### Stops de protección con trailing

- Cada fractal opuesto confirmado actualiza la orden de stop de protección: las posiciones largas siguen el fractal bajo más reciente, las posiciones cortas siguen el fractal alto más reciente.
- Los stops solo se ajustan — los nuevos niveles deben mejorar sobre el precio de la orden existente antes de que se produzca un reemplazo.
- Cuando la posición se cierra, cualquier orden de stop restante se cancela inmediatamente.

### Gestión de riesgo y control de racha

- `CalculateOrderVolume` replica el cálculo de riesgo de MT5: riesgo por unidad = precio de entrada menos precio de stop (o viceversa para cortos).
- El riesgo monetario objetivo equivale a `Portfolio.CurrentValue * MaximumRiskPercent / 100` con un respaldo a la propiedad `Volume` cuando la valoración del portafolio no está disponible.
- El volumen resultante se normaliza por tamaño de lote, paso de volumen, volumen mínimo y restricciones de volumen máximo expuestas por `Security`.
- Después de una operación perdedora el contador de racha se incrementa; las operaciones rentables o planas reinician el contador. Si ocurre más de una pérdida consecutiva, el tamaño se escala hacia abajo en `losses / DecreaseFactor`.

### Seguimiento del resultado de las operaciones

- `OnOwnTradeReceived` agrega ejecuciones para determinar cuándo se completa un ciclo de posición y si terminó positivo, negativo o plano.
- El contador de racha y la última marca de tiempo rentable reflejan la lógica original, permitiendo extensiones adicionales (p. ej., análisis) si se desea.

## Notas de uso

1. Adjunte la estrategia a cualquier par instrumento/portafolio, ajuste `CandleType` a la resolución deseada y configure los parámetros de riesgo según el tamaño de la cuenta.
2. Asegúrese de que el adaptador/broker admita órdenes stop; de lo contrario, reemplace las órdenes de protección con salidas manuales en `UpdateTrailingStops`.
3. Dado que la implementación procesa solo velas completadas, los picos intra-barra más pequeños que la resolución de la vela no desencadenarán órdenes exactamente como en las pruebas de MT5 basadas en ticks.
4. Considere habilitar el registro para revisar los mensajes de comentarios producidos por el port en C#, reflejando la retroalimentación del experto original.
