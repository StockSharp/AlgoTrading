# Estrategia de Trading de Noticias EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia straddle basada en tiempo diseñada para operar en torno a publicaciones de noticias económicas. En un horario programado, la estrategia coloca órdenes simétricas de compra stop y venta stop a una distancia fija del precio actual. Las órdenes se actualizan en cada vela durante la ventana de activación para seguir el precio de mercado. Si se abre una posición, la orden pendiente opuesta se cancela y niveles opcionales de take-profit y stop-loss gestionan las salidas.

## Detalles

- **Criterios de entrada**:
  - Durante la ventana straddle, colocar compra stop en close + Distance * step y venta stop en close - Distance * step.
- **Largo/Corto**: Ambos
- **Criterios de salida**: Stop opuesto, take-profit/stop-loss o expiración de la orden
- **Stops**: Stop loss y take profit fijos
- **Valores predeterminados**:
  - `StartDateTime` = DateTime.Now
  - `StartStraddle` = 0
  - `StopStraddle` = 15
  - `Volume` = 0.01m
  - `Distance` = 55m
  - `TakeProfit` = 30m
  - `StopLoss` = 30m
  - `Expiration` = 20
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoría: News
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Principiante
  - Marco temporal: Evento
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto
