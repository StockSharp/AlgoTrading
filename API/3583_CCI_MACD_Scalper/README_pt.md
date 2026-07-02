# CCI MACD Escalpelador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
O CCI MACD Scalper transporta o MetaTrader 5 consultor especialista "CCI + MACD Scalper" para a estratégia de alto nível StockSharp API. A conversão mantém a pilha de indicadores original - um filtro de tendência EMA, um gatilho de linha zero CCI e uma verificação de divergência MACD - enquanto traduz a lógica de gerenciamento de dinheiro em convenções StockSharp. As ordens são dimensionadas a partir do patrimônio do portfólio, os stops são rejeitados quando a distância é muito pequena e um trailing stop opcional pode garantir lucros fechando parcialmente as posições após o primeiro ajuste. Um resfriamento de cinco velas evita que a estratégia entre novamente imediatamente após uma execução, replicando o comportamento do temporizador MQL.

## Lógica estratégica
### Indicadores e processamento de dados
* **Velas** – um período configurável conduz cada cálculo. Os sinais são avaliados exclusivamente em velas concluídas para evitar repintura.
* **EMA(34)** – a média móvel exponencial do preço de fechamento atua como filtro direcional. As posições longas exigem que o último fechamento fique acima do valor anterior EMA, as posições curtas exigem um fechamento abaixo dele.
* **CCI(50)** – usado como gatilho de impulso. A estratégia espera por um cruzamento da linha zero que ocorreu nas duas velas finalizadas mais recentes (a vela atual confirma a configuração, mas não participa da comparação lógica).
* **MACD(12,26,9)** – as linhas principal e de sinal MACD devem permanecer no mesmo lado de zero nas duas velas anteriores. A entrada requer que a linha de sinal MACD cruze a linha principal em favor da posição entre essas duas barras (cruzamento de alta para posições compradas, cruzamento de baixa para vendas).
* **Buffers de oscilação** – os últimos cinco máximos e mínimos de velas concluídos formam a referência de stop-loss. Os comprados ancoram no mínimo mais baixo, os vendidos no máximo mais alto, correspondendo exatamente às chamadas MetaTrader `iLowest/iHighest` com uma mudança de uma barra.

### Regras de entrada
* **Controle de sessão** – a negociação é permitida somente quando o tempo de fechamento da vela estiver dentro de `[MinHour, MaxHour]` no horário do terminal local.
* **Recarga** – após cada entrada preenchida, o sistema aguarda cinco durações de velas antes de permitir uma nova negociação, espelhando `EventSetTimer` do código original.
* **Configuração longa**
  * Nenhuma posição longa ativa (`Position <= 0`).
  * Preço de fechamento acima do valor anterior de EMA.
  * CCI passou de negativo para positivo nas duas velas fechadas mais recentes.
  * O cruzamento de MACD ocorreu abaixo de zero durante as mesmas duas barras (o sinal subiu acima de MACD).
  * O stop loss posicionado no mínimo de oscilação mais recente satisfaz a restrição de distância mínima.
* **Configuração curta**
  * Nenhuma posição curta ativa (`Position >= 0`).
  * Preço de fechamento abaixo do valor anterior de EMA.
  * CCI passou de positivo para negativo nas duas últimas velas concluídas.
  * O cruzamento de MACD ocorreu acima de zero (o sinal caiu abaixo de MACD).
  * O stop loss no swing high respeita o requisito de distância mínima.

### Gestão de risco e comércio
* **Dimensionamento dinâmico da posição** – o tamanho da negociação é derivado do `RiskPercent` configurado do patrimônio do portfólio. O risco por contrato é calculado a partir da distância stop-loss, da etapa do preço do título e do valor da etapa. O resultado é ajustado ao passo de volume do instrumento e fixado entre o volume mínimo e máximo.
* **Stop Loss/Take Profit** – Stop Loss usa o extremo de swing escolhido e é rejeitado quando a distância é inferior a `MinimalStopLossPoints`. O lucro é igual a `entry ± RiskReward × stopDistance`, correspondendo ao cálculo de recompensa por risco de EA.
* **Trailing stop (opcional)** – quando ativado, o stop se move `TrailingStopPoints` quando o preço fecha longe o suficiente além do stop anterior. O primeiro ajuste final aciona uma saída parcial que fecha metade do volume original, espelhando fielmente a implementação MetaTrader.
* **Saídas de proteção** – para posições compradas, a posição fecha se o preço ultrapassar o nível de stop (vela mínima) ou atingir o nível de take-profit (vela máxima). As vendas espelham a lógica usando máximos e mínimos de velas, respectivamente.

## Parâmetros
| Nome | Descrição | Padrão |
|------|-------------|---------|
| `CandleType` | Prazo que conduz os cálculos do indicador. | Velas de 15 minutos |
| `RiskPercent` | Porcentagem do patrimônio do portfólio arriscado em cada negociação. | 2% |
| `RiskReward` | Multiplicador de recompensa por risco para o nível de lucro. | 1,5 |
| `EmaPeriod` | Comprimento do filtro de tendência EMA. | 34 |
| `CciPeriod` | Comprimento do Índice de Canal de Commodities. | 50 |
| `MinHour` | Primeira hora (inclusive) em que novas negociações podem ser abertas. | 0 |
| `MaxHour` | Última hora (inclusive) em que novas negociações poderão ser abertas. | 24 |
| `MinimalStopLossPoints` | Distância mínima permitida entre a entrada e o stop loss expressa em pontos de preço. | 100 |
| `UseTrailingStop` | Ativa o módulo de trailing stop e take-profit parcial. | Desativado |
| `TrailingStopPoints` | Distância do trailing stop medida em faixas de preço. | 100 |

## Notas adicionais
* A conversão do preço depende do `PriceStep` do título. Os símbolos sem um passo válido recuam para uma distância de uma unidade de preço.
* O patrimônio do portfólio é obtido de `Portfolio.CurrentValue` e volta para `BeginValue` quando a avaliação atual não está disponível. Se ambos estiverem faltando, a estratégia reverte para a propriedade base `Volume`.
* Não há porta Python para esta estratégia; apenas a versão C# está incluída no pacote API.
