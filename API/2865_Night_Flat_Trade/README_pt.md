# Estratégia de Trading Noturno em Range Plano
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia de Trading Noturno em Range Plano reproduz o assessor especialista clássico MQL5 que procura ranges noturnos estreitos em velas H1 do EURUSD. Foca na hora que rodeia a mudança do dia de trading, aguardando que o preço volte às bordas de um canal de consolidação estreito e apostando na continuação do rompimento. A versão StockSharp mantém as idéias originais intactas enquanto depende de assinaturas de velas de alto nível, vínculos de indicadores e objetos de parâmetros para melhor configurabilidade.

## Visão geral

- **Mercado e timeframe**: Projetado para EURUSD no timeframe H1, mas qualquer instrumento com um passo de preço claramente definido pode ser usado.
- **Janela de sessão**: As entradas são permitidas somente durante uma janela de duas horas que começa no `OpenHour` configurado e termina em `OpenHour + 1` (hora da bolsa).
- **Filtro de range**: O intervalo alto-baixo das últimas três velas completadas deve permanecer entre `DiffMinPips` e `DiffMaxPips` (convertidos em unidades de preço).
- **Viés**: Somente comprado ou somente vendido dependendo de onde o último fechamento se situa dentro do range que se qualifica.

## Lógica de trading

1. **Calcular os limites do range**
   - A estratégia vincula aos indicadores incorporados `Highest` e `Lowest` (comprimento = 3) para obter o máximo mais alto e o mínimo mais baixo ao longo das últimas três velas.
   - A distância entre essas fronteiras é o range de trabalho usado para todas as verificações subsequentes.

2. **Condições de entrada**
   - **Configuração comprada**: Durante a sessão ativa, se o preço de fechamento está acima do mínimo do range mas ainda dentro do quarto inferior (`lowest + range/4`), a estratégia abre uma posição comprada com um stop protetor inicial em `lowest - range/3`.
   - **Configuração vendida**: Simetricamente, se o fechamento está abaixo do máximo do range mas ainda dentro do quarto superior (`highest - range/4`), uma posição vendida é aberta com um stop em `highest + range/3`.

3. **Gestão de saídas**
   - **Stop-Loss**: Os stops são simulados internamente e ativam uma saída de mercado quando a próxima vela viola o limite armazenado.
   - **Take-Profit**: Quando `TakeProfitPips > 0`, um nível adicional fixo de take-profit (em pips) é criado relativo ao preço de entrada.
   - **Trailing Stop**: Quando tanto `TrailingStopPips` quanto `TrailingStepPips` são positivos, o stop é ajustado somente após o preço avançar `TrailingStop + TrailingStep` pips a favor da operação. Ajustes subsequentes requerem um progresso adicional de `TrailingStepPips` para refletir o comportamento de trailing escalonado original.

4. **Controle de re-entrada**
   - O algoritmo sempre aguarda a posição atual estar completamente fechada antes de buscar um novo sinal, mantendo o sistema plano entre operações como no assessor especialista de referência.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `CandleType` | Série de velas para assinar (padrão H1). | Velas de 1 hora |
| `TakeProfitPips` | Distância de take-profit opcional em pips. | 50 |
| `TrailingStopPips` | Distância entre preço e trailing stop em pips (0 desativa trailing). | 15 |
| `TrailingStepPips` | Pips adicionais necessários antes de cada atualização do trailing stop. | 5 |
| `DiffMinPips` | Range mínimo permitido de três velas (pips). | 18 |
| `DiffMaxPips` | Range máximo permitido de três velas (pips). | 28 |
| `OpenHour` | Hora de início da sessão em hora da bolsa (entradas permitidas até `OpenHour + 1`). | 0 |

## Indicadores

- `Highest(Length = 3)` para monitorar a parte superior do range recente.
- `Lowest(Length = 3)` para monitorar a parte inferior do range recente.

## Notas de implementação

- A conversão de pips se adapta automaticamente a instrumentos com 3 ou 5 casas decimais multiplicando o passo de preço relatado por 10, exatamente como a implementação original do MQ5.
- Como o StockSharp opera em velas completadas neste exemplo, as condições de entrada intra-vela são aproximadas usando o preço de fechamento. Isso mantém a lógica determinista enquanto permanece fiel ao intento do código fonte.
- Todos os parâmetros de risco são expostos por meio de objetos `StrategyParam<T>`, tornando-os visíveis na UI e prontos para otimização ou experimentos em lote.
