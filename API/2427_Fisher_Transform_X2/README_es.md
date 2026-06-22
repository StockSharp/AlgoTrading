# Estrategia Fisher Transform X2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza el indicador Fisher Transform en dos marcos temporales diferentes. El marco temporal superior define la tendencia general, mientras que el inferior genera entradas cuando Fisher cruza su valor anterior contra esa tendencia. Los parámetros opcionales permiten cerrar posiciones en cambio de tendencia o en señales de cruce.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `Fisher de tendencia subiendo` && `Fisher de señal cruza por debajo de su valor anterior`
  - **Corto**: `Fisher de tendencia bajando` && `Fisher de señal cruza por encima de su valor anterior`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Cierre opcional en reversión de tendencia
  - Cierre opcional en cruce opuesto de Fisher en el marco temporal de señal
- **Stops**: Take profit y stop loss en puntos
- **Valores predeterminados**:
  - `Trend Length` = 10
  - `Signal Length` = 10
  - `Trend Timeframe` = 6 horas
  - `Signal Timeframe` = 30 minutos
  - `Take Profit` = 2000 puntos
  - `Stop Loss` = 1000 puntos
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Fisher Transform
  - Stops: Sí
  - Complejidad: Medio
  - Marco temporal: Multi-timeframe
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
