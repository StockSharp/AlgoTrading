# Estrategia Master Mind Triple WPR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Puerto del MetaTrader4 asesor experto `MasterMind3CE` (carpeta `MQL/8458`).
- Utiliza cuatro indicadores Williams %R con períodos 26, 27, 29 y 30 para detectar condiciones extremas de sobrecompra/sobreventa.
- Diseñado para entradas de reversión a la media: comprar después de una liquidación profunda, vender después de un repunte demasiado extendido.
- Incluye lógica configurable de stop-loss, take-profit y trailing-stop opcional expresada en pasos de precio del instrumento.
- Funciona en cualquier período de tiempo admitido por el terminal StockSharp conectado; El valor predeterminado son velas de 15 minutos.

## Lógica de trading
### Indicadores
- `WilliamsR(26)` — oscilador extremadamente rápido.
- `WilliamsR(27)` — oscilador rápido para confirmación.
- `WilliamsR(29)` — oscilador medio que suaviza la señal.
- `WilliamsR(30)`: oscilador lento que requiere valores extremos en múltiples retrospectivas.

Se deben formar los cuatro osciladores. La suscripción procesa solo velas terminadas para coincidir con el comportamiento `TradeAtCloseBar = true` del experto original.

### Condiciones de entrada
- **Entrada larga**: Los cuatro valores de %R de Williams son inferiores o iguales a `OversoldLevel` (valor predeterminado: `-99.99`). La estrategia apunta a una posición larga de `TradeVolume`. Si se abre un corto, se cierra y se convierte en largo en una única orden de mercado del tamaño necesario para alcanzar la exposición objetivo.
- **Entrada breve**: Los cuatro valores de %R de Williams son superiores o iguales a `OverboughtLevel` (valor predeterminado: `-0.01`). La estrategia apunta a una posición corta de `TradeVolume`, cerrando primero cualquier exposición larga existente.

### Condiciones de salida
- **Salida basada en señales**: Cuando se abre una posición larga y aparece una condición de entrada corta, la estrategia cierra/invierte la posición (y viceversa).
- **Stop de pérdidas de protección**: Distancia de paso de precio opcional aplicada desde el precio de entrada promedio. Un golpe en el máximo/mínimo de la vela desencadena una salida del mercado.
- **Take-profit**: objetivo de paso de precio opcional a partir del precio de entrada promedio. Una vez alcanzada la vela, la posición se cierra.
- **Trailing-stop**: lógica de seguimiento opcional que comienza una vez que el precio se mueve `TrailingStopSteps + TrailingStepSteps` a favor. Luego, la parada se mantiene `TrailingStopSteps` alejada del último cierre y solo avanza cuando mejora al menos `TrailingStepSteps`.

## Gestión del riesgo
Las distancias de precios se especifican en el instrumento *pasos de precios*. Por ejemplo, con `PriceStep = 0.0001` y `StopLossSteps = 2000`, el stop se sitúa a 0,2000 de la entrada. La estrategia recalcula el precio de entrada promedio cuando escala en la misma dirección para mantener consistentes los niveles de riesgo. Las paradas dinámicas están deshabilitadas a menos que tanto `TrailingStopSteps` como `TrailingStepSteps` sean positivos.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `TradeVolume` | Tamaño neto de la posición objetivo (lotes/contratos). | `1` |
| `OversoldLevel` | Williams Umbral %R que confirma condiciones de sobreventa. | `-99.99` |
| `OverboughtLevel` | Williams Umbral %R que confirma condiciones de sobrecompra. | `-0.01` |
| `StopLossSteps` | Distancia de stop-loss en `PriceStep` unidades. Establezca `0` para desactivar. | `2000` |
| `TakeProfitSteps` | Distancia de obtención de beneficios en `PriceStep` unidades. Establezca `0` para desactivar. | `0` |
| `TrailingStopSteps` | Distancia del trailing-stop en `PriceStep` unidades. Requiere `TrailingStepSteps > 0`. | `0` |
| `TrailingStepSteps` | Mejora mínima antes de que se mueva el trailing stop (en `PriceStep` unidades). | `1` |
| `CandleType` | Tipo de datos de vela/período de tiempo procesado por la estrategia. | `TimeFrame(15m)` |

## Notas de conversión
- Se omiten intencionalmente alertas, notificaciones sonoras, acceso a archivos y funciones de correo electrónico del experto MQL. En su lugar, se pueden utilizar registros StockSharp.
- El asesor original permitió negociar antes del cierre de la barra. El puerto mantiene la lógica predeterminada de "negociar al cerrar" procesando solo velas terminadas.
- Los números mágicos, los reintentos de orden repetidos y el dibujo manual de objetos eran específicos de MetaTrader y no tienen equivalentes directos de StockSharp, por lo que se eliminan.
- La gestión de riesgos se consolida dentro de la estrategia en lugar de utilizar bucles externos de modificación de órdenes; Las comprobaciones de detener/tomar se evalúan en cada vela.

## Uso
1. Configure el instrumento y el período de tiempo deseados, coincidiendo con el gráfico al que se adjuntó originalmente el experto.
2. Ajustar umbrales o parámetros de riesgo si el instrumento tiene un perfil de volatilidad diferente.
3. Lanzar la estrategia; se suscribirá a la serie de velas especificada, monitoreará Williams %R extremos y gestionará las posiciones en consecuencia.
