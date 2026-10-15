# Estrategia EMA Cross MACD al Inicio de Sesión
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia entra en largo cuando una EMA rápida cruza por encima de una EMA lenta y el histograma de MACD es positivo. Entra en corto en el cruce opuesto con un histograma negativo. Si estas condiciones ya se cumplen en la primera barra de una sesión de trading, se abre una posición de inmediato. Las posiciones se cierran en un cruce opuesto o cuando termina la sesión.

## Detalles

- **Criterios de entrada**:
  - EMA rápida cruza por encima de la EMA lenta con histograma MACD positivo.
  - O en la primera barra de sesión cuando la EMA rápida está por encima de la EMA lenta y el histograma MACD es positivo.
- **Criterios de salida**:
  - Cruce EMA opuesto o fin de sesión.
- **Indicadores**: EMA, MACD.
- **Tipo**: Seguimiento de tendencia.
- **Marco temporal**: 5 minutos (predeterminado).
