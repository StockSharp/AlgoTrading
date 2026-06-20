# Estrategia de Trading de Dispersión
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de trading de dispersión explota los períodos en que un índice de renta variable y sus componentes divergen. Cuando la correlación media por pares entre los miembros del índice cae por debajo de un umbral, la estrategia compra las acciones individuales y vende el índice en corto, apostando a que las correlaciones volverán a la media.

Las velas diarias alimentan una ventana deslizante de correlación. Si las correlaciones se recuperan por encima del umbral, se cierran todas las posiciones. Se impone un valor mínimo de operación para evitar órdenes pequeñas.

## Detalles

- **Universo**: Un instrumento de índice más las acciones que lo componen.
- **Señal**: Abrir una operación de dispersión cuando la correlación media de los componentes está por debajo de `CorrThreshold`.
- **Rebalanceo**: La correlación se comprueba cada día.
- **Posicionamiento**: Largo en los componentes y corto en el índice mientras la señal está activa.
- **Parámetros**:
  - `Constituents` – lista de valores componentes.
  - `LookbackDays` – tamaño de la ventana para el cálculo de correlación.
  - `CorrThreshold` – nivel de correlación que desencadena las operaciones.
  - `MinTradeUsd` – valor mínimo de orden en USD.
  - `CandleType` – marco temporal de las velas (predeterminado: 1 día).
- **Nota**: El ejemplo omite los costes de transacción y asume igual ponderación.
