# Estrategia Uptrick Intensity Index
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia calcula el Índice de Intensidad de Tendencia a partir de tres medias móviles y opera en los cruces del TII con su propia media móvil.

## Detalles

- **Criterios de entrada**: TII cruza por encima de su SMA (compra) o por debajo (venta)
- **Largo/Corto**: Ambos
- **Criterios de salida**: señal opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `Ma1Length` = 10
  - `Ma2Length` = 20
  - `Ma3Length` = 50
  - `TiiMaLength` = 50
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: SMA, TII
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
