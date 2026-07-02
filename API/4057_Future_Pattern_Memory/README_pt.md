# FuturePatternMemoryEstratégia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
`FuturePatternMemoryStrategy` é uma versão StockSharp dos clássicos MetaTrader especialistas **FutureMA** e **FutureMACD**. Os robôs originais registraram sequências de diferenças de indicadores em arquivos CSV, reutilizaram as estatísticas armazenadas e decidiram se as condições atuais favoreciam rompimentos de alta ou de baixa. Esta versão C# mantém a mesma ideia, mas substitui o sistema de arquivos por um armazém de padrões na memória e expõe cada botão como um parâmetro de estratégia. A estratégia pode operar no spread da média móvel suavizada (a lógica FutureMA) ou no histograma MACD (a lógica FutureMACD).

A estratégia avalia cada vela finalizada em cinco etapas:

1. **Projeção do indicador** – calcule o oscilador selecionado (spread MA ou histograma MACD) e normalize-o com um fator de escala configurável. Os valores são discretizados para números inteiros para criar assinaturas de padrões compactos.
2. **Hashing de padrão** – mantém uma janela deslizante dos valores normalizados `AnalysisBars` mais recentes. Cada vez que uma nova barra fecha, a janela é convertida em uma string hash exclusiva que identifica o contexto atual do mercado.
3. **Análise de oscilação histórica** – inspecione as velas `FractalDepth` anteriores, meça a distância entre a abertura mais antiga e os extremos mais altos/mais baixos e converta esses intervalos em pontos. Essas distâncias são as expectativas de recompensa que os robôs originais acumularam em seus arquivos CSV.
4. **Atualização de memória ponderada** – a chave hash é usada para recuperar ou criar uma entrada no dicionário de padrões. As expectativas de alta e baixa de take-profit são atualizadas com uma média móvel ponderada controlada por `ForgettingFactor`, que reproduz o coeficiente de “esquecimento” (`zabyvaemost`) do código MQL.
5. **Avaliação e execução do sinal** – se a expectativa de alta dominar, o padrão foi visto mais de `MinimumMatches` vezes e o ganho projetado estiver acima de `MinimumTakeProfit`, a estratégia entra ou aumenta para uma posição longa. O ramo de baixa funciona simetricamente. Os níveis de proteção são derivados das estatísticas armazenadas e opcionalmente acompanhados à medida que a negociação se move a favor.

## Notas de conversão
- Ambos os especialistas MetaTrader são mesclados em uma estratégia configurável por meio do parâmetro `Source`, permitindo alternar entre o mecanismo baseado em MA e o mecanismo baseado em MACD sem recompilação.
- A persistência baseada em arquivo foi substituída por um `Dictionary<string, PatternStats>` que mantém todas as estatísticas na memória durante a execução. Isso evita E/S de arquivo e permanece dentro do modelo sandbox StockSharp.
- O gerenciamento de posição replica o posicionamento stop/alvo original: o stop usa o swing médio completo, enquanto o take-profit usa `StatisticalTakeRatio` (o `Stat_Take_Profit` original). Quando `EnableTrailingStop` é verdadeiro, o stop é movido em um quarto de passo da distância do lucro, exatamente como o especialista MQL modificou suas ordens.
- O modo manual (`ManualMode`) desativa a colocação automatizada de pedidos, mas continua a coletar estatísticas, correspondendo ao comportamento original da sinalização `Ruchnik`.
- A ampliação (`AllowAddOn`) imita o sinalizador `dokupka` e permite que a estratégia adicione volume sempre que o padrão se repete em uma nova barra.

## Lógica de negociação em detalhes
- **Fonte do indicador**
  - *MA spread*: calcula duas médias móveis suavizadas (SMMA 6 e SMMA 24) sobre preços medianos e utiliza sua diferença.
  - *MACD histograma*: calcula a diferença entre a linha principal MACD e a linha de sinal (configuração 26/12/9 por padrão).
- **Normalização**: `NormalizationFactor` reproduz `tocnost`; ele dimensiona a diferença bruta antes de convertê-la em uma assinatura inteira. A conversão divide por `100 * MinPriceStep` para manter unidades baseadas em pip.
- **Memória de padrões**: o dicionário armazena, para cada assinatura, o número de correspondências de alta, a distância média de alta, o número de correspondências de baixa e a distância média de baixa. Os valores são atualizados com a fórmula ponderada `(current + input * ForgettingFactor) / (1 + ForgettingFactor)`.
- **Regras de entrada**:
  - Longo: expectativa de alta ≥ expectativa de baixa, correspondências de alta > `MinimumMatches`, distância de alta esperada > `MinimumTakeProfit`.
  - Curto: expectativa de baixa ≥ expectativa de alta, correspondências de baixa > `MinimumMatches`, distância de baixa esperada > `MinimumTakeProfit`.
- **Gerenciamento de risco**: o stop loss é definido como uma oscilação média completa em relação à posição; o take-profit usa `StatisticalTakeRatio` dessa oscilação. Os trailing stops se movem depois que o preço percorre um quarto da distância, assim como a rotina de trailing original.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `CandleType` | Prazo principal utilizado para cálculos. | Velas de 30 minutos |
| `Source` | Escolha entre spread MA (`FutureMA`) e MACD histograma (`FutureMACD`). | `MaSpread` |
| `FastMaLength` / `SlowMaLength` | Comprimentos de SMMA quando `Source = MaSpread`. | 24/06 |
| `MacdFastLength` / `MacdSlowLength` / `MacdSignalLength` | MACD períodos em que `Source = MacdHistogram`. | 26/12/9 |
| `AnalysisBars` | Número de barras que formam a assinatura do padrão. | 8 |
| `FractalDepth` | Número de velas anteriores usadas para medir distâncias de rompimento. | 4 |
| `MinimumMatches` | Número necessário de ocorrências armazenadas antes de realizar uma negociação. | 5 |
| `MinimumTakeProfit` | Distância mínima esperada (em pontos) para aceitar o sinal. | 30 |
| `NormalizationFactor` | Fator de escala aplicado à diferença do indicador. | 10 |
| `ForgettingFactor` | Peso aplicado a novas medições na memória padrão. | 1,5 |
| `StatisticalTakeRatio` | Taxa de lucro em relação à oscilação medida. | 0,5 |
| `EnableTrailingStop` | Ativa a lógica de trailing stop de um quarto de passo. | `false` |
| `ManualMode` | Colete estatísticas, mas ignore a colocação de pedidos. | `false` |
| `AllowAddOn` | Permitir a ampliação quando um padrão se repete. | `true` |
| `Volume` | Tamanho do pedido usado para entradas. | 0,1 |

## Conselhos práticos
- A estratégia depende de hashes discretizados, então escolha `NormalizationFactor` e `AnalysisBars` com cuidado: valores muito grandes produzem assinaturas esparsas, enquanto valores muito pequenos misturam estados distintos.
- Ao executar negociações ao vivo, considere exportar o dicionário de padrões após a sessão se precisar de persistência entre as execuções.
- Como a versão MQL armazenou dados por símbolo/período, é recomendado manter uma instância de estratégia dedicada por instrumento/período para evitar contaminação cruzada de estatísticas.
