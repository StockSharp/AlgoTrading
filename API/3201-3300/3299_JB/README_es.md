# Estrategia JB
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen

La estrategia JB procede de un asesor experto de fxDreema que combina filtros de tendencia de largo plazo, confirmación de impulso y rupturas de volatilidad:

- **Filtro de tendencia:** exige que el cierre de la vela anterior permanezca por encima (largos) o por debajo (cortos) de una media móvil simple de 100 periodos.
- **Filtro de impulso:** confirma la dirección con un Force Index de 100 periodos (positivo para largos, negativo para cortos).
- **Disparador de volatilidad:** entra cuando el cierre anterior perfora la banda de Bollinger correspondiente (20 periodos, desviación 2,0).
- **Gestión de posición:** aumenta el volumen de la orden con un multiplicador de estilo martingala después de un ciclo perdedor y vuelve al tamaño base después de ciclos rentables.
- **Regla de salida:** cierra todas las posiciones abiertas cuando el beneficio no realizado medio por contrato alcanza un objetivo monetario configurable.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| `SmaPeriod` | Longitud del filtro de tendencia SMA. Predeterminado: 100. |
| `ForcePeriod` | Longitud del indicador Force Index. Predeterminado: 100. |
| `BollingerPeriod` | Longitud de las bandas de Bollinger. Predeterminado: 20. |
| `BollingerDeviation` | Multiplicador de desviación estándar para las bandas de Bollinger. Predeterminado: 2,0. |
| `BaseVolume` | Volumen inicial de orden antes de los ajustes de martingala. Predeterminado: 0,1. |
| `LossMultiplier` | Multiplicador aplicado al siguiente volumen de orden después de un ciclo perdedor. Predeterminado: 1,55. |
| `AverageProfitTarget` | Beneficio no realizado medio por contrato necesario para cerrar todas las posiciones. Predeterminado: 2,8. |
| `CandleType` | Tipo de vela utilizado para los cálculos (por defecto, marco temporal de 1 minuto). |

## Señales

### Entrada larga
1. El cierre de la vela anterior está por debajo o igual a la banda de Bollinger inferior.
2. El cierre anterior es mayor que la SMA de 100 periodos (tendencia al alza).
3. El valor del Force Index es positivo.

### Entrada corta
1. El cierre de la vela anterior está por encima o igual a la banda de Bollinger superior.
2. El cierre anterior es menor que la SMA de 100 periodos (tendencia a la baja).
3. El valor del Force Index es negativo.

### Salidas
- Cuando el beneficio no realizado medio por contrato de todas las posiciones abiertas alcanza `AverageProfitTarget`, todas las posiciones se cierran a mercado.
- Después de cada posición plana, la estrategia ajusta el siguiente volumen de orden: lo multiplica por `LossMultiplier` tras un ciclo perdedor y lo reinicia a `BaseVolume` tras un ciclo rentable.

## Notas

- La adaptación de martingala usa el PnL realizado para decidir cuándo se produjo una racha de pérdidas; asegúrese de usar la estrategia solo en instrumentos donde sea aceptable aumentar el volumen.
- Como las estrategias de StockSharp trabajan con posiciones netas, la cobertura de la versión MQL (cestas largas y cortas simultáneas) se aproxima mediante posiciones agregadas.
