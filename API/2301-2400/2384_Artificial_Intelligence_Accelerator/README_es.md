# Estrategia Acelerador de Inteligencia Artificial
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa un modelo de perceptrón simple sobre el **Oscilador de Aceleración/Desaceleración (AC)** de Bill Williams. Se toman cuatro lecturas del oscilador con rezagos de 0, 7, 14 y 21 barras y se multiplican por pesos ajustables. La suma ponderada actúa como señal de decisión: los valores positivos implican impulso alcista y los valores negativos implican impulso bajista. La estrategia revierte su posición cuando la señal cambia de signo y coloca un stop-loss fijo desde el precio de entrada.

El propio AC se deriva del Awesome Oscillator (AO) restando una media móvil de 5 períodos del AO. Esto hace que la estrategia sea sensible a los cambios en la aceleración del mercado.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Señal del perceptrón > 0.
  - **Corto**: Señal del perceptrón < 0.
- **Largo/Corto**: Ambos lados; la estrategia revierte si la señal cambia.
- **Criterios de salida**:
  - Stop-loss activado desde el precio de entrada.
  - Revertir cuando la señal cruza cero.
- **Stops**: Sí, stop-loss fijo en unidades de precio.
- **Valores predeterminados**:
  - `X1` = 76
  - `X2` = 47
  - `X3` = 153
  - `X4` = 135
  - `StopLoss` = 8355
  - `CandleType` = velas de 1 minuto
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: AC (derivado de AO)
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Corto plazo
  - Redes neuronales: Perceptrón
  - Nivel de riesgo: Alto
