# Estrategia CCI Failure Swing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
El CCI Failure Swing se basa en el Commodity Channel Index formando un máximo más bajo por encima de +100 o un mínimo más alto por debajo de -100.
Esta incapacidad para establecer un nuevo extremo suele señalar el fin de la tendencia anterior.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 73%. Funciona mejor en el mercado de criptomonedas.

La estrategia entra en largo cuando el CCI se mantiene por encima de -100 y gira al alza, o en corto cuando falla cerca de +100 y gira a la baja.

Un stop porcentual mantiene el riesgo pequeño y las operaciones salen si el CCI cruza de nuevo el nivel del swing anterior.

## Detalles

- **Criterios de entrada**: señal del indicador
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: CCI
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

