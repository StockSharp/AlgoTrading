# Estratégia especializada Alligator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia Expert Alligator** é uma versão fiel StockSharp do consultor especialista integrado do MetaTrader 5 `Expert_Alligator.mq5`. O especialista original orienta suas decisões de negociação a partir do indicador Bill Williams' Alligator, que consiste em três médias móveis suavizadas deslocadas para o futuro: a mandíbula (azul), os dentes (vermelho) e os lábios (verde). Ao monitorar como essas linhas se contraem e se expandem, o EA identifica novos cruzamentos e espera que a "boca" se abra antes que outra negociação possa ser realizada. Esta conversão C# recria o mesmo fluxo de trabalho com a estratégia de alto nível API e o conjunto de indicadores de StockSharp.

## Lógica de negociação

1. **Preparação de indicadores**
   - Construa três médias móveis suavizadas do preço mediano usando os comprimentos Alligator clássicos (13, 8 e 5) e aplique os deslocamentos para frente padrão (8, 5 e 3 barras, respectivamente).
   - Armazene um histórico contínuo de cada linha deslocada para que os deslocamentos passados e futuros usados pelo MetaTrader EA (por exemplo, `LipsTeethDiff(-2)`) possam ser avaliados com segurança.

2. **Condições de entrada**
   - *Negociações longas*: são acionadas quando os spreads lábios-dentes e dentes-mandíbula diminuem por três barras consecutivas deslocadas, permanecendo acima de zero. Isso reproduz a exigência do EA de que a linha verde cruze para baixo através da vermelha, confirmando uma abertura da boca para cima.
   - *Negociações curtas*: refletem a lógica longa com spreads diminuindo abaixo de zero, indicando os lábios cruzando para cima através dos dentes e da mandíbula.
   - Depois que uma negociação é aberta, a estratégia levanta um sinalizador interno `crossed` que bloqueia entradas adicionais até que os três spreads Alligator aumentem pelo menos na distância **Cross Measure** configurada.

3. **Condições de saída**
   - *Posições longas* fecham quando o spread lábios-dentes fica negativo no valor deslocado mais recente, enquanto permanece positivo nos dois valores mais antigos (índices `-1`, `0`, `1` no EA original).
   - *Posições curtas* saem quando a mesma sequência ocorre na direção oposta.

## Parâmetros

| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `Order Volume` | Tamanho da negociação em lotes ou contratos repassados para `BuyMarket`/`SellMarket`. | `0.1` |
| `Candle Type` | Prazo para assinatura da vela. | `1 Hour` |
| `Jaw Period` | Comprimento médio móvel suavizado para a linha da mandíbula. | `13` |
| `Jaw Shift` | Deslocamento para frente (em barras) da linha da mandíbula. | `8` |
| `Teeth Period` | Comprimento médio móvel suavizado para a linha dos dentes. | `8` |
| `Teeth Shift` | Deslocamento para frente (em barras) da linha dos dentes. | `5` |
| `Lips Period` | Comprimento médio móvel suavizado para a linha dos lábios. | `5` |
| `Lips Shift` | Deslocamento para frente (em barras) da linha dos lábios. | `3` |
| `Cross Measure` | Spread mínimo (em MetaTrader pontos) que deve se desenvolver após um cruzamento antes que outra negociação possa ser disparada. | `5` |

## Notas de implementação

- A estratégia calcula o preço médio `(High + Low) / 2` para cada vela finalizada e o alimenta em três instâncias `SmoothedMovingAverage`.
- Os históricos deslocados são implementados com matrizes de tamanho fixo para espelhar a maneira como MetaTrader expõe índices futuros como `-1` ou `-2` quando as linhas Alligator são deslocadas para frente.
- O valor MetaTrader `_Point` é emulado por meio do símbolo `PriceStep`. Quando este último não está disponível, o código volta para `10^-Decimals` ou `0.0001`.
- A saída do gráfico corresponde ao EA plotando a mandíbula, os dentes e os lábios no painel principal da vela, permitindo uma validação visual rápida.

## Uso

1. Anexe a estratégia a um `Connector` com um título que forneça o tipo de vela desejado (velas padrão de uma hora).
2. Ligue para `Start()` quando o fluxo de dados de mercado estiver pronto.
3. Opcional: ajuste os comprimentos Alligator, os turnos ou o limite de medida cruzada para testar comportamentos personalizados.
4. Monitore posições e desempenho por meio das interfaces padrão StockSharp.

Não são necessários trailing stops ou módulos de gerenciamento de dinheiro adicionais porque o EA original usa tamanho de lote fixo e depende exclusivamente da geometria da linha Alligator para gerenciamento de negociação.
