# Estrategia RSI Adaptativo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia RSI Adaptativo deriva un coeficiente de suavizado del Índice de Fuerza Relativa. Cuando el RSI se desvía del nivel neutro de 50, el coeficiente aumenta, haciendo que el RSI adaptativo siga el precio más de cerca. Cerca de 50, el coeficiente se reduce y la curva se suaviza. Se abre una posición larga cuando el RSI adaptativo sube, mientras que se abre una posición corta cuando baja.

## Detalles

- **Criterios de entrada**:
  - El RSI adaptativo cruza por encima de su valor anterior.
  - El RSI adaptativo cruza por debajo de su valor anterior.
- **Largo/Corto**: Operaciones largas y cortas.
- **Criterios de salida**:
  - Señal opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Length` = 14
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: RSI
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
