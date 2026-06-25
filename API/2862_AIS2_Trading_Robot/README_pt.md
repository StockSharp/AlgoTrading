# Robô de Trading AIS2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
O Robô de Trading AIS2 é um sistema de rompimento multi-timeframe convertido do assessor especialista original do MetaTrader 5. Escaneia um timeframe superior (padrão velas de 15 minutos) para detectar rompimentos direcionais, enquanto um timeframe mais rápido (padrão velas de 1 minuto) fornece trailing stops adaptativos. A colocação de ordens, o orçamento de risco e a lógica de trailing seguem as regras codificadas na versão MQ5 legada, mas são implementadas sobre a API de estratégia de alto nível do StockSharp.

## Lógica de trading
- **Vela de sinal primária**: Para cada vela terminada no timeframe primário a estratégia captura o máximo, mínimo, fechamento, ponto médio e intervalo.
- **Configuração comprada**:
  - O fechamento anterior deve estar acima do ponto médio da vela, sinalizando pressão altista.
  - O preço ask atual deve operar acima do máximo anterior mais o spread medido (confirmação de rompimento).
  - O preço de entrada é o ask atual. O stop-loss é igual a `high + spread - (range × StopFactor)`. O take-profit é igual a `ask + (range × TakeFactor)`.
  - Verificações adicionais de segurança do broker garantem que tanto o risco quanto a recompensa sejam maiores que a distância de buffer de stop configurada.
- **Configuração vendida**:
  - O fechamento anterior deve estar abaixo do ponto médio, sinalizando pressão baixista.
  - O bid atual deve imprimir abaixo do mínimo anterior (rompimento de baixa).
  - O preço de entrada é o bid atual. O stop-loss é igual a `low + (range × StopFactor)`. O take-profit é igual a `bid - (range × TakeFactor)`.
- **Resolução de conflitos**: Novas operações são tomadas somente quando a estratégia está plana ou posicionada na direção oposta (o volume de entrada compensa automaticamente a exposição existente antes de abrir o novo posição).

## Gestão de ordens
- **Trailing stop**: O intervalo do timeframe secundário é multiplicado por `TrailFactor` para construir um trail dinâmico. Para posições compradas o stop é levado para `bid - trailDistance`; para vendidas é empurrado para `ask + trailDistance`. As atualizações de trailing são ignoradas quando o preço não está em lucro ou quando a modificação solicitada é menor que o step de trail configurado e os buffers de congelamento.
- **Tomada de lucro e saída por stop**: Tanto as posições compradas quanto vendidas são liquidadas com ordens de mercado quando os preços bid/ask cruzam os níveis de stop-loss ou take-profit armazenados.
- **Feed de livro de ordens**: Uma assinatura ao livro de ordens em tempo real rastreia os preços bid/ask atuais para que a estratégia possa reproduzir a lógica MQ5 que dependia dos valores `SymbolInfo.Ask/Bid`.

## Dimensionamento de posição e controles de risco
- **Reserva de conta**: Uma fração configurável do capital do portfólio está bloqueada e não pode ser usada para trading. Isso replica o parâmetro `Inp_aed_AccountReserve` do EA original.
- **Reserva de ordem**: O capital restante é ainda mais limitado por uma fração de alocação de ordem que limita o orçamento máximo de risco por operação.
- **Verificações de risco**:
  - Se o capital reservado é menor que o limite de alocação (`Equity × OrderReserve`), a estratégia se recusa a colocar novas operações.
  - O tamanho da posição é calculado como `riskBudget / |entry - stop|`, alinhado ao passo de volume de segurança. Quando não há informações do portfólio disponíveis o parâmetro de fallback `BaseVolume` é usado.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `AccountReserve` | Fração do capital retida do trading (0–0.95).
| `OrderReserve` | Fração do capital negociável que define o orçamento de risco por operação (0–1).
| `PrimaryCandleType` | Timeframe de trabalho para detecção de rompimentos (padrão 15 minutos).
| `SecondaryCandleType` | Timeframe mais rápido que impulsiona atualizações do trailing stop (padrão 1 minuto).
| `TakeFactor` | Multiplicador aplicado ao intervalo primário para calcular a distância de take-profit.
| `StopFactor` | Multiplicador aplicado ao intervalo primário para calcular a distância de stop-loss.
| `TrailFactor` | Multiplicador aplicado ao intervalo secundário para calcular a distância de trailing.
| `BaseVolume` | Tamanho de ordem de fallback usado quando métricas do portfólio não estão disponíveis.
| `StopBufferTicks` | Distância adicional (em ticks) necessária além das restrições de stop da bolsa.
| `FreezeBufferTicks` | Buffer adicional que evita ajustes menores de trailing perto do nível de congelamento.
| `TrailStepMultiplier` | Multiplicador de spread que define o incremento mínimo entre atualizações de trailing.

## Notas
- Sempre alimente a estratégia com ambas as séries de velas primária e secundária mais um stream do livro de ordens em tempo real para desbloquear todos os ramos de lógica.
- As verificações de rompimento dependem de preços bid/ask, portanto o paper trading com preços do último negócio apenas pode entregar comportamento diferente em comparação com um ambiente real.
- A proteção de posição é iniciada automaticamente uma vez que a estratégia é executada, refletindo as rotinas de segurança presentes na versão MQ5.
