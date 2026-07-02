# Estratégia Cs2011
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Cs2011 é um sistema de reversão traduzido do consultor especialista `cs2011.mq5` original. Ele monitora o histograma MACD e a linha de sinal em cada vela finalizada e procura padrões de exaustão em torno da linha zero. A porta C# mantém as principais regras de temporização enquanto as expõe por meio do StockSharp API de alto nível.

## Lógica de negociação
- **Reversões de linha zero** – quando o valor MACD da barra anterior está acima de zero enquanto a barra anterior estava abaixo de zero, a estratégia emite um sinal **curto**. A transição oposta (de positivo para negativo) emite um sinal **longo**. Isso imita as entradas contrárias implementadas no script MQL5.
- **Extremos da linha de sinal** – a estratégia armazena as últimas três leituras da linha de sinal. Um máximo local enquanto MACD permaneceu negativo aciona uma entrada curta adicional; um mínimo local enquanto MACD permanece positivo desencadeia uma entrada longa. Isso reproduz as verificações de padrão baseadas em `Sig[0]`, `Sig[1]` e `Sig[2]` na fonte EA.
- Os sinais são avaliados apenas em velas finalizadas fornecidas por `SubscribeCandles`, portanto, os dados parciais são ignorados.

## Manipulação de posição
- A estratégia visa um **tamanho de posição absoluto fixo** (`TargetVolume`). Quando chega um sinal de alta, ele compra contratos suficientes para alcançar `+TargetVolume`. Os sinais de baixa fazem o mesmo para `-TargetVolume`. A exposição existente na mesma direção é respeitada – nenhum pedido adicional é feito se a meta já tiver sido alcançada.
- `StartProtection` reflete as configurações originais de take-profit e stop-loss. As distâncias dos pontos são convertidas em valores `UnitTypes.Point` e passadas para o módulo de risco integrado. Deixar qualquer um dos valores em `0` desativa a barreira correspondente.
- Auxiliares de alto nível (`BuyMarket`, `SellMarket`) são usados em vez da estrutura de solicitação de baixo nível da versão MQL.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `TargetVolume` | `1` lote | Tamanho absoluto da posição alcançado após um sinal. Substitui a rotina de dimensionamento de saldo `Risk` × do EA. |
| `TakeProfitPoints` | `2200` | Distância em faixas de preço para gerenciamento de lucro. `0` desativa o take-profit. |
| `StopLossPoints` | `0` | Distância em pontos de preço para o stop loss. `0` desativa o stop-loss, correspondendo aos padrões de EA. |
| `FastEmaPeriod` | `30` | Comprimento EMA rápido para o núcleo MACD. |
| `SlowEmaPeriod` | `500` | Comprimento EMA lento para MACD. |
| `SignalPeriod` | `36` | Período de suavização da linha de sinal. |
| `CandleType` | `1 hour` período de tempo | Fonte de vela usada por `SubscribeCandles`. Ajuste isso para corresponder ao período do gráfico usado em MetaTrader. |

Todos os parâmetros são registrados por meio de `Param()` para que possam ser otimizados dentro da UI do otimizador StockSharp.

## Diferenças da versão MQL5
- A rotina de gerenciamento de dinheiro (`Money_M`) dependia de transações históricas e do saldo da conta MetaTrader. As estratégias StockSharp operam em carteiras independentes de corretoras, portanto a porta expõe um parâmetro `TargetVolume` simples. Os usuários podem conectar seu próprio gerenciamento de dinheiro substituindo o valor do parâmetro ou o método `ExecuteSignals`.
- As solicitações de pedidos são simplificadas para ordens de mercado único. Lógica de nova tentativa, desvio baseado em spread e verificações de contexto comercial são gerenciadas pela infraestrutura StockSharp.
- A estratégia é executada com assinaturas de velas em vez do auxiliar `IsNewBar` personalizado. Isto garante que apenas velas totalmente formadas sejam processadas.

## Notas de uso
1. Configure o título, o portfólio e o tipo de vela antes de lançar a estratégia.
2. Ajuste `TargetVolume` para corresponder ao tamanho nominal do lote desejado.
3. Opcionalmente, ajuste `TakeProfitPoints` e `StopLossPoints` para reproduzir os níveis de proteção do EA original.
4. Inicie a estratégia – as mensagens de registro registram cada gatilho de negociação junto com a exposição desejada.

O código contém comentários embutidos em inglês que descrevem cada etapa do processo de portabilidade.
