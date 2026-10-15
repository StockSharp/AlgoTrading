# Estratégia de Rompimento BB
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia de Rompimento BB replica o expert advisor MetaTrader *Breakthrough_BB* dentro da API de alto nível do StockSharp. O sistema combina Bandas de Bollinger com uma média móvel simples rápida para capturar rompimentos explosivos que ocorrem após a compressão do preço perto dos limites da banda. As operações são geradas exclusivamente em candles completos para manter sinais determinísticos e espelhar o comportamento original do MQL5.

## Lógica de negociação
- **Filtro de tendência:** Uma média móvel simples (SMA) com período configurável valida a direção da tendência. A estratégia compara o último valor da SMA com o valor da SMA de quatro barras antes. Operações compradas exigem que a SMA tenha inclinação ascendente, enquanto vendidas exigem inclinação descendente.
- **Rompimento das Bandas de Bollinger:** A estratégia observa como o fechamento de quatro barras atrás interagiu com a banda superior ou inferior de Bollinger e compara com o preço de fechamento mais recente. Um rompimento válido ocorre quando o preço se move de dentro da banda para fora entre esses dois momentos.
- **Modelo de posição única:** O algoritmo mantém no máximo uma posição aberta. Qualquer operação aberta é fechada antes de avaliar novas entradas para evitar exposição sobreposta.

## Condições de entrada
### Configuração comprada
1. O preço de fechamento de quatro candles atrás estava abaixo da Banda de Bollinger superior.
2. O preço de fechamento mais recente terminou acima da Banda de Bollinger superior atual.
3. O valor da SMA calculado no último candle é maior que o valor da SMA de quatro candles atrás (inclinação positiva).
4. Nenhuma posição está aberta atualmente.

### Configuração vendida
1. O preço de fechamento de quatro candles atrás estava acima da Banda de Bollinger inferior.
2. O preço de fechamento mais recente terminou abaixo da Banda de Bollinger inferior atual.
3. O valor da SMA calculado no último candle é menor que o valor da SMA de quatro candles atrás (inclinação negativa).
4. Nenhuma posição está aberta atualmente.

Quando uma condição de entrada é satisfeita, a estratégia envia uma ordem a mercado usando o parâmetro de volume configurado.

## Regras de saída
- **Saída de posição comprada:** Se uma operação comprada estiver ativa e o último fechamento cair abaixo da linha média de Bollinger, a posição é fechada imediatamente com uma ordem de venda a mercado.
- **Saída de posição vendida:** Se uma operação vendida estiver aberta e o último fechamento subir acima da linha média de Bollinger, a posição é coberta com uma ordem de compra a mercado.

Essas regras de saída imitam o expert advisor original, que removia operações sempre que o mercado revertia de volta para dentro da linha média da banda.

## Indicadores
- **Média Móvel Simples (SMA):** Define o viés direcional e fornece a comparação de inclinação em um intervalo de quatro candles.
- **Bandas de Bollinger:** Fornece os envelopes superior, médio e inferior usados para detectar entradas por rompimento e gerenciar saídas.

## Parâmetros
| Nome | Descrição | Padrão | Otimizável |
| --- | --- | --- | --- |
| `MaPeriod` | Comprimento da SMA usada para o filtro de tendência. | `9` | ✔ |
| `BandsPeriod` | Comprimento de retrospectiva para cálculos das Bandas de Bollinger. | `28` | ✔ |
| `Deviation` | Multiplicador de desvio padrão aplicado às Bandas de Bollinger. | `1.6` | ✔ |
| `Volume` | Tamanho da ordem (em lotes ou contratos, dependendo do instrumento). | `1` | ✔ |
| `CandleType` | Tipo de agregação de candles processado pela estratégia. | Período de `1 hora` | ✖ |

Todos os parâmetros expõem metadados `StrategyParam` do StockSharp para que possam ser ajustados na interface ou otimizados no designer.

## Requisitos de dados
- Funciona com qualquer instrumento que forneça dados de candles compatíveis com o `CandleType` selecionado.
- Os sinais são avaliados apenas em candles concluídos. Candles incompletos são ignorados para manter a lógica determinística.
- A configuração padrão usa candles horários, mas qualquer período suportado pela fonte de dados pode ser fornecido.

## Notas adicionais
- O algoritmo evita consultas ao histórico do indicador e mantém um cache deslizante de quatro barras para valores de fechamento e SMA, seguindo as diretrizes do projeto.
- Recursos de proteção como stop-loss ou take-profit podem ser adicionados via `StartProtection` se desejado; eles não fazem parte da implementação MQL original e, portanto, são omitidos aqui.
- Como a estratégia emite ordens a mercado, garanta liquidez suficiente no instrumento escolhido para minimizar o deslizamento.
