# Estrategia de Ciclo de Tendencia Color Schaff MFI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una traducción del experto MQL5 `Exp_ColorSchaffMFITrendCycle`.
Emplea el indicador **Color Schaff MFI Trend Cycle**, que combina
valores del Índice de Flujo de Dinero con un cálculo de doble estocástico. El indicador
produce ocho estados de color que representan zonas de impulso y sobrecompra/sobreventa.

Lógica de trading:

- Cuando el color anterior del indicador es **verde** (índices 6-7) y el color actual
  cae por debajo de la zona de fuerte tendencia alcista, la estrategia cierra posiciones cortas
  y abre una nueva posición larga.
- Cuando el color anterior del indicador es **naranja** (índices 0-1) y el color actual
  sube por encima de la zona de fuerte tendencia bajista, la estrategia cierra posiciones largas
  y abre una nueva posición corta.

Parámetros:

- `FastMfiPeriod` – período del MFI rápido.
- `SlowMfiPeriod` – período del MFI lento.
- `CycleLength` – longitud del buffer cíclico usado en el indicador.
- `HighLevel` / `LowLevel` – umbrales de sobrecompra y sobreventa para el valor STC.
- `CandleType` – marco temporal de las velas de entrada (predeterminado 1 hora).

La estrategia utiliza la API de alto nivel de StockSharp y procesa únicamente velas terminadas.
