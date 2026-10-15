# Estrategia de correlación de dos pares
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia de correlación de dos pares** transfiere el asesor experto MetaTrader *"Correlación de 2 pares EA"* (paquete `MQL/52043`) al API de alto nivel de StockSharp. Observa los precios de oferta de dos símbolos criptográficos altamente correlacionados (BTCUSD como tramo principal y ETHUSD como tramo de cobertura) y realiza una operación neutral en el mercado cuando su diferencial se desvía de un umbral configurable.

### Flujo de trabajo principal

1. **Control de riesgos**: el capital de la cartera se monitorea continuamente. Si la reducción desde el pico histórico supera `MaxDrawdownPercent`, las nuevas operaciones se suspenden hasta que el capital se recupere por encima de `RecoveryPercent` del valor máximo.
2. **Filtro de volatilidad**: ambos instrumentos introducen un flujo de velas de 5 minutos en un indicador `AverageTrueRange` de longitud `AtrPeriod`. Las operaciones se omiten cuando ATR excede `PriceDifferenceThreshold * 0.01`, imitando la "pausa de alta volatilidad" del código MQL.
3. **Detección de diferencial**: la estrategia se suscribe a datos de nivel uno para ambos instrumentos y evalúa el diferencial de precio de oferta en cada actualización. Cuando `Bid(BTCUSD) - Bid(ETHUSD) > PriceDifferenceThreshold`, compra BTCUSD y vende ETHUSD. Cuando el diferencial cae por debajo de `-PriceDifferenceThreshold`, las posiciones se invierten (BTCUSD corto, ETHUSD largo).
4. **Tamaño de lote dinámico**: el volumen por tramo se deriva de `RiskPercent` del capital de la cartera actual, dividido por la distancia de parada sintética `StopLossPips * PriceStep`. El resultado se normaliza con las restricciones de volumen de intercambio antes de enviar las órdenes.
5. **Salida de la cesta**: el beneficio flotante total de ambos tramos se rastrea en la moneda de la cuenta. Una vez que llega a `MinimumTotalProfit`, la estrategia cierra todo el par independientemente de la dirección de entrada.

## Datos de mercado requeridos

- **Nivel 1** (mejor oferta/demanda) tanto para el valor principal (`Security`) como para el valor de cobertura (`SecondSecurity`).
- **Velas** de tipo `AtrCandleType` (predeterminado en un período de tiempo de 5 minutos) para que los mismos dos instrumentos alimenten el filtro ATR.

Asegúrese de que los valores expongan valores significativos de volumen `PriceStep`, `StepPrice`, `VolumeStep` y mínimo/máximo para que el tamaño del lote y la conversión de ganancias reflejen el comportamiento de MetaTrader.

## Parámetros

| Nombre | Tipo | Predeterminado | Descripción |
| ---- | ---- | ------- | ----------- |
| `SecondSecurity` | `Security` | — | Instrumento de cobertura (ETHUSD en el EA original). |
| `MaxDrawdownPercent` | `decimal` | `20` | Umbral de reducción que detiene nuevas operaciones. |
| `RiskPercent` | `decimal` | `2` | Participación de la cartera arriesgada por operación para el tamaño de la posición. |
| `PriceDifferenceThreshold` | `decimal` | `100` | Se requiere divergencia del precio de oferta para abrir el par. |
| `MinimumTotalProfit` | `decimal` | `0.30` | Objetivo de beneficio en la moneda de la cuenta para cerrar ambos tramos. |
| `AtrPeriod` | `int` | `14` | ATR longitud para el filtro de volatilidad. |
| `RecoveryPercent` | `decimal` | `95` | Porcentaje del capital máximo necesario para reanudar la negociación después de una reducción. |
| `StopLossPips` | `int` | `50` | Parada sintética utilizada para traducir `RiskPercent` en lotes. |
| `AtrCandleType` | `DataType` | `TimeSpan.FromMinutes(5).TimeFrame()` | Serie de velas utilizada para el cálculo de ATR. |

## Archivos

- `CS/TwoPairCorrelationStrategy.cs`: implementación de estrategia basada en el nivel alto API.
- `README.md` – esta documentación (inglés).
- `README_zh.md` – documentación en chino.
- `README_ru.md` – documentación en ruso.
