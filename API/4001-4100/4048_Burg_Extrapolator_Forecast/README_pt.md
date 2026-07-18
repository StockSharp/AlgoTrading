# Estratégia de previsão do extrapolador Burg
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia Burg Extrapolator é uma versão StockSharp do MetaTrader 4 consultor especialista "Burg Extrapolator". O sistema original ajusta um modelo autorregressivo de Burg (AR) a uma janela móvel de preços abertos (ou suas transformações de momentum/ROC) e projeta uma trajetória de preços futuros. As decisões de negociação são derivadas dos valores de previsão mais extremos: se a excursão prevista em uma direção for grande o suficiente, a estratégia acumula novas posições ou liquida a exposição na direção oposta. A conversão mantém os mesmos blocos de modelagem enquanto mapeia o gerenciamento de posição e o gerenciamento de dinheiro para StockSharp primitivos.

## Lógica de negociação
1. **Preparação de dados**
   - Crie um histórico contínuo de `PastBars + 1` preços de abertura para o `CandleType` selecionado.
   - Opcionalmente, transforme os dados em momento logarítmico (padrão) ou taxa percentual de mudança antes de alimentá-los no modelo AR. Os preços brutos são centrados pela média móvel para espelhar o código MT4.
2. **Previsão linear de Burg**
   - Estime os coeficientes de reflexão até a ordem `PastBars * ModelOrder` usando o algoritmo Burg.
   - Gere uma sequência de valores futuros (`PastBars` passos à frente na prática) expandindo recursivamente o modelo AR. As transformações são invertidas de volta ao espaço de preços para que todas as previsões operem em unidades de preços absolutos.
3. **Detecção de sinal**
   - Percorra o caminho da previsão e registre o preço previsto mais alto e mais baixo antes que outro extremo apareça. A distância entre o primeiro extremo e o outro lado do intervalo de previsão é comparada com os limites `MaxLoss` e `MinProfit` (convertidos em preço absoluto multiplicando pelo instrumento `PriceStep`).
   - Uma alta suficientemente grande aciona `OpenSignal = 1` enquanto uma grande desaceleração produz `OpenSignal = -1`. Se o extremo oposto aparecer primeiro, a lógica define `CloseSignal` para sair da exposição atual, mesmo que nenhuma nova entrada seja planejada.
4. **Gerenciamento de pedidos**
   - As saídas de proteção (stop-loss, take-profit e trailing stop opcional) são executadas antes de qualquer novo sinal ser executado. O trailing-stop reutiliza o melhor preço desde a última entrada e fecha a posição quando o preço retrocede `TrailingStop` pontos, correspondendo ao comportamento MT4 de mover a ordem de proteção.
   - Se um sinal solicitar o fechamento da exposição na direção oposta, a estratégia envia uma ordem de mercado dimensionada para nivelar a posição líquida atual.
   - Os sinais de entrada empilham ordens de mercado adicionais na direção indicada até que `MaxTrades` seja alcançado. O volume do pedido é dimensionado linearmente com o número de negociações ativas usando o fator `1 + existingTrades * MaxRisk`, um substituto amigável do StockSharp para a rotina original de dimensionamento baseada em margem.

## Indicadores e Dados
- Assinatura de vela definida por `CandleType` (período padrão de 30 minutos).
- Modelo autoregressivo de Burg interno (implementado sem indicadores externos).
- Momento logarítmico opcional e transformações de taxa percentual de mudança.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `CandleType` | Velas de 30 minutos | Período primário processado pela estratégia. |
| `MaxRisk` | 0,5 | Multiplicador de risco usado ao empilhar múltiplas negociações. |
| `MaxTrades` | 5 | Número máximo de negociações simultâneas por direção. |
| `MinProfit` | 160 | Lucro mínimo previsto (em pontos) necessário para abrir novas negociações. |
| `MaxLoss` | 130 | Perda prevista máxima tolerada (em pontos) antes do fechamento das negociações. |
| `TakeProfit` | 0 | Distância de take-profit fixa opcional em pontos (0 desativa). |
| `StopLoss` | 180 | Distância de stop-loss fixa opcional em pontos (0 desativa). |
| `TrailingStop` | 10 | Distância de parada final em pontos, ativa somente quando `StopLoss > 0`. |
| `PastBars` | 200 | Número de velas históricas utilizadas pelo modelo Burg. |
| `ModelOrder` | 0,37 | Fração de `PastBars` convertida na ordem Burg. |
| `UseMomentum` | verdade | Aplique a transformação de momento logarítmico aos dados de entrada. |
| `UseRateOfChange` | falso | Aplicar taxa percentual de alteração (ignorada quando o momentum está ativado). |

Todos os parâmetros são `StrategyParam<T>` instâncias e podem ser otimizados ou ajustados no StockSharp Designer.

## Notas de implementação
- O algoritmo Burg é implementado diretamente em C# e mantém a mesma recursão da versão MT4. Todos os cálculos são executados com dupla precisão enquanto as previsões finais são convertidas de volta para `decimal` antes das verificações de sinal.
- O EA original poderia contar com as informações da conta MetaTrader para dimensionar as posições. Em StockSharp o bloco de gerenciamento de dinheiro é substituído por uma regra de escalabilidade determinística baseada em `Volume` e `MaxRisk`. Defina `Volume` para o lote base desejado e a estratégia dimensionará as entradas subsequentes proporcionalmente.
- A lógica protetora fecha posições com ordens de mercado explícitas em vez de modificar as paradas do lado da corretora; isso corresponde ao design de alto nível API de API e evita estado obsoleto durante a execução na simulação.
- As matrizes de previsão são recriadas sempre que `PastBars` ou `ModelOrder` mudam, de modo que as edições instantâneas dos parâmetros afetam imediatamente o modelo AR sem reiniciar a estratégia.
- Para visualizar o comportamento você pode anexar um gráfico no Designer: a estratégia já desenha velas e executa negociações na área padrão. Estender a amostra com séries personalizadas (por exemplo, caminho de previsão) é simples, se desejado.
