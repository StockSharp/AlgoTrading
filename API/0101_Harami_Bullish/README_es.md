# Estrategia Bullish Harami
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El Harami Alcista es un patrón de dos velas donde un cuerpo pequeño está contenido dentro del rango de la vela bajista anterior. Indica que el impulso vendedor se ha detenido y los compradores pueden volver a entrar.

Las pruebas indican un rendimiento anual promedio de aproximadamente 40%. Funciona mejor en el mercado de criptomonedas.

Esta estrategia entra largo una vez que la segunda vela cierra dentro de la primera, esperando continuación al alza en la siguiente barra.

Un stop porcentual por debajo del patrón proporciona protección, y la operación se cierra si el precio cae de vuelta por debajo del setup.

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
