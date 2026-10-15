# Estrategia de Oscilador de Pronóstico
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia adapta el indicador clásico Forecast Oscillator a StockSharp. Combina una línea base de regresión lineal con suavizado Tillson T3 para resaltar reversiones de tendencia. Una señal de compra aparece cuando el oscilador cruza hacia arriba su línea suavizada mientras la línea suavizada permanece por debajo de cero. Una señal de venta se produce en las condiciones opuestas.

El algoritmo sigue la implementación MQL original y admite habilitar o deshabilitar la apertura y el cierre de posiciones por separado.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El oscilador cruza hacia arriba el T3 y el T3 es negativo.
  - **Corto**: El oscilador cruza hacia abajo el T3 y el T3 es positivo.
- **Largo/Corto**: Ambas direcciones son compatibles.
- **Criterios de salida**:
  - Señales opuestas si las opciones de cierre correspondientes están habilitadas.
- **Stops**: Ninguno.
- **Filtros**: Ninguno.
