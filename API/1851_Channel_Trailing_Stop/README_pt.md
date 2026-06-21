# Estratégia de Canal com Trailing Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que utiliza entradas por rompimento do canal Donchian e gestão com trailing stop.

O sistema abre operações quando o preço fecha fora do canal. Um trailing stop acompanha o lado oposto do canal mais um deslocamento. O trailing "laço" opcional mantém o stop loss à mesma distância entre o preço atual e o take profit. Ordens pendentes podem ser eliminadas após as execuções.

## Detalhes

- **Critérios de entrada**: Fechamento fora do intervalo do canal.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Trailing stop ou sinal oposto.
- **Stops**: Trailing stop, laço opcional.
- **Valores padrão**:
  - `TrailPeriod` = 5
  - `TrailStop` = 50
  - `UseNooseTrailing` = true
  - `UseChannelTrailing` = true
  - `DeletePendingOrders` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Donchian Channel
  - Stops: Trailing
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
