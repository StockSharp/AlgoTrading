# Estratégia de reversão de força extrema
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Resumo
- Sistema de contratendência convertido do consultor especialista EXSR MetaTrader.
- Combina Bollinger bandas e RSI extremos para localizar movimentos de exaustão.
- Usa dimensionamento de posição baseado em porcentagem com stop-loss fixo e take-profit em pips.

## Lógica de negociação
1. Assine a série de velas configurada (o padrão é velas de 1 hora).
2. Calcule um envelope de bandas Bollinger (período, desvio) e um oscilador RSI.
3. Quando uma vela se fecha:
   - Uma configuração longa requer: RSI abaixo do nível de sobrevenda, mas acima de zero, a mínima da vela abaixo da banda inferior e um corpo de alta (fechamento acima da abertura).
   - Uma configuração curta requer: RSI acima do nível de sobrecompra, a vela alta acima da banda superior e um corpo de baixa (fechamento abaixo da abertura).
4. Apenas uma posição poderá ser aberta por vez. A exposição oposta é fechada antes de reverter.
5. Stops e metas são projetados a partir do preço de preenchimento usando pips estilo MetaTrader. O motor monitora as velas subsequentes e sai quando qualquer um dos níveis é tocado.

## Gestão de capital
- O tamanho do pedido é padronizado para a propriedade `Volume` da estratégia. Quando é zero, a estratégia deriva o volume de `RiskPercent` e a distância de parada.
- O risco é calculado a partir do patrimônio atual do portfólio (substituição para equilíbrio/valor inicial). A distância de stop é traduzida em preço ou unidades monetárias usando o passo e o preço do passo do instrumento.
- O volume é normalizado para a etapa de volume do instrumento, restrições mínimas e máximas.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| Porcentagem de risco | Porcentagem de patrimônio arriscado por negociação. | 1% |
| Stop Loss (pips) | Distância de parada em MetaTrader pips. | 150 |
| Obter lucro (pips) | Distância de lucro em pips. | 300 |
| Bollinger Período | Velas usadas para Bollinger bandas. | 20 |
| Bollinger Desvio | Multiplicador de desvio padrão. | 2,0 |
| RSI Período | Velas usadas para RSI. | 14 |
| RSI Sobrecomprado | Nível RSI considerado extremamente sobrecomprado. | 80 |
| RSI Sobrevenda | Nível RSI considerado extremamente sobrevendido. | 20 |
| Tipo de vela | Prazo de vela para a análise. | 1 hora |

## Notas
- Certifique-se de que o símbolo selecionado exponha a etapa de preço, a etapa de preço e a etapa de volume para um dimensionamento preciso. A estratégia volta a padrões razoáveis ​​quando indisponível.
- A gestão de risco é acionada mesmo quando a negociação está temporariamente desativada, de modo que as saídas de proteção permanecem ativas.
- A lógica processa apenas velas concluídas, espelhando o EA original que funciona na barra anterior.
