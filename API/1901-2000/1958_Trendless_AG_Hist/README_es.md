# Estrategia Trendless AG Histogram
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia opera reversiones detectadas por el indicador **Trendless AG Histogram**. El indicador mide la distancia entre el precio y una media móvil suavizada y luego vuelve a suavizar el resultado, formando un histograma alrededor de cero. Los mínimos locales indican posibles reversiones alcistas mientras que los máximos locales sugieren reversiones bajistas.

Las posiciones se abren cuando el histograma cambia de dirección. Si el indicador sube después de estar por debajo de valores anteriores, se abre una posición larga. Si cae después de estar por encima de valores anteriores, se abre una posición corta. Los niveles opcionales de stop-loss y take-profit gestionan el riesgo.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El valor del histograma está subiendo mientras el valor anterior era menor que su predecesor.
  - **Corto**: El valor del histograma está bajando mientras el valor anterior era mayor que su predecesor.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**:
  - Señal opuesta o niveles de stop-loss/take-profit.
- **Stops**: Stop-loss y take-profit fijos en unidades de precio.
- **Valores predeterminados**:
  - `Fast Length` = 7.
  - `Slow Length` = 5.
  - `Stop Loss` = 1000.
  - `Take Profit` = 2000.
  - `Candle Type` = velas de 12 horas.
- **Filtros**:
  - Categoría: Seguimiento de tendencia.
  - Dirección: Ambos.
  - Indicadores: Indicador personalizado basado en medias móviles.
  - Stops: Sí.
  - Complejidad: Moderado.
  - Marco temporal: Medio plazo.
  - Estacionalidad: No.
  - Redes neuronales: No.
  - Divergencia: Sí.
  - Nivel de riesgo: Medio.
