# Estrategia de Three Neural Networks
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es un port de alto nivel de StockSharp del asesor experto de MetaTrader "Three neural networks". Funciona completamente a través de la API de suscripción de velas de StockSharp y reutiliza indicadores `SmoothedMovingAverage` integrados para emular las tres capas neuronales de la implementación original. La estrategia opera en tres marcos temporales diferentes (H1, H4, D1) y analiza la pendiente de cada promedio suavizado para derivar una decisión de negociación colectiva.

## Flujo de trabajo

1. Cuando la estrategia comienza, se suscribe a velas de los marcos temporales H1, H4 y D1 y vincula medias móviles suavizadas que usan el precio mediano, reflejando las llamadas `iMA(..., MODE_SMMA, PRICE_MEDIAN)` de MetaTrader.
2. Cada marco temporal mantiene un historial continuo que respeta el desplazamiento configurado. Una vez que hay cuatro valores desplazados disponibles, el algoritmo calcula tres salidas neuronales usando exactamente la misma fórmula de diferencia ponderada que el EA y redondea el resultado a cuatro decimales.
3. Después de que la vela H1 termina, la estrategia combina las salidas neuronales:
   - Si los tres valores son positivos → abrir o mantener una posición larga.
   - Si la salida H1 es positiva mientras las salidas H4 y D1 son negativas → abrir o mantener una posición corta.
4. Las posiciones se dimensionan con un lote fijo o un modelo de porcentaje de riesgo. En modo de riesgo la estrategia asigna `VolumeOrRisk` por ciento del valor del portafolio y lo convierte en volumen dividiendo por el precio actual.
5. La lógica protectora replica los controles del EA: un stop-loss y take-profit se colocan en variables locales inmediatamente después de que el director de la posición cambia, y un trailing stop se ajusta cada vez que la barra H1 cierra si el precio avanza más allá de la distancia de trailing más el paso configurado.
6. Cada vela H1 terminada primero verifica si los niveles actuales de stop-loss o take-profit son superados y cierra la posición con una orden de mercado si es necesario. El registro detallado opcional reproduce el indicador `InpPrintLog` original.

## Parámetros

| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `StopLossPips` | `50` | Distancia de stop protector en pips. Establecer en `0` para desactivar el stop-loss. |
| `TakeProfitPips` | `50` | Distancia de take-profit en pips. Establecer en `0` para desactivar el objetivo. |
| `TrailingStopPips` | `15` | Distancia entre el precio actual y el trailing stop. |
| `TrailingStepPips` | `5` | Mejora mínima requerida antes de mover el trailing stop nuevamente. |
| `ManagementMode` | `RiskPercent` | Modo de dimensionamiento de volumen. `FixedLot` usa el valor como tamaño de lote directo; `RiskPercent` lo usa como porcentaje del capital del portafolio. |
| `VolumeOrRisk` | `1` | Tamaño de lote o porcentaje de riesgo, dependiendo del modo de gestión monetaria. |
| `H1Period`, `H1Shift` | `2`, `5` | Período y desplazamiento de la media móvil suavizada H1. |
| `H4Period`, `H4Shift` | `2`, `5` | Período y desplazamiento de la media móvil suavizada H4. |
| `D1Period`, `D1Shift` | `2`, `5` | Período y desplazamiento de la media móvil suavizada D1. |
| `P1`, `P2`, `P3` | `0.1` | Pesos aplicados a los tres componentes neuronales H1. |
| `Q1`, `Q2`, `Q3` | `0.1` | Pesos aplicados a los tres componentes neuronales H4. |
| `K1`, `K2`, `K3` | `0.1` | Pesos aplicados a los tres componentes neuronales D1. |
| `EnableDetailedLog` | `false` | Activa mensajes de diagnóstico detallados que reflejan la salida del registro del EA. |

## Gestión de riesgo

- Los niveles de stop-loss y take-profit se traducen de distancias en pips usando el tamaño de pip detectado (con ajuste automático de 3/5 dígitos idéntico al código original) y se aplican inmediatamente después de que cambia la dirección de la posición.
- La lógica de trailing sigue las condiciones de MetaTrader: se activa una vez que el precio se mueve más de `TrailingStopPips + TrailingStepPips` desde la entrada y solo avanza si la mejora supera el paso configurado.
- Todas las salidas se ejecutan con órdenes de mercado `ClosePosition()` porque las órdenes stop/límite del lado del servidor no están disponibles en la API de alto nivel.

## Notas

- La validación de nivel de congelamiento/stop del EA no está disponible en StockSharp, por lo que la estrategia solo se basa en la conversión de tamaño de pip y la normalización de volumen a través de `VolumeStep`, `VolumeMin` y `VolumeMax`.
- El dimensionamiento basado en riesgo usa el valor actual del portafolio y el precio de entrada para aproximar la verificación de margen de MetaTrader. Esto refleja el comportamiento general sin depender de calculadoras de margen específicas del broker.
- El registro opcional puede habilitarse a través de `EnableDetailedLog` para diagnósticos paso a paso similares a `InpPrintLog` en MetaTrader.
