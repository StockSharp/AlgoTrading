# Estratégia CandleStop Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
Esta estratégia implementa gerenciamento de stop trailing baseado na abordagem CandleStop. Ela analisa velas completadas e move o nível de stop apenas na direção da operação. O algoritmo baseia-se em canais de Donchian com períodos de lookback separados para posições compradas e vendidas, tornando-o adequado para conectar a operações manuais ou outras estratégias de entrada.

## Parâmetros
- **Up Trail Periods** – número de velas usadas para calcular a máxima mais alta para o trailing de posições vendidas.
- **Down Trail Periods** – número de velas usadas para calcular a mínima mais baixa para o trailing de posições compradas.
- **Candle Type** – período das velas usadas para análise.

## Lógica da Estratégia
1. Aguardar uma posição existente. A estratégia não abre operações por conta própria.
2. Para posições compradas:
   - Calcular a mínima mais baixa ao longo de *Down Trail Periods*.
   - Mover o stop para este valor se for mais alto que o stop anterior.
   - Se o preço tocar ou cair abaixo do stop, sair da posição com uma ordem de mercado.
3. Para posições vendidas:
   - Calcular a máxima mais alta ao longo de *Up Trail Periods*.
   - Mover o stop para este valor se for mais baixo que o stop anterior.
   - Se o preço tocar ou subir acima do stop, recomprar a posição com uma ordem de mercado.

## Notas de Uso
- Projetado para uso com a API de alto nível do StockSharp e subscrições de velas.
- Adequado para proteger posições abertas manualmente ou por outras estratégias.
- A saída do gráfico inclui velas, linhas de canal e operações executadas para visualização.
