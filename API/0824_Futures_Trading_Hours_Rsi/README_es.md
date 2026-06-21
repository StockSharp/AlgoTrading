# Estrategia de Futuros en Horario de Trading con RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera únicamente durante las horas de sesión de futuros estadounidenses (08:30–15:00 CT). Utiliza el Índice de Fuerza Relativa (RSI) para entrar largo cuando el oscilador cruza hacia arriba del nivel de sobreventa y entrar corto cuando cruza hacia abajo del nivel de sobrecompra. A las 15:00 CT o después, todas las posiciones abiertas se cierran.

## Detalles

- **Criterios de entrada**:
  - **Largo**: RSI cruza hacia arriba del nivel de sobreventa durante la sesión
  - **Corto**: RSI cruza hacia abajo del nivel de sobrecompra durante la sesión
- **Largo/Corto**: Ambos lados
- **Criterios de salida**:
  - Todas las posiciones se cierran al final de la sesión (15:00 CT)
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `RsiLength` = 14
  - `OverSoldLevel` = 30
  - `OverBoughtLevel` = 70
  - `SessionStart` = 08:30
  - `SessionEnd` = 15:00
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: RSI
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
