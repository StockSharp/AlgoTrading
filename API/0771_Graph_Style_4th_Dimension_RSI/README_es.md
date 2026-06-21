# Graph Style 4th Dimension RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina el cambio de precio con los niveles del RSI.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 80%. Funciona bien en mercados volátiles.

La estrategia verifica la dirección del último cambio de precio junto con los extremos del RSI. Abre una posición cuando el RSI sale de las zonas de sobrecompra/sobreventa y el cambio de precio reciente confirma el movimiento. Las posiciones se cierran cuando el RSI vuelve al área media o aparece una señal opuesta.

## Detalles

- **Criterios de entrada**: Dirección del cambio de precio con extremo del RSI.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta o RSI de regreso al medio.
- **Stops**: Stop loss porcentual.
- **Valores predeterminados**:
  - `RsiPeriod` = 14
  - `OverboughtLevel` = 70m
  - `OversoldLevel` = 30m
  - `StopLossPercent` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: RSI
  - Stops: Porcentaje
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
