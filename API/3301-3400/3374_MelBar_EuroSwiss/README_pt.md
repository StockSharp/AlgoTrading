# Estratégia MelBar EuroSwiss
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia MelBar EuroSwiss reproduz a lógica do consultor especialista "MelBar EuroSwiss M30 500 1.85x 2Y". Ele combina Bollinger entradas de quebra de banda com um filtro de saída baseado no Índice de Vigor Relativo (RVI). O modelo padrão é ajustado para o par EUR/CHF no período M30, mas os parâmetros podem ser otimizados para outros símbolos.

No início de cada vela finalizada, a estratégia lê as bandas Bollinger e os valores RVI calculados nos preços de fechamento. Novas posições são abertas quando a barra atual abre além do envelope enquanto a barra anterior abre dentro do canal. Este comportamento imita a lógica de ruptura no estilo gap do robô MQL5 original. As negociações longas usam a banda inferior como gatilho, enquanto as negociações curtas reagem à banda superior. As posições existentes são fechadas quando o RVI atrasado cruza acima ou abaixo de um nível absoluto, indicando esgotamento do impulso na direção da negociação. Ordens de proteção opcionais são definidas usando distâncias fixas de pip.

O volume padrão é 0,2 lote, mas o parâmetro `TradeVolume` permite um controle preciso sobre o tamanho da posição. Tanto o stop loss quanto o takeprofit são expressos em pips e convertidos em compensações de preço por meio do parâmetro configurável `PipSize`. O mesmo tamanho de pip é reutilizado para armar o módulo de proteção na inicialização. Todos os cálculos baseiam-se em velas finalizadas para evitar viés antecipado.

## Detalhes
- **Critérios de entrada**:
  - **Longo**: Vela atual aberta < banda inferior anterior Bollinger E vela anterior aberta > banda inferior de duas velas atrás.
  - **Curto**: Vela atual aberta > banda superior anterior Bollinger E vela anterior aberta < banda superior de duas velas atrás.
- **Critérios de saída**:
  - **Longo**: Fecha quando o valor histórico do RVI excede +`RviLevel`.
  - **Curto**: Fecha quando o valor histórico do RVI cai abaixo de -`RviLevel`.
- **Stops**: Stop loss fixo opcional e distâncias de take-profit em pips.
- **Indicadores**: Faixas Bollinger (período `BollingerPeriod`, desvio `BollingerDeviation`) e Índice de Vigor Relativo (`RviPeriod`).
- **Valores padrão**:
  - `TradeVolume` = 0,2 lotes
  - `BollingerPeriod` = 18
  - `BollingerDeviation` = 2,75
  - `RviPeriod` = 15
  - `RviLevel` = 0,30
  - `StopLossPips` = 13
  - `TakeProfitPips` = 61
  - `PipSize` = 0,0001
  - `CandleType` = TimeSpan.FromMinutes(30)
- **Outras notas**:
  - Categoria: Reversão de rompimento
  - Direção: longo e curto
  - Prazo: intradiário (M30 por padrão)
  - Nível de risco: Médio devido a controles de risco fixos baseados em pip
  - Trailing stop: Não habilitado por padrão (pode ser implementado externamente)

Os parâmetros fornecidos refletem a configuração original e servem como um ponto de partida sólido para testes de acompanhamento ou execuções de otimização em StockSharp.
