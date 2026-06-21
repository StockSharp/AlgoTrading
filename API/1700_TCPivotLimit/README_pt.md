# Estratégia TCPivotLimit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera em torno dos níveis clássicos de pontos de pivô diários. Os pontos de pivô são calculados a partir da máxima, mínima e preços de fechamento do dia anterior. Ordens limitadas são colocadas nos níveis de suporte ou resistência selecionados e as posições são gerenciadas com níveis predefinidos de stop-loss e take-profit.

## Parâmetros
- **Volume** – volume da ordem.
- **Target Variant** – seleciona quais níveis de suporte/resistência são usados para entrada, stop e alvo:
  1. Entrada em S1/R1, stop em S2/R2, alvo em R1/S1.
  2. Entrada em S1/R1, stop em S2/R2, alvo em R2/S2.
  3. Entrada em S2/R2, stop em S3/R3, alvo em R1/S1.
  4. Entrada em S2/R2, stop em S3/R3, alvo em R2/S2.
  5. Entrada em S2/R2, stop em S3/R3, alvo em R3/S3.
- **Intraday Close** – fechar qualquer posição aberta às 23:00.
- **Modify Stop Loss** – mover o stop loss para o primeiro nível alvo após ele ser atingido.

## Lógica de negociação
1. No início de cada dia, a estratégia calcula o pivô e três níveis de resistência e três de suporte usando os dados do dia anterior.
2. Quando o preço toca o nível de suporte ou resistência escolhido, uma ordem limitada é enviada na direção oposta.
3. A posição é fechada quando o nível de stop-loss ou take-profit é atingido. A modificação opcional do stop-loss pode reduzir o risco após o primeiro alvo.
4. Se *Intraday Close* estiver ativado, qualquer posição aberta é fechada no final da sessão de negociação.
