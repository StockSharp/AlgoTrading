# Estrategia MaDelta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia MaDelta mide la diferencia entre una media móvil rápida y una lenta. La diferencia se escala por un multiplicador y se eleva a la tercera potencia, produciendo un valor oscilante `px`. Dos umbrales dinámicos separados por `Delta` (en pips) rastrean el máximo y mínimo reciente de este valor. Cuando `px` rompe por encima del umbral superior, la estrategia cambia a sesgo largo; cuando `px` cae por debajo del umbral inferior, cambia a sesgo corto. Las posiciones existentes opuestas al nuevo sesgo se cierran y se abre una nueva operación en la dirección de la señal.

El enfoque captura eficazmente las ráfagas de momentum cuando la distancia entre las dos medias móviles se expande rápidamente. Elevar al cubo la diferencia exagera los movimientos fuertes mientras filtra las pequeñas fluctuaciones. El parámetro `Delta` define cuánto debe recorrer `px` antes de que se reconozca un giro, evitando las señales falsas en mercados planos.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `px > hi` establece `trade = 1` y abre un largo cuando no existe posición.
  - **Corto**: `px < lo` establece `trade = -1` y abre un corto cuando está plano.
- **Lógica de reversión**:
  - Señal larga mientras está corto cierra el corto con una compra de mercado antes de entrar largo.
  - Señal corta mientras está largo cierra el largo con una venta de mercado antes de entrar corto.
- **Indicadores**:
  - Media móvil rápida (SMA) con período `FastMaPeriod`.
  - Media móvil lenta (EMA) con período `SlowMaPeriod`.
  - Oscilador: `px = ((Multiplier * 0.1) * (FastMA - SlowMA))^3`.
- **Parámetros**:
  - `Delta` – tamaño del canal alto/bajo en pips.
  - `Multiplier` – escala la diferencia de MA antes de elevar al cubo.
  - `FastMaPeriod` – longitud de la SMA rápida.
  - `SlowMaPeriod` – longitud de la EMA lenta.
  - `Volume` – volumen de la orden en las entradas.
  - `CandleType` – marco temporal de las velas procesadas.
- **Otras notas**:
  - Solo funciona con velas completadas.
  - Sin stop-loss o take-profit explícitos; las posiciones se revierten con señales opuestas.
  - Usa la API de alto nivel con vinculación de indicadores y gráficos automáticos.
