# Estrategia Tweezer Bottom
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El Tweezer Bottom es un patrón de reversión de dos velas que aparece después de un declive. Ambas velas comparten un mínimo similar, señalando que los vendedores no lograron superar ese nivel.

Las pruebas indican un rendimiento anual promedio de aproximadamente 184%. Funciona mejor en el mercado de criptomonedas.

Esta estrategia entra largo después de que la segunda vela confirma el fondo compartido, anticipando un rebote a medida que la presión vendedora se agota.

Los stops se colocan justo por debajo del mínimo común para gestionar el riesgo, y la posición se cierra si el precio no logra recuperarse.

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
