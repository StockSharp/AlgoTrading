# Estratégia de reversão de pullback TST
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de reversão de pullback TST** observa reversões intrabarras agressivas. Ele foi convertido do MetaTrader 4 consultor especialista original `TST.mq4` e reconstruído usando o StockSharp API de alto nível. O método procura velas onde o preço se afastou drasticamente da abertura da vela após definir um extremo intradiário e, em seguida, desvanece esse movimento esperando uma reversão à média. A estratégia é negociada tanto longa quanto curta e usa níveis estáticos de stop-loss e take-profit medidos em etapas de preços.

## Lógica de Sinais
- **Configuração longa**
  1. A vela fecha abaixo de sua abertura (`Open > Close`).
  2. A distância entre o máximo e o fechamento da vela é maior que `GapPoints * PriceStep`.
  3. Nenhuma negociação foi executada anteriormente na mesma barra.
Quando satisfeita, a estratégia fecha qualquer exposição curta e compra `OrderVolume` unidades (mais o tamanho necessário para passar de uma posição curta para uma posição longa).

- **Configuração curta**
  1. A vela fecha acima de sua abertura (`Close > Open`).
  2. A distância entre o fechamento e a mínima da vela é maior que `GapPoints * PriceStep`.
  3. Nenhuma negociação foi executada anteriormente na mesma barra.
Quando satisfeita, a estratégia fecha qualquer exposição longa e vende `OrderVolume` unidades (mais o tamanho necessário para passar de uma posição longa para uma posição curta).

## Gerenciamento de posição
- Uma nova negociação atribui imediatamente níveis estáticos de stop-loss e take-profit calculados a partir do preço de preenchimento e dos parâmetros `StopLossPoints`/`TakeProfitPoints`.
- Em cada vela finalizada, a estratégia verifica a máxima/mínima da vela para ver se o stop ou alvo foi tocado e sai da posição se for acionado. As verificações de stop-loss têm precedência sobre as verificações de take-profit.
- Após uma saída, os níveis de risco armazenados são apagados, mas a estratégia ainda lembra o tempo da barra para evitar a reentrada durante a mesma vela (reproduzindo a guarda `NevBar()` da versão MQL4).

## Parâmetros
- `StopLossPoints` (padrão `500`): distância da entrada até a parada de proteção, expressa em etapas de preço.
- `TakeProfitPoints` (padrão `100`): distância da entrada até a meta de lucro, expressa em etapas de preço.
- `GapPoints` (padrão `500`): pullback mínimo entre o extremo da vela e o fechamento necessário para gerar um sinal.
- `OrderVolume` (padrão `0.1`): quantidade enviada a cada nova ordem de mercado.
- `CandleType` (padrão `1 hour`): prazo das velas fornecidas através de `SubscribeCandles`.

Todas as configurações baseadas em distância são multiplicadas pelo `PriceStep` do instrumento. Se a segurança não relatar uma etapa, a estratégia volta para `1`.

## Notas de implementação
- A conversão usa StockSharp de alto nível API e não cria coleções de indicadores personalizados.
- Apenas velas finalizadas são processadas para permanecerem compatíveis com o Strategy Designer; isso aproxima as decisões intrabarras do robô MT4 usando dados de barras completos.
- Um sinalizador dedicado `_lastSignalBarTime` replica a proteção `NevBar()` do código MQL4 para que apenas um pedido possa ser aberto por vela.
- O tratamento do volume de ordens reflete o comportamento do MT4: as posições opostas existentes são achatadas antes de estabelecer a nova direção em uma única ordem de mercado.
- As ordens stop-loss e take-profit são simuladas dentro da lógica da estratégia (em vez de ordens do lado do servidor) para corresponder às distâncias originais, mantendo o controle dentro de StockSharp.

## Dicas de uso
- Escolha `GapPoints` em relação à volatilidade do instrumento negociado; valores maiores reduzem a frequência de negociação, mas filtram retrocessos menores.
- Como as verificações de stop e alvo dependem de velas concluídas, considere usar velas de duração mais curta se precisar de uma execução mais rigorosa.
- Combine a estratégia com filtros adicionais (tendência, hora do dia, volume) ao implantar em mercados ativos para reduzir negociações de chicote.
