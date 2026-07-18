# Estrategia de Reversión Gaussian Detrended
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Gaussian Detrended Reversion es una estrategia de reversión a la media que utiliza un oscilador de precio detendenciado suavizado con una Media Móvil Arnaud Legoux (ALMA). Las posiciones largas se abren cuando el oscilador suavizado cruza por encima de su versión retardada mientras está por debajo de cero; los cortos se abren en cruces descendentes por encima de cero. Las posiciones se cierran en cruces opuestos o cuando el oscilador cruza la línea cero.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - **Largo**: El DPO suavizado por ALMA cruza por encima de su línea de retardo y está por debajo de cero.
  - **Corto**: El DPO suavizado por ALMA cruza por debajo de su línea de retardo y está por encima de cero.
- **Criterios de salida**: Cruce de retardo opuesto o cruce de la línea cero.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `PriceLength` = 52
  - `SmoothingLength` = 52
  - `LagLength` = 26
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Largo/Corto
  - Indicadores: EMA, ALMA
  - Complejidad: Bajo
  - Nivel de riesgo: Medio
