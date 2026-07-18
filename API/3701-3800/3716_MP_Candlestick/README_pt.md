# Estratégia de castiçal MP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia MP Candlestick** é uma conversão do MetaTrader 5 Expert Advisor `mp candlestick.mq5` na estrutura de estratégia de alto nível StockSharp. O sistema avalia a direção das velas concluídas e abre negociações na mesma direção, aplicando uma gestão de risco rigorosa. Ele suporta distâncias de stop-loss fixas expressas em MetaTrader pips e posicionamento de stop-loss adaptativo derivado do Average True Range (ATR).

## Lógica de negociação
1. A estratégia assina uma única série de velas configuráveis (padrão: velas de 1 hora).
2. Em cada vela acabada:
   - Vela de alta (fechada acima da aberta) → considere uma posição longa.
   - Vela de baixa (fechamento abaixo da abertura) → considere uma posição curta.
   - As velas Doji são ignoradas.
3. Antes de qualquer entrada, a estratégia calcula um preço de stop loss a partir de ATR ou da distância fixa do pip. O preço take-profit é calculado usando a relação risco-recompensa configurada.
4. Se a utilização da margem permanecer dentro da porcentagem permitida e o tamanho da posição calculada for válido, a negociação será aberta no mercado.
5. Enquanto a posição está ativa, a estratégia monitora cada nova vela para:
   - Acertos de stop-loss ou take-profit usando extremos de velas.
   - Ajuste final que move o stop em direção ao ponto de equilíbrio quando ATR stops estão ativados.
6. Quando a posição estiver plana, o processo será reiniciado com a próxima vela finalizada.

## Gestão de Risco e Dinheiro
- **Porcentagem de risco** define a fração de capital arriscada por negociação. O tamanho da posição é derivado da distância do preço entre a entrada e o stop loss e o valor do preço/passo do instrumento.
- **Relação risco/recompensa** determina a distância entre o preço de entrada e a meta de lucro em relação ao risco inicial.
- **Max Margin Usage** restringe a quantidade de margem estimada que a nova negociação pode consumir em comparação com o patrimônio do portfólio atual.
- **Trailing Stop** é ativado automaticamente quando o gerenciamento de risco baseado em ATR é usado. Ele move o stop até a metade do caminho em direção à meta de lucro, sem exceder o último fechamento da vela, tentando bloquear os lucros, respeitando as restrições cambiais.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `RiskPercent` | 1 | Porcentagem do patrimônio do portfólio alocado como perda máxima para uma única negociação. |
| `RiskRewardRatio` | 1,5 | Multiplicador aplicado à distância de risco inicial para definir a meta de take-profit. |
| `MaxMarginUsage` | 30 | Limite superior para o consumo de margem expresso como percentagem do capital próprio. |
| `StopLossPips` | 50 | Tamanho do stop loss corrigido em MetaTrader pips quando ATR está desativado. |
| `UseAutoSl` | verdade | Permite dimensionamento de stop loss ATR (comprimento 14) com multiplicador 1,5. |
| `CandleType` | Período de 1 hora | Série de velas usada para sinais e cálculo ATR. |

## Notas de implementação
- A estratégia depende de StockSharp assinaturas de alto nível (`SubscribeCandles`) e vinculação de indicadores (`AverageTrueRange`).
- O dimensionamento da posição se alinha com a etapa de volume do instrumento e com as restrições de volume mínimo e máximo.
- As verificações de margem reutilizam dicas de margem de instrumento disponíveis (`MarginBuy`/`MarginSell`) e recorrem a uma estimativa baseada em preço.
- Os níveis de stop-loss e take-profit são aplicados internamente, monitorando os máximos e mínimos das velas, garantindo um comportamento consistente entre os corretores.
- Todos os comentários do código estão em inglês, conforme exigido pelas diretrizes de conversão.

## Arquivos
- `CS/MpCandlestickStrategy.cs` — principal implementação da estratégia C#.
- `README.md` — Documentação em inglês (este arquivo).
- `README_zh.md` — Tradução chinesa.
- `README_ru.md` — Tradução para russo.
