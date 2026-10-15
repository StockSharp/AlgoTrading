# Estrategia Promediada Stoch & WPR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina el oscilador Stochastic con Williams %R para detectar condiciones extremas del mercado.
Se abre una posición larga cuando el valor de Stochastic cae por debajo de 0.1 y Williams %R está por debajo de -90, señalando una fuerte presión de sobreventa.
Se abre una posición corta cuando el Stochastic sube por encima de 99.9 y Williams %R supera -5, indicando condiciones de fuerte sobrecompra.

La estrategia funciona con cualquier instrumento y marco temporal soportado por el tipo de vela seleccionado. Puede operar tanto en posiciones largas como cortas y ofrece un stop-loss porcentual opcional para la gestión del riesgo.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Stochastic < 0.1 y Williams %R < -90.
  - **Corto**: Stochastic > 99.9 y Williams %R > -5.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta o stop-loss activado.
- **Stops**: Stop-loss porcentual opcional.
- **Indicadores**:
  - Oscilador Stochastic (período predeterminado 26).
  - Williams %R (período predeterminado 26).

## Parámetros

- `StochPeriod` – período de cálculo del Stochastic.
- `WprPeriod` – período de cálculo de Williams %R.
- `StopLossPercent` – tamaño del stop-loss porcentual.
- `CandleType` – tipo de vela utilizado para los cálculos de indicadores.
