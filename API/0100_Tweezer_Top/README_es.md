# Tweezer Top Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El Tweezer Top es el espejo de la versión inferior, pero aparece después de un avance. Dos velas comparten casi el mismo máximo, mostrando que los compradores no pudieron superar cierto nivel.

Las pruebas indican un rendimiento anual promedio de aproximadamente 187%. Funciona mejor en el mercado de acciones.

La estrategia abre un corto una vez que la segunda vela confirma el techo, esperando un retroceso a medida que el impulso alcista se detiene.

Un stop ajustado por encima de los máximos gemelos mantiene el riesgo bajo control, y la operación se cierra si el precio sube de vuelta por encima de esa resistencia.

## Detalles

- **Criterios de entrada**: coincidencia de patrón
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minutos
  - `StopLoss` = 2%
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: Candlestick
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
