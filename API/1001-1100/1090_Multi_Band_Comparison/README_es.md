# Comparación Multi-Banda
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Comparación Multi-Banda utiliza SMA, desviación estándar y bandas de cuantiles de precio. La estrategia va largo cuando el precio cierra por encima del cuantil superior menos la desviación estándar durante un número definido de barras y sale cuando el precio cae por debajo de ese nivel durante un número determinado de barras.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - **Largo**: Cierre por encima de (cuantil superior - desviación estándar) durante `EntryConfirmBars` barras.
- **Criterios de salida**: Cierre por debajo de esa línea durante `ExitConfirmBars` barras.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Length` = 20
  - `BollingerMultiplier` = 1
  - `UpperQuantile` = 0.95
  - `EntryConfirmBars` = 1
  - `ExitConfirmBars` = 1
- **Filtros**:
  - Categoría: Estadística
  - Dirección: Largo
  - Indicadores: SMA, Standard Deviation
  - Complejidad: Moderado
  - Nivel de riesgo: Medio
