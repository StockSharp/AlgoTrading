# Plantilla de Estrategia ProfitView
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una estrategia básica de cruce de medias móviles con tipos de suavizado configurables, derivada de la plantilla ProfitView.

## Detalles

- **Criterios de entrada**:
  - **Largo**: MA1 cruza por encima de MA2.
  - **Corto**: MA1 cruza por debajo de MA2.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**: Cruce opuesto.
- **Stops**: No.
- **Valores predeterminados**:
  - `MA1 Type` = SMA, `MA1 Length` = 10
  - `MA2 Type` = SMA, `MA2 Length` = 100
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Medias móviles
  - Stops: No
  - Complejidad: Básico
