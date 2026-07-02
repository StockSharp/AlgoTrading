# Média Móvel com Quadros
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Conversão do consultor especialista MetaTrader 5 **"Média Móvel com Quadros"**. O sistema original avalia a relação entre os preços de abertura/fechamento de cada vela e uma média móvel simples deslocada (SMA) enquanto exibe vários "quadros" de otimização nos gráficos. Esta porta StockSharp concentra-se na lógica de negociação: ela reage apenas uma vez por barra concluída, abre uma única posição de compensação e reflete as regras de gerenciamento de dinheiro do código-fonte.

## Lógica de negociação

- **Fonte de dados** – a estratégia assina o período configurado (`CandleType`) e processa apenas velas finalizadas, o que reproduz a restrição MetaTrader `if(rt[1].tick_volume>1) return;`.
- **Indicador** – uma média móvel simples com período `MovingPeriod`. A saída do indicador é deslocada para frente em `MovingShift` velas concluídas, mantendo um buffer de valores anteriores.
- **Aquecimento** – a negociação é suspensa até que pelo menos 100 velas concluídas sejam coletadas, correspondendo à guarda `Bars(_Symbol,_Period)>100` original.
- **Condições de entrada**
  - Vá **comprado** quando a vela abrir abaixo do SMA deslocado e fechar acima dele.
  - Opere **short** quando a vela abrir acima do SMA deslocado e fechar abaixo dele.
  - O motor impõe uma posição única: a exposição oposta é achatada antes de entrar na nova direção.
- **Condições de saída** – uma posição longa existente é fechada quando o preço de abertura está acima e o preço de fechamento está abaixo do deslocado SMA; os shorts são fechados no cruzamento oposto. Novas negociações não são abertas na mesma barra após uma saída, assim como o especialista original.

## Dimensionamento de posição e risco

- **MaximumRisk** – determina o volume bruto do pedido como `Portfolio.CurrentValue * MaximumRisk / price` quando os dados do portfólio estão disponíveis. Se o feed do corretor não fornecer informações de patrimônio, a estratégia volta para a propriedade manual `Volume`.
- **DecreaseFactor** – após mais de uma negociação consecutiva perdida, o tamanho da próxima posição é reduzido em `volume * losses / DecreaseFactor`, imitando a lógica de redução de lote de MetaTrader. Qualquer negociação lucrativa zera o contador.
- **Alinhamento de volume** – o tamanho calculado é normalizado para o `VolumeStep` do instrumento, fixado entre `MinVolume` e `MaxVolume` e arredondado para duas casas decimais quando a exchange não publica uma etapa.

## Notas adicionais

- A visualização de "frames" MetaTrader não é portada porque StockSharp já fornece painéis de otimização avançados. A lógica de negociação, o tempo do sinal e o comportamento de dimensionamento permanecem fiéis à fonte.
- Todos os valores do indicador são consumidos diretamente do retorno de chamada `Bind`; nenhuma chamada manual `GetValue` é usada.
- O rastreamento de perdas consecutivas é implementado dentro de `OnOwnTradeReceived`, permitindo que a estratégia reaja corretamente aos preenchimentos parciais e ao comportamento da compensação.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `MaximumRisk` | `0.02` | Fração do patrimônio do portfólio arriscado em cada entrada. |
| `DecreaseFactor` | `3` | Divisor usado para diminuir o tamanho da posição após duas ou mais perdas consecutivas. |
| `MovingPeriod` | `12` | Comprimento da média móvel simples aplicada aos preços de fechamento. |
| `MovingShift` | `6` | Número de velas concluídas usadas para compensar o SMA avanço no tempo. |
| `CandleType` | `1h time frame` | Série de velas primárias processadas pela estratégia. |

## Dicas de uso

1. Anexe a estratégia a um título e portfólio no StockSharp Designer ou código.
2. Ajuste o tipo de vela para corresponder ao período do gráfico MetaTrader desejado.
3. Ajuste `MaximumRisk` e `DecreaseFactor` para corresponder ao tamanho da sua conta e à tolerância ao risco desejada.
4. Execute backtests para validar se os sinais de cruzamento estão alinhados com os resultados MetaTrader originais.
