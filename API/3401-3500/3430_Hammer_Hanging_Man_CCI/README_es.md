# Martillo y hombre colgado con confirmación de CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia reimplementa al experto MetaTrader "AH HM CCI" en StockSharp. Vela por martillo y candelero ahorcado
patrones y requiere confirmación del índice del canal de productos básicos (CCI) antes de ingresar a una operación. Los filtros de confirmación adicionales
detecta patrones débiles y ayuda a alinear las entradas con el cambio de impulso señalado por CCI.

La lógica se ejecuta únicamente con velas completadas y utiliza un promedio móvil simple corto (SMA) para definir la tendencia predominante. el anterior
La vela debe ser un martillo en una tendencia bajista con sobrecompra CCI para comprar, o un ahorcado en una tendencia alcista con sobrecompra CCI para vender. Salidas
se administran cuando CCI cruza niveles de activación configurables, replicando la lógica de salida basada en votos del experto original.

## Lógica de trading

1. **Filtro de tendencia**: el punto medio de la vela anterior debe estar por debajo (para largos) o por encima (para cortos) de un SMA calculado en
precios de cierre. Esto imita la comprobación de tendencia de media móvil del asistente original.
2. **Detección de patrones** – La estrategia evalúa la barra anterior y comprueba:
   - Cuerpo enteramente en el tercio superior del rango de velas.
   - Brecha entre la apertura/cierre de la vela anterior y la vela anterior.
   - Contexto direccional (martillo para una tendencia bajista, hombre colgado para una tendencia alcista).
3. **Confirmación de CCI**: el CCI de la barra anterior debe estar por debajo del umbral largo o por encima del umbral corto. Los valores predeterminados
coincide con la plantilla MetaTrader (40 para largos y 60 para cortos).
4. **Salidas de posiciones**: las posiciones existentes se cierran cuando CCI cruza los umbrales de salida inferior o superior. Cruzando desde
abajo cierra largos; cruzar desde arriba cierra los pantalones cortos.

## Parámetros

| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `CandleType` | Tipo de vela y período de tiempo utilizados para el reconocimiento de patrones. | `TimeSpan.FromMinutes(15)` |
| `CciPeriod` | Número de barras utilizadas por el Commodity Channel Index. | `11` |
| `MaPeriod` | Número de barras en el filtro de tendencia SMA. | `5` |
| `LongConfirmationThreshold` | Valor máximo CCI permitido para una señal de martillo. | `40` |
| `ShortConfirmationThreshold` | Valor mínimo CCI permitido para una señal de ahorcado. | `60` |
| `ExitUpperThreshold` | CCI nivel que desencadena salidas tras un cruce alcista. | `70` |
| `ExitLowerThreshold` | Nivel de salida secundario para señales tempranas. | `30` |

Todos los parámetros están disponibles para su optimización. Los umbrales aceptan valores negativos, por lo que puedes adaptar la estrategia a otros
mercados o niveles de ruido apretando o aflojando los filtros.

## Gestión de órdenes

- **Las entradas** utilizan órdenes de mercado de tamaño `Volume + |Position|`, lo que garantiza que las reversiones se ejecuten en una sola operación.
- **Las salidas** dependen exclusivamente de los cruces CCI para mantenerse cerca del experto MetaTrader. Agregue `StartProtection` llamadas si lo necesita
niveles explícitos de stop-loss o take-profit.

## Consejos de uso

- Aplique la estrategia en instrumentos líquidos donde los huecos y las colas de las velas son informativos.
- Experimente con valores `CciPeriod` y `MaPeriod` más largos para suavizar el ruido al operar con marcos de tiempo más altos.
- Reducir `LongConfirmationThreshold` o aumentar `ShortConfirmationThreshold` reducirá el número de operaciones pero mejorará
selectividad.
