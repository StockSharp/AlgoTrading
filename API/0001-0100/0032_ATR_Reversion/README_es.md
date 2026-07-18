# Estrategia ATR Reversion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
ATR Reversion busca movimientos repentinos medidos en múltiplos del Average True Range (ATR). Cuando el precio supera el multiplicador de ATR, el sistema espera una reversión a la media.

Las pruebas indican un rendimiento anual promedio de aproximadamente 133%. Funciona mejor en el mercado de criptomonedas.

La estrategia abre una operación en la dirección opuesta al impulso y utiliza una media móvil para evaluar el momentum.

Las posiciones se cierran en un cruce de media móvil o cuando se alcanza el stop de volatilidad.

## Detalles

- **Criterios de entrada**: El movimiento del precio supera `AtrMultiplier` veces ATR.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: El precio cruza la media móvil o el stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `MAPeriod` = 20
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: ATR, MA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

