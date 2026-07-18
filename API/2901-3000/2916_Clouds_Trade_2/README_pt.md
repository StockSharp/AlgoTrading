# Estratégia Clouds Trade 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma portagem em C# do consultor especializado "cloud's trade 2" de Vladimir Karputov. Ela negocia rompimentos confirmados por dois fractais recentes de Bill Williams e um cruzamento de sobrecomprado/sobrevendido no oscilador estocástico. O gerenciamento de operações replica os inputs originais com stop loss, take profit, trailing stop e bloqueios mínimos de lucro configuráveis.

## Lógica de negociação

- **Dados**: velas de período único (padrão 15 minutos).
- **Indicadores**:
  - Oscilador estocástico usando o lookback %K configurado, desaceleração e suavização %D.
  - Janela deslizante de máximo/mínimo de cinco velas para reconstruir fractais superiores e inferiores.
- **Entrada**:
  - **Comprado**: dois fractais inferiores consecutivos aparecem mais recentemente do que qualquer fractal superior **ou** o %D estocástico cai abaixo de 20 enquanto cruza abaixo de %K. Nenhuma posição deve estar aberta e o filtro opcional de uma operação por dia deve permitir uma nova entrada.
  - **Vendido**: dois fractais superiores consecutivos aparecem primeiro **ou** o %D estocástico sobe acima de 80 enquanto cruza acima de %K.
- **Saídas e proteção**:
  - Offsets estáticos de stop loss e take profit do preço de entrada.
  - Trailing stop opcional que se move apenas quando o lucro atual excede a distância de trailing configurada mais o passo.
  - Fechar posições assim que um alvo de lucro baseado em dinheiro ou em distância de preço é atingido.
  - Os stops são emulados inspecionando as máximas/mínimas das velas, replicando o comportamento gerenciado pelo corretor na versão MQL.

## Parâmetros

- **Order Volume**: tamanho de ordem base para entradas.
- **Stop/Take Offsets**: distâncias de preço absolutas; ajustar ao valor de tick do instrumento para reproduzir os inputs originais baseados em pips.
- **Trailing Stop & Step**: offsets em unidades de preço que governam quando o stop é movido.
- **Min Profit (Currency / Points)**: fechar operações assim que o lucro não realizado exceder esses limites.
- **Use Fractals / Use Stochastic**: habilitar qualquer componente de sinal de forma independente.
- **One Trade Per Day**: evitar múltiplas entradas durante a mesma data de negociação.
- **Stochastic Settings**: comprimentos de lookback de %K, desaceleração de %K e suavização de %D.
- **Candle Type**: período para a assinatura de velas da estratégia.

## Notas

- As verificações de lucro de posição aproximam os ajustes originais de comissão/swap usando o movimento de preço vezes o tamanho da posição.
- A lógica de trailing segue a implementação MQL ao exigir que o lucro exceda a distância de trailing mais o passo antes de deslocar o stop.
- Para imitar os inputs padrão MQL baseados em pips em pares Forex, definir os offsets de stop/take como o valor de pip desejado multiplicado pelo valor do ponto do instrumento (por exemplo, 50 pips ≈ 0.005 para cotações de cinco dígitos).
