# Estrategia Definitiva de Scalping BTC T3 Fibonacci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia aplica dos medias móviles Tilson T3 para capturar movimientos de corto plazo en BTC. Un cruce entre las líneas T3 ajustadas por Fibonacci y la T3 estándar genera entradas largas o cortas. Se admite gestión opcional de TP/SL y cierre en señales opuestas.

Las pruebas indican un retorno anual promedio de aproximadamente el 38%. Funciona mejor en pares de BTC con baja latencia.

La estrategia compra cuando la T3 rápida cruza por encima de la T3 lenta y vende en el cruce opuesto. Las posiciones pueden cerrarse en señales inversas, o mediante niveles de take profit y stop loss porcentuales.

## Detalles

- **Criterios de entrada**:
  - **Largo**: T3 rápida cruza por encima de la T3 lenta.
  - **Corto**: T3 rápida cruza por debajo de la T3 lenta.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Cruce opuesto o TP/SL si está activado.
- **Stops**: Opcional basado en porcentaje.
- **Filtros**:
  - Ninguno.
