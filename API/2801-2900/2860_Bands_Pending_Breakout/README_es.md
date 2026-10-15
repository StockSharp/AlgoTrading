# Estrategia de Rompimiento Pendiente de Bands
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el asesor experto de MetaTrader "Bands 2" sobre la API de alto nivel de StockSharp. Monitorea velas terminadas, verifica que la hora actual esté dentro de la ventana de trading configurada y que el precio esté operando dentro del canal de Bollinger. Cuando se cumplen esas condiciones, coloca una cuadrícula simétrica de tres órdenes stop de compra y tres stop de venta alrededor del envelope de Bollinger. Cada orden lleva sus propias distancias de stop-loss y take-profit, y cualquier ejecución elimina las demás órdenes pendientes.

El enfoque está diseñado para rompimientos desde las bandas de Bollinger. La referencia del stop-loss puede cambiarse entre la banda opuesta o la media móvil central. Un módulo de trailing stop separado ajusta continuamente el stop protector una vez que la posición se mueve en beneficio por un paso configurable.

## Detalles

- **Datos de mercado**: Funciona con cualquier instrumento/tipo de vela proporcionado a través de StockSharp.
- **Horario de trading**: Usa `HourStart`/`HourEnd` para restringir la colocación de órdenes. Las órdenes se actualizan en cada vela terminada dentro de esa ventana.
- **Lógica de entrada**:
  - Esperar una vela terminada con precio de cierre estrictamente entre las bandas de Bollinger desplazadas superior e inferior.
  - Eliminar las órdenes pendientes sobrantes de la barra anterior y colocar tres stop de compra por encima de la banda superior y tres stop de venta por debajo de la banda inferior.
  - Cada nivel está separado por `StepPips` convertido a ticks.
- **Modos de Stop-Loss**:
  - *BollingerBands*: El stop-loss usa la banda opuesta desplazada por la misma distancia de paso que la orden de entrada.
  - *MovingAverage*: El stop-loss usa el valor de la media móvil más/menos la distancia de paso (usa el precio aplicado y método configurados).
  - *None*: No se establece stop inicial; el trailing stop puede activarse después.
- **Lógica de Take-Profit**:
  - El primer nivel usa `FirstTakeProfitPips` para órdenes de compra y venta.
  - Las órdenes de compra segunda y tercera usan distancias de take-profit `Second`/`Third`, mientras que las órdenes de venta siguen el comportamiento del script MQL original y siempre reutilizan la primera distancia de take-profit.
- **Gestión de órdenes**:
  - Cuando cualquier orden pendiente se ejecuta, la estrategia cancela todas las demás órdenes de entrada y crea órdenes protectoras independientes del mercado (stop + limit) para el volumen ejecutado.
  - El módulo trailing mueve la orden stop hacia el mercado una vez que el precio se mueve por `TrailingStopPips + TrailingStepPips` desde la entrada.
  - Las órdenes protectoras de stop/limit se cancelan automáticamente cuando la posición queda plana.
- **Normalización de precios**: Todos los niveles de precios se redondean al tamaño de tick del instrumento y la conversión punto-a-pip imita el manejo original de 3/5 dígitos.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `OrderVolume` | Volumen para cada orden pendiente (mismo volumen para las seis órdenes). |
| `CandleType` | Marco temporal/tipo de datos usado para los cálculos del indicador. |
| `HourStart`, `HourEnd` | Horas inclusivas/exclusivas (0-24) que permiten colocar nuevas órdenes pendientes. `HourEnd` debe ser mayor que `HourStart`. |
| `StopLossModes` | Referencia de colocación para el stop-loss inicial (`BollingerBands`, `MovingAverage`, `None`). |
| `FirstTakeProfitPips`, `SecondTakeProfitPips`, `ThirdTakeProfitPips` | Distancias de take-profit (en pips) convertidas a compensaciones de precio para las entradas primera, segunda y tercera. |
| `TrailingStopPips`, `TrailingStepPips` | Distancia del trailing stop y el paso adicional requerido antes de avanzar el stop. Cero para desactivar trailing. |
| `StepPips` | Espaciado entre órdenes pendientes consecutivas (convertido a precio). |
| `MaPeriod`, `MaShift`, `MaMethod`, `MaPriceType` | Configuración de media móvil usada para la entrada de Bollinger y opcionalmente para la colocación de stop cuando `StopLossModes` es `MovingAverage`. El `MaShift` emula el desplazamiento hacia adelante del EA original. |
| `BandsPeriod`, `BandsShift`, `BandsDeviation`, `BandsPriceType` | Configuración de bandas de Bollinger (período, desplazamiento, multiplicador de desviación y precio aplicado). |

## Resumen del comportamiento

1. Suscribirse a velas terminadas del marco temporal seleccionado.
2. En cada vela terminada dentro de la ventana de trading, calcular las bandas de Bollinger desplazadas y la media móvil usando los precios aplicados seleccionados.
3. Asegurar que el cierre de la vela está dentro del canal de bandas, luego colocar la cuadrícula de stop de compra/venta alrededor de los bordes del canal con stops y objetivos individuales.
4. Cuando una orden se ejecuta, cancelar las órdenes de entrada restantes, enviar órdenes protectoras de stop/limit y comenzar trailing según los parámetros configurados.
5. Cerrar las órdenes protectoras cuando la posición sale, listo para la próxima oportunidad de rompimiento.
