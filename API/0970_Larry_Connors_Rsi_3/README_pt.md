# Larry Connors RSI 3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de reversão à média baseada nas regras RSI de Larry Connors.

A estratégia compra quando o preço está acima da SMA de 200 períodos e o RSI de 2 períodos caiu três dias consecutivos de acima de um nível de gatilho para território de sobrevenda. As posições são encerradas quando o RSI sobe acima do nível de sobrecompra.

## Detalhes

- **Critérios de entrada**: Fechamento acima da SMA e RSI de 2 períodos caindo três dias de acima do gatilho para a sobrevenda.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: RSI acima do nível de sobrecompra.
- **Stops**: Não.
- **Valores padrão**:
  - `RsiPeriod` = 2
  - `SmaPeriod` = 200
  - `DropTrigger` = 60m
  - `OversoldLevel` = 10m
  - `OverboughtLevel` = 70m
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Comprado
  - Indicadores: RSI, SMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
