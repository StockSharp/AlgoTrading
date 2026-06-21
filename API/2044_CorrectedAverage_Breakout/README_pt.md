# Estratégia de Rompimento de CorrectedAverage
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera rompimentos relativos a uma média móvel **CorrectedAverage**. O indicador suaviza o preço usando uma média móvel e ajusta o fator de suavização com base no desvio padrão das variações de preço.

Quando o preço fecha acima da média corrigida por um número especificado de pontos e depois recua ao nível de rompimento, a estratégia abre uma posição comprada. A lógica inversa é usada para operações vendidas. Stop-loss e take-profit são aplicados em pontos de preço absolutos.

## Parâmetros

- `Candle Type` – período dos candles usados para os cálculos.
- `Length` – período para a média móvel e o desvio padrão.
- `MA Type` – tipo de média móvel (SMA, EMA, SMMA, LWMA).
- `Level Points` – distância de rompimento da média corrigida em passos de preço.
- `Stop Loss Points` – distância do stop-loss do preço de entrada em passos de preço.
- `Take Profit Points` – distância do take-profit do preço de entrada em passos de preço.
- `Enable Long` – permitir abertura de posições compradas.
- `Enable Short` – permitir abertura de posições vendidas.

## Lógica de operação

1. Calcular a média móvel e o desvio padrão.
2. Construir a média corrigida usando valores anteriores e a relação de variância para suavizar saltos repentinos.
3. Detectar rompimentos quando a barra anterior fecha além da média corrigida mais ou menos o nível configurado.
4. Após um rompimento, aguardar o retorno da próxima barra ao nível de rompimento e abrir uma posição na direção do rompimento.
5. Fechar posições opostas quando um novo sinal de rompimento aparecer.
6. Aplicar proteções de stop-loss e take-profit.

## Notas

Esta estratégia é uma conversão do script MQL *Exp_CorrectedAverage.mq5*. Destina-se a fins educacionais e requer testes adicionais antes do uso em operações reais.
