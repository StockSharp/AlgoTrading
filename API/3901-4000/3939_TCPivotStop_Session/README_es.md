# Estrategia de detención de sesión de TCPivot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia TCPivot Session Stop es un puerto directo del MetaTrader 4 asesor experto `gpfTCPivotStop`. Opera alrededor del clásico nivel de pivote diario calculado a partir del día de negociación anterior. La estrategia:

- Calcula el punto de pivote, tres niveles de resistencia y tres niveles de soporte a partir del máximo, mínimo y cierre del día anterior.
- Espera a que el cierre actual cruce el nivel de pivote desde abajo (configuración larga) o desde arriba (configuración corta).
- Abre una posición de mercado en la dirección de la ruptura y asigna un límite de pérdidas y una toma de ganancias en el nivel de pivote seleccionado.
- Opcionalmente, fuerza el cierre de la posición al comienzo de una hora de sesión específica para emular la salida intradiaria original.

La implementación se basa en el StockSharp nivel alto API. Las posiciones se dimensionan con la propiedad `Volume` de la clase base `Strategy`.

## Parámetros

| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `TargetLevel` | Nivel de pivote utilizado para detener pérdidas y obtener ganancias (1, 2 o 3). | `1` |
| `CloseAtSessionStart` | Si está habilitado, cierra posiciones abiertas cuando comienza la hora configurada. | `false` |
| `SessionCloseHour` | Hora de sesión (0-23) evaluada cuando `CloseAtSessionStart` está habilitado. | `0` |
| `CandleType` | Marco de tiempo que alimenta las señales comerciales. | `H1` |

## Lógica de trading

1. Suscríbase a velas horarias (o configuradas) para señales y velas diarias para cálculo de pivote.
2. Al finalizar cada vela diaria, calcule los niveles de pivote clásicos:
   - `Pivot = (High + Low + Close) / 3`
   - `R1 = 2 * Pivot - Low`, `S1 = 2 * Pivot - High`
   - `R2 = Pivot + (R1 - S1)`, `S2 = Pivot - (R1 - S1)`
   - `R3 = High + 2 * (Pivot - Low)`, `S3 = Low - 2 * (High - Pivot)`
3. Cuando termina una vela de señal:
   - Si `CloseAtSessionStart` está habilitado y la vela se abre en `SessionCloseHour`, aplana la posición inmediatamente.
   - Si es plano y el cierre anterior estuvo por debajo del pivote mientras que el cierre actual está por encima de él, ingrese largo con el objetivo/parada seleccionado por `TargetLevel`.
   - Si está plano y el cierre anterior estuvo por encima del pivote mientras que el cierre actual está por debajo de él, entre en corto con el objetivo/stop reflejado.
   - Si ya está en una posición, salga cuando el cierre toque el nivel de stop-loss o take-profit configurado.

## Notas

- La estrategia utiliza `StartProtection()` para integrarse con los controles de riesgo integrados de la plataforma. Las salidas de stop-loss y take-profit se manejan explícitamente dentro de la lógica de la estrategia.
- La versión MetaTrader incluía notificaciones por correo electrónico opcionales y un tamaño de posición dinámico basado en el riesgo de la cuenta. Estas funciones no forman parte del puerto StockSharp; utilice los módulos de notificación y administración de dinero de la plataforma si es necesario.
- El asesor experto original cerró las operaciones a medianoche cuando `isTradeDay` estaba habilitado. Este comportamiento se reproduce mediante `CloseAtSessionStart` + `SessionCloseHour` (establecido en `0` para imitar la medianoche).
