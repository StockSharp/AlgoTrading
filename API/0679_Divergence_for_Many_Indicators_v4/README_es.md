# Estrategia de Divergencia para Múltiples Indicadores v4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia detecta divergencias entre el precio y múltiples indicadores de momentum (MACD, RSI, Stochastic, CCI, Momentum, OBV, MFI).
Se abre una posición cuando al menos un número especificado de indicadores muestra divergencia en la misma dirección.

## Detalles
- **Criterios de entrada**: Entrar largo cuando el precio cae mientras la mayoría de indicadores suben (divergencia positiva). Entrar corto cuando el precio sube mientras la mayoría de indicadores caen (divergencia negativa).
- **Largo/Corto**: Ambos
- **Criterios de salida**: Divergencia opuesta o protección de posición
- **Stops**: Porcentajes de take profit y stop loss configurables
- **Valores predeterminados**: Velas de 5m, 2 confirmaciones, 4% take profit, 2% stop loss
- **Filtros**: Utiliza varios indicadores de momentum para confirmación
