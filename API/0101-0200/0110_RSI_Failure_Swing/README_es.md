# Estrategia de Oscilación de Fallo del RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La Oscilación de Fallo del RSI es una técnica clásica de reversión donde el RSI forma un mínimo más alto en territorio de sobreventa o un máximo más bajo en territorio de sobrecompra.
Este fracaso en alcanzar un nuevo extremo a menudo precede a un cambio de tendencia.

Las pruebas indican un rendimiento anual promedio de aproximadamente 67%. Funciona mejor en el mercado de acciones.

La estrategia compra cuando el RSI se mantiene por encima de su mínimo anterior y luego cruza por encima de 30, o vende cuando falla en superar un máximo anterior y cruza por debajo de 70.

Un stop porcentual limita la pérdida, y las posiciones se cierran cuando el RSI cruza el nivel opuesto.

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
  - Indicadores: RSI
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

