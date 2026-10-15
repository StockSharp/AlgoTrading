# Estrategia de Inteligencia Artificial Perceptron
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza los valores del Accelerator Oscillator (AC) como entradas de un perceptrón lineal simple. Cuatro lecturas de AC separadas siete barras se ponderan mediante coeficientes definidos por el usuario. Una salida positiva del perceptrón abre una posición larga, y una salida negativa abre una posición corta.

La estrategia siempre aplica un stop-loss. Si aparece una señal opuesta después de que el beneficio supere el doble del stop-loss, la posición se invierte con volumen aumentado. De lo contrario, el stop-loss se mueve al punto de equilibrio.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Salida del perceptrón > 0.
  - **Corto**: Salida del perceptrón < 0.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Señal opuesta con beneficio > 2 * StopLoss → reversión.
  - Señal opuesta con menor beneficio → stop movido a la entrada.
  - Stop-loss alcanzado.
- **Stops**: Stop-loss fijo en puntos.
- **Filtros**: Ninguno.

## Parámetros
- `StopLoss` – distancia del stop-loss en puntos (por defecto 850).
- `Shift` – desplazamiento de barra para los valores del indicador (por defecto 1).
- `X1`, `X2`, `X3`, `X4` – pesos del perceptrón.
- `CandleType` – marco temporal de velas.
