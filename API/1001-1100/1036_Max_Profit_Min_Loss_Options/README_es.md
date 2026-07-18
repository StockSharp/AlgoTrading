# Estrategia Max Profit Min Loss Options
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina medias móviles rápidas y lentas con RSI, MACD y un filtro de volumen. Entra en largo cuando las condiciones de tendencia y momentum se alinean, y usa un stop loss y trailing profit para las salidas.

## Detalles

- **Criterios de entrada**:
  - **Largo**: MA rápida > MA lenta, MACD cruza por encima de la señal, RSI > sobrevendido con patrón alcista, volumen por encima del promedio.
  - **Corto**: MA rápida < MA lenta, MACD cruza por debajo de la señal, RSI < sobrecomprado con patrón bajista, volumen por encima del promedio.
- **Salida**: señal opuesta o stop-loss/trailing profit.
- **Stops**: stop loss porcentual y trailing profit.
- **Valores predeterminados**:
  - Longitud de MA rápida = 9
  - Longitud de MA lenta = 21
  - Longitud de RSI = 14
  - Longitud de SMA de volumen = 20
  - Stop loss = 1%
  - Trailing profit = 4%
- **Indicadores**: MA, RSI, MACD, SMA de volumen
- **Marco temporal**: velas de 5 minutos por defecto
