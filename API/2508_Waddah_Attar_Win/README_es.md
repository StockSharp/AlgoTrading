# Estrategia Waddah Attar Win
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia refleja el asesor experto original Waddah Attar Win. Mantiene continuamente una cuadrícula simétrica de órdenes límite de compra y venta espaciadas a un número fijo de puntos desde el bid/ask actual. Siempre que el precio de mercado se acerca a la última orden enviada, la estrategia apila un nuevo límite a la misma distancia con un incremento de volumen opcional. La ganancia flotante se monitorea en cada actualización del libro de órdenes y todas las posiciones junto con las órdenes pendientes se cierran una vez que se alcanza el objetivo de ganancia configurado en la moneda de la cuenta.

## Detalles

- **Criterios de entrada**:
  - Buy-limit inicial colocado `Step Points` por debajo del bid y sell-limit colocado la misma distancia por encima del ask.
  - Se añaden órdenes pendientes adicionales cuando el precio se acerca a menos de cinco pasos de precio de la última orden en cada lado.
- **Largo/Corto**: Ambos, cuadrícula con cobertura.
- **Criterios de salida**:
  - Cerrar todas las posiciones y cancelar órdenes una vez que el patrimonio supere el balance almacenado en `Min Profit`.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Step Points` = 20
  - `First Volume` = 0.1
  - `Increment Volume` = 0.0
  - `Min Profit` = 910
- **Notas**:
  - Funciona con portafolios de cobertura porque las posiciones largas y cortas pueden coexistir.
  - Usa datos del libro de órdenes para reaccionar inmediatamente a los cambios en bid/ask.
