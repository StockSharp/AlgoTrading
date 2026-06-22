# Estratégia de Alinhamento Duplo ZigZag
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um port StockSharp do especialista MQL5 «Double ZigZag». Recria a lógica de confirmação dual de ZigZag combinando
dois detectores de swings com diferentes janelas de retrocesso. Uma operação é acionada somente quando ambos os detectores concordam
em três pivôs consecutivos e o swing mais recente mostra força suficiente comparado aos anteriores.

## Conceito

- O detector de swings rápido aproxima as configurações originais ZigZag(13, 5, 3) usando uma janela deslizante de máximos/mínimos.
- O detector de swings lento usa uma janela mais longa (padrão x8) para confirmar pontos de virada principais.
- Quando ambos os detectores mudam de direção na mesma vela, um pivô de «alinhamento» é registrado junto com o número de swings
  rápidos ocorridos desde o alinhamento anterior. Esses contadores são análogos diretos dos contadores `up` e `dw` do EA original.

## Configuração Comprado

1. O alinhamento mais recente é um swing de alta, o alinhamento anterior é um swing de baixa, e o anterior a esse também é um swing de alta.
2. O número de swings rápidos acumulados desde o último alinhamento é maior que a contagem do segmento anterior multiplicada por
   `StrengthMultiplier` (padrão 2.1). Isso emula a condição original `up > dw * k`.
3. O swing de alta mais recente rompe acima do swing de baixa intermediário de forma mais agressiva que a alta mais antiga,
   ou seja `(previousHigh - swingLow) * BreakoutMultiplier < (newestHigh - swingLow)` com o mesmo multiplicador padrão de 2.1.
4. Quando todos os critérios são atendidos, a estratégia compra um volume igual ao `Volume` configurado mais qualquer posição vendida
   pendente para que a posição líquida fique comprada.

## Configuração Vendido

1. O alinhamento mais recente é um swing de baixa, o alinhamento anterior é um swing de alta, e o anterior a esse é outro swing de baixa.
2. A contagem do segmento mais recente é menor que a contagem anterior dividida por `StrengthMultiplier` (a verificação traduzida
   `up * k < dw`).
3. O swing de baixa atual rompe abaixo do swing de alta intermediário de forma mais agressiva que a baixa mais antiga usando `BreakoutMultiplier`.
4. A estratégia vende volume suficiente para fechar qualquer posição comprada existente e estabelecer uma posição líquida vendida.

## Gestão de Posições

- Os sinais são mutuamente exclusivos; um novo comprado fecha automaticamente qualquer vendido e vice-versa.
- Não há ordens de stop-loss ou take-profit. As posições são mantidas até que um sinal de alinhamento oposto apareça.
- A estratégia opera no tipo de vela especificado por `CandleType` (padrão período de 1 minuto).

## Valores Padrão

- `FastLength` = 13
- `SlowLength` = 104
- `StrengthMultiplier` = 2.1
- `BreakoutMultiplier` = 2.1
- `CandleType` = período `TimeSpan.FromMinutes(1)`

## Tags

- **Categoria**: Seguidor de tendência / Reconhecimento de padrão
- **Direção**: Comprado/Vendido
- **Indicadores**: ZigZag (aproximado), Highest/Lowest
- **Stops**: Nenhum
- **Período**: Intradiário por padrão
- **Complexidade**: Intermediário (requer rastreamento sincronizado de swings)
- **Sazonalidade**: Não
- **Redes neurais**: Não
- **Divergência**: Não
- **Nível de risco**: Médio devido à exposição contínua sem stops
