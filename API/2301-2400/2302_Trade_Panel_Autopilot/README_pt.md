# Estratégia do Painel de Operações em Piloto Automático
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia reproduz a lógica central do expert MQL4 original "trade panel with autopilot". Ela agrega a direção do preço em múltiplos períodos e abre ou fecha uma única posição de acordo com o sentimento dominante do mercado.

A estratégia monitora as duas últimas velas em oito períodos diferentes (M1, M5, M15, M30, H1, H4, D1, W1). Para cada período, compara vários componentes de preço entre as duas velas mais recentes:

- Open
- High
- Low
- (High + Low) / 2
- Close
- (High + Low + Close) / 3
- (High + Low + Close + Close) / 4

Cada comparação contribui para uma pontuação de **compra** ou **venda**. As pontuações de todos os períodos são somadas e convertidas em percentuais. Quando o percentual de compra ou venda cruza um limiar configurado, a estratégia entra em uma posição. A posição existente é fechada se o percentual oposto cair abaixo do limiar de fechamento.

## Parâmetros

- `Autopilot` — ativa ou desativa a negociação automática.
- `OpenThreshold` — nível percentual necessário para abrir uma nova posição. Padrão: 85.
- `CloseThreshold` — nível percentual para fechar uma posição existente. Padrão: 55.
- `LotFixed` — volume fixo da ordem quando `UseFixedLot` está habilitado.
- `LotPercent` — volume como percentual do valor da carteira quando `UseFixedLot` está desabilitado.
- `UseFixedLot` — alterna entre volume fixo e percentual.
- `UseStopLoss` — inicia a proteção da posição quando habilitado.

## Lógica de negociação

1. Subscrever candles em todos os períodos configurados.
2. Calcular as pontuações de compra/venda para cada novo candle concluído.
3. Somar as pontuações por períodos e calcular os percentuais de compra/venda.
4. Se `Autopilot` estiver desabilitado, a estratégia apenas rastreia as pontuações.
5. Se não houver posição aberta e o percentual de compra exceder `OpenThreshold`, entrar em uma posição comprada. Se o percentual de venda exceder o limiar, entrar em uma posição vendida.
6. Se existir uma posição comprada e o percentual de compra cair abaixo de `CloseThreshold`, sair da posição. A mesma lógica se aplica para posições vendidas usando o percentual de venda.

## Notas

- A estratégia mantém no máximo uma posição aberta por vez.
- O gerenciamento opcional de stop-loss é ativado via `StartProtection()` quando `UseStopLoss` é verdadeiro.
