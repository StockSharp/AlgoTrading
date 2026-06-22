# Estrategia de Cobertura HTH Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es una conversión directa del asesor experto MetaTrader "HTH Trader". Opera una cesta forex de cuatro tramos e intenta capturar la reversión a la media diaria entre EURUSD y una cesta espejo de USDCHF, GBPUSD y AUDUSD. El puerto de StockSharp mantiene los controles de riesgo originales y las reglas de tiempo, usando la API de alto nivel para el trading multi-activo.

Características clave:

- Abre una cesta cubierta una vez al día entre las 00:05 y las 00:12 hora terminal.
- Usa los dos cierres diarios anteriores de EURUSD para decidir la dirección de la cesta.
- Gestiona cuatro instrumentos simultáneamente: EURUSD (seguridad primaria), USDCHF, GBPUSD y AUDUSD.
- Rastrea la ganancia abierta en pips y soporta objetivos de ganancia y pérdida para toda la cesta.
- Incluye una función de duplicación de emergencia que agrega a las patas rentables cuando el drawdown de la cesta supera un umbral.
- Cierra todas las operaciones a las 23:00 hora terminal o cuando la cesta alcanza los límites configurados de ganancia/pérdida.

## Requisitos de datos

- **Velas intradía**: Los cuatro símbolos deben entregar velas intradía para el marco temporal configurado en `IntradayCandleType` (por defecto 5 minutos). Estas velas proporcionan el precio más reciente y el reloj de sesión.
- **Velas diarias**: Cada símbolo debe proporcionar velas diarias para que la estrategia pueda monitorear los últimos dos cierres diarios completados.

## Lógica de trading

1. Al final de cada vela intradía finalizada, la estrategia verifica la ganancia abierta actual:
   - Si `AllowEmergencyTrading` está habilitado y la ganancia abierta total ≤ `-EmergencyLossPips`, la estrategia duplica cada pata que esté actualmente en ganancia y deshabilita más operaciones de emergencia para ese día.
   - Si `UseProfitTarget` está habilitado y la ganancia abierta total ≥ `ProfitTargetPips`, la cesta se cierra inmediatamente.
   - Si `UseLossLimit` está habilitado y la ganancia abierta total ≤ `-LossLimitPips`, la cesta se cierra inmediatamente.
2. Una vez que el reloj alcanza las 23:00, la cesta se cierra independientemente de la ganancia.
3. Cuando no hay posiciones abiertas y el reloj está dentro de la ventana 00:05–00:12, la estrategia verifica los últimos dos cierres diarios completados del símbolo primario (EURUSD por defecto):
   - Si el cambio porcentual día a día es **positivo**, la estrategia abre: largo EURUSD, largo USDCHF, corto GBPUSD, largo AUDUSD.
   - Si el cambio es **negativo**, abre: corto EURUSD, corto USDCHF, largo GBPUSD, corto AUDUSD.
   - Si el cambio es cero o falta algún cierre diario, la estrategia omite el trading por ese día.
4. Todas las posiciones se cierran usando órdenes de mercado a través de `ClosePosition`.

## Parámetros

| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `TradeEnabled` | Habilita o deshabilita la colocación de órdenes. | `true` |
| `ShowProfitInfo` | Registra la ganancia de la cesta en pips en cada actualización mientras hay posiciones abiertas. | `true` |
| `UseProfitTarget` | Habilita el cierre automático cuando se alcanza `ProfitTargetPips`. | `false` |
| `UseLossLimit` | Habilita el cierre automático cuando se alcanza `LossLimitPips`. | `false` |
| `AllowEmergencyTrading` | Permite la función de duplicación de emergencia. | `true` |
| `EmergencyLossPips` | Drawdown de la cesta (en pips) que activa la duplicación de emergencia. | `60` |
| `ProfitTargetPips` | Ganancia de la cesta (en pips) que activa el cierre cuando `UseProfitTarget` está habilitado. | `80` |
| `LossLimitPips` | Pérdida de la cesta (en pips) que activa el cierre cuando `UseLossLimit` está habilitado. | `40` |
| `TradingVolume` | Volumen de orden para cada pata. | `0.01` |
| `Symbol2` | Segunda seguridad (USDCHF por defecto). | `null` |
| `Symbol3` | Tercera seguridad (GBPUSD por defecto). | `null` |
| `Symbol4` | Cuarta seguridad (AUDUSD por defecto). | `null` |
| `IntradayCandleType` | Marco temporal intradía usado para programación y actualizaciones de precio. | Velas de `5` minutos |

## Notas de uso

- Asigne la seguridad primaria (`Strategy.Security`) a EURUSD (o el par líder deseado) y mapee `Symbol2`, `Symbol3`, `Symbol4` a los instrumentos correlacionados antes de iniciar.
- Asegúrese de que cada seguridad tenga un `PriceStep` válido; de lo contrario, los cálculos de ganancia en pips no pueden realizarse y la lógica de emergencia permanecerá inactiva.
- La función de duplicación de emergencia solo agrega a las patas que actualmente son rentables; las patas perdedoras se dejan intactas para evitar amplificar el drawdown.
- La implementación asume que las órdenes de mercado se ejecutan cerca del último cierre de vela. Para una contabilización precisa, conecte la estrategia a un feed de datos que entregue velas intradía oportunas.
- Dado que la lógica es impulsada por una sola barra por minuto (o marco temporal elegido), el comportamiento original tick a tick de MQL puede diferir ligeramente en el tiempo de ejecución, pero la secuencia y condiciones de operación coinciden con el asesor experto de referencia.
