# Estratégia de Tendência RSI + EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este sistema combina um oscilador clássico de Relative Strength Index (RSI) com um filtro de tendência de dupla média móvel. O RSI fornece leituras de curto prazo de sobrecompra e sobrevenda enquanto as duas médias móvies exponenciais (EMAs) definem a tendência mais ampla. A estratégia só faz operações na direção da EMA rápida em relação à EMA lenta, ajudando a evitar configurações contratendência durante movimentos direcionais fortes.

Quando o momentum do preço empurra o RSI abaixo do limiar de sobrevenda e a EMA rápida está acima da EMA lenta, assume-se que o mercado está em tendência de alta e uma posição comprada é aberta. Ao contrário, se RSI sobe acima do nível de sobrecompra enquanto a EMA rápida ainda supera a EMA lenta, a estratégia inicia uma operação vendida, esperando um recuo de curto prazo dentro do canal de tendência maior.

As posições são fechadas quando RSI sai da zona extrema para o lado oposto, sinalizando que o movimento de reversão à média provavelmente se esgotou. O método é simples mas eficaz para capturar breves oscilações de momentum em ambientes de tendência. Funciona bem em instrumentos líquidos onde extremos de RSI ocorrem frequentemente mas a direção da tendência permanece intacta.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `RSI < oversold` e `EMA1 > EMA2`.
  - **Vendido**: `RSI > overbought` e `EMA1 > EMA2`.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: `RSI > overbought`.
  - **Vendido**: `RSI < oversold`.
- **Stops**: Nenhum integrado.
- **Valores padrão**:
  - `RSI Length` = 14.
  - `Overbought/Oversold` = 70 / 30.
  - `EMA Lengths` = 150 / 600.
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: Múltiplos
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
