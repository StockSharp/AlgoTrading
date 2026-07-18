# Estratégia de Rede MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é uma porta C# do consultor especialista MetaTrader 5 **MAGrid.mq5**. Ele mantém uma grade protegida de posições de compra e venda em torno de uma média móvel exponencial (EMA). A ideia é manter a grade equilibrada em torno da âncora EMA. Quando o preço cruza etapas de distância predefinidas acima ou abaixo de EMA, a estratégia fecha uma posição do lado oposto da grade e abre uma nova posição na direção do rompimento. Isso centraliza constantemente a cesta em torno da média móvel.

## Fonte Original

- **MQL pasta do repositório:** `MQL/38303`
- **Arquivo original:** `MAGrid.mq5`
- **Plataforma:** MetaTrader 5 (modo de hedge)

## Lógica de negociação

1. **EMA Âncora**
   - O período EMA é configurável (padrão 48).
   - O EMA é calculado na série de velas selecionada.
   - Os níveis de grade são calculados como múltiplos do parâmetro `Distance` acima e abaixo de EMA.

2. **Inicialização da grade**
   - O tamanho efetivo da grade é forçado a ser uniforme para espelhar ambos os lados em torno do EMA.
   - O índice de grade atual é determinado comparando o último preço de fechamento com os níveis baseados em EMA.
   - Uma cesta simétrica de ordens de compra e venda de mercado é aberta de modo que metade das posições fique abaixo de EMA e a outra metade acima dele.

3. **Manutenção da rede**
   - Quando o preço fecha acima do próximo nível superior da grade, a estratégia:
     - Incrementa o índice da grade.
     - Fecha uma ordem longa se sobrar alguma exposição.
     - Abre uma nova ordem curta para estender a metade superior da grade.
   - Quando o preço fecha abaixo do próximo nível inferior da grade, a estratégia:
     - Diminui o índice da grade.
     - Fecha uma ordem curta se sobrar alguma exposição.
     - Abre uma nova ordem longa para reconstruir a metade inferior da grade.
   - Se um lado da grade ficar sem exposição, o gatilho correspondente será desativado até que novas ordens sejam abertas.

4. **Tratamento de pedidos**
   - Os pedidos são rastreados por meio de um mapa interno simples para distinguir entre preenchimentos de abertura e fechamento.
   - A estratégia armazena contadores de exposição separados para as cestas longas e curtas. Isso reflete o comportamento de hedge da versão MQL ao usar o modelo de posição líquida de StockSharp.

## Parâmetros

| Nome | Padrão | Descrição |
| --- | --- | --- |
| `MaPeriod` | 48 | EMA período usado para o nível âncora. |
| `GridAmount` | 6 | Número de etapas da grade; automaticamente arredondado para um valor par. |
| `Distance` | 0,005 | Espaçamento relativo entre os níveis da grade (por exemplo, 0,005 = 0,5%). |
| `OrderVolume` | 0,1 | Volume enviado com cada ordem de mercado. |
| `CandleType` | Período diário | Série de velas usada para calcular EMA e avaliar sinais. |

## Gestão de risco

- A estratégia não implementa regras de stop-loss ou take-profit; o risco é controlado através do número de etapas da grade e do volume do pedido.
- Como a grade mantém exposição longa e curta, o valor do portfólio pode permanecer relativamente estável, mas o uso da margem cresce com o tamanho e a distância da grade.
- Considere usar controles de risco de portfólio (rebaixamento máximo, uso de capital) no nível da estratégia ou do portfólio.

## Notas de conversão

- A implementação C# reproduz a lógica protegida rastreando separadamente a exposição longa e curta.
- O cálculo do volume dependente da conta de MQL foi substituído por um parâmetro `OrderVolume` configurável para maior clareza.
- As assinaturas de velas dependem do StockSharp API de alto nível usando `SubscribeCandles().Bind(...)` de acordo com as diretrizes do projeto.
