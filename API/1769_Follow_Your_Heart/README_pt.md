# Estratégia Follow Your Heart
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma portagem para StockSharp do assessor especialista MetaTrader "Follow Your Heart". Ela analisa as últimas várias velas e soma as variações relativas dos preços de abertura, fechamento, máximo e mínimo. Uma posição comprada é aberta quando todas as variações estão acima de um limiar e o valor combinado é positivo. Uma posição vendida é aberta nas condições opostas. Apenas uma posição pode existir de cada vez.

As posições são protegidas por limites de lucro e perda medidos em moeda da conta e por take-profit/stop-loss em pontos. Sessões de trading opcionais permitem sinais apenas dentro de horas especificadas.

## Parâmetros
- `Bars` – número de velas usadas para acumular variações de preço. Padrão: 6.
- `Level` – limiar para variações de abertura e fechamento. Padrão: 2.3.
- `ProfitBuy` – alvo de lucro monetário para sair da posição comprada. Padrão: 75.
- `ProfitSell` – alvo de lucro monetário para sair da posição vendida. Padrão: 56.
- `LossBuy` – limiar de perda monetária para sair da posição comprada. Padrão: -54.
- `LossSell` – limiar de perda monetária para sair da posição vendida. Padrão: -51.
- `TakeProfit` – take profit em pontos. Padrão: 550.
- `StopLoss` – stop loss em pontos. Padrão: 550.
- `TradingHoursOn` – ativar filtragem por sessão. Padrão: true.
- `OpenHourBuy` / `CloseHourBuy` – horas permitidas para sinais de compra. Padrão: 6 / 12.
- `OpenHourSell` / `CloseHourSell` – horas permitidas para sinais de venda. Padrão: 4 / 10.
- `CandleType` – período de velas. Padrão: 1 minuto.

## Lógica da estratégia
1. Para cada vela concluída calcula-se a variação relativa de abertura, fechamento, máximo e mínimo em comparação com a vela anterior e atualizam-se as somas móveis.
2. Se não existe nenhuma posição:
   - **Compra** quando a soma total é positiva, as variações de abertura e fechamento estão acima de `Level`, e a variação de fechamento é maior que a de abertura durante a sessão de compra.
   - **Venda** quando a soma total é negativa, as variações de abertura e fechamento estão abaixo de `-Level`, e a variação de fechamento é menor que a de abertura durante a sessão de venda.
3. Quando existe uma posição, ela é encerrada se o lucro ou a perda ultrapassar os limites monetários configurados ou se o preço se mover `TakeProfit`/`StopLoss` pontos.

## Notas
- Apenas ordens a mercado são usadas.
- O gerenciamento monetário do código original é simplificado; o volume da posição é obtido da propriedade `Volume` da estratégia.
