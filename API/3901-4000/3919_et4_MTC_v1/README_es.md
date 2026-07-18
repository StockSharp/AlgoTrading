# Estrategia Et4 MTC v1 (conversión StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- **Origen**: MetaTrader 4 asesores expertos `et4_MTC_v1.mq4` de la colección GlobeInvestFund.
- **Propósito**: Proporcionar una plantilla nativa StockSharp que refleje los asistentes de administración de dinero y las salvaguardas de tiempo del asesor original, al tiempo que deja la lógica de entrada/salida comercial abierta para un mayor desarrollo.
- **Estilo de negociación**: estrategia esqueleto: no se generan entradas automáticas de forma predeterminada. La clase se centra en hacer cumplir las restricciones de tiempo y replicar la interfaz de parámetros del script MQL4 para que pueda servir como base para reglas personalizadas.

## Características principales
1. **Paridad de parámetros**
   - Expone propiedades `TakeProfit`, `StopLoss`, `Slippage`, `Lots` y `EnableLogging` que se asignan uno a uno con las variables externas del experto.
   - Agrega `TradeCooldown` para describir el retraso codificado de 30 segundos entre operaciones en el código fuente.
   - Publica el contexto de datos del gráfico a través de `CandleType` para emular el comportamiento del "período de tiempo actual" de los gráficos MetaTrader.
2. **Tamaño de posición basado en el equilibrio**
   - Admite entradas de lotes negativos (valor predeterminado del script original) para derivar el volumen del pedido del saldo de la cuenta: `floor((balance / 1000 * |Lots|) / 10) / 10`, con un mínimo de 0,1 lote.
3. **Aplicación del enfriamiento del comercio**
   - Bloquea cualquier intento de negociación adicional hasta que transcurra `TradeCooldown` después de la actividad de orden más reciente (registro, modificación, cancelación o transacción completada). Esto refleja el guardia `CurTime() - LastTradeTime < 30` en `start()`.
4. **Detección de nueva vela**
   - Mantiene la semántica `CheckLevels` marcando `IsNewCandle` mediante una comparación de tiempo entre velas terminadas posteriores. Si bien la bandera es interna, los enlaces en `OpenPosition`, `ManagePosition` y `ClosePosition` pueden usarla cuando se agrega lógica personalizada.
5. **Uso de alto nivel de StockSharp API**
   - Utiliza `SubscribeCandles().Bind(...)` para la entrega de datos.
   - Aplica `StartProtection()` una vez al inicio, siguiendo las mejores prácticas del marco.
   - No asigna colecciones personalizadas ni solicita explícitamente el historial de indicadores, alineándose con las pautas de todo el proyecto.

## Referencia de parámetros
| Propiedad | Predeterminado | Optimizable | Descripción |
| --- | --- | --- | --- |
| `TakeProfit` | 150 | ✔️ | Distancia objetivo en puntos (marcador de posición para reglas de salida personalizadas). |
| `Lots` | -10 | ✔️ | Lotes fijos cuando ≥ 0; Dimensionamiento proporcional al equilibrio cuando es negativo. |
| `StopLoss` | 50 | ✔️ | Distancia de parada en puntos, lista para lógica de extensión. |
| `Slippage` | 3 | ✖️ | Tolerancia de ejecución en puntos; conservado por compatibilidad. |
| `EnableLogging` | `false` | ✖️ | Imprime mensajes informativos cuando el tiempo de reutilización bloquea los intercambios. |
| `TradeCooldown` | 30 segundos | ✖️ | Retraso mínimo entre operaciones consecutivas. |
| `CandleType` | Velas con marco de tiempo de 1 minuto | ✖️ | Suscripción a datos de mercado utilizada para sincronizar las velas. |

## Flujo de ejecución
1. **Inicio**
   - Calcula el `Volume` inicial utilizando el asistente de tamaño que tiene en cuenta el saldo.
   - Se suscribe al flujo de velas configurado e inicia mecanismos de protección.
2. **Al cerrar la vela**
   - Confirma que la vela ha terminado antes de continuar (equivalente al cierre de `Time[0]` en MT4).
   - Actualiza el rastreador de nuevas velas (`_isNewCandle`).
   - Comprueba `IsFormedAndOnlineAndAllowTrading()` para respetar el estado del motor.
   - Se cancela si el tiempo de reutilización del comercio está activo y registra el próximo tiempo disponible cuando está habilitado.
   - Ejecuta ganchos de marcador de posición (`OpenPosition`, `ManagePosition`, `ClosePosition`) y regresa antes de tiempo cuando cualquier paso realiza una acción.
3. **Devoluciones de llamadas de pedidos e intercambios**
   - `OnOrderRegistered`, `OnOrderChanged`, `OnOrderCanceled` y `OnNewMyTrade` actualizan `_lastTradeTime`, lo que garantiza que cada tipo de operación restablezca el tiempo de reutilización tal como lo hicieron las funciones contenedoras (`MOrderSend`, `MOrderModify`, etc.) en el código original.

## Ampliando la plantilla
- Implemente la lógica de entrada dentro de `OpenPosition` (devuelva `true` después de enviar órdenes para detener el procesamiento adicional en la misma vela).
- Inserte el comportamiento de gestión de parada dentro de `ManagePosition` utilizando los parámetros conservados.
- Complete `ClosePosition` con reglas de salida. Actualmente, el método devuelve `false` para coincidir con el comportamiento inactivo del script fuente.
- Utilice `_isNewCandle` si las reglas deben activarse una vez por barra.

## Notas de portabilidad
- El experto MQL4 envió sin reglas comerciales; sólo estaban presentes rutinas de infraestructura. En consecuencia, la conversión StockSharp prioriza la paridad de las funciones de soporte en lugar de agregar indicadores especulativos.
- Todos los comentarios están escritos en inglés, cumpliendo con los estándares del repositorio.
- Las tabulaciones se utilizan para que la sangría coincida con las pautas de estilo definidas en `AGENTS.md`.
- La traducción de Python se omite intencionalmente según la solicitud de conversión.

## Pasos de uso
1. Haga referencia a `Et4MtcV1Strategy` en un proyecto StockSharp y asigne un `Security` y `Portfolio` antes de comenzar.
2. Ajuste `Lots` u otros parámetros a través de las propiedades proporcionadas o enlaces de interfaz de usuario.
3. Anule los métodos de marcador de posición o herede de la clase para inyectar una lógica comercial concreta.
4. Ejecute la estrategia; el protector de enfriamiento garantiza que no haya operaciones consecutivas dentro del intervalo especificado.

## Pruebas
- No hay pruebas automatizadas que acompañen a esta plantilla porque la fuente ascendente también carecía de reglas ejecutables. Las extensiones de estrategias manuales deberían introducir pruebas relevantes cuando se implemente un comportamiento comercial concreto.
