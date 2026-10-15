# Estrategia de Scalping Rampok
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sistema de scalping que opera cuando el precio rompe las envolventes de la media móvil.
La estrategia entra en largo cuando el precio cruza hacia arriba la banda inferior y
en corto cuando el precio cruza hacia abajo la banda superior. Las posiciones están protegidas
por parámetros opcionales de take profit, stop loss y stop de seguimiento.

## Detalles

- **Criterios de entrada**:
  - **Compra**: cierre anterior por debajo de la banda inferior y cierre actual por encima de ella.
  - **Venta**: cierre anterior por encima de la banda superior y cierre actual por debajo de ella.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Take profit, stop loss o stop de seguimiento.
- **Stops**: SL/TP y trailing configurables.
- **Filtros**: ninguno.
