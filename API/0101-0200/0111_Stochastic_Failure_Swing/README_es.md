# Estrategia de Oscilación de Fallo del Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La Oscilación de Fallo del Stochastic monitorea el oscilador en busca de un máximo más bajo por encima de 80 o un mínimo más alto por debajo de 20.
Cuando el indicador falla en alcanzar un nuevo extremo y luego revierte, a menudo señala un cambio de tendencia.

Las pruebas indican un rendimiento anual promedio de aproximadamente 70%. Funciona mejor en el mercado de acciones.

La estrategia compra cuando un mínimo más alto se forma por debajo de 20 y %K cruza de vuelta por encima de %D, o vende cuando un máximo más bajo ocurre por encima de 80 y %K cruza por debajo.

Las operaciones emplean un pequeño stop porcentual y se cierran cuando el stochastic cruza de vuelta a través del nivel del swing anterior.

## Detalles

- **Criterios de entrada**: señal de indicador
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basados en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: Stochastic
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

