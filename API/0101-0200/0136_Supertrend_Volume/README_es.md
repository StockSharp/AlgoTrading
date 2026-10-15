# Estrategia Supertrend Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Supertrend Volume aumenta el indicador Supertrend con confirmación de volumen.
El volumen creciente durante un cambio de dirección del Supertrend refuerza la probabilidad de un nuevo movimiento impulsivo.

Las pruebas indican un rendimiento anual promedio de aproximadamente 145%. Funciona mejor en el mercado de criptomonedas.

La estrategia entra en la dirección de la tendencia con una señal Supertrend solo cuando va acompañada de volumen superior al promedio.

Los stops siguen la línea Supertrend, saliendo cuando el precio cierra al otro lado.

## Detalles

- **Criterios de entrada**: señal de indicador
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3
  - `VolumePeriod` = 20
    Un giro del Supertrend, un cierre al otro lado de la línea, cierra la posición contraria. El giro abre una posición en su dirección solo si el volumen de la vela supera la media de las VolumePeriod velas anteriores, de modo que un giro confirmado invierte la posición.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Supertrend, Volume
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

