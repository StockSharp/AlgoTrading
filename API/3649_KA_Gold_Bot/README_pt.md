# Estratégia do bot KA-Gold
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia KA-Gold Bot** é uma versão direta do MetaTrader consultor especialista "KA-Gold Bot". Ele negocia rompimentos de um canal personalizado no estilo Keltner e alinha sinais com filtros de tendência de médio prazo. A porta depende de StockSharp assinaturas de vela de alto nível, ligações de indicadores e parâmetros de estratégia para que o comportamento permaneça configurável na IU e pronto para otimização.

## Lógica de negociação

- Calcule três médias móveis exponenciais (EMA):
  - EMA(10) para confirmação rápida do impulso.
  - EMA(200) para detectar a tendência de período de tempo mais alto.
  - EMA(ponto final) como centro do canal; o mesmo comprimento é usado para calcular a média do intervalo da vela (alto-baixo).
- Calcule a média do intervalo diário com uma média móvel simples para formar envelopes dinâmicos:
  - Banda superior = EMA(período) + SMA(máximo-baixo, ponto final).
  - Banda inferior = EMA(período) - SMA(alto-baixo, ponto final).
- Uma configuração **longa** requer todos os seguintes itens na última vela fechada:
  - Preço de fechamento acima da banda superior.
  - Preço de fechamento acima de EMA(200).
  - EMA(10) cruzou de baixo da banda superior anterior para acima da última banda superior.
- Uma configuração **curta** reflete as regras:
  - Preço de fechamento abaixo da banda inferior.
  - Preço de fechamento abaixo de EMA(200).
  - EMA(10) cruzou de cima da banda inferior anterior para abaixo da última banda inferior.
- Apenas uma posição poderá estar aberta por vez; sinais opostos são ignorados até que a estratégia seja plana.

## Dimensionamento de posições

Dois modelos de volume são suportados:

1. **Modo de lote fixo** – use o parâmetro `BaseVolume` diretamente.
2. **Modo percentual de risco** – quando `UseRiskPercent = true`, o proxy de free equity (`Portfolio.CurrentValue` ou `Portfolio.BeginValue`) é multiplicado por `RiskPercent`. O resultado é dimensionado em 100.000 (MetaTrader convenção de lote) e arredondado para múltiplos de `BaseVolume`, respeitando `Security.MinVolume`, `Security.MaxVolume` e `Security.VolumeStep`.

## Gestão de risco

- As compensações de stop-loss e take-profit são definidas em pips. Os pips são convertidos em distâncias de preços absolutos usando a etapa de segurança. Os símbolos forex de três e cinco decimais reutilizam a regra MetaTrader `pip = step × 10`.
- As ordens de proteção iniciais são registradas imediatamente após o primeiro preenchimento e mantidas em sincronia com o tamanho da posição atual.
- Os trailing stops são ativados quando o lucro não realizado atinge `TrailingTriggerPips`:
  - As posições longas seguem mantendo o stop `TrailingStopPips` longe do fechamento.
  - As posições curtas utilizam a distância simétrica acima do mercado.
  - A parada é movida somente se a distância melhorar em pelo menos `TrailingStepPips` para evitar acionamento excessivo.
- Quando a posição é fechada, as ordens de proteção pendentes são canceladas automaticamente.

## Filtros de Sessão e Spread

- Janela de negociação opcional controlada por `UseTimeFilter`, `StartHour`, `StartMinute`, `EndHour` e `EndMinute` (janela inclusiva-exclusiva). As janelas noturnas são suportadas (terminam antes da meia-noite).
- Um filtro de spread opcional rejeita novas entradas se o spread atual (diferença entre a melhor oferta e venda nas etapas de preço) exceder `MaxSpreadPoints`.

## Notas de implementação

- As velas são processadas via `SubscribeCandles().Bind(...)`; os valores EMA(10) e EMA(200) chegam por meio da ligação, enquanto o canal EMA e a média do intervalo são atualizados dentro do manipulador sem usar `GetValue`.
- O estado do indicador é armazenado apenas por meio de campos escalares que espelham a lógica de deslocamento MetaTrader `iClose` e `CopyBuffer`, preservando o requisito de comparação das duas últimas barras fechadas.
- A lógica protetora e final usa auxiliares de ordem de alto nível (`BuyStop`, `SellStop`, `BuyLimit`, `SellLimit`) para espelhar as chamadas `PositionModify` de MetaTrader.
- O dimensionamento baseado em portfólio depende das informações de patrimônio disponíveis em StockSharp; se estiver faltando, a estratégia volta ao volume fixo.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `KeltnerPeriod` | Período para o canal EMA e suavização de intervalo. | 50 |
| `FastEmaPeriod` | Comprimento do filtro EMA rápida. | 10 |
| `SlowEmaPeriod` | Comprimento do filtro de tendência lenta EMA. | 200 |
| `BaseVolume` | Volume mínimo de pedido (tamanho do lote). | 0,01 |
| `UseRiskPercent` | Ative o dimensionamento de posição baseado em equilíbrio. | verdade |
| `RiskPercent` | Percentagem do capital utilizado por negociação quando o dimensionamento de risco está ativo. | 1 |
| `StopLossPips` | Distância de stop-loss em pips. | 500 |
| `TakeProfitPips` | Distância de lucro em pips (0 desabilita). | 500 |
| `TrailingTriggerPips` | Limite de lucro para armar o trailing stop. | 300 |
| `TrailingStopPips` | Distância mantida pelo trailing stop uma vez armado. | 300 |
| `TrailingStepPips` | Melhoria mínima antes que o stop seja movido. | 100 |
| `UseTimeFilter` | Alterne o filtro da sessão de negociação. | verdade |
| `StartHour`, `StartMinute` | Hora de início da sessão. | 02:30 |
| `EndHour`, `EndMinute` | Horário de término da sessão (exclusivo). | 21:00 |
| `MaxSpreadPoints` | Spread máximo permitido nas etapas de preço (0 = desabilitado). | 65 |
| `CandleType` | Prazo usado para velas de sinalização. | Velas de 5 minutos |

## Diferenças em comparação com a versão MetaTrader

- A implementação do trailing stop recria a sequência `PositionModify` usando ordens de parada StockSharp; a funcionalidade é equivalente, mas depende de pedidos confirmados pela bolsa.
- MetaTrader calculou a largura do canal a partir da faixa média alta-baixa; a porta reproduz a mesma média com uma média móvel simples para manter os rompimentos idênticos.
- O dimensionamento do risco acessa o patrimônio do portfólio em vez da margem livre. Esta aproximação corresponde à intenção (percentagem de capital), mas pode diferir se os dados de margem específicos da alavancagem não estiverem disponíveis.
- As verificações de spread usam `Security.BestAskPrice` e `Security.BestBidPrice`. Quando a profundidade não está disponível, o filtro é ignorado, espelhando a opção "spread flutuante" no especialista original.

## Dicas de uso

- Anexe a estratégia a instrumentos onde a definição do pip segue as convenções forex (3 ou 5 casas decimais) para manter os parâmetros de risco alinhados com o especialista original.
- Otimize os períodos EMA e a duração do canal para instrumentos que não sejam de ouro porque a estratégia de origem foi ajustada para XAUUSD.
- Monitore a janela do portfólio para garantir que os valores de patrimônio sejam preenchidos quando `UseRiskPercent` estiver ativado.
