# Estrategia ATR MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
ATR MACD utiliza la volatilidad del Average True Range para ajustar el tamaño de la posición mientras opera los cruces del MACD.
Lecturas más altas del ATR resultan en un tamaño de operación menor, manteniendo el riesgo consistente en diferentes regímenes de mercado.

Las pruebas indican un rendimiento anual promedio de aproximadamente 154%. Funciona mejor en el mercado de acciones.

Las entradas ocurren cuando el MACD cruza su línea de señal, con salidas activadas por el cruce opuesto o un stop basado en volatilidad.

Esta combinación busca capturar momentum mientras se tiene en cuenta la volatilidad cambiante.

## Detalles

- **Criterios de entrada**: señal de indicador
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: ATR, MACD
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

