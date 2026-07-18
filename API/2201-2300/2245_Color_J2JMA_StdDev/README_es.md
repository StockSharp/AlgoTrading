# Estrategia Color J2JMA StdDev
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia calcula la pendiente de una Media Móvil Jurik (JMA) y la compara con la desviación estándar de las pendientes recientes. La idea es capturar movimientos direccionales fuertes cuando la pendiente supera un múltiplo de su volatilidad reciente.

Se abre una nueva posición larga cuando la pendiente del JMA sube por encima del umbral alto (K2 × desviación estándar). Se abre una nueva posición corta cuando la pendiente cae por debajo del umbral alto negativo. Las posiciones existentes se cierran cuando la pendiente cruza el umbral bajo opuesto (K1 × desviación estándar). Los niveles de stop loss y take profit se aplican en puntos desde el precio de entrada.

Parámetros:
- **JMA Length** – período de la media móvil Jurik.
- **StdDev Period** – número de pendientes recientes utilizadas para la desviación estándar.
- **K1** – multiplicador para el umbral bajo usado para cerrar posiciones.
- **K2** – multiplicador para el umbral alto usado para abrir posiciones.
- **Candle Type** – marco temporal de velas para los cálculos.
- **Stop Loss** – stop de protección en puntos.
- **Take Profit** – objetivo de ganancias en puntos.
