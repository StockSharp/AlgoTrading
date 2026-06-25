# Estratégia SilverTrend ColorJFatl Digit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia SilverTrend ColorJFatl Digit mescla dois sistemas MetaTrader clássicos em uma estratégia unificada de alto nível do StockSharp. O bloco SilverTrend identifica rompimentos direcionais medindo o quanto o preço percorre dentro de um canal de estilo Donchian curto. O bloco ColorJFatl Digit suaviza o preço com uma Média Móvel Jurik (JMA) e avalia sua inclinação após arredondar a saída para o número configurado de dígitos. Somente quando ambos os subsistemas concordam sobre a direção a estratégia abre ou mantém uma posição. Quando os sinais divergem, a estratégia sai para flat.

O design mantém o espírito do consultor especialista original enquanto aproveita a API de alto nível do StockSharp: assinaturas de candles, vínculos de indicadores, atrasos de sinal baseados em filas e auxiliares de desenho de gráficos. Cada etapa está amplamente documentada para tornar pesquisa e otimização posteriores simples.

## Lógica da estratégia

### 1. Detector de rompimento SilverTrend

* Usa indicadores `Highest` e `Lowest` com `SilverTrendLength + 1` candles para formar o canal de preço recente.
* O canal é apertado pelo parâmetro `SilverTrendRisk`: quanto maior o valor de risco, mais próximos os limiares de rompimento ficam da linha central do canal (fórmula original `33 - risk`).
* Quando o preço de fechamento rompe acima do limiar superior ajustado, o bloco SilverTrend reporta uma tendência altista (`+1`). Quando rompe abaixo do limiar inferior, o bloco reporta uma tendência baixista (`-1`).
* Um atraso configurável (`SilverTrendSignalBar`) aguarda `n` candles totalmente fechados antes que o sinal seja considerado válido, imitando a lógica `SignalBar` do MQL.

### 2. Filtro de confirmação ColorJFatl Digit

* Um `JurikMovingAverage` suaviza o preço aplicado selecionado por `JmaPriceType`. Todos os tipos de preço aplicado do MetaTrader são suportados (fechamento, abertura, mediana, típico, ponderado, simples, quarto, modos de seguimento de tendência e cálculo Demark).
* A saída Jurik é arredondada para `JmaRoundDigits`, reproduzindo o comportamento do indicador "digit" discretizado.
* O sinal de inclinação do JMA arredondado se torna o sinal de tendência. Quando a inclinação é positiva, o filtro emite `+1`; quando negativa, `-1`. Inclinações planas herdam o estado anterior para evitar a alternância abrupta.
* Como com SilverTrend, `JmaSignalBar` atrasa a execução, exigindo que a inclinação se mantenha pelo número solicitado de candles fechados.

### 3. Execução de trades

* **Entrada:**
  * Ir comprado quando tanto SilverTrend quanto ColorJFatl reportam `+1` e não há exposição comprada existente.
  * Ir vendido quando ambos os blocos reportam `-1` e não há exposição vendida existente.
* **Saída:**
  * Fechar a posição atual imediatamente quando os sinais divergem (por exemplo, um bloco diz `+1`, o outro `-1` ou `0`).
  * Reversões fecham automaticamente a exposição oposta antes de abrir a nova posição para evitar médias.
* Ordens ativas são canceladas antes das reversões para manter o livro limpo.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| `SilverTrendCandleType` | Série de candles usada para calcular o canal de rompimento SilverTrend. Padrão equivalente a H4. |
| `SilverTrendLength` | Comprimento de retrospectiva para o cálculo do canal (parâmetro `SSP` no EA original). |
| `SilverTrendRisk` | Modificador de risco que aperta os limiares de rompimento (`33 - risk`). Valores mais altos reagem mais rápido mas com mais sinais falsos. |
| `SilverTrendSignalBar` | Número de candles totalmente fechados a aguardar antes de aceitar uma mudança de cor SilverTrend. |
| `ColorJfatlCandleType` | Série de candles que alimenta o filtro Jurik. Pode diferir do período SilverTrend. |
| `JmaLength` | Comprimento da Média Móvel Jurik. |
| `JmaSignalBar` | Atraso (em barras) antes de agir sobre as viradas de inclinação Jurik. |
| `JmaPriceType` | Modo de preço aplicado para a entrada Jurik (fechamento, abertura, mediana, variantes de seguimento de tendência, Demark, etc.). |
| `JmaRoundDigits` | Número de decimais usados ao arredondar a saída Jurik, emulando o indicador digitalizado. |

## Notas de implementação

* Os atrasos de sinal são implementados com pequenas filas FIFO em vez de grandes arrays históricos, garantindo que a estratégia permaneça eficiente em memória e fiel ao Consultor Especialista original.
* O código nunca consulta buffers de indicadores diretamente. Em vez disso, vincula indicadores através da API de alto nível `SubscribeCandles().Bind(...)`, seguindo as diretrizes em `AGENTS.md`.
* Comentários inline em inglês explicam cada decisão: quando os limiares são recalculados, como as inclinações são computadas, por que as ordens são canceladas e como o consenso é imposto.
* O suporte a gráficos está incluído: quando um gráfico está disponível, a estratégia desenha candles de preço, linhas do canal SilverTrend e os próprios trades para visualizar decisões ao vivo.

## Dicas de uso

1. **Mercados e período:** O sistema original foi projetado para gráficos H4 de forex. Criptomoedas e futuros de commodities com comportamento de swing claro também funcionam bem. Para mercados mais rápidos, reduzir `SilverTrendLength` e `JmaLength` com cautela.
2. **Otimização:** Otimizar tanto o comprimento de rompimento (`SilverTrendLength`) quanto o comprimento de confirmação (`JmaLength`) juntos — encurtar apenas uma perna geralmente cria sinais conflitantes.
3. **Experimentos com preço aplicado:** Experimentar os modos de preço de seguimento de tendência ao trabalhar com feeds Heikin-Ashi ou Renko; eles geralmente suavizam o ruído melhor do que preços de fechamento puros.
4. **Controle de risco:** Combinar as saídas incorporadas com stops no nível do portfólio. Como ambos os módulos têm um ligeiro atraso, picos de volatilidade ainda podem se estender além do canal antes do filtro virar.
5. **Dimensionamento de posição:** A estratégia deixa o gerenciamento de volume para a propriedade base `Strategy.Volume`. Ajustá-la ou integrar as extensões de gestão monetária do StockSharp se piramidação ou escalonamento for necessário.

## Ideias para pesquisa adicional

* Adicionar proteção de stop-loss e take-profit baseada em ATR através de `StartProtection` uma vez que testes confirmem os limiares preferidos.
* Alimentar candles de período superior (por exemplo, Diário) na confirmação Jurik enquanto mantém SilverTrend em H4 para introduzir um filtro de tendência.
* Combinar com filtros baseados em volume (Volume em Balanço, divergência VWAP) para confirmação adicional antes das entradas.
