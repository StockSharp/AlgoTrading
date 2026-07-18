# Estrategia del Sistema de Impulso Elder
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el Sistema de Impulso Elder que combina la dirección de una Media Móvil Exponencial (EMA) con el momentum del histograma MACD. Abre operaciones cuando el impulso alcista o bajista se desvanece en velas de marcos temporales superiores.

El enfoque observa impulsos codificados por colores derivados de la pendiente de la EMA y la dinámica del histograma MACD:
- **Verde (2)** — EMA subiendo y el histograma MACD subiendo y positivo.
- **Rojo (1)** — EMA bajando y el histograma MACD bajando y negativo.
- **Azul (0)** — cualquier otro estado.

Se abre una posición larga cuando un impulso alcista previo (verde) se debilita, mientras que las posiciones cortas aparecen después de que un impulso bajista (rojo) se debilita. Las posiciones opuestas se cierran cuando se detecta el impulso correspondiente.

## Detalles

- **Criterios de entrada**: Cambio de color de Elder Impulse en velas finalizadas.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Impulso opuesto o protección de posición.
- **Stops**: Usa `StartProtection` con stop y take profit del 2% por defecto.
- **Valores predeterminados**:
  - `EmaPeriod` = 13
  - `MacdFastPeriod` = 12
  - `MacdSlowPeriod` = 26
  - `MacdSignalPeriod` = 9
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: EMA, MACD
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: 4H
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
