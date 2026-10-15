# Estrategia de Fuerza Interna de Barra IBS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

IBS Internal Bar Strength es una estrategia de reversión a la media que utiliza el cierre de la barra anterior dentro de su rango para detectar condiciones de sobreventa o sobrecompra. Un filtro EMA opcional alinea las operaciones con la tendencia y las entradas solo se permiten cuando el precio se mueve un porcentaje mínimo desde la última entrada. Las posiciones se cierran cuando el IBS cruza el umbral opuesto o se alcanza el tiempo máximo de mantenimiento.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - **Largo**: IBS por debajo del umbral de entrada, condición EMA cumplida y dirección permitida.
  - **Corto**: IBS por encima del umbral de salida, condición EMA cumplida y dirección permitida.
- **Criterios de salida**: IBS cruzando el umbral opuesto o límite de duración de la operación.
- **Stops**: Salida basada en tiempo.
- **Valores predeterminados**:
  - `IbsEntryThreshold` = 0.09
  - `IbsExitThreshold` = 0.985
  - `EmaPeriod` = 220
  - `MinEntryPct` = 0
  - `MaxTradeDuration` = 14
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Largo & Corto
  - Indicadores: IBS, EMA
  - Complejidad: Bajo
  - Nivel de riesgo: Medio
