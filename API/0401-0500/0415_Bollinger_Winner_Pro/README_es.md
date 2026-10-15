# Estrategia Bollinger Winner Pro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Bollinger Winner Pro amplía la versión Lite añadiendo filtros modulares y controles
de riesgo. Sigue buscando precios que cierren fuera de las Bollinger Bands, pero
las operaciones se ejecutan solo cuando las confirmaciones opcionales están de acuerdo.

Los operadores pueden habilitar filtros de RSI, Aroon y media móvil para confirmar el
impulso y la dirección de la tendencia. También se puede activar un stop-loss integrado
para limitar el riesgo. Esta flexibilidad permite que la estrategia se adapte a
diferentes mercados o necesidades de prueba.

El enfoque apunta a la reversión a la media: una vez que el precio vuelve a entrar en
las bandas o toca el lado opuesto, la posición se cierra o se alcanza el stop. Dado
que se pueden acumular múltiples filtros, las señales son menos frecuentes pero de
mayor calidad.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**: La vela cierra fuera de una banda y todos los filtros habilitados coinciden.
- **Criterios de salida**: Retorno a la banda central/opuesta o stop-loss si `UseSL` es verdadero.
- **Stops**: Stop-loss opcional controlado por `UseSL`.
- **Valores predeterminados**:
  - `UseRSI` = True
  - `UseAroon` = False
  - `UseMA` = True
  - `UseSL` = True
- **Filtros**:
  - Categoría: Reversión a la media con confirmaciones
  - Dirección: Largo/Corto
  - Indicadores: Bollinger Bands, RSI, Aroon, Moving Average
  - Complejidad: Avanzado
  - Nivel de riesgo: Medio
