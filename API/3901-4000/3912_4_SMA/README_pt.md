# 4 SMA Estratégia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia 4 SMA replica o consultor especialista MetaTrader **4 SMA.mq4**. Ele funciona em velas de 30 minutos calculadas com preços medianos e compara quatro médias móveis simples (5, 20, 40 e 60 períodos) para detectar rompimentos de impulso. A porta StockSharp mantém o comportamento de posição única do código original e usa auxiliares API de alto nível para entradas de mercado e gerenciamento de risco.

## Lógica de negociação
- Calcule o preço médio `(high + low) / 2` para cada vela finalizada e insira-o nos quatro SMAs.
- **Entrada longa** acontece quando o rápido SMA está acima do médio SMA, o médio SMA está acima do lento SMA, o lento SMA está acima do muito lento SMA em pelo menos uma etapa de preço, e o lento anterior SMA estava abaixo ou igual ao muito lento SMA. Apenas uma posição longa pode estar ativa por vez.
- **Entrada curta** é a condição de espelho: o rápido SMA está abaixo do médio SMA, o médio SMA está abaixo do lento SMA, o muito lento SMA está acima do lento SMA em pelo menos uma etapa de preço, e o lento anterior SMA estava acima ou igual ao muito lento SMA. Apenas uma posição curta pode estar ativa por vez.

## Gerenciamento de posição
- A estratégia fecha posições compradas quando o lento SMA cruza abaixo do muito lento SMA e fecha posições curtas quando o lento SMA cruza acima do muito lento SMA.
- Os níveis de proteção são pré-calculados após cada entrada. As distâncias de stop-loss e take-profit seguem as configurações originais baseadas em pontos e dependem da etapa do preço do título.
- Os trailing stops são ativados depois que o preço ultrapassa a distância final configurada. A parada é seguida vela por vela e nunca afrouxada.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| Tipo de vela | Série de velas usada para cálculos (30 minutos por padrão). | Prazo M30 |
| Obter lucro | Distância de lucro em pontos. | 50 |
| StopLoss | Distância de stop-loss em pontos. | 50 |
| TrailingStop | Distância de parada final em pontos. | 11 |
| Comprimento Rápido | Comprimento do rápido SMA. | 5 |
| Comprimento Médio | Comprimento do meio SMA. | 20 |
| Comprimento Lento | Comprimento da lentidão SMA. | 40 |
| Comprimento muito lento | Comprimento do muito lento SMA. | 60 |

Todos os parâmetros numéricos são expostos para otimização por meio da IU de parâmetro StockSharp.

## Diferenças da versão MQL
- O trailing stop original manipulou ordens MT4 diretamente; o porto recalcula os preços de saída e emite ordens de mercado quando os níveis são ultrapassados.
- Os cálculos conscientes do nível de preço permitem que a estratégia opere em instrumentos com tamanhos de ticks não forex.
- A implementação do StockSharp depende de ligações `SubscribeCandles` de alto nível e parâmetros de estratégia, mantendo-se próximo das práticas recomendadas da estrutura.
