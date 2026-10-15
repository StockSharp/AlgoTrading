# Estrategia Panel de Control de Trading
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Panel de Control de Trading** porta el panel de trading manual del script MQL5 original a la API de alto nivel de StockSharp. La clase expone métodos auxiliares que replican cada botón del panel: alternadores de presets de volumen, acciones de compra/venta de mercado, cierre de la posición actual, reversión de la exposición y una rutina dedicada de punto de equilibrio. Las órdenes de stop-loss y take-profit de protección pueden generarse automáticamente alrededor del precio promedio de entrada, reflejando las características de seguridad del asesor experto fuente.

En lugar de dibujar controles de gráfico, la implementación de StockSharp proporciona una interfaz fuertemente tipada que puede llamarse desde código de UI, scripts o flujos de trabajo automatizados. La estrategia realiza un seguimiento de los presets de volumen seleccionados, redondea los volúmenes al paso del exchange más cercano y emite órdenes de mercado/stop/límite a través de los helpers integrados de `Strategy` como `BuyMarket`, `SellMarket`, `SellStop` y `BuyLimit`.

## Parámetros
- **VolumeList** – presets de volumen separados por punto y coma que se comportan como las casillas de verificación originales. Solo se usan los primeros nueve valores para mantener compatibilidad con la disposición MQL. Los espacios en blanco se ignoran y los números inválidos se omiten.
- **CurrentVolume** – volumen agregado basado en los presets actualmente activados. El setter redondea el valor usando `Security.VolumeStep` (cuando esté disponible) o dos lugares decimales (lotes estilo forex). También puede establecer este parámetro manualmente al integrarse con una UI externa.
- **BreakEvenSteps** – número de pasos de precio añadidos al precio de entrada al mover el stop protector al punto de equilibrio a través de `ApplyBreakEven()`. Si el instrumento no tiene `PriceStep`, el valor se trata como un desplazamiento de precio directo.
- **StopLossSteps** – distancia inicial de stop-loss expresada en pasos de precio. Un valor de cero deshabilita los stops automáticos cuando una posición se abre o cambia.
- **TakeProfitSteps** – distancia inicial de take-profit en pasos de precio. Funciona de la misma manera que el parámetro de stop-loss.

## Controles manuales
Todas las acciones en tiempo real se exponen a través de métodos públicos para que la aplicación anfitriona pueda vincularlos a botones, teclas de acceso rápido o scripts:

- `ToggleVolumeSelection(int index)` – imita las casillas de verificación de presets añadiendo o quitando un volumen de la cantidad agregada. Los índices inválidos lanzan para prevenir errores silenciosos.
- `ResetVolumeSelection()` – limpia cada preset y restablece `CurrentVolume` a cero.
- `ExecuteBuy()` / `ExecuteSell()` – envían órdenes de mercado usando el volumen actual. Ambos métodos devuelven `false` cuando no hay volumen seleccionado.
- `CloseAllPositions()` – envía una orden de mercado opuesta al tamaño de posición actual (`BuyMarket` para cortos, `SellMarket` para largos).
- `ReversePosition()` – cierra la posición existente e inmediatamente abre una nueva en la dirección opuesta usando el volumen agregado, exactamente como el botón "Reverse" en el panel MQL.
- `ApplyBreakEven()` – recalcula el stop protector como `precio promedio de entrada ± BreakEvenSteps * PriceStep` y coloca una nueva orden de stop (`SellStop` para largos, `BuyStop` para cortos). Devuelve `true` solo cuando la estrategia tiene una posición abierta y se proporciona un desplazamiento mayor que cero.

Siempre que el tamaño de la posición cambia, `OnPositionChanged` reconstruye las órdenes de protección. Primero cancela el par anterior de stop/objetivo, luego los recrea usando el último precio promedio de entrada y los desplazamientos configurados. Cerrar la posición (manualmente o por llenados de stop/objetivo) elimina cualquier orden de protección activa para evitar instrucciones huérfanas en el exchange.

## Flujo de trabajo de uso
1. Configure los presets de volumen deseados en **VolumeList** (por ejemplo `0.05; 0.10; 0.25; 0.50; 1.00`).
2. Active uno o más presets con `ToggleVolumeSelection`. El parámetro `CurrentVolume` muestra el valor acumulado después del redondeo.
3. Llame a `ExecuteBuy` o `ExecuteSell` para entrar al mercado. Si **StopLossSteps** o **TakeProfitSteps** son mayores que cero, la estrategia colocará automáticamente órdenes `SellStop`/`BuyStop` y `SellLimit`/`BuyLimit` relativas al precio promedio de entrada.
4. Use `ApplyBreakEven` cuando el precio se mueva a su favor para arrastrar el stop por encima (para largos) o por debajo (para cortos) de la entrada por el desplazamiento configurado.
5. `CloseAllPositions` sale del mercado inmediatamente, mientras que `ReversePosition` cierra y voltea la exposición reutilizando el tamaño de lote actualmente seleccionado.
6. `ResetVolumeSelection` prepara el panel para la próxima operación limpiando todos los presets.

## Notas y recomendaciones
- La lógica de punto de equilibrio y protección depende de `PositionAvgPrice` y el `Security.PriceStep` actual. Asegúrese de que los metadatos del instrumento estén poblados antes de iniciar la estrategia.
- `StartProtection()` se llama durante `OnStarted` para que el motor de protección integrado pueda rastrear las órdenes de stop/objetivo que esta estrategia registra.
- Los métodos auxiliares son envoltorios síncronos alrededor de los helpers de orden de StockSharp. Los exchanges o adaptadores que requieren confirmación asíncrona deben esperar eventos de órdenes antes de emitir el siguiente comando si se necesita secuenciación estricta.
- La clase puede embeberse en paneles personalizados WPF/WinForms, servicios REST o herramientas de consola mapeando eventos de UI a los métodos expuestos.
