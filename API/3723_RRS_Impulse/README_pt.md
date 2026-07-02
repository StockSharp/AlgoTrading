# Estratégia de Impulso RRS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia RRS Impulse** é uma versão StockSharp de alto nível do consultor especialista MetaTrader "RRS Impulse". O robô original
filtros de bandas RSI, Stochastic e Bollinger combinados, alternados entre vários modos de intensidade de sinal e paradas de proteção usadas e
saídas virtuais de fuga. Esta versão C# mantém o mesmo comportamento, mas depende puramente do StockSharp alto nível API: vela
as assinaturas alimentam os indicadores, enquanto `BuyMarket`, `SellMarket` e `ClosePosition` executam os pedidos.

## Lógica de negociação

1. **Modos Indicadores** – Escolha entre quatro opções:
   - `Rsi`: negocie o oscilador quando ele sai da zona de sobrecompra/sobrevenda.
   - `Stochastic`: exige que %K e %D estejam acima/abaixo dos níveis configurados.
   - `BollingerBands`: reage a fechamentos acima da banda superior ou abaixo da banda inferior.
   - `RsiStochasticBollinger`: dispara somente quando todos os três filtros confirmam a mesma direção.
2. **Direção de negociação** – `Trend` segue o indicador (sobrecompra leva a posições vendidas, sobrevenda a posições longas). `CounterTrend` desaparece o
movimento (sobrecompra aciona posições compradas, sobrevenda aciona posições vendidas).
3. **Força do sinal** – Controla quantos prazos devem ser acordados antes de entrar em uma negociação:
   - `SingleTimeFrame`: use apenas o prazo base fornecido por `CandleType`.
   - `MultiTimeFrame`: requer alinhamento entre velas M1, M5, M15, M30, H1 e H4.
   - `Strong`: concentre-se no impulso intradiário verificando M1, M5, M15 e M30.
   - `VeryStrong`: exige confirmação da escada M1… H4 completa. Quando o modo de indicador combinado é ativado a cada período
deve satisfazer *todos* os três filtros.
4. **Gerenciamento de Riscos** – Cada posição rastreia o preço médio de preenchimento e monitora três condições de saída:
   - distância fixa de stop-loss em pips;
   - distância fixa de lucro em pips;
   - trailing stop ativado quando o lucro excede `TrailingStartPips` e mantido em `TrailingGapPips`.
Sempre que a direção muda, a estratégia chama `ClosePosition()` primeiro para achatar e só abre a negociação oposta depois
o próximo tique de confirmação.

## Parâmetros

| Grupo       | Nome | Descrição |
|-------------|------|-------------|
| Dados        | `CandleType` | Série base de velas processada para decisões de negociação. |
| Pedidos      | `TradeVolume` | Volume utilizado no envio de ordens de mercado. |
| Risco        | `StopLossPips`, `TakeProfitPips`, `TrailingStartPips`, `TrailingGapPips` | Saídas de proteção virtuais expressas em pips. |
| Sinais     | `IndicatorMode`, `TradeDirection`, `SignalStrength` | Mudanças de comportamento copiadas do bloco de entrada MQL. |
| RSI         | `RsiPeriod`, `RsiUpperLevel`, `RsiLowerLevel` | Configuração RSI para detecção de sobrecompra/sobrevenda. |
| Stochastic  | `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlowing`, `StochasticUpperLevel`, `StochasticLowerLevel` | Configurações do oscilador estocástico lento. |
| Bollinger   | `BollingerPeriod`, `BollingerDeviation` | Bollinger Multiplicador de lookback e desvio de bandas. |

Todos os parâmetros suportam faixas de otimização idênticas à versão MetaTrader onde fazia sentido (por exemplo, parar e percorrer distâncias
ou limites do oscilador).

## Requisitos de dados

A estratégia precisa de velas minúsculas para a escada de confirmação. Quando `SignalStrength` solicita prazos adicionais, a estratégia
adiciona automaticamente as assinaturas necessárias (`GetWorkingSecurities` as anuncia no mecanismo). As cotações de nível 1 não são usadas;
apenas os preços de fechamento das velas finalizadas geram entradas e saídas. A lógica protetora, portanto, reproduz o stop/target "virtual"
comportamento do robô original.

## Notas sobre a conversão

- A rotação aleatória de símbolos do EA foi removida intencionalmente. As estratégias StockSharp funcionam com um único `Security`, então o
O porto concentra-se em combinar a lógica do indicador e o gerenciamento de risco, deixando a rotação do instrumento para o usuário.
- O gerenciamento de pedidos é baseado no mercado: quando a direção muda ou uma condição de proteção é acionada, `ClosePosition()` é chamado,
espelhando os loops MetaTrader que iteraram por meio de tickets.
- A conversão mantém todos os comentários em inglês e usa tabulações para recuo para cumprir as diretrizes do repositório.
