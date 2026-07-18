# Estrategia MACD en Modo Tendencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que opera utilizando el indicador MACD con tres modos de detección de tendencia seleccionables: pendiente del histograma, cruce de nube o cruce de línea cero.

## Detalles

- **Criterios de entrada**:
  - *Histograma*: el histograma estaba cayendo y luego gira hacia arriba para largos; subiendo y luego gira hacia abajo para cortos.
  - *Nube*: la línea MACD estaba previamente por encima de la línea de señal y cruza por debajo para abrir largo; el cruce opuesto abre corto.
  - *Cero*: el histograma cruza la línea cero en dirección opuesta.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Las condiciones opuestas cierran posiciones.
- **Stops**: No.
- **Valores predeterminados**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `TrendMode` = TrendMode.Cloud
  - `Volume` = 1m
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: MACD
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: 4h
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí (histograma)
  - Nivel de riesgo: Medio
