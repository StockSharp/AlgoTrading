# Auto Trailing Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Anexa automaticamente ordens de stop-loss e take-profit a posições existentes e move o stop à medida que o preço avança favoravelmente.

## Detalhes
- **Critérios de entrada**: Nenhum, a estratégia não abre operações.
- **Comprado/Vendido**: Funciona com posições compradas e vendidas já abertas.
- **Critérios de saída**: Ordens de stop-loss e take-profit. O trailing stop é atualizado após o preço mover-se pela metade da distância de trailing.
- **Stops**: Stop-loss e take-profit iniciais colocados quando a posição aparece; o stop-loss segue pelo `TrailingStopStep`.
- **Valores padrão**: TrailingStop 6, TrailingStopStep 1, TakeProfit 35, StopLoss 114.
- **Filtros**: Desativação opcional do trailing stop, take profit automático ou stop loss automático via parâmetros.

## Parâmetros
- `FridayTrade` - permitir trailing às sextas-feiras.
- `UseTrailingStop` - ativar a lógica de trailing stop.
- `AutoTrailingStop` - usar distância de trailing padrão de 6 quando verdadeiro.
- `TrailingStop` - distância de trailing em unidades de preço quando AutoTrailingStop é falso.
- `TrailingStopStep` - movimento mínimo de preço antes de mover o trailing stop.
- `AutomaticTakeProfit` - colocar automaticamente ordem de take profit.
- `TakeProfit` - distância do take profit.
- `AutomaticStopLoss` - colocar automaticamente ordem de stop loss.
- `StopLoss` - distância do stop loss.
- `CandleType` - tipo de vela para atualizações de preço (padrão 1 minuto).
