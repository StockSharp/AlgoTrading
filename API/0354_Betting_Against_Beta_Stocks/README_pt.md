# Apostando Contra o Beta em Ações
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Betting Against Beta Stocks** compra o decil de menor beta de um universo de ações e vende a descoberto o decil de maior beta. O rebalanceamento ocorre no primeiro dia de negociação de cada mês.

A abordagem visa explorar a anomalia de que ações de baixo beta tendem a superar em base ajustada ao risco. Assume-se acesso a um título de referência para cálculos de beta.

## Detalhes
- **Critérios de entrada**: Seleção mensal de ações de baixo/alto beta.
- **Comprado/Vendido**: Ambos os direções.
- **Critérios de saída**: As posições são ajustadas no próximo rebalanceamento.
- **Stops**: Sem lógica de stop explícita.
- **Valores padrão**:
  - `WindowDays = 252`
  - `Deciles = 10`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
  - `MinTradeUsd = 100`
- **Filtros**:
  - Categoria: Estatístico
  - Direção: Ambos
  - Indicadores: Beta
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
