# Estrategia de sincronización de símbolos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de sincronización de símbolos** replica la utilidad MetaTrader `SymbolSyncEA` dentro del entorno StockSharp. La estrategia mantiene sincronizados el símbolo de la estrategia principal y todas las estrategias vinculadas registradas. Cada vez que cambia el símbolo principal, la estrategia propaga automáticamente la nueva seguridad a cada estrategia vinculada, asegurando que todo el espacio de trabajo siga el mismo instrumento sin intervención manual.

## Ideas centrales
- Capture la seguridad de la estrategia inicial al inicio y reutilícela como opción alternativa.
- Mantenga una lista configurable de estrategias vinculadas que siempre deben reflejar la seguridad principal.
- Permitir cambios de símbolos desencadenados por una asignación directa de `Security` o especificando un nuevo identificador de seguridad.
- Proporcione operaciones de sincronización y restablecimiento manual para que coincidan con el comportamiento original del Asesor Experto.

## Parámetros
| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `ChartLimit` | Número máximo de estrategias vinculadas que se pueden sincronizar. Evita actualizaciones masivas accidentales. | `10` |
| `SyncSecurityId` | Identificador del valor propagado a estrategias vinculadas. Un valor vacío vuelve a la seguridad de la estrategia. | `""` |

## Métodos públicos
- `RegisterLinkedStrategy(Strategy strategy)`: agrega una instancia de estrategia a la lista de sincronización. Devuelve `true` cuando se registra correctamente.
- `UnregisterLinkedStrategy(Strategy strategy)`: elimina una estrategia de la lista.
- `ChangeSyncSecurity(Security security)`: cambia a la instancia de seguridad proporcionada y la propaga a cada estrategia vinculada.
- `ChangeSyncSecurity(string securityId)` – resuelve el identificador a través del `SecurityProvider` actual y llama a `ChangeSyncSecurity(Security)`.
- `ResetToInitialSecurity()`: restaura el símbolo capturado al inicio.
- `SyncSymbols()`: fuerza una resincronización manual sin cambiar el identificador almacenado.

## Flujo de trabajo de uso
1. Cree una instancia de `SymbolSyncStrategy` y establezca el `Security` principal o asigne `SyncSecurityId` antes de comenzar la estrategia.
2. Llame a `RegisterLinkedStrategy` para cada estrategia secundaria que deba reflejar el símbolo activo (por ejemplo, diferentes períodos de tiempo o paneles).
3. Siempre que el símbolo principal cambie, llame a `ChangeSyncSecurity(Security)` o `ChangeSyncSecurity(string)`.
4. Opcionalmente, llame a `SyncSymbols()` para forzar la propagación si un componente externo modificó una estrategia vinculada.

## Diferencias respecto a la versión MQL
- Funciona con StockSharp `Strategy` instancias en lugar de MetaTrader ventanas de gráficos.
- Utiliza la abstracción `SecurityProvider` para resolver identificadores.
- Agrega registro defensivo y un límite configurable para estrategias sincronizadas.
- Ofrece métodos explícitos de reinicio y sincronización manual para escenarios de automatización avanzados.

## Notas
- La estrategia no emite órdenes de mercado; Funciona como ayudante de infraestructura.
- Todos los comentarios del código se mantienen en inglés para cumplir con los requisitos del proyecto.
