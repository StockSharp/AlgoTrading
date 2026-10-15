# Estrategia Pipsover Chaikin Hedge
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia reproduce el asesor experto "Pipsover 2" de MetaTrader dentro de StockSharp. Busca condiciones de sobreventa o
sobrecompra con el oscilador Chaikin mientras el precio perfora una media móvil y utiliza el cuerpo de la vela anterior para
confirmar la reversión. El port de StockSharp mantiene la lógica de cobertura discrecional del código original: cuando aparece una
señal opuesta mientras ya hay una posición, la estrategia inmediatamente revierte la exposición neta para seguir el nuevo sesgo.

## Indicadores y datos
- **Oscilador Chaikin**: construido desde una línea de Acumulación/Distribución suavizada por dos medias móviles. Ambas medias son
  configurables y coinciden con la implementación de MetaTrader (simple, exponencial, suavizada o ponderada).
- **Media móvil de precio**: longitud, desplazamiento y tipo configurables. Actúa como el ancla de reversión a la media que los
  máximos o mínimos de la vela anterior deben perforar.
- **Marco temporal**: la estrategia se suscribe a un único flujo de velas elegido a través del parámetro `CandleType`.

## Lógica de trading
1. Trabajar solo con velas terminadas. El cuerpo de la vela anterior (cierre vs. apertura) proporciona el sesgo direccional.
2. Leer el valor del oscilador Chaikin de la vela anterior. Los valores negativos grandes señalan sobreventa, los valores positivos grandes marcan zonas de sobrecompra.
3. Requerir que la vela anterior perfore el valor actual de la media móvil (`Low < MA` para configuraciones alcistas y `High > MA` para las bajistas).
4. Entrar cuando no hay posición abierta:
   - **Largo**: vela anterior alcista, mínimo por debajo de MA, Chaikin por debajo de `-OpenLevel`.
   - **Corto**: vela anterior bajista, máximo por encima de MA, Chaikin por encima de `OpenLevel`.
5. Cuando existe una posición y aparece una configuración opuesta, el algoritmo revierte la posición neta (`SellMarket` / `BuyMarket` con volumen extra) para reflejar el comportamiento de cobertura de la versión MT5.
6. Los stops y objetivos se emulan dentro de la estrategia usando máximos/mínimos de velas, porque StockSharp trabaja con posiciones netas en lugar de tickets individuales cubiertos.

## Gestión de riesgos
- **Stop-loss y take-profit**: distancias en pips convertidas a través del paso de precio del instrumento. Ambos pueden deshabilitarse con cero.
- **Break-even**: una vez que el precio se mueve `BreakevenPips` a favor, el stop se mueve al precio de entrada.
- **Trailing**: después de que el movimiento supera `BreakevenPips + TrailingStopPips`, el stop sigue al precio a la distancia de trailing.
- **Restablecimiento del estado de posición**: cada vez que ocurre una salida, todos los niveles de precio internos se limpian para prepararse para el próximo trade.

## Parámetros
| Nombre | Descripción |
| ------ | ----------- |
| `OpenLevel` | Magnitud del Chaikin requerida para abrir una nueva posición (por defecto 100). |
| `CloseLevel` | Magnitud del Chaikin requerida para revertir una posición existente (por defecto 125). |
| `StopLossPips` | Distancia del stop-loss en pips (por defecto 65). |
| `TakeProfitPips` | Distancia del take-profit en pips (por defecto 100). |
| `TrailingStopPips` | Distancia de trailing en pips (por defecto 30). |
| `BreakevenPips` | Ganancia en pips antes de mover el stop a break-even (por defecto 15). |
| `MaPeriod` | Longitud de la media móvil para el filtro de precio (por defecto 20). |
| `MaShift` | Barras para desplazar la media móvil (por defecto 0). |
| `MaType` | Tipo de media móvil (Simple, Exponential, Smoothed, Weighted). |
| `ChaikinFastPeriod` | Longitud de suavizado rápido en el oscilador Chaikin (por defecto 3). |
| `ChaikinSlowPeriod` | Longitud de suavizado lento en el oscilador Chaikin (por defecto 10). |
| `ChaikinMaType` | Tipo de media móvil usada para el suavizado de Chaikin. |
| `CandleType` | Serie de velas usada para cálculos. |

## Notas
- Configurar la propiedad base `Volume` en StockSharp para controlar el tamaño de la operación.
- Los pips se calculan usando el `PriceStep` del instrumento. Si el paso corresponde a cotizaciones de 3 o 5 decimales (p.ej., 0.00001), la estrategia lo multiplica por 10 para coincidir con el espaciado de pips de MetaTrader.
- Debido a que StockSharp usa posiciones netas, las órdenes de cobertura del asesor experto MQL original se representan como reversiones inmediatas de la posición existente.
