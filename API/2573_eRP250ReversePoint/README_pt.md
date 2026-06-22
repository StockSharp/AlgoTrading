# Estratégia eRP250ReversePoint
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma portagem para StockSharp do consultor especialista do MetaTrader 5 `e_RP_250`. O sistema original opera reversões detectadas por um indicador personalizado *rPoint*. Como esse indicador não está disponível no StockSharp, a conversão recria o mesmo comportamento com rastreadores de preço mais alto e mais baixo em janela móvel. Quando um novo swing high ou swing low aparece, a estratégia inverte a posição e anexa a mesma lógica de stop-loss, take-profit e trailing opcional que a versão MQL.

O código fonte original não publicou resultados de desempenho verificados, portanto você deve realizar sua própria avaliação antes de implantar a estratégia em produção.

## Lógica de trading

- Subscrever velas definidas pelo parâmetro `CandleType` (velas de 5 minutos por padrão).
- Rastrear o maior máximo e o menor mínimo nas últimas `ReversePoint` barras (250 por padrão).
- Quando a vela atual estabelece um novo maior máximo, fechar qualquer posição comprada e abrir uma posição vendida.
- Quando a vela atual estabelece um novo menor mínimo, fechar qualquer posição vendida e abrir uma posição comprada.
- Os níveis protetores de stop-loss e take-profit são expressos em pontos de preço e são reproduzidos através de `StartProtection`.
- Stops trailing opcionais bloqueiam lucros uma vez que o preço se move pelo número de pontos configurado.

Apenas uma posição está ativa a qualquer momento. A estratégia também bloqueia ordens duplicadas durante a mesma vela ao lembrar o último tempo de execução, replicando a proteção `TimeN` do script MQL.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `TakeProfitPoints` | Distância em pontos de preço para a ordem de take-profit (padrão **15**). Definir como zero para desabilitar a tomada de lucro automática. |
| `StopLossPoints` | Distância em pontos de preço para a ordem de stop-loss (padrão **999**). Definir como zero para operar sem stop fixo. |
| `TrailingStopPoints` | Distância opcional de stop trailing em pontos de preço (padrão **0** desabilita a lógica de trailing). |
| `ReversePoint` | Número de velas usadas para detectar pontos de reversão. Valores maiores reagem mais lentamente mas filtram o ruído. |
| `CandleType` | Agregação de velas a analisar. O padrão é um período de 5 minutos mas pode ser alterado para qualquer `DataType`. |

## Gestão de posição

- `StartProtection` aplica as mesmas distâncias de stop-loss e take-profit que o especialista MT5.
- O stop trailing rastreia o preço mais favorável após a entrada e sai quando o preço reverte pelo valor configurado.
- Sinais de reversão do lado oposto fecham imediatamente a posição atual antes de abrir uma nova.

## Notas de uso

- Certifique-se de que a fonte de dados suporta o tipo de vela selecionado, caso contrário nenhum sinal será gerado.
- A estratégia depende de preços decimais. Verifique se a propriedade `PriceStep` do ativo reflete corretamente o valor do ponto.
- Teste diferentes valores de `ReversePoint` para adaptar a sensibilidade ao rompimento à volatilidade do instrumento operado.
