# Oscilador de Señal Personalizada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que utiliza la diferencia entre dos señales de precio. Entra largo cuando el oscilador cruza por encima de cero y corto cuando cruza por debajo de cero. Cuando el modo solo largo está activado, los cruces negativos cierran la posición.

## Detalles

- **Criterios de entrada**: El oscilador cruza el cero.
- **Largo/Corto**: Ambas direcciones o solo largo.
- **Criterios de salida**: Señal opuesta o cruce de cero en modo solo largo.
- **Stops**: No.
- **Valores predeterminados**:
  - `LongOnly` = false
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
