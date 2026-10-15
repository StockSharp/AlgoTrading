# Estrategia de Three EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia reproduce el asesor experto de MetaTrader "ThreeEMA" apilando tres medias móviles exponenciales (EMA). Busca alineación direccional entre una EMA rápida, media y lenta en el mismo marco temporal. Cuando los promedios están estrictamente ordenados de forma ascendente (rápida por encima de la media por encima de la lenta), la estrategia abre o mantiene una posición larga. Cuando el orden se invierte (rápida por debajo de la media por debajo de la lenta), abre o mantiene una posición corta. Las compensaciones de stop-loss y take-profit protectores reflejan los parámetros MQL originales y se expresan en puntos de precio relativos al tamaño de tick del instrumento.

## Comportamiento MQL original
La versión MQL instanciaba tres indicadores EMA (`FastPeriod`, `MediumPeriod`, `SlowPeriod`) y generaba señales de trading basadas en su ordenación relativa en la barra más recientemente cerrada:

- **Abrir largo / cerrar corto** cuando `FastEMA > MediumEMA > SlowEMA`.
- **Abrir corto / cerrar largo** cuando `FastEMA < MediumEMA < SlowEMA`.
- El stop-loss y take-profit se aplicaban como distancias fijas en puntos desde el precio de entrada.

Las órdenes se enviaban con ejecución de mercado y el bloque de gestión de dinero usaba un tamaño de lote fijo. El módulo de trailing estaba deshabilitado.

## Detalles de implementación de StockSharp
- Usa la API de suscripción de velas de alto nivel. Tres indicadores `ExponentialMovingAverage` están vinculados a la suscripción del marco temporal principal para que cada vela terminada entregue todos los valores EMA simultáneamente.
- Las decisiones de trading se evalúan solo en velas completamente formadas para evitar ruido intrabarra.
- Siempre que aparece una pila direccional, la estrategia cancela cualquier orden vigente, cierra la exposición opuesta si es necesario, y abre una nueva posición de mercado en la dirección requerida.
- `StartProtection` convierte las distancias de stop-loss y take-profit configuradas en puntos en compensaciones de precio reales usando el `PriceStep` del instrumento. Esto refleja el comportamiento protector del EA original.
- La integración de gráficos dibuja velas y las tres EMA cuando hay un área de gráfico disponible, facilitando la validación visual de señales.

## Parámetros
| Nombre | Predeterminado | Descripción |
|------|---------|-------------|
| `CandleType` | Marco temporal de 1 minuto | Marco temporal de la suscripción de velas usado para las EMA. |
| `FastPeriod` | 5 | Longitud de la EMA rápida. Debe ser menor que `MediumPeriod`. |
| `MediumPeriod` | 12 | Longitud de la EMA media. Debe estar entre los períodos rápido y lento. |
| `SlowPeriod` | 24 | Longitud de la EMA lenta. Debe ser el valor de período más alto. |
| `StopLossPoints` | 400 | Distancia de stop-loss protector expresada en puntos del instrumento (convertida a precio usando `PriceStep`). Cero para deshabilitar. |
| `TakeProfitPoints` | 900 | Distancia de take-profit en puntos del instrumento (convertida a precio usando `PriceStep`). Cero para deshabilitar. |

## Notas de uso
1. Configure `Volume` antes de iniciar la estrategia para reflejar el tamaño de orden deseado (el EA original usaba lotes fijos).
2. Asegúrese de que los períodos EMA permanezcan estrictamente crecientes; de lo contrario, se lanza una excepción durante `OnStarted` para coincidir con la validación del código fuente MQL.
3. Debido a que la lógica siempre voltea posiciones cuando la pila EMA se invierte, la estrategia está continuamente expuesta al mercado cuando las condiciones alternan entre alineaciones alcistas y bajistas.
