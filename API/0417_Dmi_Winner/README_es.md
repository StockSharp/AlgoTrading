# Estrategia DMI Winner
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

DMI Winner es una estrategia de seguimiento de tendencia basada en el Índice de
Movimiento Direccional (DMI). Abre operaciones cuando las líneas `+DI` y `-DI` se
cruzan y el Índice de Dirección Promedio (ADX) sube por encima de un umbral clave,
señalando una tendencia fuerte.

Un filtro de media móvil opcional mantiene las operaciones en la dirección de la
tendencia más amplia. También se puede habilitar un stop-loss para limitar el riesgo
a la baja, aunque por defecto el sistema se basa en las reversiones de señal para
las salidas.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - **Largo**: `+DI` cruza por encima de `-DI` Y `ADX` > `KeyLevel` (con filtro de MA opcional).
  - **Corto**: `-DI` cruza por encima de `+DI` Y `ADX` > `KeyLevel` (con filtro de MA opcional).
- **Criterios de salida**: Cruce de DI opuesto o stop-loss si está habilitado.
- **Stops**: Stop-loss opcional (`UseSL`).
- **Valores predeterminados**:
  - `DILength` = 14
  - `KeyLevel` = 23
  - `UseMA` = True
  - `UseSL` = False
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo/Corto
  - Indicadores: DMI, Moving Average
  - Complejidad: Moderado
  - Nivel de riesgo: Medio
