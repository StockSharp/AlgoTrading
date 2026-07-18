# Estrategia Grover Llorens Activator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de seguimiento adaptativa basada en ATR que cambia de dirección cuando el precio cruza la línea activadora interna.

Compra cuando la diferencia entre el precio y la línea de seguimiento cruza por encima de cero. Vende cuando cruza por debajo de cero.

## Detalles

- **Criterios de entrada**: El precio cruza la línea de stop calculada a partir del ATR.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `Length` = 480
  - `Multiplier` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: ATR
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
