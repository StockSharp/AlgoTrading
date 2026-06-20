# Estrategia de Predicción de Renta Variable con Petróleo Crudo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza la relación entre el petróleo crudo y los rendimientos de renta variable. Si el rendimiento del petróleo crudo en el último mes es positivo, la estrategia invierte en un ETF de renta variable. De lo contrario, rota el capital hacia un ETF de efectivo o bonos, manteniéndose fuera de la renta variable cuando el petróleo está débil.

El algoritmo monitorea velas diarias y comprueba la señal el primer día de negociación de cada mes. Las órdenes se envían a precios de mercado y respetan un tamaño mínimo de operación para evitar ejecuciones pequeñas.

## Detalles

- **Universo**: Un ETF de renta variable, un instrumento de petróleo crudo y un ETF de efectivo o bonos.
- **Señal**: Ir largo en el ETF de renta variable cuando el rendimiento del petróleo crudo en el período `Lookback` es mayor que cero; de lo contrario, mantener el ETF de efectivo.
- **Rebalanceo**: Mensual, al inicio del mes.
- **Posicionamiento**: Largo en renta variable o en efectivo, nunca en ambos.
- **Parámetros**:
  - `Equity` – ETF de renta variable objetivo.
  - `Oil` – instrumento de petróleo crudo para la señal.
  - `CashEtf` – activo defensivo cuando el rendimiento del petróleo es negativo.
  - `Lookback` – número de velas para calcular el rendimiento del petróleo.
  - `CandleType` – marco temporal de las velas (predeterminado: 1 día).
- **Nota**: El ejemplo se centra en la estructura y omite costes de transacción y deslizamiento.
