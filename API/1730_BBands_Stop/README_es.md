# Estrategia BBands Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza el indicador BBands Stop derivado de las Bandas de Bollinger para seguir las tendencias del mercado. Cuando la línea de stop gira hacia arriba, cierra cualquier posición corta y abre una larga. Un giro hacia abajo cierra las posiciones largas y abre cortas. Los parámetros controlan el período de Bollinger, la desviación, el desplazamiento de riesgo y los permisos para entrar o salir de largos y cortos.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La línea de stop de tendencia alcista está activa.
  - **Corto**: La línea de stop de tendencia bajista está activa.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Señal de stop opuesta.
- **Stops**: Trailing stop derivado de las Bandas de Bollinger.
- **Filtros**: Ninguno.
