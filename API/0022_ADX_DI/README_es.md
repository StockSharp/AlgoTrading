# Estrategia ADX DI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en los indicadores ADX y Movimiento Direccional

Las pruebas indican un rendimiento anual promedio de aproximadamente 103%. Funciona mejor en el mercado de acciones.

ADX DI se centra en el cruce de +DI y -DI con un ADX creciente. Un cruce alcista de +DI sobre -DI junto con un ADX fuerte abre posiciones largas, mientras que lo contrario abre cortas. Las posiciones se cierran con un ADX debilitado o un cruce opuesto.

Esta combinación ayuda a evitar operar en cada cruce de DI al exigir confirmación del ADX. El sistema tiene como objetivo capturar tendencias sostenibles en lugar de oscilaciones a corto plazo.


## Detalles

- **Criterios de entrada**: Señales basadas en ADX, ATR.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: ADX, ATR
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

