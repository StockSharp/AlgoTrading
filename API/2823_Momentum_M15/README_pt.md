# Estratégia Momentum M15
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma portagem direta do expert advisor **Momentum-M15** do MetaTrader 5 (arquivo original `Momentum-M15.mq5`).
Opera com velas de 15 minutos e combina um filtro de média móvel deslocada com um oscilador de momentum avaliado nas
aberturas de barras. A lógica visa operar contra momentum extremo quando o preço está no lado oposto da média deslocada, enquanto um
guardião de lacunas e trailing stop opcional limitam a exposição.

## Destaques da conversão

* Os indicadores são recriados com componentes StockSharp: uma média móvel configurável (padrão suavizada) e o oscilador
  `Momentum` integrado que trabalha com o preço de vela escolhido (padrão `Open`).
* O deslocamento horizontal da MA do MetaTrader é emulado armazenando em buffer os valores do indicador e recuperando o valor `MaShift`
  barras terminadas atrás. Nenhuma matemática de indicador personalizada é reimplementada.
* As verificações de monotonicidade do Momentum reutilizam os últimos valores do histórico e mantêm apenas os elementos necessários para as janelas de entrada
  ou saída, espelhando os auxiliares originais `CheckMO_Up` / `CheckMO_Down`.
* O bloqueio por lacuna grande (`GapLevel`/`GapTimeout`) é preservado. As informações de passo de preço são usadas para converter os limiares baseados em pontos
  definidos na versão MQL em passos de preço do StockSharp.
* O gerenciamento do trailing stop é tratado internamente por meio de saídas de mercado quando o preço cruza o nível rastreado, correspondendo à
  rotina MQL que modificava as ordens de stop loss uma vez por barra concluída.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `TradeVolume` | Tamanho da ordem usado para cada entrada. | `0.1` |
| `CandleType` | Período principal (velas de 15 minutos por padrão). | `15m` |
| `MaPeriod` | Comprimento de lookback da média móvel. | `26` |
| `MaShift` | Número de barras para deslocar horizontalmente a média móvil. | `8` |
| `MaMethod` | Tipo de média móvel (`Simple`, `Exponential`, `Smoothed`, `Weighted`). | `Smoothed` |
| `MaPrice` | Preço de vela alimentado à média móvel. | `Low` |
| `MomentumPeriod` | Comprimento de lookback do Momentum. | `23` |
| `MomentumPrice` | Preço de vela usado para o oscilador Momentum. | `Open` |
| `MomentumThreshold` | Nível de momentum base que separa configurações comprado/vendido. | `100` |
| `MomentumShift` | Valor adicionado/subtraído de `MomentumThreshold` para construir limites assimétricos. | `-0.2` |
| `MomentumOpenLength` | Barras necessárias para uma sequência de momentum não crescente antes de abrir comprados / não decrescente para vendidos. | `6` |
| `MomentumCloseLength` | Barras necessárias para a mesma sequência monótona antes de fechar posições. | `10` |
| `GapLevel` | Lacuna positiva mínima (em passos de preço) que pausa novas entradas. | `30` |
| `GapTimeout` | Número de barras para manter o trading desabilitado após uma lacuna grande. | `100` |
| `TrailingStop` | Distância opcional do trailing stop medida em passos de preço. | `0` (desabilitado) |

## Regras de trading

### Critérios de entrada

* **Entradas compradas**
  * O último Momentum está abaixo de `MomentumThreshold + MomentumShift` (para o deslocamento padrão de `-0.2`, isso está ligeiramente
    abaixo do limiar principal).
  * Tanto o fechamento da barra anterior quanto a abertura da barra atual estão **abaixo** da média móvel deslocada.
  * O Momentum foi não crescente por `MomentumOpenLength` barras (correspondendo a `CheckMO_Down` no código-fonte MQL).

* **Entradas vendidas**
  * O último Momentum está acima de `MomentumThreshold - MomentumShift` (com o deslocamento padrão isso está ligeiramente acima de 100).
  * Tanto o fechamento da barra anterior quanto a abertura da barra atual estão **acima** da média móvel deslocada.
  * O Momentum foi não decrescente por `MomentumOpenLength` barras (correspondendo a `CheckMO_Up`).

As entradas são avaliadas apenas quando nenhuma posição está aberta e o trading não está suspenso pelo filtro de lacunas.

### Critérios de saída

* As **posições compradas** fecham quando qualquer uma das seguintes condições é verdadeira:
  * O Momentum foi não crescente por `MomentumCloseLength` barras.
  * O fechamento da barra anterior cai abaixo da média móvel deslocada.
  * O trailing stop (se habilitado) é tocado. O stop segue o mínimo da vela menos a distância configurada expressa em
    passos de preço.

* As **posições vendidas** fecham quando qualquer uma das seguintes condições é verdadeira:
  * O Momentum foi não decrescente por `MomentumCloseLength` barras.
  * O fechamento da barra anterior sobe acima da média móvel deslocada.
  * O trailing stop (se habilitado) é tocado. O stop segue a máxima da vela mais a distância configurada.

### Lógica de suspensão por lacuna

O expert advisor original pausava o trading após lacunas ascendentes fortes. A versão StockSharp mede a diferença
entre a abertura da barra atual e o fechamento anterior em passos de preço:

1. Quando a lacuna excede `GapLevel`, o temporizador de bloqueio é reiniciado para `GapTimeout`.
2. O temporizador é decrementado a cada barra fechada; o trading é retomado somente após chegar a zero.

## Notas e suposições

* Todos os cálculos usam velas terminadas (`CandleStates.Finished`) para permanecer alinhados com as práticas da API de alto nível do StockSharp.
  Como resultado, as ordens são emitidas na próxima barra após as condições serem observadas, o que é consistente com como
  a estratégia original era acionada no primeiro tick de uma nova barra.
* O conceito de "pips" do MetaTrader é aproximado via `Security.PriceStep`. Se o instrumento carecer de dados de passo adequados,
  o filtro de lacunas e o trailing stop serão silenciosamente desabilitados.
* Os preços da média móvel e as entradas do Momentum podem ser alterados independentemente, replicando a flexibilidade dos
  parâmetros de entrada originais.
* Nenhuma ordem de stop automatizada é registrada; em vez disso, as saídas de mercado reproduzem os ajustes de stop que o código MQL emitia
  por meio de `PositionModify`.

## Dicas de uso

1. Atribua o instrumento desejado e garanta que `CandleType` corresponda ao período histórico usado durante os backtests (barras de 15
   minutos no script original).
2. Configure `TradeVolume` para o tamanho de lote suportado pelo local de trading.
3. Ajuste `MomentumOpenLength` / `MomentumCloseLength` para controlar quão estrito deve ser o filtro de monotonicidade do Momentum.
4. Se preferir espelhar exatamente a escala de "pip" padrão, defina `TrailingStop` e `GapLevel` de acordo com a proporção
   entre o passo de preço da exchange e um pip para o instrumento.
