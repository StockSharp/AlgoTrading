# Estrategia de obtención de ganancias
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica al experto en "obtención de beneficios" MetaTrader al buscar cuatro velas consecutivas con máximos y aperturas estrictamente monótonos. Cuando la vela actual se completa con máximos crecientes y se abre, el algoritmo trata la secuencia como un impulso alcista y envía una compra de mercado. Una condición reflejada con máximos descendentes y aperturas produce una venta en el mercado. Las órdenes se gestionan con un objetivo de ganancias a nivel de cuenta, un trailing stop que puede cerrar parcialmente la exposición y un stop loss fijo opcional definido en pasos de precio.

La configuración predeterminada opera con velas de un minuto. La estrategia se puede ajustar para diferentes instrumentos ajustando el tipo de vela, los índices de desplazamiento que controlan qué velas se comparan, la distancia de seguimiento, la distancia de stop-loss, el objetivo de ganancias y el modo de tamaño de la posición. Admite un tamaño de lote fijo o un volumen dinámico calculado a partir del capital de la cartera y el porcentaje de riesgo definido por el usuario. Cuando el trailing stop avanza, el algoritmo puede opcionalmente cerrar la mitad de la posición restante para asegurar ganancias mientras se mantiene activo al corredor.

Alcanzar el objetivo de beneficio configurado medido sobre el capital de la cartera liquida inmediatamente la posición actual y cancela cualquier orden de trabajo. Esto refleja al experto MQL original que cerró todas las operaciones cuando el capital de la cuenta excedía el saldo más la ganancia deseada. La rama de gestión de riesgos valida el porcentaje de riesgo configurado y garantiza que el volumen solicitado respete el paso del volumen de seguridad.

## Detalles

- **Lógica de entrada**:
  - **Largo**: las cuatro velas monitoreadas muestran máximos estrictamente crecientes y aperturas estrictamente crecientes.
  - **Corto**: las cuatro velas monitoreadas muestran máximos estrictamente decrecientes y aperturas estrictamente decrecientes.
- **Gestión de Puestos**:
  - Stop-loss opcional colocado al precio de entrada menos/más el número configurado de pasos de precio.
  - El trailing stop sigue el precio de cierre una vez que se mueve más que la distancia de seguimiento desde la entrada.
  - La salida parcial (50% del volumen restante) se ejecuta cada vez que se mueve el trailing stop, sujeto al paso de volumen de seguridad y al lote mínimo negociable.
- **Objetivo de cuenta**: cierra toda exposición y cancela órdenes activas cuando `portfolio equity ≥ initial equity + ProfitTarget`.
- **Gestión de riesgos**:
  - El modo de lote fijo utiliza el parámetro `Lots` configurado (o `Volume` de la base de estrategia si se especifica).
  - El modo de porcentaje de riesgo dimensiona el pedido como `equity * RiskPercent / max(stopDistance, price)` y normaliza el resultado por paso de volumen.
- **Parámetros predeterminados**:
  - `Shift1` = 0, `Shift2` = 1, `Shift3` = 2, `Shift4` = 3.
  - `TrailingStopPoints` = 1, `StopLossPoints` = 0, `ProfitTarget` = 1 (unidades monetarias de la cuenta).
  - `Lots` = 1, `RiskPercent` = 1, `MaxOrders` = 1.
  - `CandleType` = período de tiempo de 1 minuto.
- **Mejores mercados**: futuros de tendencia, principales divisas y pares de criptomonedas líquidos donde el impulso a corto plazo persiste en múltiples velas.
- **Fortalezas**: detección rápida del impulso, objetivo de capital configurable, ampliación parcial y controles de riesgo simples.
- **Debilidades**: sensible a rangos ruidosos, depende del tamaño de paso correcto y asume el modo de red (posición agregada única).

## Parámetros

| Nombre | Descripción |
| --- | --- |
| `Shift1` – `Shift4` | Índices de las velas comparados para la secuencia de ruptura. |
| `TrailingStopPoints` | Distancia de seguimiento en pasos de precio. |
| `StopLossPoints` | Distancia de parada inicial en pasos de precio; cero desactiva el stop-loss. |
| `ProfitTarget` | Objetivo de ganancias aplicado al capital de la cartera antes de cerrar todas las operaciones. |
| `Lots` | Volumen de operaciones fijo cuando la gestión de riesgos está deshabilitada. |
| `RiskManagement` | Habilita el dimensionamiento basado en riesgos usando `RiskPercent`. |
| `RiskPercent` | Porcentaje del capital de la cartera arriesgado en cada operación cuando la gestión de riesgos está activa. |
| `PartialClose` | Si está habilitado, cierra la mitad de la posición cada vez que se mueve el trailing stop. |
| `MaxOrders` | Número máximo de unidades base permitidas simultáneamente (límite de posición neta). |
| `CandleType` | Marco de tiempo utilizado para la generación de la señal. |

## Consejos de uso

1. Alinee los parámetros `Shift` con la volatilidad del instrumento. Los cambios más grandes analizan secuencias de impulso más largas.
2. Establezca `TrailingStopPoints` en relación con el paso del precio del valor; valores demasiado pequeños pueden generar salidas parciales rápidas.
3. Utilice el tamaño del porcentaje de riesgo con un `StopLossPoints` explícito para que el tamaño de la posición refleje el riesgo monetario real por operación.
4. Monitoree la curva de acciones: una vez que se alcanza el objetivo global, la estrategia deja de operar hasta que se reinicia, imitando el EA original.
