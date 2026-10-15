# Estrategia Lego V3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una adaptación del asesor experto MQL4 "Lego_v3".  
Combina varios indicadores clásicos para generar entradas y salidas:

- **Medias Móviles** – SMA rápida y lenta para detectar la dirección de la tendencia.
- **Oscilador Stochastic** – los valores de %K y %D definen las zonas de sobreventa y sobrecompra.
- **Awesome Oscillator** – confirma la alineación del momentum con la tendencia.
- **Average True Range** – determina las distancias de stop-loss y take-profit.

Se abre una posición larga cuando la MA rápida cruza por encima de la MA lenta, el Stochastic %K está por debajo del nivel de compra y el Awesome Oscillator es positivo.  
Las posiciones cortas se producen bajo condiciones opuestas. El ATR se usa una vez al inicio para gestionar el stop de protección.

## Parámetros

- `FastMaPeriod` – período para la media móvil rápida.
- `SlowMaPeriod` – período para la media móvil lenta.
- `StochK` – período de %K para el oscilador Stochastic.
- `StochD` – período de %D para el oscilador Stochastic.
- `StochBuy` – umbral de zona de compra para %K.
- `StochSell` – umbral de zona de venta para %K.
- `AtrPeriod` – período para el cálculo del ATR.
- `AtrMultiplier` – multiplicador aplicado al ATR para los niveles de stop.
- `CandleType` – marco temporal de las velas procesadas.
