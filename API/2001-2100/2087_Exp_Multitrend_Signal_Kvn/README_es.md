# Estrategia Exp Multitrend Signal KVN
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa el concepto MultiTrend Signal KVN. Construye un canal de precios adaptativo utilizando el Average Directional Index (ADX) para determinar la ventana de retroceso. Cuando el precio cierra por encima del canal, la estrategia abre una posición larga. Cuando el precio cierra por debajo del canal, abre una posición corta.

El ancho del canal está definido por el parámetro **K** como porcentaje del rango entre máximos y mínimos recientes. **KPeriod** establece el número base de barras utilizadas para los cálculos, mientras que el valor ADX escala la ventana real. **KStop** multiplica el rango promedio y se añade a las operaciones de ruptura para determinar la distancia del stop.

La estrategia está diseñada para operar tanto en largo como en corto y utiliza el marco temporal de 4 horas por defecto. No se proporcionan stop loss ni take profit explícitos; la protección puede habilitarse a través de la plataforma.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El precio de cierre rompe por encima de la banda adaptativa superior.
  - **Corto**: El precio de cierre rompe por debajo de la banda adaptativa inferior.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Señal inversa en la dirección opuesta.
- **Stops**: Opcional mediante la protección de la estrategia.
- **Valores predeterminados**:
  - `K` = 48
  - `KStop` = 0.5
  - `KPeriod` = 150
  - `AdxPeriod` = 14
  - `Tipo de vela` = velas de 4 horas
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: ADX, SMA, Max/Min
  - Stops: Opcional
  - Complejidad: Moderado
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Moderado
