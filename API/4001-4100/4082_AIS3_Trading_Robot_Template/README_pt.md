# Modelo de robô comercial AIS3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
O modelo de robô comercial AIS3 é um sistema de breakout MetaTrader que depende de dois prazos coordenados. O prazo principal
captura a estrutura da vela anterior, enquanto um período secundário mede a volatilidade recente para controlar as atualizações posteriores.
Esta porta StockSharp reproduz fielmente o tamanho do pedido original, as verificações de entrada e a lógica final, mas é implementada em
parte superior da estratégia de alto nível API para que possa ser executada dentro do Designer, Shell ou qualquer host StockSharp personalizado.

## Fluxo de trabalho de negociação
- **Assinaturas de dados de mercado**: a estratégia assina duas séries de velas. A série primária (padrão 15 minutos) fornece
a máxima, mínima, fechamento, ponto médio e intervalo da vela anterior. A série secundária (padrão 1 minuto) mede a faixa rápida usada
para paradas finais. Um feed de livro de pedidos ao vivo mantém os melhores preços de compra/venda atuais sincronizados com o MQL `MarketInfo` original
solicitações.
- **Validação de breakout**:
  - Uma configuração longa é acionada quando o fechamento anterior está acima do ponto médio e o preço de venda atual ultrapassa o anterior.
alto mais o spread medido. O preço de entrada é o pedido atual.
  - Uma configuração curta exige que o fechamento anterior fique abaixo do ponto médio e que a oferta ultrapasse o mínimo anterior. O preço de entrada
é o lance atual.
  - Ambas as direções herdam as verificações de segurança do corretor do modelo: a distância entre a entrada e o stop/alvo projetado
deve exceder o buffer de stop configurado, e o stop deve permanecer no lado correto do preço de entrada mesmo após adicionar o
espalhar.
- **Ordens de proteção**:
  - A distância de stop-loss é igual a `primaryRange × StopMultiplier` e está ancorada acima (para posições compradas) ou abaixo (para posições vendidas) do
vela de breakout conforme descrito no manual de integração.
  - A distância do lucro é igual a `primaryRange × TakeMultiplier` e é colocada a partir do preço de entrada na direção da negociação.
- **Gestão comercial**:
  - Quando uma posição é aberta, o intervalo de tempo secundário multiplicado por `TrailMultiplier` define a distância final.
  - O trailing stop só é atualizado se a negociação for lucrativa, o novo nível estiver mais distante do que o congelamento e o stop configurados
buffers, e a distância entre a parada atual e proposta excede `TrailStepMultiplier × spread`. Isto espelha o
requisito do modelo de que o preço deve avançar pelo menos um passo antes de modificar o stop.
  - As posições são fechadas com ordens de mercado sempre que a compra/venda atinge os níveis armazenados de stop-loss ou take-profit.

## Gestão de risco
- **Reserva de conta**: `AccountReserve` mantém uma fração do patrimônio do portfólio bloqueada. A estratégia se recusa a abrir novas posições
se o capital reservado cair abaixo do orçamento do pedido solicitado. Isso corresponde ao comportamento do modelo onde o risco
a reserva protege a conta de perdas em cascata.
- **Reserva de ordem**: `OrderReserve` controla a parcela do capital restante que pode ser arriscada por negociação. O tamanho da posição
é calculado como `riskBudget / |entry - stop|` e depois alinhado à etapa do volume de segurança. Se nenhuma métrica do portfólio for
disponível, o parâmetro substituto `BaseVolume` é usado em seu lugar.
- **Buffers de parada e congelamento**: `StopBufferTicks` e `FreezeBufferTicks` traduzem as limitações de parada do corretor (por exemplo, `MODE_STOPLEVEL`
e `MODE_FREEZELEVEL` de MetaTrader) em unidades de preço usando a etapa de preço do título. Eles impedem que a estratégia emita
ordens que violariam as restrições cambiais ou moveriam o trailing stop de forma muito agressiva.
- **Multiplicador de etapa final**: `TrailStepMultiplier` espelha a constante `acd.TrailStepping` do modelo MQ4. Isso garante
que as atualizações finais só acontecem quando o novo stop está a pelo menos um spread múltiplo do valor anterior.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `AccountReserve` | Fração do patrimônio mantida como reserva de segurança (0–0,95).
| `OrderReserve` | Fração de capital negociável alocada ao orçamento de risco por negociação (0–0,5 por padrão).
| `PrimaryCandleType` | Prazo de trabalho para detecção de rompimento (velas padrão de 15 minutos).
| `SecondaryCandleType` | Período de tempo mais rápido que controla a distância final (velas padrão de 1 minuto).
| `TakeMultiplier` | Multiplicador do intervalo primário usado para colocar a ordem de lucro.
| `StopMultiplier` | Multiplicador da faixa primária usada para calcular a parada de proteção.
| `TrailMultiplier` | Multiplicador da faixa secundária que define a distância de fuga.
| `BaseVolume` | Tamanho da posição substituta quando as métricas do portfólio não estão disponíveis.
| `StopBufferTicks` | Distância extra, em ticks de preço, que deve permanecer entre os níveis de entrada e stop/alvo.
| `FreezeBufferTicks` | Buffer adicional que evita parar atualizações muito próximas do nível de congelamento do corretor.
| `TrailStepMultiplier` | Multiplicador de spread que define o incremento mínimo entre os ajustes finais.

## Notas de uso
- Alimente a estratégia com séries de velas e um fluxo de nível 1 ou livro de pedidos para que os melhores preços de compra/venda estejam disponíveis. Correndo
isso apenas nos dados da última negociação alterará as verificações de rompimento porque elas dependem do spread.
- Os valores de parâmetro padrão replicam o exemplo do modelo MQ4 (`TakeMultiplier = 1`, `StopMultiplier = 2`,
`TrailMultiplier = 3`). Ajuste-os para corresponder aos ativos que você negocia ou para experimentar a intensidade do rompimento.
- O trailing stop é virtual – os pedidos não são modificados na bolsa. Quando a condição final é atendida, a estratégia simplesmente
emite uma saída do mercado, refletindo como o consultor especialista original administrou os stops internamente.
- Combine a estratégia com o módulo de proteção integrado do StockSharp (já habilitado no construtor) para manter a emergência
manipulação de stop-loss mesmo se a estratégia estiver temporariamente desconectada.
