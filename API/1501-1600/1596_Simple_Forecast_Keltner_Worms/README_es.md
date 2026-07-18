# Estrategia de Pronóstico Simple - Keltner Worms
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia construye un canal Keltner dinámico y opera cuando el precio se mueve fuera de la banda.

## Detalles

- **Criterios de entrada**:
  - El precio de cierre por encima del canal superior abre un largo.
  - El precio de cierre por debajo del canal inferior abre un corto.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - La señal opuesta cierra la posición.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Length` = 10
- **Filtros**:
  - Categoría: Canal
  - Dirección: Ambos
  - Indicadores: EMA, ATR
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
