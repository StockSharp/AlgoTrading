# Estratégia SilverTrend Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia SilverTrend Duplex** é um port do StockSharp do consultor especializado MetaTrader 5 `Exp_SilverTrend_Duplex`. O robô original combina dois filtros SilverTrend independentes (para decisões compradas e vendidas) e executa trades quando as cores do indicador mudam entre estados altistas e baixistas. Esta implementação em C# mantém a arquitetura de filtro duplo, permitindo ajustar a lógica comprada e vendida separadamente enquanto aproveita a API de alto nível do StockSharp.

A estratégia opera apenas em velas finalizadas. Duas subscrições separadas podem ser configuradas, portanto os sinais comprados e vendidos podem observar diferentes períodos ou instrumentos se necessário. Internamente, um `SilverTrendIndicator` personalizado reconstrói a lógica de cores da versão MQL combinando extremos do canal Donchian com o multiplicador de risco para emular as bandas SilverTrend originais.

## Lógica de trading

1. **Reconstrução do indicador**
   - Para cada vela, os limites superior e inferior do Donchian sobre `SSP` barras são calculados.
   - Os limiares adaptativos `smin` e `smax` são derivados usando o coeficiente de risco (`33 - risk`), idêntico ao algoritmo MQL.
   - Quando o preço fecha acima de `smax` um estado altista é registrado; quando fecha abaixo de `smin` um estado baixista é registrado; caso contrário o estado anterior é mantido. A direção do corpo da vela determina o código de cor final (0..4) exatamente como no indicador SilverTrend original.

2. **Preparação de sinais**
   - Os valores de cor são armazenados para as `SignalBar + 1` velas finalizadas mais recentes tanto para os filtros comprado quanto vendido.
   - Os sinais comprados disparam quando a cor no deslocamento selecionado cai abaixo de `2` (altista) enquanto a cor anterior era maior que `1` (não altista), replicando `Value[1] < 2 && Value[0] > 1` do MQL.
   - Os sinais vendidos disparam quando a cor sobe acima de `2` (baixista) e a cor anterior está acima de `0`, correspondendo a `Value[1] > 2 && Value[0] > 0` do script.

3. **Execução de ordens**
   - As entradas usam `BuyMarket` ou `SellMarket` com volume igual a `Volume + |Position|`, que fecha qualquer exposição oposta e abre o novo lado em uma única ordem a mercado.
   - As saídas dependem do indicador retornar à banda de cor oposta. Posições compradas são fechadas quando a cor sobe acima de `2`, posições vendidas quando cai abaixo de `2`.

A estratégia não replica a matriz de gestão monetária original ou a colocação de stops no servidor de `TradeAlgorithms.mqh`. O controle de risco deve, portanto, ser gerenciado através dos mecanismos de proteção integrados do StockSharp ou das regras do corretor.

## Parâmetros

| Nome | Padrão | Descrição |
| ---- | ------- | ----------- |
| `LongCandleType` | Velas de 4 horas | Tipo de dados usado para o indicador do lado comprado. |
| `LongSsp` | 9 | Comprimento de retrospectiva SilverTrend para o filtro comprado. |
| `LongRisk` | 3 | Multiplicador de risco (`33 - risk`) aplicado à largura do canal. |
| `LongSignalBar` | 1 | Deslocamento (em velas finalizadas) para avaliar sinais comprados. Deve ser ≥ 1. |
| `EnableLongEntries` | true | Ativa/desativa a abertura de posições compradas. |
| `EnableLongExits` | true | Ativa/desativa o fechamento de posições compradas quando cores baixistas aparecem. |
| `ShortCandleType` | Velas de 4 horas | Tipo de dados usado para o indicador do lado vendido. |
| `ShortSsp` | 9 | Comprimento de retrospectiva SilverTrend para o filtro vendido. |
| `ShortRisk` | 3 | Multiplicador de risco para o filtro vendido. |
| `ShortSignalBar` | 1 | Deslocamento para avaliar sinais vendidos. Deve ser ≥ 1. |
| `EnableShortEntries` | true | Ativa/desativa a abertura de posições vendidas. |
| `EnableShortExits` | true | Ativa/desativa o fechamento de posições vendidas quando cores altistas aparecem. |
| `Volume` | 1 | Volume base da ordem usado para entradas. |

## Notas de implementação

- Os sinais são avaliados apenas depois que tanto o indicador quanto o histórico de cores contêm dados suficientes (`SignalBar + 1` valores). Isso reflete as verificações `BarsCalculated` do especialista MQL.
- O indicador personalizado expõe valores de cor decimais em vez de copiar dados brutos de buffer. Não são necessárias chamadas diretas a `GetValue` graças à API de alto nível `Bind`.
- Quando os tipos de vela comprado e vendido são idênticos, duas subscrições são criadas intencionalmente para manter os conjuntos de parâmetros isolados. Isso corresponde ao comportamento de duplo identificador no consultor original.
- As opções de stop-loss, take-profit, desvio e gestão de margem do script fonte não são replicadas. Você pode adicionar regras de risco do StockSharp (por exemplo, `StopLossRule`) se um comportamento semelhante for necessário.

## Dicas de uso

- Otimize `LongSsp`, `ShortSsp` e os valores de risco correspondentes separadamente para adaptar os limiares de rompimento a cada regime de mercado.
- Se deseja emular o comportamento original de "sinal na barra anterior", mantenha `SignalBar` em `1`. Valores maiores forçam a estratégia a aguardar barras adicionais antes de reagir.
- Combine a estratégia com controles de risco a nível de portfólio ou filtros de tempo ao operar em múltiplos instrumentos, pois a mudança de cor do SilverTrend pode produzir mudanças de regime frequentes em mercados laterais.
