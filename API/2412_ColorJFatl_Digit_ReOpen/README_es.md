# Estrategia ColorJFatl Digit ReOpen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia usa una Jurik Moving Average (JMA) para identificar la dirección de la tendencia. Se abre una posición larga cuando la JMA gira hacia arriba y se cierran todas las posiciones cortas. Se abre una posición corta cuando la JMA gira hacia abajo y se cierran todas las posiciones largas. Se agregan posiciones adicionales cada vez que el precio se mueve un número fijo de puntos en la dirección de la operación, hasta un máximo.

## Detalles

- **Entrada**:
  - JMA cambia dirección hacia arriba → abrir largo y cerrar cortos.
  - JMA cambia dirección hacia abajo → abrir corto y cerrar largos.
- **Re-entrada**:
  - Después de la posición inicial, se abren nuevas posiciones cada `PriceStep` puntos en la dirección de la operación hasta alcanzar `MaxPositions`.
- **Salida**:
  - El giro opuesto de la JMA cierra las posiciones actuales.
- **Parámetros**:
  - `JmaLength` – período de JMA.
  - `PriceStep` – movimiento de precio en puntos requerido para re-entrada.
  - `MaxPositions` – número máximo de posiciones simultáneas.
  - `BuyPosOpen`, `SellPosOpen`, `BuyPosClose`, `SellPosClose` – habilitar o deshabilitar acciones.
  - `CandleType` – marco temporal para cálculos.
- **Indicador**: Jurik Moving Average.
- **Tipo**: Seguimiento de tendencia.
- **Marco temporal**: 4 horas por defecto.
