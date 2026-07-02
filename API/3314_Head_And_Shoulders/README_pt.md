# Estratégia Head and Shoulders
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Head and Shoulders** é um port direto do expert advisor "HEAD AND SHOULDERS" do MetaTrader (MQL ID 26066). O robô original combina reconhecimento do padrão ombro-cabeça-ombro com filtros de momentum, média móvel e MACD, além de gerenciar posições com trailing stops, proteção de patrimônio e regras de break-even. Esta implementação StockSharp foca na lógica discricionária do mecanismo de entrada e saída usando a API de alto nível, fornecendo bindings limpos para indicadores e gestão automatizada de risco via `StartProtection`.

## Lógica de negociação
1. **Detecção de padrão**
   - Usa uma janela fractal de cinco barras para aproximar swing highs e lows, espelhando o reconhecimento de padrões baseado em fractais do EA de origem.
   - Confirma um ombro-cabeça-ombro *baixista* quando três máximas fractais sequenciais aparecem e a máxima central (a cabeça) excede os dois ombros por um limiar de dominância configurável.
   - Confirma um ombro-cabeça-ombro *invertido* quando três mínimas fractais sequenciais se formam e a mínima central fica suficientemente abaixo dos dois ombros.
   - A linha de pescoço é calculada a partir das mínimas fractais mais recentes (padrão baixista) ou máximas (padrão altista) localizadas entre os ombros e a cabeça.
2. **Filtros de momentum e tendência**
   - Médias móveis simples rápida e lenta devem se alinhar com a direção esperada da tendência.
   - Momentum absoluto (diferença entre valor atual e período de retrospectiva) deve exceder um limiar e apontar na mesma direção da operação.
   - O valor MACD precisa concordar com a direção do rompimento para evitar sinais contra a tendência.
3. **Execução do rompimento**
   - Compras disparam quando o preço de fechamento rompe acima da linha de pescoço altista enquanto todos os filtros concordam.
   - Vendas disparam quando o fechamento rompe abaixo da linha de pescoço baixista sob filtros baixistas alinhados.
4. **Gestão de posição**
   - Posições saem se a linha de pescoço for violada na direção oposta ou se médias móveis e MACD perderem alinhamento.
   - Ordens protetoras opcionais são configuradas pelo helper integrado `StartProtection`, usando parâmetros de stop-loss, take-profit e trailing-stop expressos em passos de preço.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `CandleType` | Timeframe 1H | Série principal de candles para detecção do padrão. |
| `OrderVolume` | `1` | Tamanho base da ordem. |
| `FastMaLength` / `SlowMaLength` | `6` / `85` | Comprimentos dos filtros de tendência de média móvel. |
| `MomentumPeriod` | `14` | Período de retrospectiva do indicador de momentum. |
| `MomentumThreshold` | `0.3` | Momentum absoluto mínimo exigido para confirmação. |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | `12`, `26`, `9` | Configuração MACD. |
| `ShoulderTolerancePercent` | `5` | Desvio máximo permitido entre ombro esquerdo e direito. |
| `HeadDominancePercent` | `2` | Quantidade mínima que a cabeça deve exceder cada ombro. |
| `StopLossSteps`, `TakeProfitSteps`, `TrailingStopSteps` | `100`, `200`, `0` | Tamanhos de ordens protetoras em passos de preço (zero desativa um componente). |

Todos os parâmetros criados com `Param()` expõem metadados para exibição na UI e podem ser otimizados pelo otimizador StockSharp.

## Diferenças em relação ao expert original
- Remove stop de patrimônio, trailing e rotinas de modificação de ordens específicas do MetaTrader em favor dos mecanismos integrados de proteção de carteira do StockSharp.
- Foca puramente em ordens a mercado e chamadas de API de alto nível (`BuyMarket` / `SellMarket`).
- Simplifica recursos auxiliares como alertas, notificações push e desenho de objetos gráficos; a versão StockSharp registra detecções com `LogInfo`.
- O reconhecimento de padrões mantém o espírito da lógica fractal original, mas é reescrito para evitar acesso direto a arrays de dados e manipulação de tickets de ordens.

## Notas de uso
- Como a estratégia depende de candles concluídos, garanta que as assinaturas de dados entreguem barras finalizadas (`CandleStates.Finished`).
- A proteção trailing usa passos de preço; verifique se `Security.PriceStep` reflete o tamanho de tick do instrumento antes de habilitar trailing stops.
- O detector de padrões armazena apenas fractais recentes para evitar coleções ilimitadas, tornando-o adequado para longas sessões ao vivo.
- Para camadas adicionais de confirmação (por exemplo, MACD de timeframe superior como no EA original), estenda a estratégia com assinaturas extras usando a mesma abordagem de binding mostrada nesta implementação.

## Referências
- MetaTrader Expert Advisor: `HEAD AND SHOULDERS.mq4` (MQL ID 26066).
- Documentação StockSharp sobre [estratégias de alto nível](https://doc.stocksharp.com/topics/strategy/highlevel.html) e [binding de indicadores](https://doc.stocksharp.com/topics/strategy/highlevel/bind.html).
