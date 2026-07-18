# Estrategia envolvente de confirmación de las IMF
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el experto MetaTrader "Expert_ABE_BE_MFI" al combinar patrones envolventes de velas japonesas con la confirmación del oscilador del índice de flujo de dinero (MFI). Se abre una posición larga cuando aparece una vela envolvente alcista mientras el flujo de dinero permanece en una zona de sobreventa. Se abre una posición corta cuando se forma una vela envolvente bajista en condiciones de flujo de dinero de sobrecompra. Las posiciones se cierran cuando las IMF cruzan los umbrales de salida dinámicos, lo que indica cambios de impulso.

## Idea central

1. **Detección de patrón**: el cuerpo de la vela terminada actual debe envolver completamente la vela anterior en la dirección de la operación.
2. **Confirmación de volumen**: el indicador MFI (longitud configurable, predeterminado 37) debe estar por debajo del nivel de sobreventa (40) para entradas largas o por encima del nivel de sobrecompra (60) para entradas cortas.
3. **Salidas de impulso**: las posiciones abiertas se cierran cuando MFI cruza niveles de reversión clave (30 y 70) en la dirección opuesta, imitando la lógica de votación original del experto MQL.

## Indicadores

- **Money Flow Index (MFI)** – calculates volume-adjusted momentum. La estrategia almacena las dos últimas lecturas de MFI para detectar pasos a nivel.
- **Análisis del cuerpo de velas** – no se registra ningún indicador adicional; La detección envolvente utiliza las dos últimas velas completadas.

## Reglas de trading

### Entrada larga

- La vela anterior es bajista y la vela actual es alcista.
- El cuerpo de la vela actual se abre por debajo o igual al cierre anterior y cierra por encima o igual al cierre anterior (envolvente estricta).
- El último valor de MFI está por debajo del `OversoldLevel` configurable (predeterminado 40).

### Entrada corta

- La vela anterior es alcista y la vela actual es bajista.
- El cuerpo de la vela actual se abre por encima o igual al cierre anterior y cierra por debajo o igual al cierre anterior.
- Latest MFI value is above the configurable `OverboughtLevel` (default 60).

### Condiciones de salida

- **Cierre Corto** cuando MFI cruza por encima de `ExitLongLevel` (30) o `ExitShortLevel` (70) desde abajo.
- **Cierre largo** cuando MFI cruza por debajo de `ExitShortLevel` (70) o `ExitLongLevel` (30) desde arriba.

Estos umbrales de salida recrean la lógica de doble votación del experto original, asegurando que movimientos prolongados en el flujo de dinero desencadenen la liquidación oportuna de posiciones.

### Gestión Comercial

- Las órdenes de mercado (`BuyMarket` / `SellMarket`) se utilizan para entradas y salidas.
- No se utiliza ningún límite de pérdidas ni toma de ganancias explícito; La gestión de riesgos se basa en las señales de reversión de las IMF.

## Parámetros

| Nombre | Descripción | Predeterminado | Rango / Notas |
| ---- | ----------- | ------- | ------------- |
| `CandleType` | Plazo de vela utilizado para el análisis. | 1 minuto | Cualquier tipo de vela admitida. |
| `MfiPeriod` | Longitud del índice de flujo de dinero. | 37 | Debe ser > 0; coincide con el valor predeterminado original EA. |
| `OversoldLevel` | Nivel de IMF que confirma configuraciones envolventes alcistas. | 40 | Habilite la optimización si es necesario. |
| `OverboughtLevel` | MFI level that confirms bearish engulfing setups. | 60 | Habilite la optimización si es necesario. |
| `ExitLongLevel` | Límite inferior de las IMF para detectar reversiones. | 30 | Se utiliza tanto para salidas largas como para confirmaciones cortas. |
| `ExitShortLevel` | Límite superior de la IMF para detectar reversiones. | 70 | Se utiliza tanto para salidas cortas como para confirmaciones largas. |

## Notas sobre la conversión

- El experto original de MQL agregó “votos” de patrones envolventes y filtros de IMF. La estrategia de C# reproduce el mismo flujo de decisiones al convertir directamente las reglas de votación en condiciones de entrada y salida discretas.
- Money management and trailing modules from the MQL version are omitted; El tamaño de la posición StockSharp está controlado por el volumen de la estrategia.
- Todos los enlaces de indicadores aprovechan el API (`SubscribeCandles().Bind(...)`) de alto nivel según sea necesario.

## Consejos de uso

- Optimice `MfiPeriod`, `OversoldLevel` y `OverboughtLevel` para adaptar la estrategia a mercados específicos.
- Combínelo con controles de riesgo (paradas de protección) a través de `StartProtection` en la aplicación host si se requiere seguridad adicional.
- Asegúrese de tener suficientes datos históricos para que el índice de flujo de dinero esté completamente formado antes de permitir el comercio.
