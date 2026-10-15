# Estrategia de Cruce EMA de MicuRobert
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia usa dos Medias Móviles Exponenciales de Retardo Cero (ZLEMA) para operar cruces. Puede restringir la operativa a una sesión determinada y usar opcionalmente un stop dinámico (trailing stop).

## Detalles

- **Criterios de entrada:**
  - **Largo:** la ZLEMA rápida cruza por encima de la ZLEMA lenta, o el precio cruza por encima de la ZLEMA rápida mientras la rápida está por encima de la lenta.
  - **Corto:** la ZLEMA rápida cruza por debajo de la ZLEMA lenta, o el precio cruza por debajo de la ZLEMA rápida mientras la rápida está por debajo de la lenta.
- **Criterios de salida:** las posiciones se cierran por trailing stop o por niveles fijos de stop-loss y take-profit.
- **Stops:** trailing stop opcional con take-profit y stop-loss fijos.
- **Filtros:** filtro de tiempo de sesión.
