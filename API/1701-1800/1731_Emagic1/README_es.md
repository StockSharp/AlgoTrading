# Estrategia de Tendencia EMA MACD Signal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia entra en largo cuando la EMA rápida está por encima de la EMA lenta y la línea de señal del MACD sube. Entra en corto cuando la EMA rápida está por debajo de la EMA lenta y la línea de señal baja. El stop-loss, take-profit y trailing stop son opcionales.

## Detalles

- **Criterios de entrada**:
  - EMA rápida > EMA lenta y señal MACD en aumento → Comprar.
  - EMA rápida < EMA lenta y señal MACD en descenso → Vender.
- **Criterios de salida**:
  - La señal de entrada opuesta cierra la posición.
- **Indicadores**: EMA, MACD signal.
- **Tipo**: Seguimiento de tendencia.
- **Marco temporal**: 5 minutos (por defecto).
