# Estrategia de Cruce de Stochastic RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este método convierte el clásico Relative Strength Index en un Stochastic RSI, luego suaviza el resultado en líneas %K y %D. Cuando %K cruza %D dentro de zonas cuidadosamente elegidas, el movimiento implica un cambio de corto plazo en el momentum. El algoritmo solo opera cuando una estructura EMA de tres capas confirma la dirección de la tendencia más amplia, ayudando a filtrar las falsas señales.

Una vez que aparece un cruce, el precio de cierre también debe situarse por encima o por debajo de la EMA rápida dependiendo de la señal. Esto protege contra actuar en oscilaciones que ocurren contra la tendencia prevaleciente y mantiene la atención en los momentos cuando el momentum se alinea con la dirección. Los traders pueden ajustar los períodos de suavizado y la longitud del RSI para afinar cómo reacciona el sistema a los picos de volatilidad.

El riesgo se referencia a través de una lectura de Average True Range. Los multiplicadores del ATR actual proponen stops de pérdida y objetivos de ganancia, proporcionando un nivel dinámico que se expande en mercados volátiles y se contrae cuando la actividad se calma. Aunque el script no envía automáticamente órdenes protectoras, estos niveles calculados ayudan a la gestión manual o pueden vincularse a módulos de riesgo adicionales.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `%K` cruza por encima de `%D`, `%K` en `[10,60]`, EMAs alineadas alcistamente, precio por encima de EMA1.
  - **Corto**: `%K` cruza por debajo de `%D`, `%K` en `[40,95]`, EMAs alineadas bajistamente, precio por debajo de EMA1.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**: Ninguno integrado.
- **Stops**: Múltiplos de ATR sugeridos pero no colocados automáticamente.
- **Valores predeterminados**:
  - `SmoothK` = 3, `SmoothD` = 3.
  - `RsiLength` = 14, `StochLength` = 14.
  - `Ema1Length` = 20, `Ema2Length` = 50, `Ema3Length` = 100.
  - `AtrLength` = 14, `AtrLossMultiplier` = 1.5, `AtrProfitMultiplier` = 2.0.
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: Múltiples
  - Stops: Opcional
  - Complejidad: Moderado
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio
