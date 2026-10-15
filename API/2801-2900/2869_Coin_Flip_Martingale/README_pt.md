# Estratégia de Lançamento de Moeda Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia de Lançamento de Moeda emula o consultor especialista original do MetaTrader onde as entradas são determinadas por um lançamento de moeda pseudoaleatório. Apenas uma posição pode ser aberta de cada vez. Cada vela concluída atua como um ponto de decisão: quando a operação anterior está plana, a estratégia lança uma moeda e abre imediatamente uma posição comprada ou vendida usando o volume de operação calculado. Cada operação é protegida com níveis de stop loss e take profit, enquanto um stop de seguimento opcional pode ajustar o risco à medida que o mercado se move a favor da posição.

Um modelo de dimensionamento de posição estilo Martingale é implementado. Se a posição anterior foi stoppada, a próxima operação aumentará seu tamanho por um multiplicador configurável. Operações bem-sucedidas redefinem o volume para o tamanho base. Um volume máximo definido pelo usuário previne o crescimento descontrolado do tamanho da operação.

## Regras de operação

1. Em cada vela concluída, a estratégia avalia a posição atual.
2. Quando nenhuma posição está aberta, um número pseudoaleatório seleciona a direção comprada ou vendida. Ambos os lados têm igual probabilidade.
3. Cada nova operação usa o volume base, a menos que a operação anterior tenha terminado com um stop loss. Nesse caso, o volume é multiplicado pelo fator Martingale, respeitando o limite de volume máximo.
4. Preços de stop loss e take profit de proteção são anexados a cada posição. Quando o preço de fechamento atinge esses limites, a posição é encerrada com uma ordem a mercado.
5. O stop de seguimento monitora o movimento favorável. Uma vez que o lucro excede a distância de seguimento mais o passo, o nível de stop é movido em direção ao preço para assegurar ganhos.

## Parâmetros

- **Stop Loss** – distância em passos de preço usada para calcular o stop loss a partir do preço de entrada.
- **Take Profit** – distância em passos de preço adicionada ao preço de entrada para o take profit.
- **Trailing Stop** – distância de lucro que ativa o mecanismo de stop de seguimento. Definir como zero para desabilitar o seguimento.
- **Trailing Step** – lucro adicional mínimo necessário antes que o stop de seguimento seja movido novamente.
- **Base Volume** – volume da primeira operação em um ciclo Martingale.
- **Martingale Mult** – multiplicador aplicado ao último volume stoppado para determinar o próximo tamanho de ordem.
- **Max Volume** – limite máximo para o tamanho da ordem. Quando excedido, a operação é ignorada e um aviso é registrado.
- **Candle Type** – série de velas que define quando os lançamentos de moeda e as verificações de gestão de risco são executados.

## Notas

- A estratégia usa ordens a mercado tanto para entradas quanto para saídas para imitar o comportamento do consultor especialista original.
- Os cálculos do stop de seguimento dependem do passo de preço do instrumento. Se um passo de preço não estiver disponível, valores de pontos brutos são usados em seu lugar.
- Os números aleatórios são gerados com uma semente determinista baseada na hora atual para evitar sequências idênticas em execuções simultâneas.
