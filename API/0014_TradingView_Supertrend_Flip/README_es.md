# TradingView Supertrend Flip
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia basada en cambios del indicador Supertrend con confirmación de volumen

Las pruebas indican un retorno anual promedio de aproximadamente 79%. Funciona mejor en el mercado de acciones.

TradingView Supertrend Flip emula los cambios de color del popular indicador. Un cambio de rojo a verde señala una entrada larga y de verde a rojo señala una entrada corta. La estrategia sale en el siguiente cambio.

La confirmación de volumen se puede utilizar para evitar falsas señales durante períodos de negociación escasa. Al actuar solo en cambios con volumen de respaldo, el método apunta a capturar reversiones más confiables.


## Detalles

- **Criterios de entrada**: Señales basadas en ATR, Supertrend.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `VolumeAvgPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: ATR, Supertrend
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Neural Networks: No
  - Divergencia: No
  - Nivel de riesgo: Medio

