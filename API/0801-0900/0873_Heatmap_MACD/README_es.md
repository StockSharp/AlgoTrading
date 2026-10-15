# Estrategia Heatmap MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Heatmap MACD opera cuando los histogramas MACD de cinco marcos temporales se alinean. Se abre una posición larga cuando todos los histogramas se vuelven positivos, y una posición corta cuando todos se vuelven negativos. Opcionalmente, la posición puede cerrarse cuando cualquier histograma gira en contra de la operación.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - **Largo**: Histograma MACD > 0 en los cinco marcos temporales y previamente no todos positivos.
  - **Corto**: Histograma MACD < 0 en los cinco marcos temporales y previamente no todos negativos.
- **Criterios de salida**: Señal opuesta o cierre opcional en sentido contrario.
- **Stops**: Ninguno por defecto.
- **Valores predeterminados**:
  - `FastLength` = 9
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `TimeFrame1` = tf(60)
  - `TimeFrame2` = tf(120)
  - `TimeFrame3` = tf(240)
  - `TimeFrame4` = tf(240)
  - `TimeFrame5` = tf(480)
  - `CloseOnOpposite` = false
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Largo y Corto
  - Indicadores: MACD
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Multi-marco temporal
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
