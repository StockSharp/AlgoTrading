# Estrategia de red de seguridad con bandas elásticas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

StockSharp puerto del asesor experto RUBBERBANDS 1.6 MetaTrader. El sistema original mantiene un par de tickets de compra y venta cubiertos, reinyecta el lado cerrado después de cada ganancia y activa una red de seguridad cuando la pérdida corriente excede los umbrales de efectivo predefinidos. La conversión mantiene el ciclo alterno pero adapta la mecánica al modelo de posición neta de StockSharp promediando en la dirección de la exposición actual en lugar de mantener órdenes de cobertura independientes.

## Lógica de trading

- **Inicio del ciclo:** Al final de cada minuto, o cuando se activa `Enter Now`, la estrategia abre una posición de mercado usando `BaseVolume`. El siguiente ciclo alterna dirección (compra, luego vende, luego vuelve a comprar, etc.).
- **Objetivo de beneficio base:** El PnL en ejecución no realizado se compara con `TargetProfitPerLot * BaseVolume`. Cuando se alcanza, la posición se liquida y el siguiente ciclo cambia de dirección.
- **Control de sesión:** `UseSessionTakeProfit` y `UseSessionStopLoss` observan las ganancias acumuladas realizadas más las no realizadas medidas en efectivo por lote base. Alcanzar cualquiera de los umbrales desencadena una liquidación total y reinicia los contadores.
- **Modo de seguridad:** Cuando está habilitado y la pérdida no realizada excede `SafetyStartPerLot * BaseVolume`, el algoritmo ingresa al modo de seguridad y comienza a promediar en la dirección actual enviando órdenes adicionales de tamaño `SafetyVolume`. Cada `SafetyStepPerLot` pérdida adicional por lote de seguridad programa otro pedido promedio.
- **Salidas de seguridad:** Mientras está en modo de seguridad, la posición se aplana una vez que la ganancia no realizada alcanza `SafetyProfitPerLot * |Position|` o cuando la métrica del nivel de sesión cruza `SafetyModeTakeProfitPerLot * BaseVolume`.

## Condiciones de entrada

### Entradas largas
- No hay exposición abierta y el minuto acaba de transcurrir o `Enter Now` es verdadero.
- La estrategia actualmente espera abrir un largo (ciclos alternos).
- El interruptor de parada manual está desactivado.

### Entradas cortas
- Igual que las condiciones largas, pero la dirección del siguiente ciclo es corta.

## Gestión de salidas

- **Golpe de objetivo base:** Cierra toda la posición y cambia la dirección del ciclo.
- **Sesión TP/SL:** Cierre la posición, borre los contadores de ganancias realizadas y permanezca estable hasta el siguiente minuto.
- **Beneficio de seguridad:** Cierra la posición cuando se alcanza el objetivo de PnL neto mientras el modo de seguridad está activo.
- **Promedio de seguridad:** Se agregan órdenes de seguridad adicionales cuando la pérdida no realizada crece en incrementos de `SafetyStepPerLot`.
- **Cierre manual:** La configuración `Close Now` cierra la posición en la siguiente vela y restablece el acumulador de ganancias realizadas.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `BaseVolume` | Tamaño de la orden de mercado para el tramo principal. |
| `TargetProfitPerLot` | Objetivo de ganancias (efectivo por lote) para la operación base. |
| `UseSessionTakeProfit` / `SessionTakeProfitPerLot` | Habilite y configure la toma de ganancias en toda la sesión. |
| `UseSessionStopLoss` / `SessionStopLossPerLot` | Habilite y configure el stop loss para toda la sesión. |
| `UseSafetyMode` | Alternar la lógica de promedio de seguridad. |
| `SafetyStartPerLot` | Pérdida por lote base que activa el modo de seguridad. |
| `SafetyVolume` | Volumen de cada orden de promedio de seguridad. |
| `SafetyStepPerLot` | Pérdida adicional por lote de seguridad necesaria para poner en cola otra orden de seguridad. |
| `SafetyProfitPerLot` | Objetivo de ganancias aplicado mientras está en modo de seguridad. |
| `SafetyModeTakeProfitPerLot` | Objetivo de ganancias a nivel de sesión mientras el modo de seguridad está activo. |
| `UseInitialState`, `InitialProfitSoFar`, `InitialSafetyMode`, `InitialSafetyToBuy`, `InitialUsedSafetyCount` | Ayudantes de restauración estatal para reinicios. |
| `QuiesceNow`, `Enter Now`, `Stop Trading`, `Close Now` | Cambios manuales que reflejan las variables externas EA originales. |
| `CandleType` | Marco de tiempo de la serie de velas que impulsa el bucle (predeterminado 1 minuto). |

## Notas prácticas

- StockSharp mantiene una única posición neta por instrumento. En lugar de mantener boletos de compra y venta simultáneos, la conversión promedia la posición existente cuando el modo de seguridad está activo. Esto preserva los umbrales basados ​​en efectivo y al mismo tiempo se ajusta al modelo de compensación.
- Los umbrales de pérdidas y ganancias se expresan en la moneda de la cuenta por lote, reflejando las entradas externas MetaTrader. Ajústelos para que coincidan con el valor de tick del instrumento.
- Los interruptores manuales (`Stop Trading`, `Close Now`, `Enter Now`, `Quiesce`) se pueden cambiar sobre la marcha desde la interfaz de usuario para controlar la estrategia sin editar el código.
- `StartProtection()` se invoca al inicio para reutilizar el marco de protección estándar StockSharp para controles de riesgos.
- Asegúrese de que los metadatos del instrumento (`VolumeStep`, `VolumeMin`, `VolumeMax`) estén configurados para que los volúmenes solicitados pasen la validación de intercambio; el ayudante los alinea automáticamente con el paso válido más cercano.
