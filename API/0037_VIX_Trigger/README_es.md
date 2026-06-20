# VIX Trigger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
VIX Trigger reacciona a los cambios en el Índice de Volatilidad. Un VIX en aumento señala miedo y posibles reversiones en el instrumento subyacente. La estrategia compara la dirección del VIX con el precio relativo a una media móvil.

Las pruebas indican un rendimiento anual promedio de aproximadamente 148%. Funciona mejor en el mercado forex.

Cuando el VIX aumenta y el precio está por debajo de la media móvil, compra esperando una recuperación. Por el contrario, el VIX en alza con el precio por encima de la media invita a una posición corta.

Las posiciones se cierran cuando el VIX cae o se alcanza el porcentaje de stop-loss.

## Detalles

- **Criterios de entrada**: VIX en alza mientras que el precio relativo a la MA activa largos o cortos.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: VIX cae o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `MAPeriod` = 20
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Contrarian
  - Dirección: Ambos
  - Indicadores: VIX, MA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

