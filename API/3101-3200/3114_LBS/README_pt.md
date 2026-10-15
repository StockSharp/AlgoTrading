# Estratégia LBS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia LBS** é uma conversão direta do assessor especialista MetaTrader 5 "LBS (barabashkakvn's edition)". O sistema original monitora rompimentos da vela anterior durante uma janela de trading configurável e coloca ordens stop em ambos os extremos. O porte do StockSharp mantém as mesmas regras de gerenciamento de operações usando a API de alto nível (`SubscribeCandles`, `SubscribeLevel1`, `BuyStop`/`SellStop`) para clareza e confiabilidade.

## Lógica de trading

1. A estratégia monitora velas completadas do período de tempo selecionado (`CandleType`).
2. Quando o horário de fechamento da vela coincide com qualquer uma das horas de trading habilitadas (`Hour1`, `Hour2`, `Hour3`), o algoritmo calcula os níveis de rompimento:
   - O buy stop é colocado no maior valor entre a máxima da vela e o ask atual mais um buffer de congelamento.
   - O sell stop é colocado no menor valor entre a mínima da vela e o bid atual menos o mesmo buffer.
   - O buffer reproduz o fallback `SYMBOL_TRADE_FREEZE_LEVEL` do MetaTrader (três spreads, mas nunca menos de dez pips).
3. Se uma posição é aberta, a ordem pendente oposta é cancelada imediatamente, assim como a rotina `DeleteAllPendingOrders` do especialista MQL.
4. Os preços iniciais de stop-loss são anexados de acordo com `StopLossPips`. A lógica de trailing opcional (`TrailingStopPips` e `TrailingStepPips`) desloca o stop assim que o lucro flutuante supera os limites configurados.
5. Ordens são enviadas apenas quando a estratégia está online, nenhuma posição está aberta e cotações válidas de Level1 estão disponíveis.

## Gerenciamento de capital

`MoneyMode` espelha o interruptor `Lot/Risk` do especialista original:

- **FixedLot** – o parâmetro `VolumeOrRisk` é interpretado como um volume de trading absoluto.
- **RiskPercent** – a estratégia converte `VolumeOrRisk` em uma fração do valor do portfólio. O valor do risco é dividido pela distância entre o preço de entrada e o stop de proteção (em passos de preço) para obter o volume da ordem. Quando esse modo é usado, o stop-loss deve estar habilitado; caso contrário, a ordem é ignorada.

Todos os volumes são normalizados para os limites mínimo, máximo e de passo do instrumento para evitar rejeições do broker.

## Parâmetros

| Nome | Padrão | Descrição |
| --- | --- | --- |
| `StopLossPips` | 50 | Distância ao stop fixo em pips. Zero desabilita tanto o stop inicial quanto o módulo de trailing. |
| `TrailingStopPips` | 5 | Distância do trailing-stop em pips. Zero desabilita o trailing. |
| `TrailingStepPips` | 15 | Lucro adicional (em pips) necessário antes de mover o trailing stop. Deve ser positivo quando o trailing estiver habilitado. |
| `MoneyMode` | `FixedLot` | Seleciona entre volume fixo e dimensionamento por porcentagem de risco. |
| `VolumeOrRisk` | 1.0 | Tamanho do lote no modo `FixedLot` ou porcentagem de risco no modo `RiskPercent`. |
| `Hour1` | 10 | Primeira hora de trading. Defina como `0` para desabilitar. |
| `Hour2` | 11 | Segunda hora de trading. Defina como `0` para desabilitar. |
| `Hour3` | 12 | Terceira hora de trading. Defina como `0` para desabilitar. |
| `CandleType` | Período de 1 hora | Série de velas usada para detectar rompimentos; ajuste para espelhar o período do gráfico do MetaTrader. |

## Notas

- As comparações de horas usam o horário de fechamento da vela, que corresponde ao momento em que `TimeCurrent()` do MetaTrader é igual ao início da próxima barra.
- A aproximação do nível de congelamento/stop garante que as ordens stop nunca estejam mais próximas que dez pips do bid/ask atual, evitando os erros mais comuns do MetaTrader.
- Os trailing stops são atualizados a cada tick de Level1, garantindo um comportamento próximo ao handler `OnTick` baseado em ticks do especialista original.
- O dimensionamento baseado em risco usa `Portfolio.CurrentValue` quando disponível e recorre a `Portfolio.BeginValue` caso contrário.

## Dicas de uso

1. Anexe a estratégia a um instrumento e escolha o mesmo período de tempo usado no MetaTrader.
2. Configure as horas de trading de acordo com a sessão que deseja operar (definindo como `0` desabilita aquele slot).
3. Selecione o modo `RiskPercent` se desejar escalamento automático; certifique-se de que `StopLossPips` seja positivo.
4. Para trading de lote fixo, mantenha `MoneyMode` em `FixedLot` e defina `VolumeOrRisk` para o tamanho desejado.
5. Inicie a estratégia. Ela colocará duas ordens pendentes na próxima hora configurada e manterá o stop de proteção automaticamente.
