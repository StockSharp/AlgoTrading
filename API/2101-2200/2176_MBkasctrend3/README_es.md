# Estrategia MBKAsctrend3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia MBKAsctrend3 utiliza tres osciladores Williams %R con diferentes períodos. Su combinación ponderada define la tendencia del mercado. Se abre una posición larga cuando el valor ponderado cruza por encima de un umbral superior y el oscilador a largo plazo también es alto. Se abre una posición corta cuando los valores caen por debajo de sus umbrales inferiores. Las posiciones están protegidas por niveles configurables de stop-loss y take-profit expresados en puntos.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Weighted WPR > 67+Swing y long WPR > 50-AverageSwing.
  - **Corto**: Weighted WPR < 33-Swing y long WPR < 50+AverageSwing.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta o niveles de protección.
- **Stops**: Stop loss y take profit absolutos.
- **Filtros**: Ninguno.

## Parámetros
- `WprLength1`, `WprLength2`, `WprLength3` – períodos de los tres indicadores Williams %R.
- `Swing` – desplazamiento de los umbrales superior/inferior.
- `AverageSwing` – desplazamiento adicional basado en el oscilador a largo plazo.
- `Weight1`, `Weight2`, `Weight3` – pesos para cada indicador.
- `StopLoss`, `TakeProfit` – niveles de protección en puntos.
- `CandleType` – marco temporal de las velas, por defecto 4 horas.
