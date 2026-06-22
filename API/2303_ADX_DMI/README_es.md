# Estrategia ADX DMI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Utiliza el Índice de Movimiento Direccional (DMI) para operar los cruces entre las líneas +DI y -DI. Cuando -DI sube por encima de +DI y luego cae por debajo de él, la estrategia abre una posición larga. Cuando +DI sube por encima de -DI y luego cae por debajo, abre una posición corta. Las señales opuestas pueden opcionalmente cerrar posiciones existentes.

## Detalles

- **Criterios de entrada**:
  - **Largo**: -DI estaba por encima de +DI en la barra anterior y cruza por debajo en la barra más reciente.
  - **Corto**: +DI estaba por encima de -DI en la barra anterior y cruza por debajo en la barra más reciente.
- **Criterios de salida**:
  - Cruce inverso si la opción de cierre correspondiente está habilitada.
- **Indicadores**:
  - Directional Index (período 14 por defecto)
- **Stops**: ninguno por defecto.
- **Valores predeterminados**:
  - `DmiPeriod` = 14
  - `AllowLong` = true
  - `AllowShort` = true
  - `CloseLong` = true
  - `CloseShort` = true
- **Filtros**:
  - Funciona en cualquier marco temporal
  - Indicadores: DMI
  - Stops: opcional mediante gestión de riesgos externa
  - Complejidad: básico
