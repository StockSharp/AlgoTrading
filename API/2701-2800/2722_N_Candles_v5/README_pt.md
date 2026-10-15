# Estratégia N Candles v5
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
A estratégia N Candles v5 procura sequências de velas idênticas e abre uma operação
na mesma direção assim que a sequência necessária aparece. A implementação MQL
original de Vladimir Karputov foi traduzida para a API de alto nível do StockSharp.
A estratégia opera somente em velas fechadas e pode ser executada em qualquer período,
sendo as velas de uma hora o padrão para a versão StockSharp.

## Lógica de Trading
1. Quando uma vela fecha, a estratégia a classifica como altista (fechamento acima da
   abertura), baixista (fechamento abaixo da abertura) ou neutra (fechamento igual à abertura).
2. Velas altistas consecutivas aumentam o contador de sequência altista enquanto
   reiniciam o contador baixista, e vice-versa para velas baixistas. Velas neutras
   reiniciam ambos os contadores.
3. Se o contador de sequência altista atingir o valor configurado de `CandlesCount` e
   a posição líquida atual for plana ou vendida, a estratégia envia uma compra a mercado.
   A exposição vendida é coberta primeiro e depois o `TradeVolume` configurado é
   adicionado para estabelecer uma posição comprada.
4. Se o contador de sequência baixista atingir `CandlesCount` e a posição for plana
   ou comprada, a estratégia vende a mercado, cobrindo primeiro qualquer exposição
   comprada antes de entrar vendido.
5. As operações são abertas apenas dentro da janela opcional de sessão de trading
   definida por `StartHour` e `EndHour`. As ações de proteção (take profit, stop loss
   e trailing) continuam operando fora da sessão para garantir que as posições sejam
   gerenciadas com segurança.
6. A estratégia recusa-se a aumentar a exposição além de `MaxNetVolume`, refletindo
   a salvaguarda de volume da versão MQL.

## Gestão de Risco
- **Take Profit / Stop Loss** – expressos em pips e convertidos para distâncias de
  preço absolutas usando o passo de preço do instrumento. Ambos os níveis são
  opcionais e podem ser desativados definindo o valor correspondente como zero.
- **Trailing Stop** – ativa após o preço avançar `TrailingStopPips` do preço de
  entrada. Uma vez ativo, o stop é ajustado sempre que o preço se move um
  `TrailingStepPips` adicional na direção da operação.
- **Filtro de Sessão** – `UseTradingHours` habilita o filtro de hora de início e fim,
  impedindo novas entradas fora da janela selecionada enquanto ainda permite que o
  gerenciamento de risco feche posições.
- **Volume Líquido Máximo** – a posição absoluta (comprada ou vendida) nunca pode
  exceder `MaxNetVolume`.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `TradeVolume` | Tamanho de ordem usado para novas entradas. | `1` |
| `CandlesCount` | Número de velas idênticas consecutivas necessárias para um sinal. | `3` |
| `TakeProfitPips` | Distância do take profit em pips (0 desativa). | `50` |
| `StopLossPips` | Distância do stop loss em pips (0 desativa). | `50` |
| `TrailingStopPips` | Distância que ativa o trailing stop (0 desativa). | `10` |
| `TrailingStepPips` | Progresso adicional necessário antes de ajustar o trailing stop. | `4` |
| `UseTradingHours` | Habilita o filtro de horas de trading. | `true` |
| `StartHour` | Primeira hora (0–23) quando novas posições são permitidas. | `11` |
| `EndHour` | Última hora (0–23) quando novas posições são permitidas. | `18` |
| `MaxNetVolume` | Tamanho máximo absoluto de posição permitido. | `2` |
| `CandleType` | Tipo de dados de vela a analisar. Padrão são velas de 1 hora. | `TimeSpan.FromHours(1)` |

## Notas de Uso
- A estratégia subscreve dados de velas via a API de alto nível `SubscribeCandles`
  e funciona com qualquer instrumento que forneça séries de velas.
- Como a lógica depende de barras completas, é mais adequada para períodos intradiários
  ou superiores onde o ruído de mercado entre fechamentos é menos impactante.
- Ajuste as configurações de risco baseadas em pips de acordo com o tamanho do tick
  do instrumento.
- Ao implantar em instrumentos com diferenças significativas de spread, verifique os
  parâmetros do trailing stop para que o stop não seja acionado por uma ampliação
  normal do spread.
