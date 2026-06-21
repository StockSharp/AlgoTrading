# Estrategia de Agrupamiento Jerárquico y K-Means
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia aplica agrupamiento de volatilidad a un sistema SuperTrend. Los valores de ATR se agrupan en tres clústeres para determinar el régimen de mercado, mientras que la dirección del SuperTrend desencadena las entradas. Un filtro opcional de media móvil y ADX confirma la fortaleza de la tendencia. Las posiciones pueden cerrarse anticipadamente cuando la ratio de volumen alcista/bajista se acerca al equilibrio.

## Detalles

- **Criterios de entrada**:
  - **Largo**: SuperTrend se vuelve alcista && tendencia del clúster > 0 && filtros superados.
  - **Corto**: SuperTrend se vuelve bajista && tendencia del clúster < 0 && filtros superados.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Equilibrio de volumen o señal opuesta.
- **Stops**: Solo basados en volumen.
- **Valores predeterminados**:
  - `ATR Length` = 11.
  - `SuperTrend Factor` = 3.
  - `Training Data Length` = 200.
  - `Moving Average Length` = 50.
  - `Trend Strength Period` = 14.
  - `Trend Strength Threshold` = 20.
  - `Volume Ratio Threshold` = 0.9.
  - `Delay Bars` = 4.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Múltiples
  - Stops: Sí
  - Complejidad: Complejo
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
