# Tendencia Alexcud
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Trend Alexcud busca un fuerte movimiento direccional alineando múltiples medias móviles simples y el Accelerator Oscillator en tres marcos temporales. Fue convertida del experto MQL5 original "TREND_alexcud v_2".

El sistema observa tres marcos temporales (por defecto 15 minutos, 1 hora, 4 horas). En cada marco temporal calcula cinco medias móviles simples (períodos 5, 8, 13, 21, 34) y el Accelerator Oscillator. Un marco temporal se considera alcista cuando el precio de cierre está por encima de todas las medias móviles y el Accelerator es positivo. Un marco temporal es bajista cuando el precio de cierre está por debajo de todas las medias móviles y el Accelerator es negativo.

Solo se abre una operación cuando los tres marcos temporales coinciden. Si son simultáneamente alcistas, la estrategia compra; una lectura bajista común activa una venta. La posición se revierte cuando aparece la señal opuesta. Las órdenes de protección se gestionan a través del sistema de riesgo integrado de StockSharp.

## Detalles

- **Criterios de entrada**
  - **Largo**: Precio por encima de todas las MAs y Accelerator > 0 en cada marco temporal.
  - **Corto**: Precio por debajo de todas las MAs y Accelerator < 0 en cada marco temporal.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: La posición se revierte cuando se forma la señal opuesta.
- **Stops**: Usa protección integrada (sin valores predeterminados).
- **Valores predeterminados**:
  - Timeframe1 = 15m, Timeframe2 = 1h, Timeframe3 = 4h
  - Períodos de MA = 5, 8, 13, 21, 34
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Múltiples
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Multi-timeframe
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
