# 3874 Estrategia de captura de tendencias
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia de captura de tendencias** es una versión StockSharp de alto nivel del MetaTrader asesor experto `MQL/7772/Trendcapture.mq4`. El EA original observa la dirección de la tendencia Parabolic SAR y espera a que un entorno ADX débil ingrese a nuevas posiciones. Después de cada operación cerrada, decide si mantiene o cambia la dirección de la operación dependiendo de la ganancia obtenida, y una vez que una posición abierta gana algunos puntos, tira del stop hasta el punto de equilibrio.

Este puerto mantiene el comportamiento intacto y depende de los asistentes de pedidos y los enlaces de indicadores de StockSharp. Todas las señales se procesan en velas completas de un período de tiempo configurable.

## Lógica de trading

1. **Configuración del indicador**
   - Parabolic SAR (`ParabolicSar`) con paso y límite de aceleración configurables.
   - Índice direccional promedio (`AverageDirectionalIndex`) para el valor de fuerza de la tendencia principal.
2. **Selección de entrada**
   - Sólo se puede abrir una posición a la vez.
   - Se permite una entrada larga cuando:
     - La dirección deseada (derivada de la última operación cerrada) apunta a la compra.
     - La vela actual cierra por encima del valor SAR.
     - La línea principal ADX está debajo de `20`, lo que indica el régimen de alcance requerido por el código original.
   - Una entrada corta refleja las reglas (la dirección deseada apunta a la venta, precio de cierre por debajo de SAR, ADX por debajo de `20`).
3. **Gestión de salida**
   - Cada vez que se ejecuta, la estrategia envía órdenes de limitación de pérdidas y toma de ganancias a distancias `StopLossPoints` y `TakeProfitPoints` (convertidas a través del paso del precio del valor).
   - Cuando el beneficio flotante alcanza `GuardPoints`, el stop activo se vuelve a emitir al precio de entrada para fijar un suelo de equilibrio.
   - Las operaciones cerradas activan una actualización de dirección: las operaciones rentables mantienen el mismo sesgo, las operaciones perdedoras o planas lo invierten, reproduciendo la verificación `OrderProfit()` del experto.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `CandleType` | Tipo de datos de vela utilizado para los cálculos del indicador. | plazo de 1 hora |
| `SarStep` | Factor de aceleración inicial de Parabolic SAR. | `0.02` |
| `SarMax` | Factor de aceleración máximo para Parabolic SAR. | `0.2` |
| `AdxPeriod` | Período de suavizado de ADX. | `14` |
| `TakeProfitPoints` | Distancia de obtención de beneficios expresada en incrementos de precio. | `180` |
| `StopLossPoints` | Distancia de stop-loss expresada en pasos de precio. | `50` |
| `GuardPoints` | Umbral de beneficio (en pasos de precio) requerido antes de mover el tope al punto de equilibrio. | `5` |
| `MaximumRisk` | factor de escala de volumen; `0.03` reproduce el tamaño del lote original. | `0.03` |

## Notas de uso

- Asegúrese de que el valor seleccionado exponga `PriceStep` (o al menos `MinStep`) para que las distancias de puntos se conviertan en valores de precios correctamente.
- La propiedad base `Volume` representa el tamaño de lote utilizado cuando `MaximumRisk` es igual a `0.03`. Al aumentar el factor de riesgo, el volumen enviado se escala proporcionalmente.
- Debido a que EA cotiza en el mercado e inmediatamente coloca órdenes de protección, no quedan entradas pendientes en el libro cuando la estrategia está inactiva.
- La guardia de equilibrio cancela y vuelve a emitir el stop de protección al precio de entrada; esto refleja la llamada `OrderModify` original que movió el stop-loss al punto de equilibrio.

## Archivos

- `CS/TrendcaptureStrategy.cs` – implementación StockSharp de alto nivel de Trendcapture EA.
- `README_zh.md` – Traducción al chino de este documento.
- `README_ru.md` – Traducción al ruso de este documento.
