# Estrategia de DeMarker ganando posición Volumen 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia reproduce el MetaTrader 5 asesor experto **"DeMarker ganando posición volumen 2"** utilizando el nivel alto de StockSharp API. Analiza una serie de velas configurables con el oscilador DeMarker y reacciona cuando el valor entra en zonas extremas. La implementación mantiene el estilo original de administración de dinero con tamaño de lote fijo, inversión opcional de señales, manejo integrado de stop-loss/take-profit y un filtro de sesión comercial opcional.

## Comportamiento experto original

* **Plataforma**: MetaTrader 5.
* **Indicador**: oscilador DeMarker clásico (`DEM`), período predeterminado 14.
* **Entradas**: abrir posiciones largas cuando DeMarker cae por debajo de un umbral inferior, abrir posiciones cortas cuando sube por encima de un umbral superior.
* **Controles de riesgo**: stop-loss/take-profit fijo expresado en puntos, trailing stop opcional con escalón, ventana de tiempo opcional.
* **Gestión de posición**: asegúrese de realizar solo una operación por barra y cierre el lado opuesto antes de cambiar de dirección.

La conversión StockSharp sigue los mismos principios. Las órdenes de protección se implementan con `StartProtection`, por lo que el límite de pérdidas, la toma de ganancias y el seguimiento se administran automáticamente una vez que se abre una posición.

## Lógica comercial

1. Suscríbase al tipo de vela configurado (`CandleType`, velas de 5 minutos por defecto) y calcule el valor de DeMarker con el período elegido (`DeMarkerPeriod`).
2. Cuando se cierra una vela, evalúe el oscilador:
   * Si `ReverseSignals` es **falso** (predeterminado):
     * **Configuración larga** – `DeMarker <= LowerLevel`.
     * **Configuración breve** – `DeMarker >= UpperLevel`.
   * Si `ReverseSignals` es **verdadero**, las reglas largas/cortas se intercambian.
3. Opere únicamente dentro de la ventana de sesión opcional definida por `SessionStart`/`SessionEnd` cuando `UseTimeFilter` esté habilitado. Se admiten sesiones nocturnas.
4. Ejecute como máximo una nueva entrada por vela. Antes de abrir una nueva posición, la estrategia cierra cualquier posición opuesta para reflejar la lógica MT5.
5. Los volúmenes están fijados por el parámetro `TradeVolume`. Si la estrategia ya está parcialmente en la dirección deseada, se completa hasta el volumen solicitado.

## Gestión de riesgos

* `StopLossPoints` y `TakeProfitPoints` (en pasos de precio) se asignan a las distancias de parada y toma de ganancias basadas en puntos del experto.
* Al habilitar `EnableTrailing` se cambia la distancia de parada a `TrailingStopPoints` y se activa el motor de seguimiento integrado usando `TrailingStepPoints` como paso de ajuste.
* `StartProtection` está configurado con `useMarketOrders = true` para que las órdenes de protección se ejecuten inmediatamente, asemejándose al comportamiento de cierre comercial de MT5.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `DeMarkerPeriod` | Período promedio del indicador DeMarker. |
| `UpperLevel` / `LowerLevel` | Umbrales de sobrecompra/sobreventa que activan posiciones cortas/largas. |
| `ReverseSignals` | Intercambie condiciones largas y cortas. |
| `StopLossPoints` | Distancia de parada de protección inicial medida en pasos de precio. |
| `TakeProfitPoints` | Distancia de obtención de beneficios medida en incrementos de precios. |
| `EnableTrailing` | Habilita el bloque trailing stop. |
| `TrailingStopPoints` | Distancia del trailing stop una vez que el trailing está activo. |
| `TrailingStepPoints` | Movimiento mínimo favorable antes de que se avance el trailing stop. |
| `UseTimeFilter` | Restringe el comercio a la ventana `SessionStart`–`SessionEnd`. |
| `SessionStart` / `SessionEnd` | Límites de sesión inclusivos/exclusivos (admite la integración). |
| `TradeVolume` | Cantidad a enviar con cada orden de mercado. |
| `CandleType` | Serie de velas a analizar (por defecto 5 minutos). |

## Notas de implementación

* El experto en MT5 incluyó un umbral de "activación final". La protección de seguimiento estándar de StockSharp no expone el mismo parámetro, por lo tanto, el seguimiento se activa inmediatamente cuando `EnableTrailing` es verdadero.
* La infraestructura de StockSharp maneja el manejo de errores para tamaños de lote no válidos, niveles de congelación y lógica de actualización de oferta/demanda, por lo que se omiten de la conversión.
* El registro se realiza a través de la clase base `Strategy` (llame a `LogInfo/LogError` si se requiere seguimiento adicional).
