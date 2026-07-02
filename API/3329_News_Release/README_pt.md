# Estratégia News Release
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reproduz o comportamento central do expert advisor original **NewsReleaseEA** preparando um bracket de ordens pendentes ao redor de uma notícia agendada e gerenciando ativamente a posição resultante.

## Ideias-chave

- Cinco entradas (hora da notícia, janelas antes/depois, distâncias das ordens e espaçamento) definem quando e onde as ordens stop são colocadas.
- Um conjunto simétrico de ordens buy stop e sell stop é enviado pouco antes da hora configurada da notícia. O primeiro par é colocado a `DistancePips` do ask/bid atual e pares adicionais são deslocados por `StepPips`.
- Ordens pendentes permanecem ativas até `PostNewsMinutes` minutos após o evento. Ao fim da janela, a estratégia cancela toda ordem ativa e, se solicitado, fecha qualquer posição aberta.
- Quando uma ordem é executada, as ordens pendentes opostas são canceladas automaticamente e a posição aberta é gerenciada por regras de stop-loss, take-profit, break-even e trailing expressas em pips.
- A proteção break-even arma depois que o preço se move `BreakEvenTriggerPips` a favor da posição e força uma saída se o preço retornar ao preço de entrada mais `BreakEvenOffsetPips` (compras) ou menos esse offset (vendas).
- A gestão de trailing acompanha o melhor preço alcançado após a entrada. Quando a distância entre o preço atual e o extremo excede `TrailingPips`, a posição é fechada para proteger o lucro acumulado.
- A flag `TradeOnce` espelha o comportamento "trade one time per news" do programa MQL impedindo uma segunda ativação após a primeira operação ter sido concluída.

## Parâmetros

- `NewsTime`: hora agendada da notícia.
- `PreNewsMinutes`: quantos minutos antes da notícia as ordens pendentes são colocadas.
- `PostNewsMinutes`: quantos minutos depois da notícia as ordens pendentes permanecem vivas antes do cancelamento.
- `OrderPairs`: número de pares buy stop/sell stop que formam o bracket.
- `DistancePips`: distância em pips do primeiro par em relação ao melhor ask/bid no momento da colocação.
- `StepPips`: espaçamento adicional em pips entre pares consecutivos.
- `OrderVolume`: volume enviado com cada ordem pendente.
- `TradeOnce`: se habilitado, a estratégia pode operar apenas uma vez por janela de evento.
- `UseStopLoss` / `StopLossPips`: habilita e configura a distância do stop-loss em pips.
- `UseTakeProfit` / `TakeProfitPips`: habilita e configura a distância do take-profit em pips.
- `UseBreakEven`, `BreakEvenTriggerPips`, `BreakEvenOffsetPips`: configura o módulo break-even.
- `UseTrailing` / `TrailingPips`: habilita a lógica de saída trailing e define a distância em pips.
- `CloseAfterEvent`: fecha qualquer posição aberta quando a janela pós-notícia termina.

## Notas

- A estratégia trabalha exclusivamente com dados level1 (`SubscribeLevel1`) para reagir aos preços bid/ask mais recentes sem aguardar candles.
- Distâncias de preço expressas em pips são convertidas para preços absolutos usando o `PriceStep` do instrumento. Se `PriceStep` estiver indisponível, o valor 1 é usado como fallback seguro.
- Condições de stop-loss, take-profit, break-even e trailing fecham a posição a mercado chamando `ClosePosition()`. Isso espelha a gestão reativa do expert original mantendo a implementação compacta.
- Nenhuma versão Python é fornecida, conforme solicitado.
